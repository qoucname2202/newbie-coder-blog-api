using System.Net;
using System.Text.Json;
using NewbieCoder.Core.CQRS.CommunityAnswers;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Service implementing community answer administration operations.
/// </summary>
public sealed class CommunityAnswerService : ICommunityAnswerService
{
    private readonly AppDbContext _db;
    private readonly ICommunityAnswerRepository _repo;
    private readonly IAuditLogService _auditLog;

    public CommunityAnswerService(
        AppDbContext db,
        ICommunityAnswerRepository repo,
        IAuditLogService auditLog)
    {
        _db = db;
        _repo = repo;
        _auditLog = auditLog;
    }

    public async Task<PaginatedResponse<CommunityAnswerListItemResponse>> GetAnswersAsync(
        GetCommunityAnswersRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _repo.GetPagedAsync(filter, cancellationToken);
    }

    public async Task<CommunityAnswerDetailResponse> GetAnswerByIdAsync(
        long answerId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _repo.GetDetailByIdAsync(answerId, includeDeleted: false, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);

        return detail;
    }

    public async Task<HideCommunityAnswerResponse> HideAnswerAsync(
        HideCommunityAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        var exists = await _repo.ExistsAnyAsync(command.AnswerId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);

        var answer = await _repo.GetTrackedByIdAsync(command.AnswerId, cancellationToken);

        if (answer == null)
        {
            var isDeleted = await _repo.GetTrackedAnyByIdAsync(command.AnswerId, cancellationToken)
                is { DeletedAt: not null };
            if (isDeleted)
                throw new BusinessException(
                    ResponseMessages.CommunityAnswerCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityAnswerCannotBeModerated);

            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);
        }

        if (answer.IsHidden)
            throw new BusinessException(
                ResponseMessages.CommunityAnswerAlreadyHidden,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityAnswerAlreadyHidden);

