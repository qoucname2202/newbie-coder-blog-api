using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/register.
/// </summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
    [MaxLength(100, ErrorMessage = "Username must not exceed 100 characters.")]
    [RegularExpression(@"^[a-z0-9_-]+$",
        ErrorMessage = "Username may only contain lowercase letters, numbers, underscores, and hyphens.")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(64, ErrorMessage = "Password must not exceed 64 characters.")]
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [JsonPropertyName("confirm_password")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters.")]
    [MaxLength(150, ErrorMessage = "Full name must not exceed 150 characters.")]
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "You must accept the terms of service.")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms of service.")]
    [JsonPropertyName("accept_terms")]
    public bool AcceptTerms { get; set; }
}
