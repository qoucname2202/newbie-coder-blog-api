using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Post data access operations used by the admin module.
/// </summary>
public interface IPostRepository
{
    /// <summary>
    /// Returns a paginated list of posts matching the filter criteria.
    /// </summary>
    Task<PaginatedResponse<AdminPostListItemResponse>> GetPagedAsync(
        GetAdminPostsRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full post details (no tracking) by ID, including tags.
    /// Returns null if not found.
    /// </summary>
    Task<AdminPostDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked Post entity by ID (for updates). Returns null if not found.
    /// </summary>
    Task<Post?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked Post entity by ID if it is soft-deleted. Returns null otherwise.
    /// </summary>
    Task<Post?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an active (non-deleted) user by ID, or null.
    /// </summary>
    Task<User?> GetActiveAuthorAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a user by ID regardless of status (for validation), or null if not found.
    /// </summary>
    Task<User?> GetAuthorByIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an active (non-deleted, active status) category by ID, or null.
    /// </summary>
    Task<PostCategory?> GetActiveCategoryAsync(long categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active (non-deleted, active status) tags by IDs. Returns only those that match.
    /// </summary>
    Task<IReadOnlyList<Tag>> GetActiveTagsAsync(
        IReadOnlyList<long> tagIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a post with the given slug exists (case-insensitive).
    /// </summary>
    Task<bool> ExistsSlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a post with the given slug exists, excluding the specified post ID.
    /// </summary>
    Task<bool> ExistsSlugExcludingIdAsync(
        long id,
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a post with the given ID exists (regardless of deleted state).
    /// </summary>
    Task<bool> ExistsPostAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new post entity.
    /// </summary>
    Task AddPostAsync(Post post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the PostTag associations for a post. Removes existing and adds new ones.
    /// </summary>
    Task ReplacePostTagsAsync(
        Post post,
        IReadOnlyList<Tag> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
