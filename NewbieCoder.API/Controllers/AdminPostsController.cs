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
/// Handles administrative blog post management operations:
/// create, list, detail, update, delete, and restore.
/// </summary>
[ApiController]
[Route("api/v1/admin/posts")]
[Produces("application/json")]
[Tags("Admin — Posts")]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminPostsController : ControllerBase
{
    private readonly IAdminPostService _postService;

    public AdminPostsController(IAdminPostService postService)
    {
        _postService = postService;
    }

    #region List

    /// <summary>
    /// Returns a paginated list of all posts with optional search, filter, and sort.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<AdminPostListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPosts(
        [FromQuery] GetAdminPostsRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _postService.GetPostsAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<AdminPostListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Detail

    /// <summary>
    /// Returns full details of a specific post.
    /// </summary>
    /// <param name="postId">The ID of the post.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{postId:long}")]
    [ProducesResponseType(typeof(ApiResponse<AdminPostDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(
        [FromRoute] long postId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _postService.GetPostByIdAsync(postId, cancellationToken);

        return Ok(ApiResponse<AdminPostDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Create

    /// <summary>
    /// Creates a new blog post. Supports creating for a specific author, as the current admin,
    /// or as the system.
    /// </summary>
    /// <param name="request">Post creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created post data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminPostResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreateAdminPostRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _postService.CreatePostAsync(
            request,
            createdByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return StatusCode(
            HttpStatusCodes.Created,
            ApiResponse<CreateAdminPostResponse>.Success(
                result,
                trace,
                ResponseMessages.PostCreatedSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing post.
    /// </summary>
    /// <param name="postId">The ID of the post to update.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{postId:long}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminPostResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePost(
        [FromRoute] long postId,
        [FromBody] UpdateAdminPostRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _postService.UpdatePostAsync(
            postId,
            request,
            updatedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<UpdateAdminPostResponse>.Success(
            result,
            trace,
            ResponseMessages.PostUpdatedSuccess));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes a post.
    /// </summary>
    /// <param name="postId">The ID of the post to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{postId:long}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteAdminPostResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePost(
        [FromRoute] long postId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _postService.DeletePostAsync(
            postId,
            deletedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<DeleteAdminPostResponse>.Success(
            result,
            trace,
            ResponseMessages.PostDeletedSuccess));
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores a soft-deleted post back to Draft status.
    /// </summary>
    /// <param name="postId">The ID of the post to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{postId:long}/restore")]
    [ProducesResponseType(typeof(ApiResponse<RestoreAdminPostResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestorePost(
        [FromRoute] long postId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _postService.RestorePostAsync(
            postId,
            restoredByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<RestoreAdminPostResponse>.Success(
            result,
            trace,
            ResponseMessages.PostRestoredSuccess));
    }

    #endregion

    #region Change Post Status

    /// <summary>
    /// Changes a post's visibility status (Published &lt;-&gt; Hidden, Draft -&gt; Published, Published -&gt; Archived).
    /// </summary>
    /// <param name="postId">The ID of the post.</param>
    /// <param name="request">The target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{postId:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<ChangePostStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePostStatus(
        [FromRoute] long postId,
        [FromBody] ChangePostStatusRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _postService.ChangePostStatusAsync(
            postId,
            request,
            changedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<ChangePostStatusResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
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
