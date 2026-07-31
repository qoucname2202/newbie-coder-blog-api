using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/v1/admin/tags.
/// </summary>
public sealed class CreateTagRequest
{
    [Required(ErrorMessage = "Tag name is required.")]
    [MinLength(2, ErrorMessage = "Tag name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Tag name must not exceed 100 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [MinLength(2, ErrorMessage = "Slug must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Slug must not exceed 100 characters.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only.")]
    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// Request body for PUT /api/v1/admin/tags/{tagId}.
/// </summary>
public sealed class UpdateTagRequest
{
    [Required(ErrorMessage = "Tag name is required.")]
    [MinLength(2, ErrorMessage = "Tag name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Tag name must not exceed 100 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [MinLength(2, ErrorMessage = "Slug must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Slug must not exceed 100 characters.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only.")]
    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }
}

/// <summary>
/// Request body for PATCH /api/v1/admin/tags/{tagId}/status.
/// </summary>
public sealed class ChangeTagStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression(@"^(active|inactive)$", ErrorMessage = "Status must be 'active' or 'inactive'.")]
    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

/// <summary>
/// Request body for POST /api/v1/admin/tags/merge.
/// </summary>
public sealed class MergeTagRequest
{
    /// <summary>The tag to merge FROM. This tag will be soft-deleted after merge.</summary>
    [Required(ErrorMessage = "Source tag ID is required.")]
    [JsonPropertyName("sourceTagId")]
    public long SourceTagId { get; init; }

    /// <summary>The tag to merge INTO. All associations will be moved to this tag.</summary>
    [Required(ErrorMessage = "Target tag ID is required.")]
    [JsonPropertyName("targetTagId")]
    public long TargetTagId { get; init; }
}

/// <summary>
/// Query parameters for GET /api/v1/admin/tags.
/// </summary>
public sealed class TagFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "createdAt";

    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; set; } = "desc";
}
