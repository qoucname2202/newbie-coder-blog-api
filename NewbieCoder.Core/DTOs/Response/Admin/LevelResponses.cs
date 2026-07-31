using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response body for a single level (returned by create, update, change-status).
/// </summary>
public sealed class LevelResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("code")]
    public string Code { get; init; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Response body for a level list item (returned by GET list with pagination).
/// </summary>
public sealed class LevelListItemResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("code")]
    public string Code { get; init; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("questionCount")]
    public int QuestionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Response body for GET /api/v1/admin/levels/{id} — full level detail.
/// </summary>
public sealed class LevelDetailResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("code")]
    public string Code { get; init; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("questionCount")]
    public int QuestionCount { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("createdBy")]
    public long? CreatedBy { get; init; }

    [JsonPropertyName("updatedBy")]
    public long? UpdatedBy { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }

    [JsonPropertyName("deletedBy")]
    public long? DeletedBy { get; init; }
}
