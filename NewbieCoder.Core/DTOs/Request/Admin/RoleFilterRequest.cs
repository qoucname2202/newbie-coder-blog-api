using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Query parameters for GET /api/v1/admin/roles.
/// </summary>
public sealed class RoleFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("isSystemRole")]
    public bool? IsSystemRole { get; set; }

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "createdAt";

    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; set; } = "desc";
}
