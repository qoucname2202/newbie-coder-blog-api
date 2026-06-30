namespace NewbieCoder.Core.Constants;

/// <summary>
/// JWT claim type names used when building and reading access/refresh tokens.
/// </summary>
public static class JwtClaimTypes
{
    public const string Sub        = "sub";
    public const string Email      = "email";
    public const string Username   = "username";
    public const string SessionId  = "session_id";
    public const string DeviceId   = "device_id";
    public const string Jti        = "jti";
}
