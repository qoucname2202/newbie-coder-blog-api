using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for PUT /api/v1/admin/roles/{roleId}.
/// </summary>
public sealed class UpdateRoleRequest
{
    [Required(ErrorMessage = "Role name is required.")]
    [MinLength(2, ErrorMessage = "Role name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Role name must not exceed 100 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [MaxLength(255, ErrorMessage = "Description must not exceed 255 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}
