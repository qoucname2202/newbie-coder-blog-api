using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Extensions;
using NewbieCoder.API.Middlewares;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles user profile operations.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Tags("User Profile")]
public sealed class UsersController(
    IUserProfileService userProfileService)
    : ControllerBase
{
    #region Get My Profile

    /// <summary>
    /// Returns the authenticated user's profile, personal statistics, and current session info.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserMeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var userId = GetRequiredUserId();
        var sessionId = GetRequiredSessionId();

        var result = await userProfileService.GetMyProfileAsync(
            userId, sessionId, cancellationToken);

        return Ok(ApiResponse<UserMeResponse>.Success(result, trace, ResponseMessages.ProfileSuccess));
    }

    #endregion

    #region Private helpers

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

    #endregion
}
