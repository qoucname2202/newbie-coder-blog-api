using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response DTO for a created or updated category.
/// </summary>
public sealed class CategoryResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("parentId")]
    public long? ParentId { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response DTO for a category detail.
/// </summary>
public sealed class CategoryDetailResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("parentId")]
    public long? ParentId { get; init; }

    [JsonPropertyName("parentName")]
    public string? ParentName { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }

    [JsonPropertyName("postCount")]
    public required int PostCount { get; init; }

    [JsonPropertyName("childCount")]
    public required int ChildCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("children")]
    public IReadOnlyList<CategoryItemResponse> Children { get; init; } = [];
}

/// <summary>
/// Response DTO for a single category item in a list.
/// </summary>
public sealed class CategoryListItemResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("parentId")]
    public long? ParentId { get; init; }

    [JsonPropertyName("parentName")]
    public string? ParentName { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }

    [JsonPropertyName("postCount")]
    public required int PostCount { get; init; }

    [JsonPropertyName("childCount")]
    public required int ChildCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Summary of a category, used inside nested structures.
/// </summary>
public sealed class CategoryItemResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }
}
