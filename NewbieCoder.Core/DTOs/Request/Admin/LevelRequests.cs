using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/v1/admin/levels.
/// </summary>
public sealed class CreateLevelRequest
{
    [Required(ErrorMessage = "Level code is required.")]
    [MaxLength(50, ErrorMessage = "Level code must not exceed 50 characters.")]
    [RegularExpression(@"^[A-Z][A-Z0-9_]*$", ErrorMessage = "Level code must be uppercase alphanumeric with underscores only (e.g. JUNIOR, MIDDLE_2).")]
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [Required(ErrorMessage = "Level name is required.")]
    [MinLength(2, ErrorMessage = "Level name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Level name must not exceed 100 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Display order must be greater than or equal to zero.")]
    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; init; } = 0;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Request body for PUT /api/v1/admin/levels/{id}.
/// </summary>
public sealed class UpdateLevelRequest
{
    [Required(ErrorMessage = "Level code is required.")]
    [MaxLength(50, ErrorMessage = "Level code must not exceed 50 characters.")]
    [RegularExpression(@"^[A-Z][A-Z0-9_]*$", ErrorMessage = "Level code must be uppercase alphanumeric with underscores only (e.g. JUNIOR, MIDDLE_2).")]
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [Required(ErrorMessage = "Level name is required.")]
    [MinLength(2, ErrorMessage = "Level name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Level name must not exceed 100 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Display order must be greater than or equal to zero.")]
    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; init; } = 0;

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }
}

/// <summary>
/// Query parameters for GET /api/v1/admin/levels.
/// </summary>
public sealed class LevelFilterRequest
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    /// <summary>Search keyword matching against code, name, and description.</summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>Filter by active status.</summary>
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    /// <summary>Sort field: name, code, displayOrder, createdAt. Default: createdAt.</summary>
    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "createdAt";

    /// <summary>Sort direction: asc or desc. Default: desc.</summary>
    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; set; } = "desc";
}
