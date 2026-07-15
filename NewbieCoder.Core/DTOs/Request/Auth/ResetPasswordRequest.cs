using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/reset-password.
/// </summary>
public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "Reset token không được để trống.")]
    [JsonPropertyName("reset_token")]
    public string? ResetToken { get; set; }

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [MaxLength(64, ErrorMessage = "Mật khẩu không được vượt quá 64 ký tự.")]
    [JsonPropertyName("new_password")]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Mật khẩu xác nhận không được để trống.")]
    [JsonPropertyName("confirm_password")]
    public string? ConfirmPassword { get; set; }

    [JsonPropertyName("logout_all_devices")]
    public bool LogoutAllDevices { get; set; } = true;
}
