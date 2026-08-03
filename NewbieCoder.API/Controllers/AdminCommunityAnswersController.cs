using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Attributes;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.CQRS.CommunityAnswers;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles administrative community answer management operations:
/// list, detail, hide, show, soft delete, and restore.
/// </summary>
[ApiController]
[Route("api/v1/admin/community-answers")]
[Produces("application/json")]
[Tags("Admin — Community Answers")]
[RequiresRole(RoleConstants.Admin, RoleConstants.Moderator)]
public sealed class AdminCommunityAnswersController : ControllerBase
{
    private readonly ICommunityAnswerService _service;

    public AdminCommunityAnswersController(ICommunityAnswerService service)
    {
        _service = service;
    }

    #region List

    /// <summary>
    /// Returns a paginated list of all community answers with optional search, filter, and sort.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<CommunityAnswerListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAnswers(
        [FromQuery] GetCommunityAnswersRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _service.GetAnswersAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<CommunityAnswerListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Detail

    /// <summary>
    /// Returns full details of a specific community answer.
    /// </summary>
    /// <param name="id">The answer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CommunityAnswerDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAnswer(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _service.GetAnswerByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<CommunityAnswerDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Create

    /// <summary>
    /// Creates a new community answer.
    /// </summary>
    /// <param name="request">The answer creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateCommunityAnswerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAnswer(
        [FromBody] CreateCommunityAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.CreateAnswerAsync(
            new CreateCommunityAnswerCommand
            {
                QuestionId = request.QuestionId,
                AuthorId = request.AuthorId,
                Content = request.Content,
                CreatedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAnswer),
            new { id = result.Id },
            ApiResponse<CreateCommunityAnswerResponse>.Success(
                result,
                trace,
                ResponseMessages.CommunityAnswerCreatedSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing community answer.
    /// </summary>
    /// <param name="id">The answer ID.</param>
    /// <param name="request">The answer update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateCommunityAnswerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAnswer(
        [FromRoute] long id,
        [FromBody] UpdateCommunityAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.UpdateAnswerAsync(
            new UpdateCommunityAnswerCommand
            {
                AnswerId = id,
                Content = request.Content,
                UpdatedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<UpdateCommunityAnswerResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityAnswerUpdatedSuccess));
    }

    #endregion

    #region Hide

    /// <summary>
    /// Hides a community answer, making it invisible to public queries.
    /// </summary>
    /// <param name="id">The answer ID.</param>
    /// <param name="request">Optional hide reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/hide")]
    [ProducesResponseType(typeof(ApiResponse<HideCommunityAnswerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HideAnswer(
        [FromRoute] long id,
        [FromBody] HideCommunityAnswerRequest? request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.HideAnswerAsync(
            new HideCommunityAnswerCommand
            {
                AnswerId = id,
                ModeratedByUserId = requesterId,
                Reason = request?.Reason,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<HideCommunityAnswerResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityAnswerHiddenSuccess));
    }

    #endregion

    #region Show

    /// <summary>
    /// Shows (unhides) a previously hidden community answer.
    /// </summary>
    /// <param name="id">The answer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/show")]
    [ProducesResponseType(typeof(ApiResponse<ShowCommunityAnswerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ShowAnswer(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.ShowAnswerAsync(
            new ShowCommunityAnswerCommand
            {
                AnswerId = id,
                ModeratedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<ShowCommunityAnswerResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityAnswerShownSuccess));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes a community answer. The answer remains in the database for audit purposes.
    /// </summary>
    /// <param name="id">The answer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteCommunityAnswerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAnswer(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.DeleteAnswerAsync(
            new DeleteCommunityAnswerCommand
            {
                AnswerId = id,
                DeletedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<DeleteCommunityAnswerResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityAnswerDeletedSuccess));
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores a soft-deleted community answer.
    /// </summary>
    /// <param name="id">The answer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/restore")]
    [ProducesResponseType(typeof(ApiResponse<RestoreCommunityAnswerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestoreAnswer(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.RestoreAnswerAsync(
            new RestoreCommunityAnswerCommand
            {
                AnswerId = id,
                RestoredByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<RestoreCommunityAnswerResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityAnswerRestoredSuccess));
    }

    #endregion

    #region Private Helpers

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

    private string? GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
