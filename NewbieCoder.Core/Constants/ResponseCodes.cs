namespace NewbieCoder.Core.Constants;

/// <summary>
/// Business response codes returned in responseStatus.responseCode.
/// Each code must match the correct HTTP status — see Map table in docs/05-api-contracts.md.
/// </summary>
public static class ResponseCodes
{
    public const string Success = "000000";

    public const string ValidationError = "00000201";
    public const string NotFound = "00000202";
    public const string Conflict = "00000203";
    public const string Unauthorized = "00000204";
    public const string Forbidden = "00000205";
    public const string TooManyRequests = "00000429";

    public const string SessionRevoked = "00000401S";
    public const string InternalError = "00000500";

    // Password reset
    public const string InvalidEmailFormat = "00000201";
    public const string ResetTokenRequired = "00000201";
    public const string InvalidOrExpiredResetToken = "00000201";
    public const string ResetPasswordWeak = "00000206";
    public const string PasswordNotMatch = "00000207";
    public const string PasswordReused = "00000208";
    public const string TooManyResetRequests = "00000429";
    public const string EmailSendFailed = "00000500";

    // Update profile
    public const string EmptyUpdateBody = "00000401";
    public const string InvalidUpdateField = "00000402";
    public const string UserUsernameAlreadyExists = "00000409";
    // Registration
    public const string EmailAlreadyExistsAuth = "00010401";
    public const string UsernameAlreadyExistsAuth = "00010402";
    public const string PasswordTooWeak = "00010403";
    public const string TermsNotAccepted = "00010404";
    public const string DeviceBlocked = "00010405";
    public const string DefaultRoleNotFound = "00010406";

    // User lockout
    public const string UserAlreadyLocked    = "00020401";
    public const string CannotLockSelf       = "00020402";
    public const string CannotLockSuperAdmin = "00020403";
    public const string InvalidLockUntil     = "00020404";
    public const string UserLocked           = "00020405";
    public const string CannotUnlockUser     = "00020406";
    public const string UserNotLocked        = "00020407";
    public const string LockReasonTooShort   = "00020408";
    public const string LockReasonTooLong    = "00020409";
    public const string CannotLockDeleted    = "00020410";
    public const string CannotUnlockDeleted  = "00020411";

    // User management
    public const string EmailAlreadyExistsUser = "00020501";
    public const string UsernameAlreadyExistsUser = "00020502";
    public const string RoleNotFound = "00020503";
    public const string InvalidUserStatus = "00020504";
    public const string InvalidLogoutReason = "00020505";
    public const string SessionAlreadyRevoked = "00020506";
    public const string InvalidRefreshToken = "00020507";
    public const string EmailAlreadyExists = "00020508";
    public const string UsernameAlreadyExists = "00020509";
    public const string UserAlreadyExists = "00020510";
    public const string CannotCreateAdminUser = "00020511";
    public const string UserCreateFailed = "00020512";

    // Role management
    public const string RoleNameAlreadyExists = "00020601";
    public const string RoleCreateFailed = "00020602";
    public const string RoleUpdateFailed = "00020603";
    public const string RoleDeleteFailed = "00020604";
    public const string SystemRoleCannotBeModified = "00020605";
    public const string SystemRoleCannotBeDeleted = "00020606";
    public const string RoleIsAssignedToUsers = "00020607";
    public const string AdminRoleCannotBeDeactivated = "00020608";

    // Role assignment
    public const string CannotAssignSuperAdmin = "00020609";
    public const string CannotAssignHigherRole = "00020610";
    public const string CannotRemoveLastAdmin = "00020611";
    public const string CannotRemoveLastSuperAdmin = "00020612";
    public const string CannotChangeOwnRole = "00020613";
    public const string CannotAssignDeletedRole = "00020614";
    public const string CannotAssignInactiveRole = "00020615";
    public const string CannotAssignDeletedUser = "00020616";
    public const string CannotAssignInactiveUser = "00020617";
    public const string CannotAssignOwnRole = "00020618";
    public const string CannotRevokeOwnRole = "00020619";
    public const string RoleAssignSuccess = "00020600";
    public const string RoleRevokeSuccess = "00020622";

