using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service interface for admin interview question management operations.
/// </summary>
public interface IInterviewQuestionService
{
    /// <summary>
    /// Returns a paginated list of interview questions with optional search, filter, and sort.
    /// </summary>
    Task<PaginatedResponse<InterviewQuestionListItemResponse>> GetQuestionsAsync(
        GetInterviewQuestionsRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full question details by ID, including answers and tags.
    /// </summary>
    Task<InterviewQuestionDetailResponse> GetQuestionByIdAsync(
        long questionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new interview question with answers and tags.
    /// </summary>
    Task<CreateInterviewQuestionResponse> CreateQuestionAsync(
        CreateInterviewQuestionRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing interview question, including answer and tag synchronization.
    /// </summary>
    Task<UpdateInterviewQuestionResponse> UpdateQuestionAsync(
        long questionId,
        UpdateInterviewQuestionRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an interview question.
    /// </summary>
    Task<DeleteInterviewQuestionResponse> DeleteQuestionAsync(
        long questionId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted interview question back to Draft status.
    /// </summary>
    Task<RestoreInterviewQuestionResponse> RestoreQuestionAsync(
        long questionId,
        long restoredByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes an interview question's status with validation of the transition.
    /// </summary>
    Task<ChangeInterviewQuestionStatusResponse> ChangeQuestionStatusAsync(
        long questionId,
        ChangeInterviewQuestionStatusRequest request,
        long changedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
