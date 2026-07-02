using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Auth;
using NewbieCoder.Core.DTOs.Response.Auth;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Core authentication service. All write operations (device/session/token/history) are wrapped
/// in a single EF Core transaction — any failure rolls back the entire login sequence.
/// </summary>
public sealed partial class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IAuthRateLimitService _rateLimit;
    private readonly IAuditLogService _auditLog;
    private readonly JwtSecurityTokenHandler _jwtHandler;
    private readonly SymmetricSecurityKey _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(
        AppDbContext db,
        JwtSettings jwtSettings,
        IPasswordHasherService passwordHasher,
        IAuthRateLimitService rateLimit,
        IAuditLogService auditLog)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _rateLimit = rateLimit;
        _auditLog = auditLog;
        _jwtHandler = new JwtSecurityTokenHandler();
        _jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));
        _jwtIssuer = jwtSettings.Issuer;
        _jwtAudience = jwtSettings.Audience;
    }

    #region Login

    /// <inheritdoc />
    public async Task<AuthTokenResponse> LoginAsync(
        LoginRequest request,
        string? deviceId,
        string? deviceName,
        string? deviceType,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);

        // Normalize login_id: trim → lowercase; has '@' → email, otherwise username.
        var normalizedLoginId = NormalizeLoginId(request.LoginId!);
        var isEmail = normalizedLoginId.Contains('@');
        ipAddress ??= "unknown";

        // Query user by email or username.
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                isEmail
                    ? u.Email.ToLower() == normalizedLoginId
                    : u.Username.ToLower() == normalizedLoginId,
                cancellationToken);

        if (user == null)
        {
            await RecordFailedHistoryAsync(
                userId: null, email: normalizedLoginId, deviceId: null,
                sessionId: null, ipAddress, userAgent,
                AuthConstants.LoginHistoryConstants.ReasonUserNotFound, cancellationToken);

            _rateLimit.RecordFailedAttempt(normalizedLoginId, ipAddress);
            AuthConstants.Helpers.ThrowInvalidCredentials();
            return null!; 
        }

        switch (user.Status)
        {
            case UserStatus.Active:
                break;
            case UserStatus.Banned:
                await RecordFailedHistoryAsync(
                    user.Id, normalizedLoginId, null, null, ipAddress, userAgent,
                    AuthConstants.LoginHistoryConstants.ReasonBlockedUser, cancellationToken);
                AuthConstants.Helpers.ThrowUserBlocked();
                return null!;
            case UserStatus.Closed:
                await RecordFailedHistoryAsync(
                    user.Id, normalizedLoginId, null, null, ipAddress, userAgent,
                    AuthConstants.LoginHistoryConstants.ReasonBlockedUser, cancellationToken);
                AuthConstants.Helpers.ThrowUserBlocked();
                return null!;
            default:
                await RecordFailedHistoryAsync(
                    user.Id, normalizedLoginId, null, null, ipAddress, userAgent,
                    AuthConstants.LoginHistoryConstants.ReasonPendingUser, cancellationToken);
                AuthConstants.Helpers.ThrowUserPending();
                return null!;
        }

        // Verify password against stored BCrypt hash.
        // SECURITY: Password is never logged — we only pass it to the BCrypt verifier.
        if (!_passwordHasher.Verify(request.Password!, user.Password))
        {
            await RecordFailedHistoryAsync(
                user.Id, normalizedLoginId, null, null, ipAddress, userAgent,
                AuthConstants.LoginHistoryConstants.ReasonWrongPassword, cancellationToken);

            _rateLimit.RecordFailedAttempt(normalizedLoginId, ipAddress);
            AuthConstants.Helpers.ThrowInvalidCredentials();
            return null!;
        }

        // Check device block status.
        var effectiveDeviceId = deviceId ?? Guid.NewGuid().ToString();
        var existingDevice = await _db.UserDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceId == effectiveDeviceId, cancellationToken);

        if (existingDevice != null && existingDevice.Status == DeviceStatus.Blocked)
        {
            await RecordFailedHistoryAsync(
                user.Id, normalizedLoginId, null, null, ipAddress, userAgent,
                AuthConstants.LoginHistoryConstants.ReasonBlockedDevice, cancellationToken);
            AuthConstants.Helpers.ThrowDeviceBlocked();
            return null!;
        }

        // All writes inside a retriable transaction.
        long devicePk;
        long sessionPk;
        List<string> roles;
        (devicePk, sessionPk, roles) = await ExecuteInTransactionAsync(async () =>
        {
            //  Insert or update user_devices.
            long devicePk;
            if (existingDevice == null)
            {
                var newDevice = new UserDevice
                {
                    UserId = user.Id,
                    DeviceId = effectiveDeviceId,
                    DeviceName = deviceName,
                    DeviceType = Enum.TryParse<DeviceType>(deviceType, ignoreCase: true, out var dt) ? dt : DeviceType.Web,
                    Os = AuthConstants.Helpers.ParseOs(userAgent),
                    Browser = AuthConstants.Helpers.ParseBrowser(userAgent),
                    UserAgent = userAgent,
                    IpAddress = ipAddress,
                    Status = DeviceStatus.Active,
                    LastLoginAt = DateTimeOffset.UtcNow
                };
                _db.UserDevices.Add(newDevice);
                await _db.SaveChangesAsync(cancellationToken);
                devicePk = newDevice.Id;
            }
            else
            {
                existingDevice.LastLoginAt = DateTimeOffset.UtcNow;
                existingDevice.IpAddress = ipAddress;
                existingDevice.UserAgent = userAgent;
                existingDevice.Status = DeviceStatus.Active;
                _db.UserDevices.Update(existingDevice);
                await _db.SaveChangesAsync(cancellationToken);
                devicePk = existingDevice.Id;
            }

            // Create session.
            var sessionTokenPlain = AuthConstants.Helpers.GenerateSecureToken();
            var sessionTokenHash = AuthConstants.Helpers.HashToken(sessionTokenPlain);
            var session = new UserSession
            {
                UserId = user.Id,
                DeviceId = devicePk,
                SessionTokenHash = sessionTokenHash,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Status = SessionStatus.Active,
                LoginAt = DateTimeOffset.UtcNow,
                LastActiveAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddDays(AuthConstants.SessionExpirationDays)
            };
            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync(cancellationToken);
            var sessionPk = session.Id;

            // Create refresh token.
            var refreshTokenPlain = AuthConstants.Helpers.GenerateSecureToken();
            var refreshTokenHash = AuthConstants.Helpers.HashToken(refreshTokenPlain);
            var refreshExpiry = request.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDaysRememberMe)
                : DateTimeOffset.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDaysDefault);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                SessionId = sessionPk,
                TokenHash = refreshTokenHash,
                TokenFamily = Guid.NewGuid(),
                Status = TokenStatus.Active,
                IssuedAt = DateTimeOffset.UtcNow,
                ExpiredAt = refreshExpiry
            };
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync(cancellationToken);

            //  Load active roles.
            var roles = await LoadUserRolesAndPermissionsAsync(user.Id, cancellationToken);

            // Update last_login_at.
            await _db.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTimeOffset.UtcNow), cancellationToken);

            //  Record successful login history.
            var history = new LoginHistory
            {
                UserId = user.Id,
                Email = normalizedLoginId,
                DeviceId = devicePk,
                SessionId = sessionPk,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                LoginStatus = LoginStatus.Success
            };
            _db.LoginHistories.Add(history);
            await _db.SaveChangesAsync(cancellationToken);

            return (devicePk, sessionPk, roles);
        }, cancellationToken);

        //  Generate JWT (outside transaction — no DB dependency).
        var accessToken = GenerateAccessToken(
            user.Id,
            user.Email,
            user.Username,
            roles,
            sessionPk,
            devicePk);

        var refreshTokenJwt = GenerateRefreshToken(
            user.Id,
            user.Email,
            roles,
            sessionPk,
            devicePk);

        // Step 14: Return tokens.
        _rateLimit.ClearAttempts(normalizedLoginId, ipAddress);

        return new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshTokenJwt = refreshTokenJwt,
            TokenType = AuthConstants.TokenTypeBearer,
            ExpiresIn = AuthConstants.AccessTokenExpirationSeconds
        };
    }

    #endregion

    #region Logout

    /// <inheritdoc />
    public async Task LogoutAsync(
        long userId,
        long sessionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            var revoked = await _db.UserSessions
                .Where(s => s.Id == sessionId && s.UserId == userId && s.Status == SessionStatus.Active)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(s => s.Status, SessionStatus.Revoked)
                    .SetProperty(s => s.RevokedAt, now)
                    .SetProperty(s => s.RevokedReason, "logout"),
                    cancellationToken);

            if (revoked == 0)
                return;

            await _db.RefreshTokens
                .Where(rt => rt.SessionId == sessionId && rt.Status == TokenStatus.Active)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.Status, TokenStatus.Revoked)
                    .SetProperty(rt => rt.RevokedAt, now),
                    cancellationToken);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LogoutAllAsync(
        long userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await ExecuteInTransactionAsync(async () =>
        {
            await _db.UserSessions
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(s => s.Status, SessionStatus.Revoked)
                    .SetProperty(s => s.RevokedAt, now)
                    .SetProperty(s => s.RevokedReason, "logout_all"),
                    cancellationToken);

            await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.Status == TokenStatus.Active)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.Status, TokenStatus.Revoked)
                    .SetProperty(rt => rt.RevokedAt, now),
                    cancellationToken);
        }, cancellationToken);

        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Email = user,
                Action = "USER_LOGOUT_ALL",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = "All sessions and refresh tokens revoked",
                CreatedAt = now
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) { }
    }

    #endregion

    #region Get Current User

    /// <inheritdoc />
    public async Task<UserInfoResponse> GetCurrentUserAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new BusinessException(
                ResponseMessages.UserNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.NotFound);

        var roles = await LoadUserRolesAndPermissionsAsync(userId, cancellationToken);

        return new UserInfoResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Roles = roles
        };
    }

    #endregion

    #region Register

    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        string? deviceId,
        string? deviceName,
        string? deviceType,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Normalize inputs.
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedUsername = request.Username?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedFullName = SanitizeFullName(request.FullName ?? string.Empty);
        ipAddress ??= "unknown";

        // Pre-transactional checks (fast, no side-effects).
        await ValidateRegisterRequestAsync(request, normalizedEmail, normalizedUsername, cancellationToken);

        // Hash password — never store plain text.
        var passwordHash = _passwordHasher.Hash(request.Password!);

        // Check if device is blocked.
        var effectiveDeviceId = deviceId ?? Guid.NewGuid().ToString();
        await CheckDeviceBlockedAsync(effectiveDeviceId, cancellationToken);

        // All writes in a single transaction — any failure rolls back everything.
        (long userPk, long devicePk, long sessionPk, List<string> roles) =
            await ExecuteInTransactionAsync(async () =>
            {
                // Insert users.
                var user = new User
                {
                    Email = normalizedEmail,
                    Password = passwordHash,
                    Username = normalizedUsername,
                    FullName = normalizedFullName,
                    Location = string.Empty,
                    Status = UserStatus.Active,
                    EffDate = DateTimeOffset.UtcNow,
                    EmailVerified = false
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(cancellationToken);
                var userPk = user.Id;

                // Assign default USER role.
                var roleCode = DetermineRoleCode(null);
                var role = await _db.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.Code == roleCode &&
                        r.Status == RoleStatus.Active,
                        cancellationToken)
                    ?? throw new BusinessException(
                        RegisterResponseMessages.DefaultRoleNotFound,
                        statusCode: HttpStatusCodes.InternalServerError,
                        responseCode: ResponseCodes.DefaultRoleNotFound);

                var userRole = new UserRole
                {
                    UserId = userPk,
                    RoleId = role.Id,
                    AssignedAt = DateTimeOffset.UtcNow,
                    ExpiredAt = null,
                    Status = UserRoleStatus.Active,
                    EffDate = DateTimeOffset.UtcNow
                };
                _db.UserRoles.Add(userRole);
                await _db.SaveChangesAsync(cancellationToken);

                long devicePk;
                var existingDevice = await _db.UserDevices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d =>
                        d.UserId == userPk && d.DeviceId == effectiveDeviceId,
                        cancellationToken);

                if (existingDevice == null)
                {
                    var newDevice = new UserDevice
                    {
                        UserId = userPk,
                        DeviceId = effectiveDeviceId,
                        DeviceName = deviceName,
                        DeviceType = Enum.TryParse<DeviceType>(deviceType, ignoreCase: true, out var dt)
                            ? dt : DeviceType.Web,
                        Os = AuthConstants.Helpers.ParseOs(userAgent),
                        Browser = AuthConstants.Helpers.ParseBrowser(userAgent),
                        UserAgent = userAgent,
                        IpAddress = ipAddress,
                        Status = DeviceStatus.Active,
                        LastLoginAt = DateTimeOffset.UtcNow,
                        EffDate = DateTimeOffset.UtcNow
                    };
                    _db.UserDevices.Add(newDevice);
                    await _db.SaveChangesAsync(cancellationToken);
                    devicePk = newDevice.Id;
                }
                else
                {
                    existingDevice.LastLoginAt = DateTimeOffset.UtcNow;
                    existingDevice.IpAddress = ipAddress;
                    existingDevice.UserAgent = userAgent;
                    existingDevice.Status = DeviceStatus.Active;
                    _db.UserDevices.Update(existingDevice);
                    await _db.SaveChangesAsync(cancellationToken);
                    devicePk = existingDevice.Id;
                }

                // Create session.
                var sessionTokenPlain = AuthConstants.Helpers.GenerateSecureToken();
                var sessionTokenHash = AuthConstants.Helpers.HashToken(sessionTokenPlain);
                var session = new UserSession
                {
                    UserId = userPk,
                    DeviceId = devicePk,
                    SessionTokenHash = sessionTokenHash,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Status = SessionStatus.Active,
                    LoginAt = DateTimeOffset.UtcNow,
                    LastActiveAt = DateTimeOffset.UtcNow,
                    ExpiredAt = DateTimeOffset.UtcNow.AddDays(AuthConstants.SessionExpirationDays),
                    EffDate = DateTimeOffset.UtcNow
                };
                _db.UserSessions.Add(session);
                await _db.SaveChangesAsync(cancellationToken);
                var sessionPk = session.Id;

                // Create refresh token.
                var refreshTokenPlain = AuthConstants.Helpers.GenerateSecureToken();
                var refreshTokenHash = AuthConstants.Helpers.HashToken(refreshTokenPlain);
                var refreshToken = new RefreshToken
                {
                    UserId = userPk,
                    SessionId = sessionPk,
                    TokenHash = refreshTokenHash,
                    TokenFamily = Guid.NewGuid(),
                    Status = TokenStatus.Active,
                    IssuedAt = DateTimeOffset.UtcNow,
                    ExpiredAt = DateTimeOffset.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDaysDefault),
                    EffDate = DateTimeOffset.UtcNow
                };
                _db.RefreshTokens.Add(refreshToken);
                await _db.SaveChangesAsync(cancellationToken);

                // Record successful registration in login_histories.
                var history = new LoginHistory
                {
                    UserId = userPk,
                    Email = normalizedEmail,
                    DeviceId = devicePk,
                    SessionId = sessionPk,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    LoginStatus = LoginStatus.Success,
                    EffDate = DateTimeOffset.UtcNow
                };
                _db.LoginHistories.Add(history);
                await _db.SaveChangesAsync(cancellationToken);

                // Audit log (defer save until after transaction commits).
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = userPk,
                    Email = normalizedEmail,
                    Action = "USER_REGISTERED",
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Details = $"New user registered: {normalizedEmail}",
                    CreatedAt = DateTimeOffset.UtcNow
                });

                var roles = new List<string> { role.Code };
                return (userPk, devicePk, sessionPk, roles);
            }, cancellationToken);

        // Audit log must be saved outside the transaction so it never blocks user registration
        // if the audit_logs table doesn't exist yet.
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userPk,
                Email = normalizedEmail,
                Action = "USER_REGISTERED",
                IpAddress = ipAddress ?? "unknown",
                UserAgent = userAgent,
                Details = $"New user registered: {normalizedEmail}",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) { }

        // Generate JWTs (outside transaction — no DB dependency).
        var accessToken = GenerateAccessToken(
            userPk,
            normalizedEmail,
            normalizedUsername,
            roles,
            sessionPk,
            devicePk);

        var refreshTokenJwt = GenerateRefreshToken(
            userPk,
            normalizedEmail,
            roles,
            sessionPk,
            devicePk);

        return new RegisterResponse
        {
            AccessToken = accessToken,
            TokenType = AuthConstants.TokenTypeBearer,
            ExpiresIn = AuthConstants.AccessTokenExpirationSeconds,
            Scope = "read write"
        };
    }

    private async Task ValidateRegisterRequestAsync(
        RegisterRequest request,
        string normalizedEmail,
        string normalizedUsername,
        CancellationToken cancellationToken)
    {
        // Email uniqueness.
        var emailExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
            throw new BusinessException(
                RegisterResponseMessages.EmailAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.EmailAlreadyExists);

        // Username uniqueness.
        var usernameExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Username.ToLower() == normalizedUsername, cancellationToken);

        if (usernameExists)
            throw new BusinessException(
                RegisterResponseMessages.UsernameAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.UsernameAlreadyExists);

        // Password strength: uppercase, lowercase, digit, special char.
        if (!IsPasswordStrong(request.Password!, out var strengthError))
            throw new BusinessException(
                strengthError,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.PasswordTooWeak);

        // Password must not contain username or full name.
        if (ContainsUserInfo(request.Password!, normalizedUsername, request.FullName ?? string.Empty))
            throw new BusinessException(
                RegisterResponseMessages.PasswordContainsUserInfo,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.PasswordTooWeak);

        // Terms acceptance.
        if (!request.AcceptTerms)
            throw new BusinessException(
                RegisterResponseMessages.TermsNotAccepted,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.TermsNotAccepted);
    }

    private async Task CheckDeviceBlockedAsync(string deviceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || deviceId.Length < 10)
            return;

        var blockedDevice = await _db.UserDevices
            .AsNoTracking()
            .AnyAsync(d => d.DeviceId == deviceId && d.Status == DeviceStatus.Blocked, cancellationToken);

        if (blockedDevice)
            throw new BusinessException(
                RegisterResponseMessages.DeviceBlocked,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.DeviceBlocked);
    }

    private static string DetermineRoleCode(string? roleRequest)
    {
        if (!string.IsNullOrWhiteSpace(roleRequest))
        {
            var normalized = roleRequest.Trim().ToUpperInvariant();
            if (normalized is "USER" or "CONTRIBUTOR")
                return normalized;
        }
        return RoleConstants.User;
    }

    private static bool IsPasswordStrong(string password, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = RegisterResponseMessages.PasswordTooWeak;
            return false;
        }
        if (!password.Any(char.IsUpper))
        {
            errorMessage = RegisterResponseMessages.PasswordTooWeak;
            return false;
        }
        if (!password.Any(char.IsLower))
        {
            errorMessage = RegisterResponseMessages.PasswordTooWeak;
            return false;
        }
        if (!password.Any(char.IsDigit))
        {
            errorMessage = RegisterResponseMessages.PasswordTooWeak;
            return false;
        }
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errorMessage = RegisterResponseMessages.PasswordTooWeak;
            return false;
        }
        return true;
    }

    private static bool ContainsUserInfo(string password, string username, string fullName)
    {
        var lowerPassword = password.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(username) && lowerPassword.Contains(username.ToLowerInvariant()))
            return true;
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Length >= 3 && lowerPassword.Contains(part.ToLowerInvariant()))
                    return true;
            }
        }
        return false;
    }

    private static string SanitizeFullName(string raw) =>
        System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"<[^>]*>", string.Empty)
            .Replace("<", string.Empty)
            .Replace(">", string.Empty)
            .Trim();

    #endregion

    #region Private helpers

    private static void ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LoginId))
            throw new BusinessException(
                ResponseMessages.LoginIdRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new BusinessException(
                ResponseMessages.PasswordRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);
    }

    /// <summary>
    /// Normalizes a login_id: trims whitespace and converts to lowercase.
    /// </summary>
    private static string NormalizeLoginId(string raw) =>
        raw.Trim().ToLowerInvariant();

    private async Task RecordFailedHistoryAsync(
        long? userId,
        string? email,
        long? deviceId,
        long? sessionId,
        string? ipAddress,
        string? userAgent,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var history = new LoginHistory
        {
            UserId = userId,
            Email = email,
            DeviceId = deviceId,
            SessionId = sessionId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            LoginStatus = LoginStatus.Failed,
            FailureReason = failureReason
        };

        try
        {
            _db.LoginHistories.Add(history);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // If history recording fails (e.g. DB constraint), do not propagate.
        }
    }

    /// <summary>
    /// Executes <paramref name="action"/> inside a database transaction, wrapped in the
    /// current DbContext execution strategy so retries work correctly with user-initiated
    /// transactions (NpgsqlRetryingExecutionStrategy requires this pattern).
    /// </summary>
    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Non-generic overload for actions that don't return a value.
    /// </summary>
    private async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task<List<string>> LoadUserRolesAndPermissionsAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var roles = await _db.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.UserId == userId &&
                ur.Status == UserRoleStatus.Active &&
                (ur.ExpiredAt == null || ur.ExpiredAt > DateTimeOffset.UtcNow))
            .Join(_db.Roles.Where(r => r.Status == RoleStatus.Active),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Code)
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
            roles.Add("GUEST");

        return roles;
    }

    #endregion

    #region JWT

    /// <inheritdoc />
    public string GenerateAccessToken(
        long userId,
        string email,
        string username,
        IReadOnlyList<string> roles,
        long sessionId,
        long deviceId)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddSeconds(AuthConstants.AccessTokenExpirationSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtClaimTypes.Username, username),
            new(JwtClaimTypes.SessionId, sessionId.ToString()),
            new(JwtClaimTypes.DeviceId, deviceId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var credentials = new SigningCredentials(_jwtKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return _jwtHandler.WriteToken(token);
    }

    /// <inheritdoc />
    public long? ValidateAndGetUserId(string token)
    {
        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _jwtKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _jwtHandler.ValidateToken(token, parameters, out _);
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                           ?? principal.FindFirst(ClaimTypes.NameIdentifier);

            return subClaim != null && long.TryParse(subClaim.Value, out var userId)
                ? userId
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string GenerateRefreshToken(
        long userId,
        string email,
        IReadOnlyList<string> roles,
        long sessionId,
        long deviceId)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddSeconds(AuthConstants.RefreshTokenExpirationSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtClaimTypes.SessionId, sessionId.ToString()),
            new(JwtClaimTypes.DeviceId, deviceId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var credentials = new SigningCredentials(_jwtKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return _jwtHandler.WriteToken(token);
    }

    #endregion
}

/// <summary>
/// JWT configuration options — read from appsettings.json under JwtSettings.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
}
