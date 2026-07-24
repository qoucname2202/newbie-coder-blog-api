using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service interface for admin tag management operations.
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Creates a new tag.
    /// </summary>
    Task<TagResponse> CreateTagAsync(
        CreateTagRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of tags.
    /// </summary>
    Task<PaginatedResponse<TagListItemResponse>> GetTagsAsync(
        TagFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full tag details by ID.
    /// </summary>
    Task<TagDetailResponse> GetTagByIdAsync(
        long tagId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tag.
    /// </summary>
    Task<TagResponse> UpdateTagAsync(
        long tagId,
        UpdateTagRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of a tag.
    /// </summary>
    Task<TagResponse> ChangeStatusAsync(
        long tagId,
        ChangeTagStatusRequest request,
        long changedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a tag.
    /// </summary>
    Task DeleteTagAsync(
        long tagId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges sourceTag into targetTag: moves all associations and soft-deletes source.
    /// </summary>
    Task<MergeTagResponse> MergeTagsAsync(
        MergeTagRequest request,
        long mergedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
