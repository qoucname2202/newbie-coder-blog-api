namespace NewbieCoder.Core.Constants;

/// <summary>
/// Standard audit log action names used throughout the application.
/// </summary>
public static class AuditActions
{
    public const string UserLogin = "USER_LOGIN";
    public const string UserLoginFailed = "USER_LOGIN_FAILED";
    public const string UserLogout = "USER_LOGOUT";
    public const string UserLogoutAll = "USER_LOGOUT_ALL";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string UserRegistered = "USER_REGISTERED";
    public const string DeviceBlocked = "DEVICE_BLOCKED";
    public const string DeviceRemoved = "DEVICE_REMOVED";
}
