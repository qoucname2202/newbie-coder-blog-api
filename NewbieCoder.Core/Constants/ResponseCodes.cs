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
    public const string PostAlreadyInTargetStatus = "00020724";
    public const string PostStatusUpdateFailed = "00020725";

    // Interview question management
    public const string InterviewQuestionNotFound = "00020730";
    public const string InterviewQuestionAlreadyDeleted = "00020731";
    public const string InterviewQuestionNotDeleted = "00020732";
    public const string InterviewQuestionTitleRequired = "00020733";
    public const string InterviewQuestionContentRequired = "00020734";
    public const string InterviewQuestionAnswerRequired = "00020735";
    public const string InterviewQuestionInvalidDifficulty = "00020736";
    public const string InterviewQuestionInvalidStatus = "00020737";
    public const string InterviewQuestionInvalidStatusTransition = "00020738";
    public const string InterviewQuestionAlreadyInTargetStatus = "00020739";
    public const string InterviewQuestionCategoryNotFound = "00020740";
    public const string InterviewQuestionTagNotFound = "00020741";
    public const string InterviewQuestionInvalidAnswers = "00020742";
    public const string InterviewQuestionMultiplePreferredAnswers = "00020743";
    public const string InterviewQuestionCreateFailed = "00020750";
    public const string InterviewQuestionUpdateFailed = "00020751";
    public const string InterviewQuestionDeleteFailed = "00020752";
    public const string InterviewQuestionRestoreFailed = "00020753";
    public const string InterviewQuestionStatusUpdateFailed = "00020754";

    // Category management
    public const string CategoryNameRequired = "00020801";
    public const string CategorySlugRequired = "00020802";
    public const string CategorySlugAlreadyExists = "00020803";
    public const string CategoryInvalidStatus = "00020804";
    public const string CategoryAlreadyInTargetStatus = "00020805";
    public const string CategoryStatusUpdateFailed = "00020806";
    public const string CategoryCreateFailed = "00020807";
    public const string CategoryUpdateFailed = "00020808";
    public const string CategoryDeleteFailed = "00020809";
    public const string CategoryHasChildren = "00020810";
    public const string CategoryHasPosts = "00020811";

    // Tag management
    public const string TagNameRequired = "00020901";
    public const string TagSlugRequired = "00020902";
    public const string TagSlugAlreadyExists = "00020903";
    public const string TagInvalidStatus = "00020904";
    public const string TagAlreadyInTargetStatus = "00020905";
    public const string TagStatusUpdateFailed = "00020906";
    public const string TagCreateFailed = "00020907";
    public const string TagUpdateFailed = "00020908";
    public const string TagDeleteFailed = "00020909";
    public const string TagSourceNotFound = "00020910";
    public const string TagTargetNotFound = "00020911";
    public const string TagNotMergeableWithSelf = "00020912";
    public const string TagHasNoAssociations = "00020913";
    public const string TagMergeFailed = "00020914";

    // Image upload
    public const string ImageFileEmpty = "00021001";
    public const string ImageFileTooLarge = "00021002";
    public const string ImageTooManyFiles = "00021003";
    public const string ImageInvalidExtension = "00021004";
    public const string ImageInvalidMimeType = "00021005";
    public const string ImageInvalidFormat = "00021006";
    public const string ImageInvalidDimensions = "00021007";
    public const string ImageUnreadable = "00021008";
    public const string ImagePublicIdRequired = "00021009";
    public const string ImageUploadFailed = "00021010";
    public const string ImageDeleteFailed = "00021011";
    // Level management
    public const string LevelNotFound = "00021001";
    public const string LevelCodeRequired = "00021002";
    public const string LevelNameRequired = "00021003";
    public const string LevelCodeAlreadyExists = "00021004";
    public const string LevelNameAlreadyExists = "00021005";
    public const string LevelHasInterviewQuestions = "00021006";
    public const string LevelCreateFailed = "00021007";
    public const string LevelUpdateFailed = "00021008";
    public const string LevelDeleteFailed = "00021009";

    // Community question management
    public const string CommunityQuestionNotFound = "00021101";
    public const string CommunityQuestionAlreadyLocked = "00021102";
    public const string CommunityQuestionNotLocked = "00021103";
    public const string CommunityQuestionAlreadyDeleted = "00021104";
    public const string CommunityQuestionNotDeleted = "00021105";
    public const string CommunityQuestionCannotBeModerated = "00021106";
    public const string CommunityQuestionDeleteFailed = "00021107";
    public const string CommunityQuestionRestoreFailed = "00021108";
    public const string CommunityQuestionLockFailed = "00021109";
    public const string CommunityQuestionUnlockFailed = "00021110";
    public const string CommunityQuestionAlreadyDeletedOrNotFound = "00021111";
    public const string CommunityQuestionCreatedFailed = "00021112";
    public const string CommunityQuestionUpdatedFailed = "00021113";
    public const string CommunityQuestionAuthorNotFound = "00021114";
    public const string CommunityQuestionTitleRequired = "00021115";
    public const string CommunityQuestionContentRequired = "00021116";
    public const string CommunityQuestionTitleTooShort = "00021117";
    public const string CommunityQuestionTitleTooLong = "00021118";
    public const string CommunityQuestionContentTooShort = "00021119";
    public const string CommunityQuestionContentTooLong = "00021120";
    public const string CommunityQuestionInvalidStatus = "00021121";
    public const string CommunityQuestionInvalidStatusTransition = "00021122";
    public const string CommunityQuestionAlreadyInTargetStatus = "00021123";
    public const string CommunityQuestionStatusUpdateFailed = "00021124";
    public const string CommunityQuestionAlreadyHidden = "00021125";
    public const string CommunityQuestionNotHidden = "00021126";
    public const string CommunityQuestionAlreadyClosed = "00021127";
    public const string CommunityQuestionAlreadyOpen = "00021128";
    public const string CommunityQuestionResolvedRequiresAcceptedAnswer = "00021129";
    public const string CommunityQuestionAcceptedAnswerNotFound = "00021130";
    public const string CommunityQuestionAcceptedAnswerHidden = "00021131";
    public const string CommunityQuestionAnsweredRequiresAnswers = "00021132";
    public const string CommunityQuestionReasonRequired = "00021133";
    public const string CommunityQuestionUpdateFailed = "00021134";

    // Community answer management
    public const string CommunityAnswerNotFound = "00021201";
    public const string CommunityAnswerAlreadyHidden = "00021202";
    public const string CommunityAnswerNotHidden = "00021203";
    public const string CommunityAnswerAlreadyDeleted = "00021204";
    public const string CommunityAnswerNotDeleted = "00021205";
    public const string CommunityAnswerCannotBeModerated = "00021206";
    public const string CommunityAnswerDeleteFailed = "00021207";
    public const string CommunityAnswerRestoreFailed = "00021208";
    public const string CommunityAnswerHideFailed = "00021209";
    public const string CommunityAnswerShowFailed = "00021210";
    public const string CommunityAnswerQuestionNotFound = "00021211";
    public const string CommunityAnswerAuthorNotFound = "00021212";
    public const string CommunityAnswerCreateFailed = "00021213";
    public const string CommunityAnswerUpdateFailed = "00021214";
    public const string CommunityAnswerContentRequired = "00021215";

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
        PostAlreadyInTargetStatus => HttpStatusCodes.Conflict,
        PostStatusUpdateFailed => HttpStatusCodes.InternalServerError,

        // Interview question management
        InterviewQuestionNotFound => HttpStatusCodes.NotFound,
        InterviewQuestionAlreadyDeleted => HttpStatusCodes.Conflict,
        InterviewQuestionNotDeleted => HttpStatusCodes.Conflict,
        InterviewQuestionTitleRequired => HttpStatusCodes.BadRequest,
        InterviewQuestionContentRequired => HttpStatusCodes.BadRequest,
        InterviewQuestionAnswerRequired => HttpStatusCodes.BadRequest,
        InterviewQuestionInvalidDifficulty => HttpStatusCodes.BadRequest,
        InterviewQuestionInvalidStatus => HttpStatusCodes.BadRequest,
        InterviewQuestionInvalidStatusTransition => HttpStatusCodes.Conflict,
        InterviewQuestionAlreadyInTargetStatus => HttpStatusCodes.Conflict,
        InterviewQuestionCategoryNotFound => HttpStatusCodes.NotFound,
        InterviewQuestionTagNotFound => HttpStatusCodes.NotFound,
        InterviewQuestionInvalidAnswers => HttpStatusCodes.BadRequest,
        InterviewQuestionMultiplePreferredAnswers => HttpStatusCodes.BadRequest,
        InterviewQuestionCreateFailed => HttpStatusCodes.InternalServerError,
        InterviewQuestionUpdateFailed => HttpStatusCodes.InternalServerError,
        InterviewQuestionDeleteFailed => HttpStatusCodes.InternalServerError,
        InterviewQuestionRestoreFailed => HttpStatusCodes.InternalServerError,
        InterviewQuestionStatusUpdateFailed => HttpStatusCodes.InternalServerError,

        // Category management
        CategoryNameRequired => HttpStatusCodes.BadRequest,
        CategorySlugRequired => HttpStatusCodes.BadRequest,
        CategorySlugAlreadyExists => HttpStatusCodes.Conflict,
        CategoryInvalidStatus => HttpStatusCodes.BadRequest,
        CategoryAlreadyInTargetStatus => HttpStatusCodes.Conflict,
        CategoryStatusUpdateFailed => HttpStatusCodes.InternalServerError,
        CategoryCreateFailed => HttpStatusCodes.InternalServerError,
        CategoryUpdateFailed => HttpStatusCodes.InternalServerError,
        CategoryDeleteFailed => HttpStatusCodes.InternalServerError,
        CategoryHasChildren => HttpStatusCodes.Conflict,
        CategoryHasPosts => HttpStatusCodes.Conflict,

        // Tag management
        TagNameRequired => HttpStatusCodes.BadRequest,
        TagSlugRequired => HttpStatusCodes.BadRequest,
        TagSlugAlreadyExists => HttpStatusCodes.Conflict,
        TagInvalidStatus => HttpStatusCodes.BadRequest,
        TagAlreadyInTargetStatus => HttpStatusCodes.Conflict,
        TagStatusUpdateFailed => HttpStatusCodes.InternalServerError,
        TagCreateFailed => HttpStatusCodes.InternalServerError,
        TagUpdateFailed => HttpStatusCodes.InternalServerError,
        TagDeleteFailed => HttpStatusCodes.InternalServerError,
        TagMergeFailed => HttpStatusCodes.InternalServerError,
        TagNotMergeableWithSelf => HttpStatusCodes.BadRequest,
        TagSourceNotFound => HttpStatusCodes.NotFound,
        TagTargetNotFound => HttpStatusCodes.NotFound,
        TagHasNoAssociations => HttpStatusCodes.Conflict,

        // Image upload
        ImageFileEmpty => HttpStatusCodes.BadRequest,
        ImageFileTooLarge => HttpStatusCodes.BadRequest,
        ImageTooManyFiles => HttpStatusCodes.BadRequest,
        ImageInvalidExtension => HttpStatusCodes.BadRequest,
        ImageInvalidMimeType => HttpStatusCodes.BadRequest,
        ImageInvalidFormat => HttpStatusCodes.BadRequest,
        ImageInvalidDimensions => HttpStatusCodes.BadRequest,
        ImageUnreadable => HttpStatusCodes.BadRequest,
        ImagePublicIdRequired => HttpStatusCodes.BadRequest,
        ImageUploadFailed => HttpStatusCodes.InternalServerError,
        ImageDeleteFailed => HttpStatusCodes.InternalServerError,
        // Level management
        LevelNotFound => HttpStatusCodes.NotFound,
        LevelCodeRequired => HttpStatusCodes.BadRequest,
        LevelNameRequired => HttpStatusCodes.BadRequest,
        LevelCodeAlreadyExists => HttpStatusCodes.Conflict,
        LevelNameAlreadyExists => HttpStatusCodes.Conflict,
        LevelHasInterviewQuestions => HttpStatusCodes.Conflict,
        LevelCreateFailed => HttpStatusCodes.InternalServerError,
        LevelUpdateFailed => HttpStatusCodes.InternalServerError,
        LevelDeleteFailed => HttpStatusCodes.InternalServerError,

        // Community question management
        CommunityQuestionNotFound => HttpStatusCodes.NotFound,
        CommunityQuestionAlreadyDeletedOrNotFound => HttpStatusCodes.NotFound,
        CommunityQuestionCannotBeModerated => HttpStatusCodes.Conflict,
        CommunityQuestionAlreadyLocked => HttpStatusCodes.Conflict,
        CommunityQuestionNotLocked => HttpStatusCodes.Conflict,
        CommunityQuestionAlreadyDeleted => HttpStatusCodes.Conflict,
        CommunityQuestionNotDeleted => HttpStatusCodes.Conflict,
        CommunityQuestionDeleteFailed => HttpStatusCodes.InternalServerError,
        CommunityQuestionRestoreFailed => HttpStatusCodes.InternalServerError,
        CommunityQuestionLockFailed => HttpStatusCodes.InternalServerError,
        CommunityQuestionUnlockFailed => HttpStatusCodes.InternalServerError,
        CommunityQuestionCreatedFailed => HttpStatusCodes.InternalServerError,
        CommunityQuestionUpdatedFailed => HttpStatusCodes.InternalServerError,
        CommunityQuestionAuthorNotFound => HttpStatusCodes.NotFound,
        CommunityQuestionTitleRequired => HttpStatusCodes.BadRequest,
        CommunityQuestionContentRequired => HttpStatusCodes.BadRequest,
        CommunityQuestionTitleTooShort => HttpStatusCodes.BadRequest,
        CommunityQuestionTitleTooLong => HttpStatusCodes.BadRequest,
        CommunityQuestionContentTooShort => HttpStatusCodes.BadRequest,
        CommunityQuestionContentTooLong => HttpStatusCodes.BadRequest,
        CommunityQuestionInvalidStatus => HttpStatusCodes.BadRequest,
        CommunityQuestionInvalidStatusTransition => HttpStatusCodes.Conflict,
        CommunityQuestionAlreadyInTargetStatus => HttpStatusCodes.Conflict,
        CommunityQuestionStatusUpdateFailed => HttpStatusCodes.InternalServerError,
        CommunityQuestionAlreadyHidden => HttpStatusCodes.Conflict,
        CommunityQuestionNotHidden => HttpStatusCodes.Conflict,
        CommunityQuestionAlreadyClosed => HttpStatusCodes.Conflict,
        CommunityQuestionAlreadyOpen => HttpStatusCodes.Conflict,
        CommunityQuestionResolvedRequiresAcceptedAnswer => HttpStatusCodes.BadRequest,
        CommunityQuestionAcceptedAnswerNotFound => HttpStatusCodes.BadRequest,
        CommunityQuestionAcceptedAnswerHidden => HttpStatusCodes.BadRequest,
        CommunityQuestionAnsweredRequiresAnswers => HttpStatusCodes.BadRequest,
        CommunityQuestionReasonRequired => HttpStatusCodes.BadRequest,
        CommunityQuestionUpdateFailed => HttpStatusCodes.InternalServerError,

        // Community answer management
        CommunityAnswerNotFound => HttpStatusCodes.NotFound,
        CommunityAnswerCannotBeModerated => HttpStatusCodes.Conflict,
        CommunityAnswerAlreadyHidden => HttpStatusCodes.Conflict,
        CommunityAnswerNotHidden => HttpStatusCodes.Conflict,
        CommunityAnswerAlreadyDeleted => HttpStatusCodes.Conflict,
        CommunityAnswerNotDeleted => HttpStatusCodes.Conflict,
        CommunityAnswerHideFailed => HttpStatusCodes.InternalServerError,
        CommunityAnswerShowFailed => HttpStatusCodes.InternalServerError,
        CommunityAnswerDeleteFailed => HttpStatusCodes.InternalServerError,
        CommunityAnswerRestoreFailed => HttpStatusCodes.InternalServerError,
        CommunityAnswerQuestionNotFound => HttpStatusCodes.NotFound,
        CommunityAnswerAuthorNotFound => HttpStatusCodes.NotFound,
        CommunityAnswerCreateFailed => HttpStatusCodes.InternalServerError,
        CommunityAnswerUpdateFailed => HttpStatusCodes.InternalServerError,
        CommunityAnswerContentRequired => HttpStatusCodes.BadRequest,

        _ => HttpStatusCodes.InternalServerError
    };
}
