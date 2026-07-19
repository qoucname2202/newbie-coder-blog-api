using NewbieCoder.Core.CQRS.Posts;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service interface for admin blog post management operations.
/// </summary>
public interface IAdminPostService
{
    /// <summary>
    /// Returns a paginated list of posts with optional search, filter, and sort.
    /// </summary>
    Task<PaginatedResponse<AdminPostListItemResponse>> GetPostsAsync(
        GetAdminPostsRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full post details by ID.
    /// </summary>
    Task<AdminPostDetailResponse> GetPostByIdAsync(
        long postId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new post. Handles author resolution, slug generation, and PostTag creation.
    /// </summary>
    Task<CreateAdminPostResponse> CreatePostAsync(
        CreateAdminPostRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing post. Handles PostTag synchronization and status transitions.
    /// </summary>
    Task<UpdateAdminPostResponse> UpdatePostAsync(
        long postId,
        UpdateAdminPostRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a post.
    /// </summary>
    Task<DeleteAdminPostResponse> DeletePostAsync(
        long postId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted post back to Draft status.
    /// </summary>
    Task<RestoreAdminPostResponse> RestorePostAsync(
        long postId,
        long restoredByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a post's visibility status. Supports only the Published &lt;-&gt; Hidden toggle,
    /// Draft -&gt; Published, and Published -&gt; Archived transitions.
    /// </summary>
    Task<ChangePostStatusResponse> ChangePostStatusAsync(
        long postId,
        ChangePostStatusRequest request,
        long changedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
