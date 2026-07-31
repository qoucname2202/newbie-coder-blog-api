using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository for InterviewQuestion data access operations used by the admin module.
/// </summary>
public interface IInterviewQuestionRepository
{
    /// <summary>
    /// Returns a paginated list of interview questions matching the filter criteria.
    /// </summary>
    Task<PaginatedResponse<InterviewQuestionListItemResponse>> GetPagedAsync(
        GetInterviewQuestionsRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full question details (no tracking) by ID, including answers and tags.
    /// Returns null if not found.
    /// </summary>
    Task<InterviewQuestionDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked InterviewQuestion entity by ID (for updates). Returns null if not found or deleted.
    /// </summary>
    Task<InterviewQuestion?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked InterviewQuestion entity by ID if it is soft-deleted. Returns null otherwise.
    /// </summary>
    Task<InterviewQuestion?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an active (non-deleted, active status) category by ID, or null.
    /// </summary>
    Task<PostCategory?> GetActiveCategoryAsync(long categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active (non-deleted, active status) tags by IDs. Returns only those that match.
    /// </summary>
    Task<IReadOnlyList<Tag>> GetActiveTagsAsync(
        IReadOnlyList<long> tagIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an active InterviewAnswer by ID, or null.
    /// </summary>
    Task<InterviewAnswer?> GetActiveAnswerAsync(long answerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a question with the given ID exists (regardless of deleted state).
    /// </summary>
    Task<bool> ExistsQuestionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new interview question entity.
    /// </summary>
    Task AddQuestionAsync(InterviewQuestion question, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new interview answer entity.
    /// </summary>
    Task AddAnswerAsync(InterviewAnswer answer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the InterviewQuestionTag associations for a question. Removes existing and adds new ones.
    /// </summary>
    Task ReplaceTagsAsync(
        InterviewQuestion question,
        IReadOnlyList<Tag> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an answer (sets DeletedAt and DeletedBy).
    /// </summary>
    Task SoftDeleteAnswerAsync(InterviewAnswer answer, long deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
