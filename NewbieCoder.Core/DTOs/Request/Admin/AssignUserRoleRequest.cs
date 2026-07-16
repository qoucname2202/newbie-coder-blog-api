using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for PUT /api/v1/admin/users/{userId}/role
/// </summary>
public sealed class AssignUserRoleRequest
{
    [Required(ErrorMessage = "Role ID is required.")]
    [JsonPropertyName("roleId")]
    public required long RoleId { get; set; }
}
