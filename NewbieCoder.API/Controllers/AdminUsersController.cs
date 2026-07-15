using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Attributes;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
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
