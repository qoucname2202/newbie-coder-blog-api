using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Attributes;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles administrative interview question management operations:
/// create, list, detail, update, delete, and restore.
/// </summary>
[ApiController]
[Route("api/v1/admin/interview-questions")]
[Produces("application/json")]
[Tags("Admin — Interview Questions")]
[RequiresRole(RoleConstants.Admin)]
public sealed class InterviewQuestionsController : ControllerBase
{
    private readonly IInterviewQuestionService _questionService;

    public InterviewQuestionsController(IInterviewQuestionService questionService)
    {
        _questionService = questionService;
    }

    #region List

    /// <summary>
    /// Returns a paginated list of interview questions with optional search, filter, and sort.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<InterviewQuestionListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] GetInterviewQuestionsRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _questionService.GetQuestionsAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<InterviewQuestionListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.InterviewQuestionsRetrievedSuccess));
    }

    #endregion

    #region Detail

    /// <summary>
    /// Returns full details of a specific interview question, including answers and tags.
    /// </summary>
    /// <param name="questionId">The ID of the interview question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{questionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<InterviewQuestionDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestion(
        [FromRoute] long questionId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _questionService.GetQuestionByIdAsync(questionId, cancellationToken);

        return Ok(ApiResponse<InterviewQuestionDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.InterviewQuestionRetrievedSuccess));
    }

    #endregion

    #region Create

    /// <summary>
    /// Creates a new interview question with optional answers and tags.
    /// </summary>
    /// <param name="request">Question creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created question data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateInterviewQuestionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateQuestion(
        [FromBody] CreateInterviewQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _questionService.CreateQuestionAsync(
            request,
            createdByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return StatusCode(
            HttpStatusCodes.Created,
            ApiResponse<CreateInterviewQuestionResponse>.Success(
                result,
                trace,
                ResponseMessages.InterviewQuestionCreatedSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing interview question, including answers and tags.
    /// </summary>
    /// <param name="questionId">The ID of the interview question to update.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{questionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateInterviewQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateQuestion(
        [FromRoute] long questionId,
        [FromBody] UpdateInterviewQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _questionService.UpdateQuestionAsync(
            questionId,
            request,
            updatedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<UpdateInterviewQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.InterviewQuestionUpdatedSuccess));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes an interview question.
    /// </summary>
    /// <param name="questionId">The ID of the interview question to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{questionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteInterviewQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteQuestion(
        [FromRoute] long questionId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _questionService.DeleteQuestionAsync(
            questionId,
            deletedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<DeleteInterviewQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.InterviewQuestionDeletedSuccess));
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores a soft-deleted interview question back to Draft status.
    /// </summary>
    /// <param name="questionId">The ID of the interview question to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{questionId:long}/restore")]
    [ProducesResponseType(typeof(ApiResponse<RestoreInterviewQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreQuestion(
        [FromRoute] long questionId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _questionService.RestoreQuestionAsync(
            questionId,
            restoredByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<RestoreInterviewQuestionResponse>.Success(
            result,
            trace,
            ResponseMessages.InterviewQuestionRestoredSuccess));
    }

    #endregion

    #region Change Status

    /// <summary>
    /// Changes an interview question's status (Draft, Active, Inactive, Archived).
    /// </summary>
    /// <param name="questionId">The ID of the interview question.</param>
    /// <param name="request">The target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{questionId:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<ChangeInterviewQuestionStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeQuestionStatus(
        [FromRoute] long questionId,
        [FromBody] ChangeInterviewQuestionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _questionService.ChangeQuestionStatusAsync(
            questionId,
            request,
            changedByUserId: requesterId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            traceId: trace,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<ChangeInterviewQuestionStatusResponse>.Success(
            result,
            trace,
            ResponseMessages.Success));
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
