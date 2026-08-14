using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NewbieCoder.Core.Validation;

namespace NewbieCoder.Core.DTOs.Request.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/login.
/// </summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email hoặc username")]
    [MinLength(3, ErrorMessage = "Email hoặc username phải có ít nhất 3 ký tự")]
    [MaxLength(255, ErrorMessage = "Email hoặc username không được vượt quá 255 ký tự")]
    [JsonPropertyName("login_id")]
    public string? LoginId { get; set; }

    [TrimmedRequired(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MaxLength(64, ErrorMessage = "Mật khẩu không được vượt quá 64 ký tự")]
    public string? Password { get; set; }

    [JsonPropertyName("remember_me")]
    public bool RememberMe { get; set; } = false;
}
