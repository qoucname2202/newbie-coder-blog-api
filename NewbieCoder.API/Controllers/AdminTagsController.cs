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
/// Handles administrative tag management operations: create, list, detail, update, change status, delete, and merge.
/// </summary>
[ApiController]
[Route("api/v1/admin/tags")]
[Produces("application/json")]
[Tags("Admin — Tags")]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminTagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public AdminTagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    #region Create

    /// <summary>
    /// Creates a new tag.
    /// </summary>
    /// <param name="request">Tag creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created tag data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TagResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTag(
        [FromBody] CreateTagRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _tagService.CreateTagAsync(
            request,
            createdByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return StatusCode(
            HttpStatusCodes.Created,
            ApiResponse<TagResponse>.Success(
                result,
                trace,
                ResponseMessages.TagCreatedSuccess));
    }

    #endregion

    #region List

    /// <summary>
    /// Returns a paginated list of tags.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<TagListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTags(
        [FromQuery] TagFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _tagService.GetTagsAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<TagListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.TagRetrievedSuccess));
    }

    #endregion

    #region GetById

    /// <summary>
    /// Returns full details of a tag by ID.
    /// </summary>
    /// <param name="tagId">The tag ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{tagId:long}")]
    [ProducesResponseType(typeof(ApiResponse<TagDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTagById(
        long tagId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _tagService.GetTagByIdAsync(tagId, cancellationToken);

        return Ok(ApiResponse<TagDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.TagRetrievedSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing tag.
    /// </summary>
    /// <param name="tagId">The tag ID.</param>
    /// <param name="request">Tag update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated tag data.</returns>
    [HttpPut("{tagId:long}")]
    [ProducesResponseType(typeof(ApiResponse<TagResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTag(
        long tagId,
        [FromBody] UpdateTagRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _tagService.UpdateTagAsync(
            tagId,
            request,
            updatedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return Ok(ApiResponse<TagResponse>.Success(
            result,
            trace,
            ResponseMessages.TagUpdatedSuccess));
    }

    #endregion

    #region ChangeStatus

    /// <summary>
    /// Changes the status of a tag (active / inactive).
    /// </summary>
    /// <param name="tagId">The tag ID.</param>
    /// <param name="request">Status change data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated tag data.</returns>
    [HttpPatch("{tagId:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<TagResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeTagStatus(
        long tagId,
        [FromBody] ChangeTagStatusRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _tagService.ChangeStatusAsync(
            tagId,
            request,
            changedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return Ok(ApiResponse<TagResponse>.Success(
            result,
            trace,
            ResponseMessages.TagUpdatedSuccess));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes a tag.
    /// </summary>
    /// <param name="tagId">The tag ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{tagId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTag(
        long tagId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        await _tagService.DeleteTagAsync(
            tagId,
            deletedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return NoContent();
    }

    #endregion

    #region Merge

    /// <summary>
    /// Merges a source tag into a target tag. All associations (posts, interview questions,
    /// community questions) are transferred to the target tag, and the source tag is soft-deleted.
    /// </summary>
    /// <param name="request">Merge request containing source and target tag IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Merge summary including counts of migrated associations.</returns>
    [HttpPost("merge")]
    [ProducesResponseType(typeof(ApiResponse<MergeTagResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MergeTags(
        [FromBody] MergeTagRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _tagService.MergeTagsAsync(
            request,
            mergedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return Ok(ApiResponse<MergeTagResponse>.Success(
            result,
            trace,
            ResponseMessages.TagMergedSuccess));
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
