using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Attributes;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles administrative user management operations: lock and unlock.
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Produces("application/json")]
[Tags("Admin — Users")]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserManagementService _userManagement;

    public AdminUsersController(IUserManagementService userManagement)
    {
        _userManagement = userManagement;
    }

    #region Lock

    /// <summary>
    /// Locks a user account, revoking all active sessions and refresh tokens.
    /// </summary>
    /// <param name="userId">The ID of the user to lock.</param>
    /// <param name="request">Lock reason and optional automatic unlock time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{userId:long}/lock")]
    [ProducesResponseType(typeof(ApiResponse<LockUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LockUser(
        [FromRoute] long userId,
        [FromBody] LockUserRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var adminUserId = GetRequiredUserId();

        var result = await _userManagement.LockUserAsync(
            targetUserId: userId,
            request: request,
            adminUserId: adminUserId,
            ipAddress: GetClientIp(),
            userAgent: Request.Headers.UserAgent.FirstOrDefault(),
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<LockUserResponse>.Success(
            result,
            trace,
            ResponseMessages.LockSucceeded));
    }

    #endregion

    #region Unlock

    /// <summary>
    /// Unlocks a previously locked user account, restoring full access.
    /// </summary>
    /// <param name="userId">The ID of the user to unlock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{userId:long}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<LockedUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnlockUser(
        [FromRoute] long userId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var adminUserId = GetRequiredUserId();

        var result = await _userManagement.UnlockUserAsync(
            targetUserId: userId,
            adminUserId: adminUserId,
            ipAddress: GetClientIp(),
            userAgent: Request.Headers.UserAgent.FirstOrDefault(),
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<LockedUserDto>.Success(
            result,
            trace,
            ResponseMessages.UnlockSucceeded));
    }

    #endregion

    #region Private helpers

    private long GetRequiredUserId()
    {
        var userId = User.GetUserId();
        if (userId == null)
            throw new BusinessException(
                ResponseMessages.Unauthenticated,
                statusCode: HttpStatusCodes.Unauthorized,
                responseCode: ResponseCodes.Unauthorized);

        return userId.Value;
    }

    private string? GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
/// Admin endpoints for managing users in the system.
/// All endpoints require Admin role.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Tags("Admin - Users")]
[Authorize]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminUsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Returns a paginated list of all user accounts in the system.
    /// Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _userService.GetUsersAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<UserListItemResponse>>.Success(
            result,
            trace,
            AdminUsersResponseMessages.UsersRetrieved));
    }
}
