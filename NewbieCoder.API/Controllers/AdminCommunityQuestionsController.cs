using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Attributes;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.CQRS.CommunityQuestions;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles administrative community question management operations:
/// list, detail, create, update, lock, unlock, change status, hide, close, reopen, soft delete, and restore.
/// </summary>
[ApiController]
[Route("api/v1/admin/community-questions")]
[Produces("application/json")]
[Tags("Admin — Community Questions")]
[RequiresRole(RoleConstants.Admin, RoleConstants.Moderator)]
public sealed class AdminCommunityQuestionsController : ControllerBase
{
    private readonly ICommunityQuestionService _service;

    public AdminCommunityQuestionsController(ICommunityQuestionService service)
    {
        _service = service;
    }

    #region List

    /// <summary>
    /// Returns a paginated list of all community questions with optional search, filter, and sort.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<CommunityQuestionListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] GetCommunityQuestionsRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _service.GetQuestionsAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<CommunityQuestionListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Detail

    /// <summary>
    /// Returns full details of a specific community question including answers and tags.
    /// Admin and Moderator can view active, hidden, closed, and soft-deleted questions.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CommunityQuestionDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetQuestion(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _service.GetQuestionByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<CommunityQuestionDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityQuestionRetrievedSuccess));
    }

    #endregion

    #region Create

    /// <summary>
    /// Creates a new community question.
    /// </summary>
    /// <param name="request">The question creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateCommunityQuestionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateQuestion(
        [FromBody] CreateCommunityQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.CreateQuestionAsync(
            new CreateCommunityQuestionCommand
            {
                AuthorId = request.AuthorId,
                Title = request.Title,
                Content = request.Content,
                TagIds = request.TagIds,
                CreatedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(GetQuestion),
            new { id = result.Id },
            ApiResponse<CreateCommunityQuestionResponse>.Success(
                result,
                trace,
                ResponseMessages.CommunityQuestionCreatedSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing community question. Regenerates slug if title changes.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="request">The question update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateQuestion(
        [FromRoute] long id,
        [FromBody] UpdateCommunityQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.UpdateQuestionAsync(
            new UpdateCommunityQuestionCommand
            {
                QuestionId = id,
                Title = request.Title,
                Content = request.Content,
                TagIds = request.TagIds,
                Reason = request.Reason,
                UpdatedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<UpdateCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityQuestionUpdatedSuccess));
    }

    #endregion

    #region Lock / Unlock

    /// <summary>
    /// Locks a community question, preventing new answers from being submitted.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/lock")]
    [ProducesResponseType(typeof(ApiResponse<LockCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockQuestion(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.LockQuestionAsync(
            new LockCommunityQuestionCommand
            {
                QuestionId = id,
                LockedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<LockCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityQuestionLockedSuccess));
    }

    /// <summary>
    /// Unlocks a previously locked community question.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<UnlockCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockQuestion(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.UnlockQuestionAsync(
            new UnlockCommunityQuestionCommand
            {
                QuestionId = id,
                UnlockedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<UnlockCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityQuestionUnlockedSuccess));
    }

    #endregion

    #region Change Status

    /// <summary>
    /// Changes the status of a community question. Enforces business rules for each transition.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="request">The status change payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<ChangeCommunityQuestionStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] long id,
        [FromBody] ChangeCommunityQuestionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.ChangeStatusAsync(
            new ChangeCommunityQuestionStatusCommand
            {
                QuestionId = id,
                Status = request.Status,
                Reason = request.Reason,
                ModeratedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<ChangeCommunityQuestionStatusResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Hide

    /// <summary>
    /// Hides a community question, making it invisible to public queries but still accessible in Admin APIs.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="request">The hide reason payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/hide")]
    [ProducesResponseType(typeof(ApiResponse<HideCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HideQuestion(
        [FromRoute] long id,
        [FromBody] HideCommunityQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.HideQuestionAsync(
            new HideCommunityQuestionCommand
            {
                QuestionId = id,
                Reason = request.Reason,
                ModeratedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<HideCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Close

    /// <summary>
    /// Closes a community question, setting ClosedAt and preventing new answers.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="request">The close reason payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/close")]
    [ProducesResponseType(typeof(ApiResponse<CloseCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CloseQuestion(
        [FromRoute] long id,
        [FromBody] CloseCommunityQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.CloseQuestionAsync(
            new CloseCommunityQuestionCommand
            {
                QuestionId = id,
                Reason = request.Reason,
                ModeratedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<CloseCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Reopen

    /// <summary>
    /// Reopens a previously closed or hidden community question.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="request">The reopen reason payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/reopen")]
    [ProducesResponseType(typeof(ApiResponse<ReopenCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReopenQuestion(
        [FromRoute] long id,
        [FromBody] ReopenCommunityQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.ReopenQuestionAsync(
            new ReopenCommunityQuestionCommand
            {
                QuestionId = id,
                Reason = request.Reason,
                ModeratedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<ReopenCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes a community question. The question and its associations remain in the database
    /// for audit and restore purposes.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="request">The deletion reason payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteQuestion(
        [FromRoute] long id,
        [FromBody] DeleteCommunityQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.DeleteQuestionAsync(
            new DeleteCommunityQuestionCommand
            {
                QuestionId = id,
                Reason = request.Reason,
                DeletedByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<DeleteCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityQuestionDeletedSuccess));
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores a soft-deleted community question.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <param name="request">The restore reason payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:long}/restore")]
    [ProducesResponseType(typeof(ApiResponse<RestoreCommunityQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestoreQuestion(
        [FromRoute] long id,
        [FromBody] RestoreCommunityQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _service.RestoreQuestionAsync(
            new RestoreCommunityQuestionCommand
            {
                QuestionId = id,
                Reason = request.Reason,
                RestoredByUserId = requesterId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                TraceId = trace
            },
            cancellationToken);

        return Ok(ApiResponse<RestoreCommunityQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.CommunityQuestionRestoredSuccess));
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
