using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response DTO for a created or updated tag.
/// </summary>
public sealed class TagResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }

    [JsonPropertyName("postCount")]
    public required int PostCount { get; init; }

    [JsonPropertyName("questionCount")]
    public required int QuestionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response DTO for a tag detail.
/// </summary>
public sealed class TagDetailResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }

    [JsonPropertyName("postCount")]
    public required int PostCount { get; init; }

    [JsonPropertyName("questionCount")]
    public required int QuestionCount { get; init; }

    [JsonPropertyName("followerCount")]
    public required int FollowerCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response DTO for a single tag item in a list.
/// </summary>
public sealed class TagListItemResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }

    [JsonPropertyName("postCount")]
    public required int PostCount { get; init; }

    [JsonPropertyName("questionCount")]
    public required int QuestionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response DTO for a successful tag merge.
/// </summary>
public sealed class MergeTagResponse
{
    [JsonPropertyName("sourceTagId")]
    public required long SourceTagId { get; init; }

    [JsonPropertyName("sourceTagName")]
    public required string SourceTagName { get; init; }

    [JsonPropertyName("targetTagId")]
    public required long TargetTagId { get; init; }

    [JsonPropertyName("targetTagName")]
    public required string TargetTagName { get; init; }

    [JsonPropertyName("postsMerged")]
    public required int PostsMerged { get; init; }

    [JsonPropertyName("questionsMerged")]
    public required int QuestionsMerged { get; init; }
}
