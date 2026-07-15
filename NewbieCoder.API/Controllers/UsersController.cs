using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.User;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles user profile operations.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Tags("User Profile")]
[Authorize]
public sealed class UsersController(
    IUserProfileService userProfileService,
    IAuthService authService,
    IFileUploadService fileUploadService)
    : ControllerBase
{
    #region Get My Profile

    /// <summary>
    /// Returns the authenticated user's profile, personal statistics, and current session info.
    /// </summary>
    [HttpGet("me")]
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

    #region Update My Profile

    /// <summary>
    /// Updates the current authenticated user's profile.
    /// Only the fields provided in the request body will be updated.
    /// Fields not in the body retain their current values.
    /// </summary>
    /// <param name="request">Profile fields to update. All are optional.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UpdateProfileValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var userId = GetRequiredUserId();
        var sessionId = GetRequiredSessionId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var hasAnyField = request.FullName != null ||
                          request.Username != null ||
                          request.AvatarUrl != null ||
                          request.Bio != null ||
                          request.DisplayTitle != null ||
                          request.WebsiteUrl != null;

        if (!hasAnyField)
            throw new BusinessException(
                ResponseMessages.EmptyUpdateBody,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.EmptyUpdateBody);

        var result = await authService.UpdateProfileAsync(
            userId,
            sessionId,
            request,
            ipAddress,
            userAgent,
            cancellationToken);

        return Ok(ApiResponse<UpdateProfileResponse>.Success(
            result,
            trace,
            ResponseMessages.UpdateProfileSuccess));
    }

    #endregion

    #region Upload Avatar

    /// <summary>
    /// Uploads an avatar image for the current authenticated user.
    /// Supported formats: .jpg, .jpeg, .png, .gif, .webp
    /// Maximum file size: 5MB
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("me/avatar")]
    [ProducesResponseType(typeof(ApiResponse<FileUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            throw new BusinessException(
                "No file provided",
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: "NO_FILE_PROVIDED");

        var userId = GetRequiredUserId();

        await using var stream = file.OpenReadStream();
        var result = await fileUploadService.UploadAvatarAsync(
            userId,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return Ok(ApiResponse<FileUploadResponse>.Success(
            result,
            HttpContext.GetRequestTrace(),
            "Avatar uploaded successfully"));
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

    private string? GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
