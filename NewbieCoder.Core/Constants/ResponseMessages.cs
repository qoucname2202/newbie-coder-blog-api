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

    // User lockout
    public const string UserLocked           = "Your account has been locked.";
    public const string UserAlreadyLocked    = "Account is already locked.";
    public const string CannotLockSelf       = "You cannot lock your own account.";
    public const string CannotLockSuperAdmin = "Cannot lock a super admin account.";
    public const string InvalidLockUntil     = "Lock expiration time must be in the future.";
    public const string CannotUnlockUser     = "You do not have permission to unlock this account.";
    public const string UserNotLocked        = "Account is not currently locked.";
    public const string LockReasonEmpty   = "Lock reason cannot be empty or contain only whitespace.";
    public const string LockReasonTooShort = "Lock reason must be at least 5 characters.";
    public const string LockReasonTooLong  = "Lock reason must not exceed 500 characters.";
    public const string CannotLockDeleted  = "Cannot lock a deleted account.";
    public const string CannotUnlockDeleted = "Cannot unlock a deleted account.";
    public const string LockSucceeded      = "User account locked successfully.";
    public const string UnlockSucceeded    = "User account unlocked successfully.";

    // Role management
    public const string RoleCreatedSuccess        = "Role created successfully.";
    public const string RoleUpdatedSuccess        = "Role updated successfully.";
    public const string RoleDeletedSuccess        = "Role deleted successfully.";
    public const string RoleNameAlreadyExists     = "A role with this name already exists.";
    public const string RoleCreateFailed          = "Failed to create role.";
    public const string RoleUpdateFailed          = "Failed to update role.";
    public const string RoleDeleteFailed          = "Failed to delete role.";
    public const string SystemRoleCannotBeModified = "System roles cannot be modified.";
    public const string SystemRoleCannotBeDeleted = "System roles cannot be deleted.";
    public const string RoleIsAssignedToUsers     = "Role is currently assigned to one or more users and cannot be deleted.";
    public const string AdminRoleCannotBeDeactivated = "The admin role cannot be deactivated.";
    public const string RolesRetrieved           = "Roles retrieved successfully.";
    public const string RoleAssignedSuccess      = "Role assigned successfully.";
    public const string CannotAssignSuperAdmin  = "Cannot assign the SuperAdmin role.";
    public const string CannotAssignHigherRole  = "Cannot assign a role higher than your own role.";
    public const string CannotRemoveLastAdmin   = "Cannot remove the last Admin role.";
    public const string CannotRemoveLastSuperAdmin = "Cannot remove the last SuperAdmin role.";
    public const string CannotChangeOwnRole     = "Cannot change your own role.";
    public const string CannotAssignDeletedRole = "Cannot assign a deleted role.";
    public const string CannotAssignInactiveRole = "Cannot assign an inactive role.";
    public const string CannotAssignDeletedUser = "Cannot assign a role to a deleted user.";
    public const string CannotAssignInactiveUser = "Cannot assign a role to an inactive user.";
    public const string CannotAssignOwnRole     = "Cannot assign a role to yourself.";
    public const string CannotRevokeOwnRole   = "Cannot revoke your own role.";
    public const string RoleRevokedSuccess       = "Role revoked successfully.";
    // Password reset
    public const string InvalidEmailFormat = "Invalid email format.";
    public const string ResetTokenRequired = "Reset token is required.";
    public const string InvalidOrExpiredResetToken = "Password reset token is invalid or has expired.";
    public const string PasswordTooWeak = "Password must be at least 8 characters, including uppercase, lowercase, number and special character.";
    public const string PasswordNotMatch = "Password confirmation does not match.";
    public const string PasswordReused = "New password must not be the same as the current password.";
    public const string TooManyResetRequests = "Too many password reset requests. Please try again later.";
    public const string EmailSendFailed = "Unable to send email. Please try again later.";
    public const string ForgotPasswordSuccess = "If the email exists in our system, we have sent password reset instructions.";
    public const string ResetPasswordSuccess = "Password reset successfully. Please log in again.";
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
    public const string UpdateProfileSuccess = "Personal information updated successfully.";
    public const string UpdateValidationFailed = "Invalid update data.";
    public const string SessionInvalid = "Invalid session.";
}
