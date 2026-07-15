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
    public const string EmailAlreadyExists = "00010401";
    public const string UsernameAlreadyExists = "00010402";
    public const string PasswordTooWeak = "00010403";
    public const string TermsNotAccepted = "00010404";
    public const string DeviceBlocked = "00010405";
    public const string DefaultRoleNotFound = "00010406";

    public const string InvalidLogoutReason = "00020401";
    public const string SessionAlreadyRevoked = "00020402";
    public const string InvalidRefreshToken = "00020403";

    // User management
    public const string UserAlreadyExists     = "00030401";
    public const string RoleNotFound           = "00030402";
    public const string CannotCreateAdminUser  = "00030403";
    public const string UserCreateFailed       = "00030404";

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
        Success => HttpStatusCodes.Ok,
        ValidationError => HttpStatusCodes.BadRequest,
        NotFound => HttpStatusCodes.NotFound,
        Conflict => HttpStatusCodes.Conflict,
        Unauthorized => HttpStatusCodes.Unauthorized,
        Forbidden => HttpStatusCodes.Forbidden,
        TooManyRequests => HttpStatusCodes.TooManyRequests,
        SessionRevoked => HttpStatusCodes.Unauthorized,
        InternalError => HttpStatusCodes.InternalServerError,
        InvalidLogoutReason => HttpStatusCodes.BadRequest,
        SessionAlreadyRevoked => HttpStatusCodes.Ok,
        InvalidRefreshToken => HttpStatusCodes.BadRequest,
        EmailAlreadyExists => HttpStatusCodes.Conflict,
        UsernameAlreadyExists => HttpStatusCodes.Conflict,
        ResetPasswordWeak => HttpStatusCodes.BadRequest,
        PasswordNotMatch => HttpStatusCodes.BadRequest,
        PasswordReused => HttpStatusCodes.BadRequest,
        UserUsernameAlreadyExists => HttpStatusCodes.Conflict,
        PasswordTooWeak => HttpStatusCodes.BadRequest,
        TermsNotAccepted => HttpStatusCodes.BadRequest,
        DeviceBlocked => HttpStatusCodes.Forbidden,
        DefaultRoleNotFound => HttpStatusCodes.InternalServerError,
        UserAlreadyExists => HttpStatusCodes.Conflict,
        RoleNotFound => HttpStatusCodes.NotFound,
        CannotCreateAdminUser => HttpStatusCodes.Forbidden,
        UserCreateFailed => HttpStatusCodes.InternalServerError,
        _ => HttpStatusCodes.InternalServerError
    };
}
