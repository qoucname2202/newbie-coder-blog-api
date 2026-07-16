using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Summary of a role, used inside AssignUserRoleResponse for previous and current role.
/// </summary>
public sealed class RoleSummaryResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
