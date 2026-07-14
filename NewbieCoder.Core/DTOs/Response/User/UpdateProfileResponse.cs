using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.User;

/// <summary>
/// User data returned after a successful profile update (PATCH /api/v1/users/me).
/// </summary>
public sealed class UpdateProfileResponse
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

    [JsonPropertyName("display_title")]
    public string? DisplayTitle { get; init; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
