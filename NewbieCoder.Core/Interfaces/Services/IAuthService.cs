using NewbieCoder.Core.DTOs.Request.Auth;
using NewbieCoder.Core.DTOs.Response.Auth;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IAuthService
{
    // Auth flows

    Task<AuthTokenResponse> LoginAsync(
        LoginRequest request,
        string? deviceId,
        string? deviceName,
        string? deviceType,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        long userId,
        long sessionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task LogoutAllAsync(
        long userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<UserInfoResponse> GetCurrentUserAsync(
        long userId,
        CancellationToken cancellationToken = default);

    // JWT

    string GenerateAccessToken(
        long userId,
        string email,
        string username,
        IReadOnlyList<string> roles,
        long sessionId,
        long deviceId);

    long? ValidateAndGetUserId(string token);

    string GenerateRefreshToken(
        long userId,
        string email,
        IReadOnlyList<string> roles,
        long sessionId,
        long deviceId);
}
