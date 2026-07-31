using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository for PostCategory data access operations used by the admin module.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Returns a paginated list of categories matching the filter criteria.
    /// </summary>
    Task<PaginatedResponse<CategoryListItemResponse>> GetPagedAsync(
        CategoryFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full category details (no tracking) by ID, including children and parent.
    /// Returns null if not found or deleted.
    /// </summary>
    Task<CategoryDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked PostCategory entity by ID (for updates). Returns null if not found or deleted.
    /// </summary>
    Task<PostCategory?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an active (non-deleted) category by ID, or null.
    /// </summary>
    Task<PostCategory?> GetActiveByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a category with the given slug exists (case-insensitive).
    /// </summary>
    Task<bool> ExistsSlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a category with the given slug exists, excluding the specified category ID.
    /// </summary>
    Task<bool> ExistsSlugExcludingIdAsync(
        long id,
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a category has any child categories.
    /// </summary>
    Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a category has any associated posts.
    /// </summary>
    Task<bool> HasPostsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new category entity.
    /// </summary>
    Task AddAsync(PostCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
