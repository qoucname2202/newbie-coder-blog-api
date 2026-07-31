using NewbieCoder.Core.CQRS.CommunityQuestions;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service contract for community question administration operations.
/// </summary>
public interface ICommunityQuestionService
{
    /// <summary>
    /// Returns a paginated list of community questions with search, filter, and sort support.
    /// </summary>
    Task<PaginatedResponse<CommunityQuestionListItemResponse>> GetQuestionsAsync(
        GetCommunityQuestionsRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full details of a community question including answers and tags.
    /// </summary>
    Task<CommunityQuestionDetailResponse> GetQuestionByIdAsync(
        long questionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a community question, preventing new answers from being submitted.
    /// </summary>
    Task<LockCommunityQuestionResponse> LockQuestionAsync(
        LockCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a previously locked community question.
    /// </summary>
    Task<UnlockCommunityQuestionResponse> UnlockQuestionAsync(
        UnlockCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a community question.
    /// </summary>
    Task<DeleteCommunityQuestionResponse> DeleteQuestionAsync(
        DeleteCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted community question.
    /// </summary>
    Task<RestoreCommunityQuestionResponse> RestoreQuestionAsync(
        RestoreCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new community question.
    /// </summary>
    Task<CreateCommunityQuestionResponse> CreateQuestionAsync(
        CreateCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing community question.
    /// </summary>
    Task<UpdateCommunityQuestionResponse> UpdateQuestionAsync(
        UpdateCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of a community question with validation of business rules.
    /// </summary>
    Task<ChangeCommunityQuestionStatusResponse> ChangeStatusAsync(
        ChangeCommunityQuestionStatusCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides a community question, making it invisible to public queries.
    /// </summary>
    Task<HideCommunityQuestionResponse> HideQuestionAsync(
        HideCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a community question, setting ClosedAt and preventing new answers.
    /// </summary>
    Task<CloseCommunityQuestionResponse> CloseQuestionAsync(
        CloseCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reopens a closed or hidden community question.
    /// </summary>
    Task<ReopenCommunityQuestionResponse> ReopenQuestionAsync(
        ReopenCommunityQuestionCommand command,
        CancellationToken cancellationToken = default);
}
