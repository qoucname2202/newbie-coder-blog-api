using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// User management service. Handles administrative user operations such as creating accounts.
/// All DB writes are wrapped in a single transaction — any failure rolls back everything.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;

    public UserService(AppDbContext db, IPasswordHasherService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task<CreateUserResponse> CreateUserAsync(
        CreateUserRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // Normalize inputs.
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedUsername = request.Username?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedFullName = SanitizeFullName(request.FullName ?? string.Empty);
        ipAddress ??= "unknown";

        // Pre-transactional validations (fast, no side-effects).
        await ValidateCreateUserRequestAsync(request, normalizedEmail, normalizedUsername, cancellationToken);

        // Hash password — never store plain text.
        var passwordHash = _passwordHasher.Hash(request.Password!);

        // Determine which role to assign.
        Role? role;
        if (request.RoleId.HasValue)
        {
            role = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.RoleId.Value && r.Status == RoleStatus.Active, cancellationToken)
                ?? throw new BusinessException(
                    UserManagementResponseMessages.RoleNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.RoleNotFound);

            // Security: prevent direct admin role assignment during user creation.
            if (role.Code == RoleConstants.Admin)
                throw new BusinessException(
                    UserManagementResponseMessages.CannotCreateAdminUser,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.CannotCreateAdminUser);
        }
        else
        {
            role = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Code == RoleConstants.User &&
                    r.Status == RoleStatus.Active,
                    cancellationToken)
                ?? throw new BusinessException(
                    UserManagementResponseMessages.RoleNotFound,
                    statusCode: HttpStatusCodes.InternalServerError,
                    responseCode: ResponseCodes.DefaultRoleNotFound);
        }

        // All writes in a single transaction.
        long userPk;
        DateTimeOffset createdAt;
        (userPk, createdAt) = await ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                Email = normalizedEmail,
                Password = passwordHash,
                Username = normalizedUsername,
                FullName = normalizedFullName,
                AvatarUrl = request.AvatarUrl?.Trim(),
                Bio = request.Bio?.Trim(),
                CoverUrl = null,
                GithubUrl = null,
                LinkedinUrl = null,
                Location = string.Empty,
                Status = UserStatus.Active,
                EffDate = DateTimeOffset.UtcNow,
                EmailVerified = false,
                ReputationScore = 0,
                FollowerCount = 0,
                FollowingCount = 0,
                PostCount = 0
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedBy = createdByUserId,
                AssignedAt = DateTimeOffset.UtcNow,
                ExpiredAt = null,
                Status = UserRoleStatus.Active,
                EffDate = DateTimeOffset.UtcNow
            };
            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync(cancellationToken);

            // Audit log.
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = createdByUserId,
                Email = normalizedEmail,
                Action = "USER_CREATED_BY_ADMIN",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = $"Admin user ID {createdByUserId} created account: {normalizedEmail} with role {role.Code}",
                CreatedAt = DateTimeOffset.UtcNow
            });

            return (user.Id, user.EffDate);
        }, cancellationToken);

        return new CreateUserResponse
        {
            Id = userPk,
            FullName = normalizedFullName,
            Username = normalizedUsername,
            Email = normalizedEmail,
            AvatarUrl = request.AvatarUrl?.Trim(),
            Bio = request.Bio?.Trim(),
            DisplayTitle = request.DisplayTitle?.Trim(),
            WebsiteUrl = request.WebsiteUrl?.Trim(),
            Status = UserStatus.Active.ToString(),
            Role = role.Code,
            CreatedAt = createdAt
        };
    }

    #region Validations

    private async Task ValidateCreateUserRequestAsync(
        CreateUserRequest request,
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
                UserManagementResponseMessages.EmailAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.UserAlreadyExists);

        // Username uniqueness.
        var usernameExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Username.ToLower() == normalizedUsername, cancellationToken);

        if (usernameExists)
            throw new BusinessException(
                UserManagementResponseMessages.UserAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.UserAlreadyExists);

        // Password strength: uppercase, lowercase, digit, special char.
        if (!IsPasswordStrong(request.Password!))
            throw new BusinessException(
                UserManagementResponseMessages.PasswordTooWeak,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);

        // Password must not contain username or full name.
        if (ContainsUserInfo(request.Password!, normalizedUsername, request.FullName ?? string.Empty))
            throw new BusinessException(
                RegisterResponseMessages.PasswordContainsUserInfo,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);
    }

    private static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(c => !char.IsLetterOrDigit(c))) return false;
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

    #region Transaction helper

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

    #endregion
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaginatedResponse<UserListItemResponse>> GetUsersAsync(
        UserFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _userRepository.GetUsersAsync(filter, cancellationToken);

        var items = result.Items.Select(u => new UserListItemResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Username = u.Username,
            Email = u.Email,
            AvatarUrl = u.AvatarUrl,
            DisplayTitle = null,
            Bio = u.Bio,
            WebsiteUrl = u.WebsiteUrl,
            Status = u.Status,
            Role = u.Role,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return new PaginatedResponse<UserListItemResponse>
        {
            Items = items,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage
        };
    }
}
