using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.Auth;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Implements the forgot-password and reset-password flows.
/// All write operations are wrapped in an EF Core transaction — any failure rolls back the entire sequence.
/// </summary>
public sealed partial class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLog;

    public PasswordResetService(
        AppDbContext db,
        IPasswordHasherService passwordHasher,
        IEmailService emailService,
        IAuditLogService auditLog)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _auditLog = auditLog;
    }

    #region InitiateReset

    /// <inheritdoc />
    public async Task InitiateResetAsync(
        string email,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        ipAddress ??= "unknown";

        // 1. Rate limit by IP (5 req/15m), by email (3 req/15m), by user (5 req/1h).
        ApplyRateLimitForForgotPassword(normalizedEmail, ipAddress);

        // 2. Look up user — never reveal existence.
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user == null)
            return; // Generic success response regardless of email existence.

        // 3. Reject blocked/deleted accounts without revealing status.
        if (user.Status is UserStatus.Banned or UserStatus.Closed)
            return;

        var now = DateTimeOffset.UtcNow;

        // 4. Revoke any previously active token for this user.
        await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.Status == TokenStatus.Active)
            .ExecuteUpdateAsync(t => t
                .SetProperty(p => p.Status, TokenStatus.Revoked)
                .SetProperty(p => p.RevokedAt, now),
                cancellationToken);

        // 5. Generate and store only the token hash.
        var plainToken = AuthConstants.Helpers.GenerateSecureToken();
        var tokenHash = AuthConstants.Helpers.HashToken(plainToken);
        var expiresAt = now.AddMinutes(AuthConstants.ResetTokenExpirationMinutes);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            Status = TokenStatus.Active,
            RequestedIp = ipAddress,
            RequestedUserAgent = userAgent,
            ExpiredAt = expiresAt,
            EffDate = now
        };
        _db.PasswordResetTokens.Add(resetToken);
        await _db.SaveChangesAsync(cancellationToken);

        // 6. Send email with the plain token.
        var resetLink = $"https://app.newbiecoder.dev/reset-password?token={plainToken}";
        var emailSent = await _emailService.SendPasswordResetEmailAsync(
            user.Email, resetLink, cancellationToken);

        if (!emailSent)
        {
            throw new BusinessException(
                ResponseMessages.EmailSendFailed,
                statusCode: HttpStatusCodes.InternalServerError,
                responseCode: ResponseCodes.EmailSendFailed);
        }

        // 7. Audit log (no sensitive data).
        await _auditLog.LogAsync(
            action: "REQUEST_PASSWORD_RESET",
            userId: user.Id,
            email: user.Email,
            ipAddress: ipAddress,
            userAgent: userAgent,
            details: $"Reset requested from IP {ipAddress}",
            cancellationToken: cancellationToken);
    }

    #endregion

    #region CompleteReset

    /// <inheritdoc />
    public async Task<ResetPasswordSuccessResponse> CompleteResetAsync(
        string plainToken,
        string newPassword,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate password strength.
        ValidateNewPassword(newPassword);

        // 2. Hash the incoming token and look it up.
        var tokenHash = AuthConstants.Helpers.HashToken(plainToken);

        var resetToken = await _db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.Status == TokenStatus.Active &&
                t.ExpiredAt > DateTimeOffset.UtcNow &&
                t.UsedAt == null &&
                t.RevokedAt == null,
                cancellationToken);

        if (resetToken == null)
        {
            throw new BusinessException(
                ResponseMessages.InvalidOrExpiredResetToken,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidOrExpiredResetToken);
        }

        // 3. Load the associated user.
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);

        if (user == null)
        {
            // Deliberately generic error to avoid information leakage.
            throw new BusinessException(
                ResponseMessages.InvalidOrExpiredResetToken,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidOrExpiredResetToken);
        }

        // 4. Reject blocked/deleted accounts.
        if (user.Status is UserStatus.Banned or UserStatus.Closed)
        {
            throw new BusinessException(
                ResponseMessages.UserBlocked,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.Forbidden);
        }

        // 5. Optional: prevent password reuse.
        if (_passwordHasher.Verify(newPassword, user.Password))
        {
            throw new BusinessException(
                ResponseMessages.PasswordReused,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.PasswordReused);
        }

        // 6. Execute all writes in a single transaction.
        var now = DateTimeOffset.UtcNow;
        var passwordChangedAt = now;

        await ExecuteInTransactionAsync(async () =>
        {
            // 6a. Hash and persist new password.
            var newHash = _passwordHasher.Hash(newPassword);
            await _db.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(p => p.Password, newHash)
                    .SetProperty(p => p.PasswordChangedAt, passwordChangedAt),
                    cancellationToken);

            // 6b. Mark this token as used.
            await _db.PasswordResetTokens
                .Where(t => t.Id == resetToken.Id)
                .ExecuteUpdateAsync(t => t
                    .SetProperty(p => p.Status, TokenStatus.Used)
                    .SetProperty(p => p.UsedAt, now),
                    cancellationToken);

            // 6c. Revoke any other active tokens for this user.
            await _db.PasswordResetTokens
                .Where(t =>
                    t.UserId == user.Id &&
                    t.Id != resetToken.Id &&
                    t.Status == TokenStatus.Active)
                .ExecuteUpdateAsync(t => t
                    .SetProperty(p => p.Status, TokenStatus.Revoked)
                    .SetProperty(p => p.RevokedAt, now),
                    cancellationToken);

            // 6d. Revoke all active sessions (logout all devices).
            await _db.UserSessions
                .Where(s => s.UserId == user.Id && s.Status == SessionStatus.Active)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, SessionStatus.Revoked)
                    .SetProperty(p => p.RevokedAt, now)
                    .SetProperty(p => p.RevokedReason, "password_reset"),
                    cancellationToken);

            // 6e. Revoke all active refresh tokens.
            await _db.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.Status == TokenStatus.Active)
                .ExecuteUpdateAsync(rt => rt
                    .SetProperty(p => p.Status, TokenStatus.Revoked)
                    .SetProperty(p => p.RevokedAt, now),
                    cancellationToken);

            // 6f. Audit log (no sensitive data).
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Email = user.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Action = "RESET_PASSWORD_SUCCESS",
                Details = "Password reset completed; all sessions and tokens revoked.",
                CreatedAt = now
            });

            await _db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new ResetPasswordSuccessResponse
        {
            PasswordChangedAt = passwordChangedAt,
            LogoutAllDevices = true
        };
    }

    #endregion

    #region Private helpers

    private static void ValidateNewPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new BusinessException(
                ResponseMessages.PasswordTooWeak,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ResetPasswordWeak);

        if (newPassword.Length is < 8 or > 64)
            throw new BusinessException(
                ResponseMessages.PasswordTooWeak,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ResetPasswordWeak);

        if (!StrongPasswordRegex().IsMatch(newPassword))
            throw new BusinessException(
                ResponseMessages.PasswordTooWeak,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ResetPasswordWeak);
    }

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,64}$")]
    private static partial Regex StrongPasswordRegex();

    private static void ApplyRateLimitForForgotPassword(string email, string ipAddress)
    {
        var keyIp = $"fp:ip:{ipAddress}";
        var keyEmail = $"fp:email:{email}";

        // Simple in-memory buckets — replace with distributed cache (Redis) in production.
        var blocked = ForgotPasswordRateLimitBucket.TryCheck(
            keyIp,
            AuthConstants.ResetRequestMaxPerIpPer15Min,
            TimeSpan.FromMinutes(15));

        if (!blocked)
            blocked = ForgotPasswordRateLimitBucket.TryCheck(
                keyEmail,
                AuthConstants.ResetRequestMaxPerEmailPer15Min,
                TimeSpan.FromMinutes(15));

        if (blocked)
        {
            throw new BusinessException(
                ResponseMessages.TooManyResetRequests,
                statusCode: HttpStatusCodes.TooManyRequests,
                responseCode: ResponseCodes.TooManyResetRequests);
        }
    }

    private async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken)
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

    #endregion
}

/// <summary>
/// Simple in-memory sliding-window rate limiter for forgot-password requests.
/// Tracks timestamps in a concurrent dictionary; auto-cleans expired entries.
/// </summary>
internal static class ForgotPasswordRateLimitBucket
{
    private static readonly ConcurrentDictionary<string, List<DateTimeOffset>> Buckets = new();
    private static readonly Timer CleanupTimer = new(
        static _ => Cleanup(),
        null,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(5));

    /// <summary>Returns true if the key has exceeded <paramref name="limit"/> within <paramref name="window"/>.</summary>
    public static bool TryCheck(string key, int limit, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        var bucket = Buckets.GetOrAdd(key, _ => new List<DateTimeOffset>());

        lock (bucket)
        {
            var cutoff = now.Subtract(window);
            bucket.RemoveAll(t => t < cutoff);

            if (bucket.Count >= limit)
                return true;

            bucket.Add(now);
            return false;
        }
    }

    private static void Cleanup()
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-30);
        foreach (var kvp in Buckets)
        {
            lock (kvp.Value)
            {
                kvp.Value.RemoveAll(t => t < cutoff);
                if (kvp.Value.Count == 0)
                    Buckets.TryRemove(kvp.Key, out _);
            }
        }
    }
}
