using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service for assigning roles to users.
/// Spec A09 — Assign Role to User.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Assigns a role to a user. All operations execute inside a single transaction.
    /// </summary>
    /// <param name="targetUserId">The user to assign the role to.</param>
    /// <param name="request">The role assignment request containing the roleId.</param>
    /// <param name="adminUserId">The admin performing the action.</param>
    /// <param name="ipAddress">Client IP for audit.</param>
    /// <param name="userAgent">Client User-Agent for audit.</param>
    /// <param name="traceId">Request trace ID for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Assignment result with previous and current role info.</returns>
    Task<AssignUserRoleResponse> AssignRoleAsync(
        long targetUserId,
        AssignUserRoleRequest request,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all elevated roles from a user and assigns them back to the base USER role.
    /// All operations execute inside a single transaction.
    /// </summary>
    /// <param name="targetUserId">The user to revoke roles from.</param>
    /// <param name="adminUserId">The admin performing the action.</param>
    /// <param name="ipAddress">Client IP for audit.</param>
    /// <param name="userAgent">Client User-Agent for audit.</param>
    /// <param name="traceId">Request trace ID for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Revocation result with previous and current role info.</returns>
    Task<RevokeUserRoleResponse> RevokeRoleAsync(
        long targetUserId,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
