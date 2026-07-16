using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response body returned after successfully assigning a role to a user.
/// Matches spec A09: { userId, username, email, previousRole, currentRole, updatedAt }.
/// </summary>
public sealed class AssignUserRoleResponse
{
    [JsonPropertyName("userId")]
    public required long UserId { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("previousRole")]
    public RoleSummaryResponse? PreviousRole { get; init; }

    [JsonPropertyName("currentRole")]
    public required RoleSummaryResponse CurrentRole { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
