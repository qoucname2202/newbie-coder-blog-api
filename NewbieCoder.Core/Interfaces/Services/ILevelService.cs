using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service interface for admin level management operations.
/// </summary>
public interface ILevelService
{
    /// <summary>
    /// Creates a new level.
    /// </summary>
    Task<LevelResponse> CreateLevelAsync(
        CreateLevelRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of levels.
    /// </summary>
    Task<PaginatedResponse<LevelListItemResponse>> GetLevelsAsync(
        LevelFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full level details by ID.
    /// </summary>
    Task<LevelDetailResponse> GetLevelByIdAsync(
        long levelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing level.
    /// </summary>
    Task<LevelResponse> UpdateLevelAsync(
        long levelId,
        UpdateLevelRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a level. Fails if the level has associated interview questions.
    /// </summary>
    Task DeleteLevelAsync(
        long levelId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted level.
    /// </summary>
    Task<LevelResponse> RestoreLevelAsync(
        long levelId,
        long restoredByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
