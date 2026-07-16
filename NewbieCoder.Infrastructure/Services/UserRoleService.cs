using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
/// Handles assigning roles to users with full business-rule enforcement.
/// Spec A09 — Assign Role to User.
/// </summary>
public sealed class UserRoleService : IUserRoleService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(AppDbContext db, IAuditLogService auditLog, ILogger<UserRoleService> logger)
    {
        _db = db;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<AssignUserRoleResponse> AssignRoleAsync(
        long targetUserId,
        AssignUserRoleRequest request,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            // Load admin with their current active role for privilege checks
            var adminUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == adminUserId, cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.Unauthenticated,
                    statusCode: HttpStatusCodes.Unauthorized,
                    responseCode: ResponseCodes.Unauthorized);

            var adminRoleCode = adminUser.UserRoles
                .Select(ur => ur.Role.Code)
                .FirstOrDefault();

            var adminRoleIndex = Array.IndexOf(RoleConstants.AllCodes, adminRoleCode ?? "");

            // Load target user with active roles
            var targetUser = await _db.Users
                .Include(u => u.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.UserNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.NotFound);

            // Validation: target user must not be deleted
            if (targetUser.DeletedAt != null)
                throw new BusinessException(
                    ResponseMessages.CannotAssignDeletedUser,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.CannotAssignDeletedUser);

            // Validation: target user must be active
            if (targetUser.Status != UserStatus.Active)
                throw new BusinessException(
                    ResponseMessages.CannotAssignInactiveUser,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CannotAssignInactiveUser);

            // Business rule: cannot assign a role to yourself
            if (targetUserId == adminUserId)
                throw new BusinessException(
                    ResponseMessages.CannotAssignOwnRole,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.CannotAssignOwnRole);

            // Load the target role
            var targetRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.RoleNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.RoleNotFound);

            // Validation: role must not be deleted
            if (targetRole.DeletedAt != null)
                throw new BusinessException(
                    ResponseMessages.CannotAssignDeletedRole,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.CannotAssignDeletedRole);

            // Validation: role must be active
            if (targetRole.Status != RoleStatus.Active)
                throw new BusinessException(
                    ResponseMessages.CannotAssignInactiveRole,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CannotAssignInactiveRole);

            // Business rule: cannot assign SuperAdmin (ADMIN) role to another user.
            // Self-assignment is excluded — it is handled by the self-demotion rule below.
            if (targetRole.Code == RoleConstants.Admin && targetUserId != adminUserId)
                throw new BusinessException(
                    ResponseMessages.CannotAssignSuperAdmin,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.CannotAssignSuperAdmin);

            // Business rule: cannot assign a role higher than the current admin's role
            var targetRoleIndex = Array.IndexOf(RoleConstants.AllCodes, targetRole.Code);
            if (targetRoleIndex > adminRoleIndex)
                throw new BusinessException(
                    ResponseMessages.CannotAssignHigherRole,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.CannotAssignHigherRole);

            // Capture current role before changes
            var currentActiveUserRole = targetUser.UserRoles
                .OrderByDescending(ur => ur.EffDate)
                .FirstOrDefault();

            RoleSummaryResponse? previousRole = null;
            if (currentActiveUserRole != null)
            {
                previousRole = new RoleSummaryResponse
                {
                    Id = currentActiveUserRole.Role.Id,
                    Name = currentActiveUserRole.Role.Name
                };
            }

            // Business rule: cannot remove the last admin
            if (currentActiveUserRole?.Role.Code == RoleConstants.Admin)
            {
                var adminCount = await _db.UserRoles
                    .AsNoTracking()
                    .Include(ur => ur.Role)
                    .Where(ur =>
                        ur.Role.Code == RoleConstants.Admin &&
                        ur.Status == UserRoleStatus.Active &&
                        ur.DeletedAt == null &&
                        ur.User.DeletedAt == null)
                    .CountAsync(cancellationToken);

                if (adminCount <= 1)
                    throw new BusinessException(
                        ResponseMessages.CannotRemoveLastAdmin,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.CannotRemoveLastAdmin);
            }

            // Business rule: cannot remove the last SuperAdmin
            if (currentActiveUserRole?.Role.Code == RoleConstants.Admin)
            {
                var superAdminCount = await _db.UserRoles
                    .AsNoTracking()
                    .Include(ur => ur.Role)
                    .Where(ur =>
                        ur.Role.Code == RoleConstants.Admin &&
                        ur.Status == UserRoleStatus.Active &&
                        ur.DeletedAt == null &&
                        ur.User.DeletedAt == null)
                    .CountAsync(cancellationToken);

                if (superAdminCount <= 1)
                    throw new BusinessException(
                        ResponseMessages.CannotRemoveLastSuperAdmin,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.CannotRemoveLastSuperAdmin);
            }

            // Business rule: admin cannot demote themselves.
            // Only fires when the admin is changing their own role to a lower-privilege role.
            var adminCurrentRole = adminUser.UserRoles
                .OrderByDescending(ur => ur.EffDate)
                .FirstOrDefault();

            if (targetUserId == adminUserId &&
                adminCurrentRole?.Role.Code == RoleConstants.Admin &&
                targetRole.Code != RoleConstants.Admin)
            {
                throw new BusinessException(
                    ResponseMessages.CannotChangeOwnRole,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.CannotChangeOwnRole);
            }

            var now = DateTimeOffset.UtcNow;

            // Revoke all existing active roles
            foreach (var existingRole in targetUser.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
            {
                existingRole.Status = UserRoleStatus.Revoked;
                existingRole.DeletedAt = now;
                existingRole.DateLastMaint = now;
            }

            // Assign new role
            var newUserRole = new UserRole
            {
                UserId = targetUser.Id,
                RoleId = targetRole.Id,
                AssignedBy = adminUserId,
                AssignedAt = now,
                Status = UserRoleStatus.Active,
                EffDate = now,
                DateLastMaint = now
            };

            await _db.UserRoles.AddAsync(newUserRole, cancellationToken);

            // Update user audit fields
            targetUser.DateLastMaint = now;

            await _db.SaveChangesAsync(cancellationToken);

            // Non-fatal audit log
            await SafeWriteAuditAsync(
                action: AuditActions.UserRoleAssigned,
                targetUserId: targetUser.Id,
                targetEmail: targetUser.Email,
                adminUserId: adminUserId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                traceId: traceId,
                previousRoleName: previousRole?.Name,
                newRoleName: targetRole.Name,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "USER_ROLE_ASSIGNED. TargetUser={TargetUserId}, Admin={AdminUserId}, PreviousRole={PreviousRole}, NewRole={NewRole}, TraceId={TraceId}",
                targetUserId, adminUserId,
                previousRole?.Name ?? "(none)",
                targetRole.Name,
                traceId ?? "(none)");

            return new AssignUserRoleResponse
            {
                UserId = targetUser.Id,
                Username = targetUser.Username,
                Email = targetUser.Email,
                PreviousRole = previousRole,
                CurrentRole = new RoleSummaryResponse
                {
                    Id = targetRole.Id,
                    Name = targetRole.Name
                },
                UpdatedAt = now
            };
        }, cancellationToken);
    }

    public async Task<RevokeUserRoleResponse> RevokeRoleAsync(
        long targetUserId,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            // Load admin with their current active role for privilege checks
            var adminUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == adminUserId, cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.Unauthenticated,
                    statusCode: HttpStatusCodes.Unauthorized,
                    responseCode: ResponseCodes.Unauthorized);

            // Load target user with active roles
            var targetUser = await _db.Users
                .Include(u => u.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.UserNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.NotFound);

            // Validation: target user must not be deleted
            if (targetUser.DeletedAt != null)
                throw new BusinessException(
                    ResponseMessages.CannotAssignDeletedUser,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.CannotAssignDeletedUser);

            // Validation: target user must be active
            if (targetUser.Status != UserStatus.Active)
                throw new BusinessException(
                    ResponseMessages.CannotAssignInactiveUser,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CannotAssignInactiveUser);

            // Business rule: cannot revoke your own role
            if (targetUserId == adminUserId)
                throw new BusinessException(
                    ResponseMessages.CannotRevokeOwnRole,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.CannotRevokeOwnRole);

            // Capture current role
            var currentActiveUserRole = targetUser.UserRoles
                .OrderByDescending(ur => ur.EffDate)
                .FirstOrDefault();

            var previousRoleName = currentActiveUserRole?.Role.Name;
            var previousRoleId = currentActiveUserRole?.Role.Id;

            // Get the base USER role
            var baseRole = await _db.Roles
                .FirstOrDefaultAsync(r =>
                    r.Code == RoleConstants.User &&
                    r.Status == RoleStatus.Active &&
                    r.DeletedAt == null,
                    cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.RoleNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.RoleNotFound);

            // Cannot revoke if user is already at base USER role
            if (currentActiveUserRole?.Role.Code == RoleConstants.User)
                throw new BusinessException(
                    "User already has the base USER role.",
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.Conflict);

            // Business rule: cannot revoke the last Admin
            if (currentActiveUserRole?.Role.Code == RoleConstants.Admin)
            {
                var adminCount = await _db.UserRoles
                    .AsNoTracking()
                    .Include(ur => ur.Role)
                    .Where(ur =>
                        ur.Role.Code == RoleConstants.Admin &&
                        ur.Status == UserRoleStatus.Active &&
                        ur.DeletedAt == null &&
                        ur.User.DeletedAt == null)
                    .CountAsync(cancellationToken);

                if (adminCount <= 1)
                    throw new BusinessException(
                        ResponseMessages.CannotRemoveLastAdmin,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.CannotRemoveLastAdmin);
            }

            // Business rule: cannot revoke the last SuperAdmin
            if (currentActiveUserRole?.Role.Code == RoleConstants.Admin)
            {
                var superAdminCount = await _db.UserRoles
                    .AsNoTracking()
                    .Include(ur => ur.Role)
                    .Where(ur =>
                        ur.Role.Code == RoleConstants.Admin &&
                        ur.Status == UserRoleStatus.Active &&
                        ur.DeletedAt == null &&
                        ur.User.DeletedAt == null)
                    .CountAsync(cancellationToken);

                if (superAdminCount <= 1)
                    throw new BusinessException(
                        ResponseMessages.CannotRemoveLastSuperAdmin,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.CannotRemoveLastSuperAdmin);
            }

            var now = DateTimeOffset.UtcNow;

            // Revoke all existing active roles
            foreach (var existingRole in targetUser.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
            {
                existingRole.Status = UserRoleStatus.Revoked;
                existingRole.DeletedAt = now;
                existingRole.DateLastMaint = now;
            }

            // Assign base USER role
            var newUserRole = new UserRole
            {
                UserId = targetUser.Id,
                RoleId = baseRole.Id,
                AssignedBy = adminUserId,
                AssignedAt = now,
                Status = UserRoleStatus.Active,
                EffDate = now,
                DateLastMaint = now
            };

            await _db.UserRoles.AddAsync(newUserRole, cancellationToken);

            // Update user audit fields
            targetUser.DateLastMaint = now;

            await _db.SaveChangesAsync(cancellationToken);

            // Non-fatal audit log
            await SafeWriteAuditAsync(
                action: AuditActions.UserRoleRevoked,
                targetUserId: targetUser.Id,
                targetEmail: targetUser.Email,
                adminUserId: adminUserId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                traceId: traceId,
                previousRoleName: previousRoleName,
                newRoleName: baseRole.Name,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "USER_ROLE_REVOKED. TargetUser={TargetUserId}, Admin={AdminUserId}, PreviousRole={PreviousRole}, NewRole={NewRole}, TraceId={TraceId}",
                targetUserId, adminUserId,
                previousRoleName ?? "(none)",
                baseRole.Name,
                traceId ?? "(none)");

            return new RevokeUserRoleResponse
            {
                UserId = targetUser.Id,
                Username = targetUser.Username,
                Email = targetUser.Email,
                PreviousRole = new RoleSummaryResponse
                {
                    Id = previousRoleId ?? 0,
                    Name = previousRoleName ?? ""
                },
                CurrentRole = new RoleSummaryResponse
                {
                    Id = baseRole.Id,
                    Name = baseRole.Name
                },
                UpdatedAt = now
            };
        }, cancellationToken);
    }

    #region Private helpers

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
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

    private async Task SafeWriteAuditAsync(
        string action,
        long targetUserId,
        string targetEmail,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        string? previousRoleName,
        string? newRoleName,
        CancellationToken cancellationToken)
    {
        try
        {
            var details = $"Role changed from '{previousRoleName ?? "none"}' to '{newRoleName}'. " +
                          $"AdminUserId: {adminUserId}.";

            await _auditLog.LogAsync(
                action: action,
                userId: targetUserId,
                email: targetEmail,
                ipAddress: ipAddress,
                userAgent: userAgent,
                entityType: nameof(User),
                entityId: targetUserId,
                details: details,
                oldValue: previousRoleName,
                newValue: newRoleName,
                traceId: traceId,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Audit log write failed for {Action} on user {UserId}. TraceId={TraceId}",
                action, targetUserId, traceId ?? "(none)");
        }
    }

    #endregion
}
