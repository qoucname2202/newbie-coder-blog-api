using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NewbieCoder.Core.Converters;
using NewbieCoder.Core.Validation;

namespace NewbieCoder.Core.DTOs.Request.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/register.
/// </summary>
public sealed class RegisterRequest
{
    [TrimmedEmail(ErrorMessage = "Email is not valid.")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [TrimmedRequired(ErrorMessage = "Username is required.")]
    [Username(ErrorMessage = "Username invalid format.")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [TrimmedRequired(ErrorMessage = "Password is required.")]
    [PasswordStrength(ErrorMessage = "Password must be between 6 and 20 characters and contain only ASCII characters.")]
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [TrimmedRequired(ErrorMessage = "Confirm password is required.")]
    [JsonPropertyName("confirmPassword")]
    public string? ConfirmPassword { get; set; }

    [TrimmedRequired(ErrorMessage = "Full name is required.")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters after trimming whitespace.")]
    [MaxLength(150, ErrorMessage = "Full name must not exceed 150 characters.")]
    [FullName(ErrorMessage = "Full name must not contain leading or trailing whitespace.")]
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonConverter(typeof(NullableBoolConverter))]
    [JsonPropertyName("acceptTerms")]
    public bool? AcceptTerms { get; set; }
}
