using NewbieCoder.Core.CQRS.CommunityAnswers;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service contract for community answer administration operations.
/// </summary>
public interface ICommunityAnswerService
{
    /// <summary>
    /// Returns a paginated list of community answers with search, filter, and sort support.
    /// </summary>
    Task<PaginatedResponse<CommunityAnswerListItemResponse>> GetAnswersAsync(
        GetCommunityAnswersRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full details of a community answer.
    /// </summary>
    Task<CommunityAnswerDetailResponse> GetAnswerByIdAsync(
        long answerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides a community answer, making it invisible to public queries.
    /// </summary>
    Task<HideCommunityAnswerResponse> HideAnswerAsync(
        HideCommunityAnswerCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows (unhides) a previously hidden community answer.
    /// </summary>
    Task<ShowCommunityAnswerResponse> ShowAnswerAsync(
        ShowCommunityAnswerCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a community answer.
    /// </summary>
    Task<DeleteCommunityAnswerResponse> DeleteAnswerAsync(
        DeleteCommunityAnswerCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted community answer.
    /// </summary>
    Task<RestoreCommunityAnswerResponse> RestoreAnswerAsync(
        RestoreCommunityAnswerCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new community answer.
    /// </summary>
    Task<CreateCommunityAnswerResponse> CreateAnswerAsync(
        CreateCommunityAnswerCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing community answer.
    /// </summary>
    Task<UpdateCommunityAnswerResponse> UpdateAnswerAsync(
        UpdateCommunityAnswerCommand command,
        CancellationToken cancellationToken = default);
}
