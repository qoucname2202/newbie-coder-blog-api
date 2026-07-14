using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.User;

/// <summary>
/// Request body for PATCH /api/v1/users/me.
/// All fields are optional — only non-null values will be updated.
/// Fields set to null will retain their current value (not cleared).
/// To clear a nullable field (avatar_url, bio, display_title, website_url),
/// use JSON value null in the request body.
/// </summary>
public sealed class UpdateProfileRequest
{
    [JsonPropertyName("full_name")]
    [MaxLength(150, ErrorMessage = "Full name must not exceed 150 characters")]
    public string? FullName { get; set; }

    [JsonPropertyName("username")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [MaxLength(100, ErrorMessage = "Username must not exceed 100 characters")]
    [RegularExpression(@"^[a-z0-9_-]+$", ErrorMessage = "Username can only contain lowercase letters, numbers, underscores, and hyphens")]
    public string? Username { get; set; }

    [JsonPropertyName("avatar_url")]
    [MaxLength(500, ErrorMessage = "Avatar URL must not exceed 500 characters")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("bio")]
    [MaxLength(500, ErrorMessage = "Bio must not exceed 500 characters")]
    public string? Bio { get; set; }

    [JsonPropertyName("display_title")]
    [MaxLength(100, ErrorMessage = "Display title must not exceed 100 characters")]
    public string? DisplayTitle { get; set; }

    [JsonPropertyName("website_url")]
    [MaxLength(255, ErrorMessage = "Website URL must not exceed 255 characters")]
    public string? WebsiteUrl { get; set; }
}
