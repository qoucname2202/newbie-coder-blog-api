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
/// Administrative user management operations.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Tags("Admin - Users")]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Creates a new user account. Only Admin role can access this endpoint.
    /// </summary>
    /// <param name="request">User creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user data.</returns>
    /// <response code="201">User created successfully.</response>
    /// <response code="400">Validation error — missing or invalid fields.</response>
    /// <response code="401">Unauthorized — authentication required.</response>
    /// <response code="403">Forbidden — insufficient permissions.</response>
    /// <response code="404">Role not found.</response>
    /// <response code="409">Email or username already exists.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [Authorize]
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
        var userId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _userService.CreateUserAsync(
            request,
            userId,
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
