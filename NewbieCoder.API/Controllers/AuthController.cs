using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Extensions;
using NewbieCoder.API.Middlewares;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Auth;
using NewbieCoder.Core.DTOs.Response.Auth;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles authentication operations: login, logout, token refresh, and current user profile.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public sealed class AuthController(
    IAuthService authService,
    AuthRateLimitService rateLimit,
    IHttpContextAccessor httpContextAccessor)
    : ControllerBase
{
    #region Login

    /// <summary>
    /// Authenticates a user and returns JWT access token + refresh token.
    /// Rate-limited per login_id (5 attempts/5 min) and per IP (10 attempts/5 min).
    /// </summary>
    /// <param name="request">login_id (email or username) + password + remember_me</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var ipAddress = GetClientIp();
        var loginId = request.LoginId?.Trim().ToLowerInvariant() ?? string.Empty;

        // Reject before touching the database if rate limit is exceeded.
        if (rateLimit.IsBlocked(loginId, ipAddress))
        {
            return StatusCode(
                HttpStatusCodes.TooManyRequests,
                BuildTooManyAttemptsResponse(trace));
        }

        // Attempt login.
        var result = await authService.LoginAsync(
            request,
            Request.Headers["X-Device-Id"].FirstOrDefault(),
            Request.Headers["X-Device-Name"].FirstOrDefault(),
            Request.Headers["X-Device-Type"].FirstOrDefault(),
            Request.Headers.UserAgent.FirstOrDefault(),
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    #endregion

    #region Logout

    /// <summary>
    /// Revokes the current session and all its refresh tokens on this device.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var userId = GetRequiredUserId();
        var sessionId = GetRequiredSessionId();

        await authService.LogoutAsync(
            userId,
            sessionId,
            GetClientIp(),
            Request.Headers.UserAgent.FirstOrDefault(),
            cancellationToken);

        return Ok(ApiResponse<string>.Success(
            "OK",
            trace,
            ResponseMessages.LogoutSuccess));
    }

    #endregion

    #region Logout All Devices

    /// <summary>
    /// Revokes ALL sessions and refresh tokens for the current user across all devices.
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var userId = GetRequiredUserId();

        await authService.LogoutAllAsync(
            userId,
            GetClientIp(),
            Request.Headers.UserAgent.FirstOrDefault(),
            cancellationToken);

        return Ok(ApiResponse<string>.Success(
            "OK",
            trace,
            ResponseMessages.LogoutSuccess));
    }

    #endregion

    #region Get Current User

    /// <summary>
    /// Returns the authenticated user's profile with their current roles and permissions.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var userId = GetRequiredUserId();

        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);

        return Ok(ApiResponse<UserInfoResponse>.Success(result, trace));
    }

    #endregion

    #region Private helpers

    /// <summary>
    /// Extracts the real client IP from X-Forwarded-For header (if behind a proxy) or RemoteIpAddress.
    /// </summary>
    private string? GetClientIp()
    {
        var forwarded = Request.Headers[XForwardedFor].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private long GetRequiredUserId()
    {
        var userId = User.GetUserId();
        if (userId == null)
            throw new BusinessException(
                ResponseMessages.Unauthenticated,
                statusCode: HttpStatusCodes.Unauthorized,
                responseCode: ResponseCodes.Unauthorized);

        return userId.Value;
    }

    private long GetRequiredSessionId()
    {
        var sessionId = User.GetSessionId();
        if (sessionId == null)
            throw new BusinessException(
                ResponseMessages.Unauthenticated,
                statusCode: HttpStatusCodes.Unauthorized,
                responseCode: ResponseCodes.Unauthorized);

        return sessionId.Value;
    }

    private static AuthErrorResponse BuildTooManyAttemptsResponse(string trace)
    {
        return new AuthErrorResponse
        {
            RequestTrace = trace,
            ResponseDateTime = DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ResponseData = string.Empty,
            ResponseStatus = new AuthErrorStatus
            {
                ResponseCode = ResponseCodes.TooManyRequests,
                ResponseMessage = ResponseMessages.TooManyLoginAttempts,
                TracingMessage = null
            }
        };
    }

    private const string XForwardedFor = "X-Forwarded-For";

    #endregion
}
