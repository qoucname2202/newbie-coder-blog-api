using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/v1/admin/categories.
/// </summary>
public sealed class CreateCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [MinLength(2, ErrorMessage = "Category name must be at least 2 characters.")]
    [MaxLength(255, ErrorMessage = "Category name must not exceed 255 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [MinLength(2, ErrorMessage = "Slug must be at least 2 characters.")]
    [MaxLength(255, ErrorMessage = "Slug must not exceed 255 characters.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only.")]
    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The ID of the parent category. Null means root category.</summary>
    [JsonPropertyName("parentId")]
    public long? ParentId { get; init; }
}

/// <summary>
/// Request body for PUT /api/v1/admin/categories/{categoryId}.
/// </summary>
public sealed class UpdateCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [MinLength(2, ErrorMessage = "Category name must be at least 2 characters.")]
    [MaxLength(255, ErrorMessage = "Category name must not exceed 255 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [MinLength(2, ErrorMessage = "Slug must be at least 2 characters.")]
    [MaxLength(255, ErrorMessage = "Slug must not exceed 255 characters.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only.")]
    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The ID of the parent category. Null means root category.</summary>
    [JsonPropertyName("parentId")]
    public long? ParentId { get; init; }

    /// <summary>Whether the category is active. Omit to keep current value.</summary>
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }
}

/// <summary>
/// Request body for PATCH /api/v1/admin/categories/{categoryId}/status.
/// </summary>
public sealed class ChangeCategoryStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression(@"^(active|inactive)$", ErrorMessage = "Status must be 'active' or 'inactive'.")]
    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

/// <summary>
/// Query parameters for GET /api/v1/admin/categories.
/// </summary>
public sealed class CategoryFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    /// <summary>Filter by parent ID. Null means all categories. 0 means root categories only.</summary>
    [JsonPropertyName("parentId")]
    public long? ParentId { get; set; }

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "createdAt";

    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; set; } = "desc";
}