    // Post management
    public const string PostNotFound = "00020701";
    public const string PostAlreadyDeleted = "00020702";
    public const string PostNotDeleted = "00020703";
    public const string PostTitleRequired = "00020704";
    public const string PostContentRequired = "00020705";
    public const string InvalidPostStatus = "00020706";
    public const string InvalidPostStatusTransition = "00020707";
    public const string AuthorNotFound = "00020708";
    public const string AuthorInactive = "00020709";
    public const string AuthorLocked = "00020710";
    public const string InvalidAuthorSource = "00020711";
    public const string CategoryNotFound = "00020712";
    public const string CategoryInactive = "00020713";
    public const string TagNotFound = "00020714";
    public const string TagInactive = "00020715";
    public const string InvalidTagIds = "00020716";
    public const string PostSlugAlreadyExists = "00020717";
    public const string PostCreateFailed = "00020718";
    public const string PostUpdateFailed = "00020719";
    public const string PostDeleteFailed = "00020720";
    public const string PostRestoreFailed = "00020721";
    public const string PostConcurrencyConflict = "00020722";
    public const string InvalidPostVisibility = "00020723";

    /// <summary>
    /// Maps HTTP status to the default business response code when none is specified.
    /// </summary>
    public static string FromHttpStatus(int statusCode) => statusCode switch
    {
        HttpStatusCodes.Ok => Success,
        HttpStatusCodes.BadRequest => ValidationError,
        HttpStatusCodes.NotFound => NotFound,
        HttpStatusCodes.Conflict => Conflict,
        HttpStatusCodes.Unauthorized => Unauthorized,
        HttpStatusCodes.Forbidden => Forbidden,
        HttpStatusCodes.TooManyRequests => TooManyRequests,
        HttpStatusCodes.InternalServerError => InternalError,
        _ => InternalError
    };

