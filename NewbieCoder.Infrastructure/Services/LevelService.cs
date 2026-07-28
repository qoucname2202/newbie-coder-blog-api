using System.Text.Json;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Service implementation for admin level management operations.
/// </summary>
public sealed class LevelService : ILevelService
{
    private readonly ILevelRepository _levelRepo;
    private readonly IAuditLogService _auditLog;

    public LevelService(ILevelRepository levelRepo, IAuditLogService auditLog)
    {
        _levelRepo = levelRepo;
        _auditLog = auditLog;
    }

    #region CreateLevelAsync

    public async Task<LevelResponse> CreateLevelAsync(
        CreateLevelRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var trimmedCode = request.Code!.Trim().ToUpperInvariant();
        var trimmedName = request.Name!.Trim();

        if (string.IsNullOrWhiteSpace(trimmedCode))
            throw new BusinessException(
                ResponseMessages.LevelCodeRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.LevelCodeRequired);

        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.LevelNameRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.LevelNameRequired);

        if (await _levelRepo.ExistsCodeAsync(trimmedCode, cancellationToken))
            throw new BusinessException(
                ResponseMessages.LevelCodeAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.LevelCodeAlreadyExists);

        if (await _levelRepo.ExistsNameAsync(trimmedName, cancellationToken))
            throw new BusinessException(
                ResponseMessages.LevelNameAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.LevelNameAlreadyExists);

        var level = new Level
        {
            Code = trimmedCode,
            Name = trimmedName,
            Description = request.Description?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };

        await _levelRepo.AddAsync(level, cancellationToken);
        await _levelRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.LevelCreated,
            createdByUserId,
            ipAddress,
            userAgent,
            entityId: level.Id,
            details: $"Level '{level.Name}' ({level.Code}) created.",
            newValue: SerializeLevel(level),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(level);
    }

    #endregion

    #region GetLevelsAsync

    public async Task<PaginatedResponse<LevelListItemResponse>> GetLevelsAsync(
        LevelFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _levelRepo.GetPagedAsync(filter, cancellationToken);
    }

    #endregion

    #region GetLevelByIdAsync

    public async Task<LevelDetailResponse> GetLevelByIdAsync(
        long levelId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _levelRepo.GetDetailByIdAsync(levelId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.LevelNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.LevelNotFound);

        return detail;
    }

    #endregion

    #region UpdateLevelAsync

    public async Task<LevelResponse> UpdateLevelAsync(
        long levelId,
        UpdateLevelRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var level = await _levelRepo.GetTrackedByIdAsync(levelId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.LevelNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.LevelNotFound);

        var trimmedCode = request.Code!.Trim().ToUpperInvariant();
        var trimmedName = request.Name!.Trim();

        if (string.IsNullOrWhiteSpace(trimmedCode))
            throw new BusinessException(
                ResponseMessages.LevelCodeRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.LevelCodeRequired);

        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.LevelNameRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.LevelNameRequired);

        if (await _levelRepo.ExistsCodeExcludingIdAsync(levelId, trimmedCode, cancellationToken))
            throw new BusinessException(
                ResponseMessages.LevelCodeAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.LevelCodeAlreadyExists);

        if (await _levelRepo.ExistsNameExcludingIdAsync(levelId, trimmedName, cancellationToken))
            throw new BusinessException(
                ResponseMessages.LevelNameAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.LevelNameAlreadyExists);

        var oldValue = SerializeLevel(level);

        level.Code = trimmedCode;
        level.Name = trimmedName;
        level.Description = request.Description?.Trim();
        level.DisplayOrder = request.DisplayOrder;
        if (request.IsActive.HasValue)
            level.IsActive = request.IsActive.Value;
        level.DateLastMaint = DateTimeOffset.UtcNow;

        await _levelRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.LevelUpdated,
            updatedByUserId,
            ipAddress,
            userAgent,
            entityId: level.Id,
            details: $"Level '{level.Name}' ({level.Code}) updated.",
            oldValue: oldValue,
            newValue: SerializeLevel(level),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(level);
    }

    #endregion

    #region DeleteLevelAsync

    public async Task DeleteLevelAsync(
        long levelId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var level = await _levelRepo.GetTrackedByIdAsync(levelId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.LevelNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.LevelNotFound);

        if (await _levelRepo.HasInterviewQuestionsAsync(levelId, cancellationToken))
            throw new BusinessException(
                ResponseMessages.LevelHasInterviewQuestions,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.LevelHasInterviewQuestions);

        var oldValue = SerializeLevel(level);

        level.DeletedAt = DateTimeOffset.UtcNow;
        level.DeletedBy = deletedByUserId;
        level.IsActive = false;
        level.DateLastMaint = DateTimeOffset.UtcNow;

        await _levelRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.LevelDeleted,
            deletedByUserId,
            ipAddress,
            userAgent,
            entityId: level.Id,
            details: $"Level '{level.Name}' ({level.Code}) deleted.",
            oldValue: oldValue,
            traceId: traceId,
            cancellationToken: cancellationToken);
    }

    #endregion

    #region RestoreLevelAsync

    public async Task<LevelResponse> RestoreLevelAsync(
        long levelId,
        long restoredByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var level = await _levelRepo.GetTrackedDeletedByIdAsync(levelId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.LevelNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.LevelNotFound);

        var oldValue = SerializeLevel(level);

        level.DeletedAt = null;
        level.DeletedBy = null;
        level.IsActive = true;
        level.DateLastMaint = DateTimeOffset.UtcNow;

        await _levelRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.LevelRestored,
            restoredByUserId,
            ipAddress,
            userAgent,
            entityId: level.Id,
            details: $"Level '{level.Name}' ({level.Code}) restored.",
            oldValue: oldValue,
            newValue: SerializeLevel(level),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(level);
    }

    #endregion

    #region Private Helpers

    private static LevelResponse ToResponse(Level level) =>
        new()
        {
            Id = level.Id,
            Code = level.Code,
            Name = level.Name,
            Description = level.Description,
            DisplayOrder = level.DisplayOrder,
            IsActive = level.IsActive,
            CreatedAt = level.EffDate,
            UpdatedAt = level.DateLastMaint
        };

    private static string SerializeLevel(Level level) =>
        JsonSerializer.Serialize(level, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private async Task SafeAuditLogAsync(
        string action,
        long? userId,
        string? ipAddress,
        string? userAgent,
        long? entityId,
        string? details,
        string? oldValue = null,
        string? newValue = null,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _auditLog.LogAsync(
                action: action,
                userId: userId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                entityType: nameof(Level),
                entityId: entityId,
                details: details,
                oldValue: oldValue,
                newValue: newValue,
                traceId: traceId,
                cancellationToken: cancellationToken);
        }
        catch { /* non-fatal */ }
    }

    #endregion
}
