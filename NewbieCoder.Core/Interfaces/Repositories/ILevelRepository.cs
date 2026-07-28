using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Level data access operations used by the admin module.
/// </summary>
public interface ILevelRepository
{
    /// <summary>
    /// Returns a paginated list of levels matching the filter criteria.
    /// </summary>
    Task<PaginatedResponse<LevelListItemResponse>> GetPagedAsync(
        LevelFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full level details (no tracking) by ID.
    /// Returns null if not found or soft-deleted.
    /// </summary>
    Task<LevelDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked Level entity by ID (for updates).
    /// Returns null if not found or soft-deleted.
    /// </summary>
    Task<Level?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked Level entity by ID if it is soft-deleted.
    /// Returns null if not found or not deleted.
    /// </summary>
    Task<Level?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a level with the given code exists (case-insensitive), excluding soft-deleted ones.
    /// </summary>
    Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a level with the given code exists, excluding the specified level ID.
    /// </summary>
    Task<bool> ExistsCodeExcludingIdAsync(
        long id,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a level with the given name exists (case-insensitive), excluding soft-deleted ones.
    /// </summary>
    Task<bool> ExistsNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a level with the given name exists, excluding the specified level ID.
    /// </summary>
    Task<bool> ExistsNameExcludingIdAsync(
        long id,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any interview questions are currently associated with the given level.
    /// </summary>
    Task<bool> HasInterviewQuestionsAsync(long levelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new level entity.
    /// </summary>
    Task AddAsync(Level level, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
