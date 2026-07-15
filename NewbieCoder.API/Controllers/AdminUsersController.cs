using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Attributes;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.User;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Admin user management endpoints. All actions require Admin role.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Tags("Admin - User Management")]
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
    /// Creates a new user account. Only Admin role can access this endpoint.
    /// </summary>
    /// <param name="request">User creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _userService.CreateUserAsync(
            request,
            requesterId,
            ipAddress,
            userAgent,
            cancellationToken);

        return StatusCode(
            HttpStatusCodes.Created,
            ApiResponse<CreateUserResponse>.Success(
                result,
                trace,
                UserManagementResponseMessages.UserCreatedSuccess));
    }

    /// <summary>
    /// Updates an existing user account. Only Admin or users with the User.Update permission
    /// can access this endpoint. Email, username, role, status, and profile fields can be updated.
    /// Password changes are not allowed through this endpoint — use the dedicated password-reset API.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The update payload. Only non-null fields are applied.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile.</returns>
    [HttpPut("{userId:long}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] long userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _userService.UpdateUserAsync(userId, request, cancellationToken);

        return Ok(ApiResponse<UpdateUserResponse>.Success(
            result,
            trace,
            ResponseMessages.UserUpdatedSuccess));
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
}
