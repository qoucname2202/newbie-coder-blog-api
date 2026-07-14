using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.User;

/// <summary>
/// User profile data returned by GET /api/v1/users/me.
/// </summary>
public sealed class UserProfileDto
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("full_name")]
    public required string FullName { get; init; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("last_login_at")]
    public DateTimeOffset? LastLoginAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }
}
