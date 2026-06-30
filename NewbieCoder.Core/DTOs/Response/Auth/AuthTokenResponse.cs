using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Auth;

/// <summary>
/// JWT access token + refresh token returned on successful login.
/// </summary>
public sealed class AuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public required string RefreshTokenJwt { get; init; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }
}
