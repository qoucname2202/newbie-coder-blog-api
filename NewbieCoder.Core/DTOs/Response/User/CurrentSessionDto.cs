using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.User;

/// <summary>
/// Current session/device information included in the profile response.
/// </summary>
public sealed class CurrentSessionDto
{
    [JsonPropertyName("session_id")]
    public required long SessionId { get; init; }

    [JsonPropertyName("device_id")]
    public required long DeviceId { get; init; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; init; }

    [JsonPropertyName("device_type")]
    public required string DeviceType { get; init; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; init; }

    [JsonPropertyName("login_at")]
    public DateTimeOffset LoginAt { get; init; }

    [JsonPropertyName("last_active_at")]
    public DateTimeOffset? LastActiveAt { get; init; }

    [JsonPropertyName("expired_at")]
    public DateTimeOffset ExpiredAt { get; init; }
}
