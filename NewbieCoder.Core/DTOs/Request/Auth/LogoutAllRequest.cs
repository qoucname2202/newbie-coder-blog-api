using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NewbieCoder.Core.DTOs.Request.Auth;

namespace NewbieCoder.Core.DTOs.Request.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/logout-all.
/// </summary>
public sealed class LogoutAllRequest
{
    /// <summary>
    /// When <c>true</c>, all sessions except the current one are revoked.
    /// When <c>false</c> (default), all sessions including the current one are revoked.
    /// </summary>
    [JsonPropertyName("keep_current_session")]
    public bool KeepCurrentSession { get; set; } = false;

    /// <summary>
    /// Reason for the logout. Controls the value written to <c>user_sessions.revoked_reason</c>.
    /// </summary>
    [JsonPropertyName("logout_reason")]
    [EnumDataType(typeof(LogoutReason), ErrorMessage = "Invalid logout_reason. Allowed: logout_all, security, password_changed.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogoutReason LogoutReason { get; set; } = LogoutReason.LogoutAll;
}
