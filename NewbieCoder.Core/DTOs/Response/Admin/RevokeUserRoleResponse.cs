using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response body returned after successfully revoking an elevated role from a user.
/// The user is assigned back to the base USER role.
/// </summary>
public sealed class RevokeUserRoleResponse
{
    [JsonPropertyName("userId")]
    public required long UserId { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("previousRole")]
    public required RoleSummaryResponse PreviousRole { get; init; }

    [JsonPropertyName("currentRole")]
    public required RoleSummaryResponse CurrentRole { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
