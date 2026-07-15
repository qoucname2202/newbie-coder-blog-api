using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Auth;

/// <summary>
/// Response data for POST /api/v1/auth/reset-password on success.
/// </summary>
public sealed class ResetPasswordSuccessResponse
{
    [JsonPropertyName("password_changed_at")]
    public DateTimeOffset PasswordChangedAt { get; set; }

    [JsonPropertyName("logout_all_devices")]
    public bool LogoutAllDevices { get; set; }
}
