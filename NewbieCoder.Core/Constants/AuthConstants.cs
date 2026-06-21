namespace NewbieCoder.Core.Constants;

/// <summary>
/// Constants for authentication flows (login, sessions, devices).
/// </summary>
public static class AuthConstants
{
    public const string TokenTypeBearer = "Bearer";

    public const int AccessTokenExpirationSeconds = 900;
    public const int RefreshTokenExpirationDaysDefault = 7;
    public const int RefreshTokenExpirationDaysRememberMe = 30;
    public const int SessionExpirationDays = 7;

    public const int LoginIdMinLength = 3;
    public const int LoginIdMaxLength = 255;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 64;

    public static readonly string[] AllowedDeviceTypes = ["web", "mobile", "tablet", "desktop"];

    public const string HeaderDeviceId = "X-Device-Id";
    public const string HeaderDeviceName = "X-Device-Name";
    public const string HeaderDeviceType = "X-Device-Type";
    public const string HeaderForwardedFor = "X-Forwarded-For";
}

/// <summary>
/// User account status values stored in users.status (DB check constraint).
/// </summary>
public static class UserAccountStatus
{
    public const string Active = "ACT";
    public const string Inactive = "INACT";
    public const string Banned = "BAN";
    public const string Closed = "CLS";
}

/// <summary>
/// Device, session, and role status values used in auth queries.
/// </summary>
public static class AuthEntityStatus
{
    public const string Active = "active";
    public const string Blocked = "blocked";
    public const string Revoked = "revoked";
    public const string RoleActive = "active";
    public const string PermissionActive = "ACT";
}

/// <summary>
/// Login history status and failure reason codes.
/// </summary>
public static class LoginHistoryConstants
{
    public const string StatusSuccess = "success";
    public const string StatusFailed = "failed";

    public const string ReasonUserNotFound = "user_not_found";
    public const string ReasonWrongPassword = "wrong_password";
    public const string ReasonBlockedUser = "blocked_user";
    public const string ReasonBlockedDevice = "blocked_device";
    public const string ReasonPendingUser = "pending_user";
}

/// <summary>
/// Auth-specific error codes returned in tracingMessage for client diagnostics.
/// </summary>
public static class AuthErrorCodes
{
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserBlocked = "USER_BLOCKED";
    public const string UserPending = "USER_PENDING";
    public const string DeviceBlocked = "DEVICE_BLOCKED";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string LoginIdRequired = "LOGIN_ID_REQUIRED";
    public const string PasswordRequired = "PASSWORD_REQUIRED";
}
