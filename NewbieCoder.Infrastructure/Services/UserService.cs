using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.User;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// User management service. Handles user profile updates from the admin panel.
/// All write operations are wrapped in a transaction — any failure rolls back everything.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
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
                AssignedAt = DateTimeOffset.UtcNow,
                ExpiredAt = null,
                Status = UserRoleStatus.Active,
                EffDate = DateTimeOffset.UtcNow
            };
            _db.UserRoles.Add(newUserRole);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Reload the primary role after changes.
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

    private static string SanitizeFullName(string raw) =>
        System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"<[^>]*>", string.Empty)
            .Replace("<", string.Empty)
            .Replace(">", string.Empty)
            .Trim();
}
