using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NewbieCoder.Core.Validation;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/admin/users.
/// </summary>
public sealed class CreateUserRequest
{
    [TrimmedRequired(ErrorMessage = "Full name is required.")]
    [MaxLength(100, ErrorMessage = "Full name must not exceed 100 characters.")]
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [TrimmedRequired(ErrorMessage = "Username is required.")]
    [Username(ErrorMessage = "Username invalid format.")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [TrimmedRequired(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [TrimmedRequired(ErrorMessage = "Password is required.")]
    [PasswordStrength(ErrorMessage = "Password must be between 8 and 128 characters.")]
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
