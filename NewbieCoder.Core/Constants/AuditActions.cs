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

    // Role management
    public const string RoleCreated = "ROLE_CREATED";
    public const string RoleUpdated = "ROLE_UPDATED";
    public const string RoleDeleted = "ROLE_DELETED";
    public const string RoleActivated = "ROLE_ACTIVATED";
    public const string RoleDeactivated = "ROLE_DEACTIVATED";
}
