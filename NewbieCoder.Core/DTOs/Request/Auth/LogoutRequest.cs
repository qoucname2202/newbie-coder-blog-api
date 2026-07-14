using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Request.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/logout.
/// Both fields are optional — logout is always valid from the current session alone.
/// </summary>
public sealed class LogoutRequest
{
    /// <summary>
    /// Optional refresh token. If provided, the server hashes and matches it against
    /// <c>refresh_tokens.token_hash</c>. Never logged in plain text.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Reason for the logout. Controls the value written to <c>user_sessions.revoked_reason</c>.
    /// </summary>
    [JsonPropertyName("logout_reason")]
    [EnumDataType(typeof(LogoutReason), ErrorMessage = "Invalid logout_reason. Allowed: user_logout, security, switch_account.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogoutReason LogoutReason { get; set; } = LogoutReason.UserLogout;
}

/// <summary>
/// Reason for a logout action. Controls the value written to <c>user_sessions.revoked_reason</c>.
/// Allowed values vary by endpoint — see <c>LogoutRequest</c> and <c>LogoutAllRequest</c>.
/// </summary>
public enum LogoutReason
{
    // POST /logout
    /// <summary>Normal logout by the user.</summary>
    UserLogout,

    /// <summary>Logout triggered by a security event (e.g., suspicious activity).</summary>
    Security,

    /// <summary>User switched to another account on the same device.</summary>
    SwitchAccount,

    // POST /logout-all
    /// <summary>Logout all devices via the explicit logout-all action.</summary>
    LogoutAll,

    /// <summary>Logout all devices triggered by a password change.</summary>
    PasswordChanged
}
