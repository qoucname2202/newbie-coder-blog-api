using System.Security.Cryptography;
using System.Text;
using NewbieCoder.Core.Exceptions;

namespace NewbieCoder.Core.Constants;

public static class AuthConstants
{
    // JWT
    public const string TokenTypeBearer = "Bearer";

    public const int AccessTokenExpirationSeconds = 900;
    public const int RefreshTokenExpirationSeconds = 604800;
    public const int RefreshTokenExpirationDaysDefault = 7;
    public const int RefreshTokenExpirationDaysRememberMe = 30;
    public const int SessionExpirationDays = 7;

    // JWT claims
    public const string ClaimSub       = "sub";
    public const string ClaimEmail     = "email";
    public const string ClaimUsername  = "username";
    public const string ClaimSessionId = "session_id";
    public const string ClaimDeviceId  = "device_id";
    public const string ClaimJti       = "jti";

    // Password hashing (BCrypt)
    public const int BcryptWorkFactor = 11;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 64;

    // Login / device
    public const int LoginIdMinLength = 3;
    public const int LoginIdMaxLength = 255;

    public static readonly string[] AllowedDeviceTypes = ["web", "mobile", "tablet", "desktop"];

    public const string HeaderDeviceId     = "X-Device-Id";
    public const string HeaderDeviceName   = "X-Device-Name";
    public const string HeaderDeviceType   = "X-Device-Type";
    public const string HeaderForwardedFor = "X-Forwarded-For";

    // Entity statuses — moved to enums: DeviceStatus, SessionStatus, TokenStatus, RoleStatus, UserRoleStatus, EntityStatus
    public static class UserAccountStatus
    {
        public const string Active   = "ACT";
        public const string Inactive = "INACT";
        public const string Banned   = "BAN";
        public const string Closed   = "CLS";
    }

    // Login history — moved to LoginStatus enum; failure reasons remain here
    public static class LoginHistoryConstants
    {
        public const string ReasonUserNotFound   = "USER_NOT_FOUND";
        public const string ReasonWrongPassword = "WRONG_PASSWORD";
        public const string ReasonBlockedUser   = "BLOCKED_USER";
        public const string ReasonBlockedDevice = "BLOCKED_DEVICE";
        public const string ReasonPendingUser   = "PENDING_USER";
    }

    // Helpers — token generation, hashing, User-Agent parsing, typed error throwers
    public static class Helpers
    {
        /// <summary>Generates a cryptographically random token using base64url encoding (no padding).</summary>
        public static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        /// <summary>Creates a SHA-256 hash of a token for safe storage in the database.</summary>
        public static string HashToken(string token) =>
            Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        /// <summary>Throws 401 INVALID_CREDENTIALS.</summary>
        public static void ThrowInvalidCredentials() =>
            throw new BusinessException(
                ResponseMessages.InvalidCredentials,
                statusCode: HttpStatusCodes.Unauthorized,
                responseCode: ResponseCodes.Unauthorized);

        /// <summary>Throws 403 USER_BLOCKED.</summary>
        public static void ThrowUserBlocked() =>
            throw new BusinessException(
                ResponseMessages.UserBlocked,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.Forbidden);

        /// <summary>Throws 403 USER_PENDING.</summary>
        public static void ThrowUserPending() =>
            throw new BusinessException(
                ResponseMessages.UserPending,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.Forbidden);

        /// <summary>Throws 403 DEVICE_BLOCKED.</summary>
        public static void ThrowDeviceBlocked() =>
            throw new BusinessException(
                ResponseMessages.DeviceBlocked,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.Forbidden);

        /// <summary>Parses the OS name from a User-Agent string using simple pattern matching.</summary>
        public static string? ParseOs(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return null;
            if (userAgent.Contains("Windows")) return "Windows";
            if (userAgent.Contains("Mac OS")) return "macOS";
            if (userAgent.Contains("Linux")) return "Linux";
            if (userAgent.Contains("Android")) return "Android";
            if (userAgent.Contains("iOS") || userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
                return "iOS";
            return null;
        }

        /// <summary>Parses the browser name from a User-Agent string using simple pattern matching.</summary>
        public static string? ParseBrowser(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return null;
            if (userAgent.Contains("Edg/")) return "Edge";
            if (userAgent.Contains("Chrome/") && !userAgent.Contains("Chromium/")) return "Chrome";
            if (userAgent.Contains("Firefox/")) return "Firefox";
            if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/")) return "Safari";
            if (userAgent.Contains("Chromium/")) return "Chromium";
            return null;
        }
    }
}
