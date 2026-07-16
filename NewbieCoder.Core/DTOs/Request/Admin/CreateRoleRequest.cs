using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/v1/admin/roles.
/// </summary>
public sealed class CreateRoleRequest
{
    [Required(ErrorMessage = "Role name is required.")]
    [MinLength(2, ErrorMessage = "Role name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Role name must not exceed 100 characters.")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [MaxLength(255, ErrorMessage = "Description must not exceed 255 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
