using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for PATCH /api/v1/admin/users/{userId}/lock.
/// Spec A04 — admin lock (no auto-unlock).
/// </summary>
public sealed class LockUserRequest
{
    [Required(ErrorMessage = "Lock reason is required.")]
    [MinLength(5, ErrorMessage = "Lock reason must be at least 5 characters.")]
    [MaxLength(500, ErrorMessage = "Lock reason must not exceed 500 characters.")]
    [JsonPropertyName("reason")]
    public required string Reason { get; set; }
}
