using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// User summary returned by GET /api/admin/users.
/// Sensitive fields (password hash, refresh token, security stamp, etc.) are intentionally omitted.
/// </summary>
public sealed class UserListItemResponse
{
    public required long Id { get; init; }

    [JsonPropertyName("full_name")]
    public required string FullName { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("display_title")]
    public string? DisplayTitle { get; init; }

    public string? Bio { get; init; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; init; }

    public required string Status { get; init; }

    public required string Role { get; init; }

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("last_login_at")]
    public DateTimeOffset? LastLoginAt { get; init; }
}
