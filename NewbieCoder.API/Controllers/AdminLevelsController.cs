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
/// Handles administrative level management operations: create, list, detail, update, delete, and restore.
/// </summary>
[ApiController]
[Route("api/v1/admin/levels")]
[Produces("application/json")]
[Tags("Admin — Levels")]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminLevelsController : ControllerBase
{
    private readonly ILevelService _levelService;

    public AdminLevelsController(ILevelService levelService)
    {
        _levelService = levelService;
    }

    #region Create

    /// <summary>
    /// Creates a new level.
    /// </summary>
    /// <param name="request">Level creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created level data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LevelResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateLevel(
        [FromBody] CreateLevelRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _levelService.CreateLevelAsync(
            request,
            createdByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return StatusCode(
            HttpStatusCodes.Created,
            ApiResponse<LevelResponse>.Success(
                result,
                trace,
                ResponseMessages.LevelCreatedSuccess));
    }

    #endregion

    #region List

    /// <summary>
    /// Returns a paginated list of levels.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<LevelListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLevels(
        [FromQuery] LevelFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _levelService.GetLevelsAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<LevelListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.LevelRetrievedSuccess));
    }

    #endregion

    #region GetById

    /// <summary>
    /// Returns full details of a level by ID.
    /// </summary>
    /// <param name="levelId">The level ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{levelId:long}")]
    [ProducesResponseType(typeof(ApiResponse<LevelDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLevelById(
        long levelId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _levelService.GetLevelByIdAsync(levelId, cancellationToken);

        return Ok(ApiResponse<LevelDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.LevelRetrievedSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing level.
    /// </summary>
    /// <param name="levelId">The level ID.</param>
    /// <param name="request">Level update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated level data.</returns>
    [HttpPut("{levelId:long}")]
    [ProducesResponseType(typeof(ApiResponse<LevelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateLevel(
        long levelId,
        [FromBody] UpdateLevelRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _levelService.UpdateLevelAsync(
            levelId,
            request,
            updatedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return Ok(ApiResponse<LevelResponse>.Success(
            result,
            trace,
            ResponseMessages.LevelUpdatedSuccess));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes a level. Fails if the level has associated interview questions.
    /// </summary>
    /// <param name="levelId">The level ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{levelId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteLevel(
        long levelId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        await _levelService.DeleteLevelAsync(
            levelId,
            deletedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return NoContent();
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores a soft-deleted level.
    /// </summary>
    /// <param name="levelId">The level ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restored level data.</returns>
    [HttpPost("{levelId:long}/restore")]
    [ProducesResponseType(typeof(ApiResponse<LevelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestoreLevel(
        long levelId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _levelService.RestoreLevelAsync(
            levelId,
            restoredByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return Ok(ApiResponse<LevelResponse>.Success(
            result,
            trace,
            ResponseMessages.LevelRestoredSuccess));
    }

    #endregion

    #region Private Helpers

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