        var now = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            answer.IsHidden = true;
            answer.DateLastMaint = now;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityAnswerHidden,
            command.ModeratedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: answer.Id,
            details: $"Community answer (ID: {answer.Id}) hidden by user {command.ModeratedByUserId}.",
            reason: command.Reason,
            oldValue: SerializeState(answer, isHidden: false),
            newValue: SerializeState(answer, isHidden: true),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new HideCommunityAnswerResponse
        {
            Id = answer.Id,
            IsHidden = true,
            ModeratedAt = now,
            ModeratedBy = command.ModeratedByUserId
        };
    }

    public async Task<ShowCommunityAnswerResponse> ShowAnswerAsync(
        ShowCommunityAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        var exists = await _repo.ExistsAnyAsync(command.AnswerId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);

        var answer = await _repo.GetTrackedByIdAsync(command.AnswerId, cancellationToken);

        if (answer == null)
        {
            var isDeleted = await _repo.GetTrackedAnyByIdAsync(command.AnswerId, cancellationToken)
                is { DeletedAt: not null };
            if (isDeleted)
                throw new BusinessException(
                    ResponseMessages.CommunityAnswerCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityAnswerCannotBeModerated);

            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);
        }

        if (!answer.IsHidden)
            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotHidden,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityAnswerNotHidden);

        var now = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            answer.IsHidden = false;
            answer.DateLastMaint = now;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityAnswerShown,
            command.ModeratedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: answer.Id,
            details: $"Community answer (ID: {answer.Id}) shown (unhidden) by user {command.ModeratedByUserId}.",
            oldValue: SerializeState(answer, isHidden: true),
            newValue: SerializeState(answer, isHidden: false),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new ShowCommunityAnswerResponse
        {
            Id = answer.Id,
            IsHidden = false,
            ModeratedAt = now,
            ModeratedBy = command.ModeratedByUserId
        };
    }

    public async Task<DeleteCommunityAnswerResponse> DeleteAnswerAsync(
        DeleteCommunityAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        var exists = await _repo.ExistsAnyAsync(command.AnswerId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);

        var answer = await _repo.GetTrackedByIdAsync(command.AnswerId, cancellationToken);

        if (answer == null)
            throw new BusinessException(
                ResponseMessages.CommunityAnswerAlreadyDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityAnswerAlreadyDeleted);

        var now = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            answer.DeletedAt = now;
            answer.DeletedBy = command.DeletedByUserId;
            answer.DateLastMaint = now;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityAnswerDeleted,
            command.DeletedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: answer.Id,
            details: $"Community answer (ID: {answer.Id}) soft-deleted by user {command.DeletedByUserId}.",
            oldValue: SerializeState(answer, isDeleted: false),
            newValue: SerializeState(answer, isDeleted: true),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new DeleteCommunityAnswerResponse
        {
            Id = answer.Id,
            IsDeleted = true,
            DeletedAt = answer.DeletedAt,
            DeletedBy = answer.DeletedBy
        };
    }

    public async Task<RestoreCommunityAnswerResponse> RestoreAnswerAsync(
        RestoreCommunityAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        var exists = await _repo.ExistsAnyAsync(command.AnswerId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);

        var answer = await _repo.GetTrackedDeletedByIdAsync(command.AnswerId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CommunityAnswerNotDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityAnswerNotDeleted);

        var now = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            answer.DeletedAt = null;
            answer.DeletedBy = null;
            answer.DateLastMaint = now;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityAnswerRestored,
            command.RestoredByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: answer.Id,
            details: $"Community answer (ID: {answer.Id}) restored by user {command.RestoredByUserId}.",
            oldValue: SerializeState(answer, isDeleted: true),
            newValue: SerializeState(answer, isDeleted: false),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new RestoreCommunityAnswerResponse
        {
            Id = answer.Id,
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null,
            UpdatedAt = answer.DateLastMaint
        };
    }

    public async Task<CreateCommunityAnswerResponse> CreateAnswerAsync(
        CreateCommunityAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await _repo.QuestionExistsAsync(command.QuestionId, cancellationToken))
            throw new BusinessException(
                ResponseMessages.CommunityAnswerQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerQuestionNotFound);

        if (!await _repo.AuthorExistsAsync(command.AuthorId, cancellationToken))
            throw new BusinessException(
                ResponseMessages.CommunityAnswerAuthorNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerAuthorNotFound);

        var now = DateTimeOffset.UtcNow;

        var answer = new CommunityAnswer
        {
            QuestionId = command.QuestionId,
            AuthorId = command.AuthorId,
            Content = command.Content.Trim(),
            VoteScore = 0,
            IsAccepted = false,
            IsHidden = false,
            EffDate = now,
            DateLastMaint = now
        };

        var created = await _repo.CreateAsync(answer, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityAnswerCreated,
            command.CreatedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: created.Id,
            details: $"Community answer (ID: {created.Id}) created by user {command.CreatedByUserId} for question {command.QuestionId}.",
            newValue: SerializeState(created),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new CreateCommunityAnswerResponse
        {
            Id = created.Id,
            QuestionId = created.QuestionId,
            AuthorId = created.AuthorId,
            Content = created.Content,
            CreatedAt = created.EffDate
        };
    }

    public async Task<UpdateCommunityAnswerResponse> UpdateAnswerAsync(
        UpdateCommunityAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Content))
            throw new BusinessException(
                ResponseMessages.CommunityAnswerContentRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityAnswerContentRequired);

        var answer = await _repo.GetTrackedByIdAsync(command.AnswerId, cancellationToken);

        if (answer == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.AnswerId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityAnswerCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityAnswerCannotBeModerated);

            throw new BusinessException(
                ResponseMessages.CommunityAnswerNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityAnswerNotFound);
        }

        var oldContent = answer.Content;
        var now = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            answer.Content = command.Content.Trim();
            answer.DateLastMaint = now;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityAnswerUpdated,
            command.UpdatedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: answer.Id,
            details: $"Community answer (ID: {answer.Id}) updated by user {command.UpdatedByUserId}.",
            oldValue: JsonSerializer.Serialize(new { Id = answer.Id, Content = oldContent }),
            newValue: SerializeState(answer),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new UpdateCommunityAnswerResponse
        {
            Id = answer.Id,
            Content = answer.Content,
            UpdatedAt = answer.DateLastMaint
        };
    }

    private async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync<object, object>(
            state: null!,
            operation: async (_, _, _) =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await action();
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
                return null!;
            },
            verifySucceeded: null,
            cancellationToken: cancellationToken);
    }

    private async Task SafeAuditLogAsync(
        string action,
        long? userId,
        string? ipAddress,
        string? userAgent,
        long? entityId,
        string? details,
        string? reason = null,
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
                entityType: nameof(CommunityAnswer),
                entityId: entityId,
                details: reason != null ? $"{details} Reason: {reason}" : details,
                oldValue: oldValue,
                newValue: newValue,
                traceId: traceId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Audit log failures must not fail the primary operation.
        }
    }

    private static string SerializeState(CommunityAnswer a, bool? isHidden = null, bool? isDeleted = null) =>
        JsonSerializer.Serialize(new
        {
            a.Id,
            a.QuestionId,
            IsHidden = isHidden ?? a.IsHidden,
            IsDeleted = isDeleted ?? (a.DeletedAt != null),
            a.DeletedAt
        });
}
