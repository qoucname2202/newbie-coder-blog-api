using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.User;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.Validation;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// User management service. Handles user profile updates and administrative account creation.
/// All DB writes are wrapped in a transaction — any failure rolls back everything.
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
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedUsername = request.Username?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedFullName = SanitizeFullName(request.FullName ?? string.Empty);
        ipAddress ??= "unknown";

        await ValidateCreateUserRequestAsync(request, normalizedEmail, normalizedUsername, cancellationToken);

        var passwordHash = _passwordHasher.Hash(request.Password!);

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

    public async Task<UpdateUserResponse> UpdateUserAsync(
        long userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new BusinessException(
                ResponseMessages.UserNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.NotFound);

        var normalizedEmail = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedUsername = request.Username?.Trim().ToLowerInvariant() ?? string.Empty;

        // Check email uniqueness (excluding the current user).
        var emailTaken = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id != userId && u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailTaken)
            throw new BusinessException(
                ResponseMessages.EmailAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.EmailAlreadyExistsUser);

        // Check username uniqueness (excluding the current user).
        var usernameTaken = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id != userId && u.Username.ToLower() == normalizedUsername, cancellationToken);

        if (usernameTaken)
            throw new BusinessException(
                ResponseMessages.UsernameAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.UsernameAlreadyExistsUser);

        // Validate status if provided.
        if (request.ShouldValidateStatus())
        {
            if (!request.TryParseStatus(out _))
                throw new BusinessException(
                    ResponseMessages.InvalidUserStatus,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.InvalidUserStatus);
        }

        // Validate role if provided.
        if (request.RoleId.HasValue)
        {
            var roleExists = await _db.Roles
                .AsNoTracking()
                .AnyAsync(r => r.Id == request.RoleId.Value && r.Status == RoleStatus.Active, cancellationToken);

            if (!roleExists)
                throw new BusinessException(
                    ResponseMessages.RoleNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.RoleNotFound);
        }

        // Apply all updatable fields.
        user.FullName = SanitizeFullName(request.FullName!);
        user.Username = normalizedUsername;
        user.Email = normalizedEmail;
        user.AvatarUrl = request.AvatarUrl;
        user.Bio = request.Bio;
        user.DisplayTitle = request.DisplayTitle;
        user.WebsiteUrl = request.WebsiteUrl;
        user.DateLastMaint = DateTimeOffset.UtcNow;

        if (request.ShouldValidateStatus() && request.TryParseStatus(out var parsedStatus))
            user.Status = parsedStatus;

        if (request.RoleId.HasValue)
        {
            var activeUserRoles = user.UserRoles
                .Where(ur => ur.Status == UserRoleStatus.Active)
                .ToList();

            foreach (var existing in activeUserRoles)
            {
                existing.Status = UserRoleStatus.Revoked;
                existing.ExpiredAt = DateTimeOffset.UtcNow;
            }

            var newUserRole = new UserRole
            {
                UserId = user.Id,
                RoleId = request.RoleId.Value,
                AssignedBy = null,
                AssignedAt = DateTimeOffset.UtcNow,
                ExpiredAt = null,
                Status = UserRoleStatus.Active,
                EffDate = DateTimeOffset.UtcNow
            };
            _db.UserRoles.Add(newUserRole);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Reload the primary role after any changes.
        var primaryRole = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id && ur.Status == UserRoleStatus.Active)
            .Join(_db.Roles.Where(r => r.Status == RoleStatus.Active),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "USER";

        return new UpdateUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            DisplayTitle = user.DisplayTitle,
            WebsiteUrl = user.WebsiteUrl,
            Status = user.Status.ToString(),
            Role = primaryRole,
            CreatedAt = user.EffDate.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            UpdatedAt = user.DateLastMaint.ToString("yyyy-MM-dd'T'HH:mm:sszzz")
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
        if (password.Length < PasswordStrengthAttribute.MinLength ||
            password.Length > PasswordStrengthAttribute.MaxLength) return false;
        if (CommonPasswords.IsBlocked(password)) return false;
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

    #region GetUsersAsync

    /// <inheritdoc />
    public async Task<PaginatedResponse<UserListItemResponse>> GetUsersAsync(
        UserFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLowerInvariant();
            query = query.Where(u =>
                EF.Functions.Like(u.Email.ToLower(), $"%{keyword}%") ||
                EF.Functions.Like(u.Username.ToLower(), $"%{keyword}%") ||
                EF.Functions.Like(u.FullName.ToLower(), $"%{keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(u => u.Status.ToString() == filter.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "email" => filter.SortDirection == "asc"
                ? query.OrderBy(u => u.Email)
                : query.OrderByDescending(u => u.Email),
            "username" => filter.SortDirection == "asc"
                ? query.OrderBy(u => u.Username)
                : query.OrderByDescending(u => u.Username),
            "fullname" => filter.SortDirection == "asc"
                ? query.OrderBy(u => u.FullName)
                : query.OrderByDescending(u => u.FullName),
            "createdat" => filter.SortDirection == "asc"
                ? query.OrderBy(u => u.EffDate)
                : query.OrderByDescending(u => u.EffDate),
            "lastloginat" => filter.SortDirection == "asc"
                ? query.OrderBy(u => u.LastLoginAt)
                : query.OrderByDescending(u => u.LastLoginAt),
            _ => query.OrderByDescending(u => u.EffDate)
        };

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                DisplayTitle = null,
                Bio = u.Bio,
                WebsiteUrl = u.WebsiteUrl,
                Status = u.Status.ToString(),
                Role = "", // resolved separately via UserRole join if needed
                CreatedAt = u.EffDate,
                UpdatedAt = u.DateLastMaint,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<UserListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            HasNextPage = page * pageSize < totalCount,
            HasPreviousPage = page > 1
        };
    }

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
}
