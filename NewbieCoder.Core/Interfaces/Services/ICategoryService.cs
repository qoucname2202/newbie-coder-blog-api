using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service interface for admin category management operations.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Creates a new category.
    /// </summary>
    Task<CategoryResponse> CreateCategoryAsync(
        CreateCategoryRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of categories.
    /// </summary>
    Task<PaginatedResponse<CategoryListItemResponse>> GetCategoriesAsync(
        CategoryFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full category details by ID.
    /// </summary>
    Task<CategoryDetailResponse> GetCategoryByIdAsync(
        long categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task<CategoryResponse> UpdateCategoryAsync(
        long categoryId,
        UpdateCategoryRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of a category.
    /// </summary>
    Task<CategoryResponse> ChangeStatusAsync(
        long categoryId,
        ChangeCategoryStatusRequest request,
        long changedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a category. Fails if it has children or associated posts.
    /// </summary>
    Task DeleteCategoryAsync(
        long categoryId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
