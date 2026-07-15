using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NewbieCoder.Core.Enums;

namespace NewbieCoder.Core.DTOs.Request.User;

/// <summary>
/// Request body for PUT /api/admin/users/{userId}.
/// </summary>
public sealed class UpdateUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Full name must not exceed 100 characters.")]
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
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

    [JsonPropertyName("roleId")]
    public long? RoleId { get; set; }

    [MaxLength(2048, ErrorMessage = "Avatar URL must not exceed 2048 characters.")]
    [RegularExpression(@"^https?://[^\s]+$",
        ErrorMessage = "Invalid avatar URL format.")]
    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [MaxLength(500, ErrorMessage = "Bio must not exceed 500 characters.")]
    public string? Bio { get; set; }

    [MaxLength(100, ErrorMessage = "Display title must not exceed 100 characters.")]
    [JsonPropertyName("displayTitle")]
    public string? DisplayTitle { get; set; }

    [MaxLength(2048, ErrorMessage = "Website URL must not exceed 2048 characters.")]
    [RegularExpression(@"^https?://[^\s]+$",
        ErrorMessage = "Invalid website URL format.")]
    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    public bool ShouldValidateStatus() =>
        !string.IsNullOrWhiteSpace(Status);

    public bool TryParseStatus(out UserStatus parsedStatus) =>
        Enum.TryParse(Status, ignoreCase: true, out parsedStatus);
}
