using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response DTO for a single role item in a list.
/// </summary>
public sealed class RoleListItemResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isSystemRole")]
    public required bool IsSystemRole { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }

    [JsonPropertyName("userCount")]
    public required int UserCount { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
