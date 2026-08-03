using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository contract for community question administration operations.
/// </summary>
public interface ICommunityQuestionRepository
{
    /// <summary>
    /// Returns a paginated list of community questions with search, filter, and sort support.
    /// </summary>
    Task<PaginatedResponse<CommunityQuestionListItemResponse>> GetPagedAsync(
        GetCommunityQuestionsRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full question details including answers and tags, or null if not found.
    /// </summary>
    Task<CommunityQuestionDetailResponse?> GetDetailByIdAsync(
        long id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked question entity by ID (non-deleted only).
    /// </summary>
    Task<CommunityQuestion?> GetTrackedByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked question entity by ID (deleted only).
    /// </summary>
    Task<CommunityQuestion?> GetTrackedDeletedByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked question entity by ID (both deleted and non-deleted).
    /// </summary>
    Task<CommunityQuestion?> GetTrackedAnyByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a question has at least one non-hidden, non-deleted answer.
    /// </summary>
    Task<bool> HasActiveAnswerAsync(
        long questionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a question exists (deleted or not).
    /// </summary>
    Task<bool> ExistsAnyAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an author (user) exists (non-deleted).
    /// </summary>
    Task<bool> AuthorExistsAsync(
        long authorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new community question and its tag associations in a single transaction.
    /// </summary>
    Task<CommunityQuestion> CreateAsync(
        CommunityQuestion question,
        IReadOnlyList<long> tagIds,
        CancellationToken cancellationToken = default);
}