    /// <summary>
    /// Returns the expected HTTP status for a business response code.
    /// </summary>
    public static int ToHttpStatus(string responseCode) => responseCode switch
    {
        // General
        Success => HttpStatusCodes.Ok,
        ValidationError => HttpStatusCodes.BadRequest,
        NotFound => HttpStatusCodes.NotFound,
        Conflict => HttpStatusCodes.Conflict,
        Unauthorized => HttpStatusCodes.Unauthorized,
        Forbidden => HttpStatusCodes.Forbidden,
        TooManyRequests => HttpStatusCodes.TooManyRequests,
        SessionRevoked => HttpStatusCodes.Unauthorized,
        InternalError => HttpStatusCodes.InternalServerError,

        // Auth / registration
        EmailAlreadyExistsAuth => HttpStatusCodes.Conflict,
        UsernameAlreadyExistsAuth => HttpStatusCodes.Conflict,
        PasswordTooWeak => HttpStatusCodes.BadRequest,
        TermsNotAccepted => HttpStatusCodes.BadRequest,
        DeviceBlocked => HttpStatusCodes.Forbidden,
        DefaultRoleNotFound => HttpStatusCodes.InternalServerError,
        InvalidLogoutReason => HttpStatusCodes.BadRequest,
        SessionAlreadyRevoked => HttpStatusCodes.Ok,
        InvalidRefreshToken => HttpStatusCodes.BadRequest,

        // Password reset
        ResetPasswordWeak => HttpStatusCodes.BadRequest,
        PasswordNotMatch => HttpStatusCodes.BadRequest,
        PasswordReused => HttpStatusCodes.BadRequest,

        // User lockout
        UserAlreadyLocked => HttpStatusCodes.Conflict,
        CannotLockSelf => HttpStatusCodes.Forbidden,
        CannotLockSuperAdmin => HttpStatusCodes.Forbidden,
        InvalidLockUntil => HttpStatusCodes.BadRequest,
        UserLocked => HttpStatusCodes.Forbidden,
        CannotUnlockUser => HttpStatusCodes.Forbidden,
        UserNotLocked => HttpStatusCodes.Conflict,
        LockReasonTooShort => HttpStatusCodes.BadRequest,
        LockReasonTooLong => HttpStatusCodes.BadRequest,
        CannotLockDeleted => HttpStatusCodes.Conflict,
        CannotUnlockDeleted => HttpStatusCodes.Conflict,

        // User management (Create, Update, List)
        EmailAlreadyExistsUser => HttpStatusCodes.Conflict,
        UsernameAlreadyExistsUser => HttpStatusCodes.Conflict,
        RoleNotFound => HttpStatusCodes.NotFound,
        InvalidUserStatus => HttpStatusCodes.BadRequest,
        UserAlreadyExists => HttpStatusCodes.Conflict,
        EmailAlreadyExists => HttpStatusCodes.Conflict,
        UsernameAlreadyExists => HttpStatusCodes.Conflict,
        UserUsernameAlreadyExists => HttpStatusCodes.Conflict,
        CannotCreateAdminUser => HttpStatusCodes.Forbidden,
        UserCreateFailed => HttpStatusCodes.InternalServerError,

        // Role management (Create, Update, List)
        RoleNameAlreadyExists => HttpStatusCodes.Conflict,
        RoleCreateFailed => HttpStatusCodes.InternalServerError,
        RoleUpdateFailed => HttpStatusCodes.InternalServerError,
        RoleDeleteFailed => HttpStatusCodes.InternalServerError,
        SystemRoleCannotBeModified => HttpStatusCodes.Forbidden,
        SystemRoleCannotBeDeleted => HttpStatusCodes.Forbidden,
        RoleIsAssignedToUsers => HttpStatusCodes.Conflict,
        AdminRoleCannotBeDeactivated => HttpStatusCodes.Forbidden,

        // Role assignment
        CannotAssignSuperAdmin => HttpStatusCodes.Forbidden,
        CannotAssignHigherRole => HttpStatusCodes.Forbidden,
        CannotRemoveLastAdmin => HttpStatusCodes.Conflict,
        CannotRemoveLastSuperAdmin => HttpStatusCodes.Conflict,
        CannotChangeOwnRole => HttpStatusCodes.Forbidden,
        CannotAssignDeletedRole => HttpStatusCodes.NotFound,
        CannotAssignInactiveRole => HttpStatusCodes.BadRequest,
        CannotAssignDeletedUser => HttpStatusCodes.NotFound,
        CannotAssignInactiveUser => HttpStatusCodes.BadRequest,
        CannotAssignOwnRole => HttpStatusCodes.Forbidden,
        CannotRevokeOwnRole => HttpStatusCodes.Forbidden,
        RoleAssignSuccess => HttpStatusCodes.Ok,
        RoleRevokeSuccess => HttpStatusCodes.Ok,

        // Post management
        PostNotFound => HttpStatusCodes.NotFound,
        PostAlreadyDeleted => HttpStatusCodes.Conflict,
        PostNotDeleted => HttpStatusCodes.Conflict,
        PostTitleRequired => HttpStatusCodes.BadRequest,
        PostContentRequired => HttpStatusCodes.BadRequest,
        InvalidPostStatus => HttpStatusCodes.BadRequest,
        InvalidPostStatusTransition => HttpStatusCodes.Conflict,
        AuthorNotFound => HttpStatusCodes.NotFound,
        AuthorInactive => HttpStatusCodes.BadRequest,
        AuthorLocked => HttpStatusCodes.Forbidden,
        InvalidAuthorSource => HttpStatusCodes.BadRequest,
        CategoryNotFound => HttpStatusCodes.NotFound,
        CategoryInactive => HttpStatusCodes.BadRequest,
        TagNotFound => HttpStatusCodes.NotFound,
        TagInactive => HttpStatusCodes.BadRequest,
        InvalidTagIds => HttpStatusCodes.BadRequest,
        PostSlugAlreadyExists => HttpStatusCodes.Conflict,
        PostCreateFailed => HttpStatusCodes.InternalServerError,
        PostUpdateFailed => HttpStatusCodes.InternalServerError,
        PostDeleteFailed => HttpStatusCodes.InternalServerError,
        PostRestoreFailed => HttpStatusCodes.InternalServerError,
        PostConcurrencyConflict => HttpStatusCodes.Conflict,
        InvalidPostVisibility => HttpStatusCodes.BadRequest,

        _ => HttpStatusCodes.InternalServerError
    };
}
