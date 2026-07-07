using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Auth;

/// <summary>
/// Successful registration response returned by POST /api/v1/auth/register.
/// </summary>
public sealed class RegisterResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }

    [JsonPropertyName("scope")]
    public required string Scope { get; init; }
}
