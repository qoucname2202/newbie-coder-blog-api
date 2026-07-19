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
    public const string UserRoleAssigned = "USER_ROLE_ASSIGNED";
    public const string UserRoleRevoked = "USER_ROLE_REVOKED";

    // Post management
    public const string PostCreatedByAdmin = "POST_CREATED_BY_ADMIN";
    public const string SystemPostCreated = "SYSTEM_POST_CREATED";
    public const string PostCreatedForAuthor = "POST_CREATED_FOR_AUTHOR";
    public const string PostUpdatedByAdmin = "POST_UPDATED_BY_ADMIN";
    public const string PostDeletedByAdmin = "POST_DELETED_BY_ADMIN";
    public const string PostRestoredByAdmin = "POST_RESTORED_BY_ADMIN";
    public const string PostPublishedByAdmin = "POST_PUBLISHED_BY_ADMIN";
    public const string PostVisibilityChanged = "POST_VISIBILITY_CHANGED";

    // Interview question management
    public const string InterviewQuestionCreated = "INTERVIEW_QUESTION_CREATED";
    public const string InterviewQuestionUpdated = "INTERVIEW_QUESTION_UPDATED";
    public const string InterviewQuestionDeleted = "INTERVIEW_QUESTION_DELETED";
    public const string InterviewQuestionRestored = "INTERVIEW_QUESTION_RESTORED";
    public const string InterviewQuestionStatusChanged = "INTERVIEW_QUESTION_STATUS_CHANGED";
}
