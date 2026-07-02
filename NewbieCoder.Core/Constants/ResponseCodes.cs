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

    /// <summary>
    /// Maps HTTP status to the default business response code when none is specified.
    /// </summary>
    public static string FromHttpStatus(int statusCode) => statusCode switch
    {
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
        _ => HttpStatusCodes.InternalServerError
    };
}
