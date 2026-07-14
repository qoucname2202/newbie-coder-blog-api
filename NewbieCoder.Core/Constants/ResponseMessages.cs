namespace NewbieCoder.Core.Constants;

public static class ResponseMessages
{
    public const string Success = "Success";
    public const string InternalError = "An unexpected error occurred.";
    public const string TooManyRequests = "Too many requests. Please try again later.";

    // Authentication
    public const string InvalidCredentials = "Invalid credentials.";
    public const string UserBlocked = "Your account has been locked.";
    public const string UserPending = "Your account is pending activation.";
    public const string DeviceBlocked = "This device has been blocked from logging in.";
    public const string ValidationError = "Invalid login data.";
    public const string LoginIdRequired = "Please enter your email or username.";
    public const string PasswordRequired = "Please enter your password.";
    public const string TooManyLoginAttempts = "Too many failed login attempts. Please try again after 15 minutes.";
    public const string LogoutSuccess = "Logged out successfully.";
    public const string SessionNotFound = "Session not found or has expired.";
    public const string SessionRevoked = "This session has been revoked.";
    public const string Unauthenticated = "Please log in to continue.";
    public const string UserNotFound = "Account does not exist.";

    // User management
    public const string UserUpdatedSuccess = "User account updated successfully.";
    public const string EmailAlreadyExists = "Email address is already in use.";
    public const string UsernameAlreadyExists = "Username is already taken.";
    public const string RoleNotFound = "The specified role does not exist.";
    public const string InvalidUserStatus = "Invalid user status.";
    public const string ProfileSuccess = "Profile retrieved successfully.";
    public const string SessionAlreadyRevoked = "Session has already been revoked.";
    public const string InvalidLogoutReason = "Invalid logout_reason value.";

    // Update profile
    public const string EmptyUpdateBody = "No update fields provided.";
    public const string InvalidUpdateField = "One or more fields are not allowed to be updated via this endpoint.";
    public const string UpdateProfileSuccess = "Cập nhật thông tin cá nhân thành công";
    public const string UpdateValidationFailed = "Dữ liệu cập nhật không hợp lệ";
    public const string UsernameAlreadyExists = "Username này đã được sử dụng.";
    public const string SessionInvalid = "Phiên đăng nhập không hợp lệ.";
}
