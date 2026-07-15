using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/admin/users.
/// </summary>
public sealed class CreateUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100, ErrorMessage = "Full name must not exceed 100 characters.")]
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(50, ErrorMessage = "Username must not exceed 50 characters.")]
    [RegularExpression(@"^[a-z0-9_-]+$",
        ErrorMessage = "Username may only contain lowercase letters, numbers, underscores, and hyphens.")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(64, ErrorMessage = "Password must not exceed 64 characters.")]
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Optional role ID to assign to the new user.
    /// If not provided, the default USER role is assigned.
    /// </summary>
    [JsonPropertyName("roleId")]
    public long? RoleId { get; set; }

    [MaxLength(500, ErrorMessage = "Bio must not exceed 500 characters.")]
    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [MaxLength(100, ErrorMessage = "Display title must not exceed 100 characters.")]
    [JsonPropertyName("displayTitle")]
    public string? DisplayTitle { get; set; }

    [Url(ErrorMessage = "Invalid avatar URL format.")]
    [MaxLength(2048, ErrorMessage = "Avatar URL must not exceed 2048 characters.")]
    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [Url(ErrorMessage = "Invalid website URL format.")]
    [MaxLength(2048, ErrorMessage = "Website URL must not exceed 2048 characters.")]
    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }
}
