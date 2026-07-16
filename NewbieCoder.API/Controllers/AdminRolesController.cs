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
/// Handles administrative role management operations: create, list, detail, update, and delete.
/// </summary>
[ApiController]
[Route("api/v1/admin/roles")]
[Produces("application/json")]
[Tags("Admin — Roles")]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminRolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public AdminRolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    #region Create

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="request">Role creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created role data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _roleService.CreateRoleAsync(
            request,
            createdByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return StatusCode(
            HttpStatusCodes.Created,
            ApiResponse<RoleResponse>.Success(
                result,
                trace,
                ResponseMessages.RoleCreatedSuccess));
    }

    #endregion

    #region List

    /// <summary>
    /// Returns a paginated list of all roles.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<RoleListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] RoleFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _roleService.GetRolesAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<RoleListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.RolesRetrieved));
    }

    #endregion

    #region Detail

    /// <summary>
    /// Returns the details of a specific role.
    /// </summary>
    /// <param name="roleId">The ID of the role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{roleId:long}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(
        [FromRoute] long roleId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _roleService.GetRoleByIdAsync(roleId, cancellationToken);

        return Ok(ApiResponse<RoleDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.ProfileSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="roleId">The ID of the role to update.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{roleId:long}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRole(
        [FromRoute] long roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _roleService.UpdateRoleAsync(
            roleId,
            request,
            updatedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<RoleResponse>.Success(
            result,
            trace,
            ResponseMessages.RoleUpdatedSuccess));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes a role.
    /// </summary>
    /// <param name="roleId">The ID of the role to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{roleId:long}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRole(
        [FromRoute] long roleId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        await _roleService.DeleteRoleAsync(
            roleId,
            deletedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<string>.Success(
            string.Empty,
            trace,
            ResponseMessages.RoleDeletedSuccess));
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
}
