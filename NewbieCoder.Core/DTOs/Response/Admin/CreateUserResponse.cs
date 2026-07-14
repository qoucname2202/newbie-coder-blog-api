using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response returned after successfully creating a user account via POST /api/admin/users.
/// Password and security-related fields are intentionally excluded.
/// </summary>
public sealed class CreateUserResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    [JsonPropertyName("displayTitle")]
    public string? DisplayTitle { get; init; }

    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
}
