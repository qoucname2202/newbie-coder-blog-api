using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.User;

/// <summary>
/// Top-level response body for GET /api/v1/users/me.
/// </summary>
public sealed class UserMeResponse
{
    [JsonPropertyName("user")]
    public required UserProfileDto User { get; init; }

    [JsonPropertyName("statistics")]
    public required UserStatisticsDto Statistics { get; init; }

    [JsonPropertyName("current_session")]
    public required CurrentSessionDto CurrentSession { get; init; }
}
