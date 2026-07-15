using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Summary of the admin who performed a lock/unlock action.
/// </summary>
public sealed class AdminSummaryDto
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }
}

/// <summary>
/// User account state returned after a lock or unlock operation.
/// </summary>
public sealed class LockedUserDto
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("locked_at")]
    public DateTimeOffset? LockedAt { get; init; }

    [JsonPropertyName("locked_reason")]
    public string? LockedReason { get; init; }

    [JsonPropertyName("locked_by")]
    public AdminSummaryDto? LockedBy { get; init; }
}

/// <summary>
/// Response body returned after successfully locking a user account.
/// Matches spec A04: { id, status, locked_at, locked_reason, locked_by }.
/// </summary>
public sealed class LockUserResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("locked_at")]
    public DateTimeOffset? LockedAt { get; init; }

    [JsonPropertyName("locked_reason")]
    public string? LockedReason { get; init; }

    [JsonPropertyName("locked_by")]
    public AdminSummaryDto? LockedBy { get; init; }
}
