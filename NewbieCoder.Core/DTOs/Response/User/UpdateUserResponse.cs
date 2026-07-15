using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.User;

/// <summary>
/// Response body returned after successfully updating a user account via PUT /api/admin/users/{userId}.
/// </summary>
public sealed class UpdateUserResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("full_name")]
    public required string FullName { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    public string? Bio { get; init; }

    [JsonPropertyName("display_title")]
    public string? DisplayTitle { get; init; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public required string UpdatedAt { get; init; }
}
