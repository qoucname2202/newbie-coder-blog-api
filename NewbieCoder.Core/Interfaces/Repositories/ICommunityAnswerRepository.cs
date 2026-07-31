using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository contract for community answer administration operations.
/// </summary>
public interface ICommunityAnswerRepository
{
    /// <summary>
    /// Returns a paginated list of community answers with search, filter, and sort support.
    /// </summary>
    Task<PaginatedResponse<CommunityAnswerListItemResponse>> GetPagedAsync(
        GetCommunityAnswersRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full answer details, or null if not found.
    /// </summary>
    Task<CommunityAnswerDetailResponse?> GetDetailByIdAsync(
        long id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked answer entity by ID (non-deleted only).
    /// </summary>
    Task<CommunityAnswer?> GetTrackedByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked answer entity by ID (deleted only).
    /// </summary>
    Task<CommunityAnswer?> GetTrackedDeletedByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked answer entity by ID (both deleted and non-deleted).
    /// </summary>
    Task<CommunityAnswer?> GetTrackedAnyByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an answer exists (deleted or not).
    /// </summary>
    Task<bool> ExistsAnyAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a question exists (non-deleted).
    /// </summary>
    Task<bool> QuestionExistsAsync(
        long questionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an author (user) exists (non-deleted).
    /// </summary>
    Task<bool> AuthorExistsAsync(
        long authorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new community answer to the database.
    /// </summary>
    Task<CommunityAnswer> CreateAsync(
        CommunityAnswer answer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
