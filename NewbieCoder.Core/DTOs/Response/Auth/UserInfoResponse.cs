using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Auth;

/// <summary>
/// Current authenticated user information returned by GET /api/v1/auth/me.
/// </summary>
public sealed class UserInfoResponse
{
    public required long Id { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("full_name")]
    public required string FullName { get; init; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    public string? Bio { get; init; }

    [JsonPropertyName("roles")]
    public required IReadOnlyList<string> Roles { get; init; }
}
