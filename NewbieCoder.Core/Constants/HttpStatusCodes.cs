namespace NewbieCoder.Core.Constants;

/// <summary>
/// Standard HTTP status codes used by this API.
/// Always pair with the matching <see cref="ResponseCodes"/> business code.
/// </summary>
public static class HttpStatusCodes
{
    public const int Ok = 200;
    public const int Created = 201;
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int TooManyRequests = 429;
    public const int InternalServerError = 500;
}
