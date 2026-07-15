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

public sealed class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        AppDbContext db,
        IAuditLogService auditLog,
        ILogger<UserManagementService> logger)
    {
        _db = db;
        _auditLog = auditLog;
        _logger = logger;
    }

    #region Lock

    public async Task<LockUserResponse> LockUserAsync(
        long targetUserId,
        LockUserRequest request,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        // Trim reason before any validation or storage
        var reason = request.Reason.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException(
                ResponseMessages.LockReasonEmpty,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);
        }

        if (reason.Length < 5)
        {
            throw new BusinessException(
                ResponseMessages.LockReasonTooShort,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.LockReasonTooShort);
        }

        if (reason.Length > 500)
        {
            throw new BusinessException(
                ResponseMessages.LockReasonTooLong,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.LockReasonTooLong);
        }

        // Prevent admin from locking their own account
        if (targetUserId == adminUserId)
        {
            throw new BusinessException(
                ResponseMessages.CannotLockSelf,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.CannotLockSelf);
        }

        // All writes wrapped in a single transaction — any failure rolls back everything
        var (lockedUser, sessionsRevoked, tokensRevoked, oldStatus) = await ExecuteInTransactionAsync(
            async () =>
            {
                // Load target user with roles so we can check SuperAdmin protection
                var targetUser = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);

                if (targetUser == null)
                {
                    throw new BusinessException(
                        ResponseMessages.UserNotFound,
                        statusCode: HttpStatusCodes.NotFound,
                        responseCode: ResponseCodes.NotFound);
                }

                // Cannot lock a soft-deleted account
                if (targetUser.DeletedAt != null)
                {
                    throw new BusinessException(
                        ResponseMessages.CannotLockDeleted,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.CannotLockDeleted);
                }

                // SuperAdmin protection — accounts with the ADMIN role cannot be locked
                var isTargetAdmin = targetUser.UserRoles
                    .Any(ur => ur.Status == UserRoleStatus.Active &&
                               ur.Role.Code == RoleConstants.Admin);

                if (isTargetAdmin)
                {
                    throw new BusinessException(
                        ResponseMessages.CannotLockSuperAdmin,
                        statusCode: HttpStatusCodes.Forbidden,
                        responseCode: ResponseCodes.CannotLockSuperAdmin);
                }

                // Already locked — return idempotent or throw; throw is chosen here per project convention
                if (targetUser.Status == UserStatus.Locked)
                {
                    throw new BusinessException(
                        ResponseMessages.UserAlreadyLocked,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.UserAlreadyLocked);
                }

                var previousStatus = targetUser.Status;
                var now = DateTimeOffset.UtcNow;

                // Set lockout fields on the user entity
                targetUser.Status = UserStatus.Locked;
                targetUser.LockedAt = now;
                targetUser.LockedReason = reason;
                targetUser.LockedBy = adminUserId;

                // EF Core change tracking auto-detects property mutations on tracked entities —
                // no explicit Update() call needed. Calling Update() here would re-mark every field
                // as Modified and cause warnings, so we skip it.
                await _db.SaveChangesAsync(cancellationToken);

                // Fetch active sessions then update in-memory — avoids ExecuteUpdateAsync
                // which is not supported by the InMemory provider used in unit tests
                var activeSessions = await _db.UserSessions
                    .Where(s => s.UserId == targetUserId && s.Status == SessionStatus.Active)
                    .ToListAsync(cancellationToken);

                foreach (var session in activeSessions)
                {
                    session.Status = SessionStatus.Revoked;
                    session.RevokedAt = now;
                    session.RevokedReason = "account_locked";
                }

                // Same approach for refresh tokens
                var activeTokens = await _db.RefreshTokens
                    .Where(rt => rt.UserId == targetUserId && rt.Status == TokenStatus.Active)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeTokens)
                {
                    token.Status = TokenStatus.Revoked;
                    token.RevokedAt = now;
                }

                // Persist revocation changes only when there are actual records to update
                if (activeSessions.Count > 0 || activeTokens.Count > 0)
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }

                // Write structured audit log — non-fatal so it never blocks the primary operation
                await SafeWriteAuditAsync(
                    action: "USER_LOCK",
                    targetUserId: targetUserId,
                    targetEmail: targetUser.Email,
                    adminUserId: adminUserId,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    traceId: traceId,
                    oldValue: previousStatus.ToString(),
                    newValue: UserStatus.Locked.ToString(),
                    reason: reason,
                    sessionsRevokedCount: activeSessions.Count,
                    tokensRevokedCount: activeTokens.Count,
                    cancellationToken: cancellationToken);

                return (targetUser, activeSessions.Count, activeTokens.Count, previousStatus);
            },
            cancellationToken);

        // Admin info loaded outside the transaction — read-only query.
        // Fallback to a synthetic summary if the admin user record is missing/deleted,
        // so the response remains consistent even in edge cases.
        var admin = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == adminUserId)
            .Select(u => new AdminSummaryDto { Id = u.Id, Username = u.Username })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new AdminSummaryDto { Id = adminUserId, Username = "(unknown admin)" };

        _logger.LogInformation(
            "USER_LOCK applied. TargetUser={TargetUserId}, Admin={AdminUserId}, SessionsRevoked={SessionsRevoked}, TokensRevoked={TokensRevoked}, OldStatus={OldStatus}, TraceId={TraceId}",
            targetUserId, adminUserId, sessionsRevoked, tokensRevoked, oldStatus, traceId ?? "(none)");

        return new LockUserResponse
        {
            Id = lockedUser.Id,
            Status = lockedUser.Status.ToString(),
            LockedAt = lockedUser.LockedAt,
            LockedReason = lockedUser.LockedReason,
            LockedBy = admin
        };
    }

    #endregion

    #region Unlock

    public async Task<LockedUserDto> UnlockUserAsync(
        long targetUserId,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var (user, oldStatus) = await ExecuteInTransactionAsync(
            async () =>
            {
                var targetUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);

                if (targetUser == null)
                {
                    throw new BusinessException(
                        ResponseMessages.UserNotFound,
                        statusCode: HttpStatusCodes.NotFound,
                        responseCode: ResponseCodes.NotFound);
                }

                if (targetUser.DeletedAt != null)
                {
                    throw new BusinessException(
                        ResponseMessages.CannotUnlockDeleted,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.CannotUnlockDeleted);
                }

                if (targetUser.Status != UserStatus.Locked)
                {
                    throw new BusinessException(
                        ResponseMessages.UserNotLocked,
                        statusCode: HttpStatusCodes.Conflict,
                        responseCode: ResponseCodes.UserNotLocked);
                }

                var previousStatus = targetUser.Status;

                // Restore user to Active and clear all lockout fields
                targetUser.Status = UserStatus.Active;
                targetUser.LockedAt = null;
                targetUser.LockedReason = null;
                targetUser.LockedBy = null;

                await _db.SaveChangesAsync(cancellationToken);

                await SafeWriteAuditAsync(
                    action: "USER_UNLOCK",
                    targetUserId: targetUserId,
                    targetEmail: targetUser.Email,
                    adminUserId: adminUserId,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    traceId: traceId,
                    oldValue: previousStatus.ToString(),
                    newValue: UserStatus.Active.ToString(),
                    reason: null,
                    sessionsRevokedCount: 0,
                    tokensRevokedCount: 0,
                    cancellationToken: cancellationToken);

                return (targetUser, previousStatus);
            },
            cancellationToken);

        var admin = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == adminUserId)
            .Select(u => new AdminSummaryDto { Id = u.Id, Username = u.Username })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new AdminSummaryDto { Id = adminUserId, Username = "(unknown admin)" };

        _logger.LogInformation(
            "USER_UNLOCK applied. TargetUser={TargetUserId}, Admin={AdminUserId}, OldStatus={OldStatus}, TraceId={TraceId}",
            targetUserId, adminUserId, oldStatus, traceId ?? "(none)");

        return new LockedUserDto
        {
            Id = user.Id,
            Status = user.Status.ToString(),
            LockedAt = user.LockedAt,
            LockedReason = user.LockedReason,
            LockedBy = admin
        };
    }

    #endregion

    #region Private helpers

    /// <summary>
    /// Writes an audit log entry. All exceptions are caught and logged via ILogger —
    /// audit log failures must never rollback the primary lock/unlock operation.
    /// </summary>
    private async Task SafeWriteAuditAsync(
        string action,
        long targetUserId,
        string targetEmail,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        string? oldValue,
        string? newValue,
        string? reason,
        int sessionsRevokedCount,
        int tokensRevokedCount,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build a structured details payload that preserves the most-asked-for fields
            // even after moving the structured bits into their own columns.
            var details = $"Reason: {reason ?? "n/a"}. " +
                          $"SessionsRevoked: {sessionsRevokedCount}. " +
                          $"TokensRevoked: {tokensRevokedCount}. " +
                          $"AdminUserId: {adminUserId}.";

            await _auditLog.LogAsync(
                action: action,
                userId: targetUserId,
                email: targetEmail,
                ipAddress: ipAddress,
                userAgent: userAgent,
                entityType: "User",
                entityId: targetUserId,
                details: details,
                oldValue: oldValue,
                newValue: newValue,
                traceId: traceId,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit log failure must not rollback the primary operation
            _logger.LogWarning(ex,
                "Audit log write failed for {Action} on user {UserId}. TraceId={TraceId}",
                action, targetUserId, traceId ?? "(none)");
        }
    }

    /// <summary>
    /// Wraps a Func&lt;Task&lt;T&gt;&gt; inside an EF Core execution strategy and a database transaction.
    /// The execution strategy allows the operation to retry on transient failures in production.
    /// In unit tests with InMemory, the strategy is a no-op and the transaction warning is suppressed.
    /// </summary>
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

    #endregion
}
