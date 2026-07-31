using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.CQRS.CommunityQuestions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Services.Helpers;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Service implementing community question administration operations.
/// </summary>
public sealed class CommunityQuestionService : ICommunityQuestionService
{
    private readonly AppDbContext _db;
    private readonly ICommunityQuestionRepository _repo;
    private readonly ITagRepository _tagRepo;
    private readonly IAuditLogService _auditLog;

    public CommunityQuestionService(
        AppDbContext db,
        ICommunityQuestionRepository repo,
        ITagRepository tagRepo,
        IAuditLogService auditLog)
    {
        _db = db;
        _repo = repo;
        _tagRepo = tagRepo;
        _auditLog = auditLog;
    }

    public async Task<PaginatedResponse<CommunityQuestionListItemResponse>> GetQuestionsAsync(
        GetCommunityQuestionsRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _repo.GetPagedAsync(filter, cancellationToken);
    }

    public async Task<CommunityQuestionDetailResponse> GetQuestionByIdAsync(
        long questionId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _repo.GetDetailByIdAsync(questionId, includeDeleted: true, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);

        return detail;
    }

    public async Task<LockCommunityQuestionResponse> LockQuestionAsync(
        LockCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);
        }

        if (question.ClosedAt != null)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionAlreadyLocked,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionAlreadyLocked);

        await ExecuteInTransactionAsync(async () =>
        {
            question.ClosedAt = DateTimeOffset.UtcNow;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionLocked,
            command.LockedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) locked by user {command.LockedByUserId}.",
            oldValue: SerializeState(question, isLocked: false),
            newValue: SerializeState(question, isLocked: true),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new LockCommunityQuestionResponse
        {
            Id = question.Id,
            IsLocked = true,
            ClosedAt = question.ClosedAt
        };
    }

    public async Task<UnlockCommunityQuestionResponse> UnlockQuestionAsync(
        UnlockCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);
        }

        if (question.ClosedAt == null)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotLocked,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionNotLocked);

        await ExecuteInTransactionAsync(async () =>
        {
            question.ClosedAt = null;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionUnlocked,
            command.UnlockedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) unlocked by user {command.UnlockedByUserId}.",
            oldValue: SerializeState(question, isLocked: true),
            newValue: SerializeState(question, isLocked: false),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new UnlockCommunityQuestionResponse
        {
            Id = question.Id,
            IsLocked = false,
            ClosedAt = null
        };
    }

    public async Task<DeleteCommunityQuestionResponse> DeleteQuestionAsync(
        DeleteCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionReasonRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionReasonRequired);

        var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);

        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionAlreadyDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

        var oldState = SerializeState(question, isDeleted: false);

        await ExecuteInTransactionAsync(async () =>
        {
            question.DeletedAt = DateTimeOffset.UtcNow;
            question.DeletedBy = command.DeletedByUserId;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionDeleted,
            command.DeletedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) soft-deleted by user {command.DeletedByUserId}. Reason: {command.Reason}",
            reason: command.Reason,
            oldValue: oldState,
            newValue: SerializeState(question, isDeleted: true),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new DeleteCommunityQuestionResponse
        {
            Id = question.Id,
            IsDeleted = true,
            DeletedAt = question.DeletedAt
        };
    }

    public async Task<RestoreCommunityQuestionResponse> RestoreQuestionAsync(
        RestoreCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionReasonRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionReasonRequired);

        var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);

        var question = await _repo.GetTrackedDeletedByIdAsync(command.QuestionId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CommunityQuestionNotDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionNotDeleted);

        var oldState = SerializeState(question, isDeleted: true);

        await ExecuteInTransactionAsync(async () =>
        {
            question.DeletedAt = null;
            question.DeletedBy = null;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionRestored,
            command.RestoredByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) restored by user {command.RestoredByUserId}. Reason: {command.Reason}",
            reason: command.Reason,
            oldValue: oldState,
            newValue: SerializeState(question, isDeleted: false),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new RestoreCommunityQuestionResponse
        {
            Id = question.Id,
            IsDeleted = false,
            DeletedAt = null,
            UpdatedAt = question.DateLastMaint
        };
    }

    public async Task<CreateCommunityQuestionResponse> CreateQuestionAsync(
        CreateCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionTitleRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionTitleRequired);

        if (command.Title.Trim().Length < 10)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionTitleTooShort,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionTitleTooShort);

        if (command.Title.Trim().Length > 300)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionTitleTooLong,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionTitleTooLong);

        if (string.IsNullOrWhiteSpace(command.Content))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionContentRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionContentRequired);

        if (command.Content.Trim().Length < 20)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionContentTooShort,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionContentTooShort);

        if (command.Content.Trim().Length > 10000)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionContentTooLong,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionContentTooLong);

        if (!await _repo.AuthorExistsAsync(command.AuthorId, cancellationToken))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionAuthorNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionAuthorNotFound);

        var slug = SlugGenerator.Generate(command.Title);
        var now = DateTimeOffset.UtcNow;

        var question = new CommunityQuestion
        {
            AuthorId = command.AuthorId,
            Title = command.Title.Trim(),
            Slug = slug,
            Content = command.Content.Trim(),
            Status = CommunityQuestionStatus.Open,
            EffDate = now,
            DateLastMaint = now
        };

        var createdQuestion = await _repo.CreateAsync(question, command.TagIds, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionCreated,
            command.CreatedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: createdQuestion.Id,
            details: $"Community question '{createdQuestion.Title}' (ID: {createdQuestion.Id}) created by user {command.CreatedByUserId}.",
            newValue: SerializeState(createdQuestion),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new CreateCommunityQuestionResponse
        {
            Id = createdQuestion.Id,
            Title = createdQuestion.Title,
            Slug = createdQuestion.Slug,
            AuthorId = createdQuestion.AuthorId,
            CreatedAt = createdQuestion.EffDate
        };
    }

    public async Task<UpdateCommunityQuestionResponse> UpdateQuestionAsync(
        UpdateCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionTitleRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionTitleRequired);

        if (command.Title.Trim().Length < 10)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionTitleTooShort,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionTitleTooShort);

        if (command.Title.Trim().Length > 300)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionTitleTooLong,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionTitleTooLong);

        if (string.IsNullOrWhiteSpace(command.Content))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionContentRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionContentRequired);

        if (command.Content.Trim().Length < 20)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionContentTooShort,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionContentTooShort);

        if (command.Content.Trim().Length > 10000)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionContentTooLong,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionContentTooLong);

        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionReasonRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionReasonRequired);

        if (command.TagIds != null && command.TagIds.Count != command.TagIds.Distinct().Count())
            throw new BusinessException(
                ResponseMessages.InvalidTagIds,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidTagIds);

        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionAlreadyDeleted,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);
        }

        var oldState = SerializeState(question);

        if (command.TagIds != null)
        {
            foreach (var tagId in command.TagIds)
            {
                var tag = await _tagRepo.GetActiveByIdAsync(tagId, cancellationToken);
                if (tag == null)
                    throw new BusinessException(
                        ResponseMessages.TagNotFound,
                        statusCode: HttpStatusCodes.NotFound,
                        responseCode: ResponseCodes.TagNotFound);
            }
        }

        await ExecuteInTransactionAsync(async () =>
        {
            question.Title = command.Title.Trim();
            question.Content = command.Content.Trim();
            question.DateLastMaint = DateTimeOffset.UtcNow;

            if (command.TagIds != null)
            {
                question.CommunityQuestionTags.Clear();

                foreach (var tagId in command.TagIds)
                {
                    var tag = await _tagRepo.GetActiveByIdAsync(tagId, cancellationToken);
                    if (tag == null) continue;

                    var exists = await _tagRepo.HasCommunityQuestionTagAsync(tagId, question.Id, cancellationToken);
                    if (!exists)
                    {
                        await _tagRepo.AddCommunityQuestionTagAsync(
                            new CommunityQuestionTag
                            {
                                QuestionId = question.Id,
                                TagId = tagId,
                                EffDate = DateTimeOffset.UtcNow
                            },
                            cancellationToken);
                    }
                }
            }

            await _tagRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionUpdated,
            command.UpdatedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) updated by user {command.UpdatedByUserId}. Reason: {command.Reason}",
            reason: command.Reason,
            oldValue: oldState,
            newValue: SerializeState(question),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new UpdateCommunityQuestionResponse
        {
            Id = question.Id,
            Title = question.Title,
            Slug = question.Slug,
            UpdatedAt = question.DateLastMaint
        };
    }

    public async Task<ChangeCommunityQuestionStatusResponse> ChangeStatusAsync(
        ChangeCommunityQuestionStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionReasonRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionReasonRequired);

        if (!Enum.IsDefined(typeof(CommunityQuestionStatus), command.Status))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionInvalidStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionInvalidStatus);

        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);
        }

        var newStatus = (CommunityQuestionStatus)command.Status;
        if (question.Status == newStatus)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionAlreadyInTargetStatus,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionAlreadyInTargetStatus);

        if (newStatus == CommunityQuestionStatus.Resolved)
        {
            if (question.AcceptedAnswerId == null)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionResolvedRequiresAcceptedAnswer,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CommunityQuestionResolvedRequiresAcceptedAnswer);

            var acceptedAnswer = await _db.CommunityAnswers
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == question.AcceptedAnswerId, cancellationToken);

            if (acceptedAnswer == null)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionAcceptedAnswerNotFound,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CommunityQuestionAcceptedAnswerNotFound);

            if (acceptedAnswer.QuestionId != question.Id)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionAcceptedAnswerNotFound,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CommunityQuestionAcceptedAnswerNotFound);

            if (acceptedAnswer.IsHidden)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionAcceptedAnswerHidden,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CommunityQuestionAcceptedAnswerHidden);
        }

        if (newStatus == CommunityQuestionStatus.Answered)
        {
            var hasActiveAnswer = await _repo.HasActiveAnswerAsync(question.Id, cancellationToken);
            if (!hasActiveAnswer)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionAnsweredRequiresAnswers,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.CommunityQuestionAnsweredRequiresAnswers);
        }

        var previousStatus = question.Status;
        var oldState = SerializeState(question);

        await ExecuteInTransactionAsync(async () =>
        {
            question.Status = newStatus;
            question.DateLastMaint = DateTimeOffset.UtcNow;

            if (newStatus == CommunityQuestionStatus.Closed)
                question.ClosedAt = DateTimeOffset.UtcNow;
            else if (question.ClosedAt != null && newStatus == CommunityQuestionStatus.Open)
                question.ClosedAt = null;

            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionStatusChanged,
            command.ModeratedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question (ID: {question.Id}) status changed from {previousStatus} to {newStatus} by user {command.ModeratedByUserId}. Reason: {command.Reason}",
            reason: command.Reason,
            oldValue: oldState,
            newValue: SerializeState(question),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new ChangeCommunityQuestionStatusResponse
        {
            Id = question.Id,
            PreviousStatus = previousStatus.ToString(),
            CurrentStatus = question.Status.ToString(),
            ClosedAt = question.ClosedAt,
            UpdatedAt = question.DateLastMaint
        };
    }

    public async Task<HideCommunityQuestionResponse> HideQuestionAsync(
        HideCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionReasonRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionReasonRequired);

        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);
        }

        if (question.Status == CommunityQuestionStatus.Hidden)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionAlreadyHidden,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionAlreadyHidden);

        var oldState = SerializeState(question);

        await ExecuteInTransactionAsync(async () =>
        {
            question.Status = CommunityQuestionStatus.Hidden;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionHidden,
            command.ModeratedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) hidden by user {command.ModeratedByUserId}. Reason: {command.Reason}",
            reason: command.Reason,
            oldValue: oldState,
            newValue: SerializeState(question),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new HideCommunityQuestionResponse
        {
            Id = question.Id,
            Status = question.Status.ToString(),
            HiddenAt = now,
            HiddenBy = command.ModeratedByUserId
        };
    }

    public async Task<CloseCommunityQuestionResponse> CloseQuestionAsync(
        CloseCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionReasonRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionReasonRequired);

        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);
        }

        if (question.ClosedAt != null || question.Status == CommunityQuestionStatus.Closed)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionAlreadyClosed,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionAlreadyClosed);

        var oldState = SerializeState(question);
        var now = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            question.Status = CommunityQuestionStatus.Closed;
            question.ClosedAt = now;
            question.DateLastMaint = now;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionClosed,
            command.ModeratedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) closed by user {command.ModeratedByUserId}. Reason: {command.Reason}",
            reason: command.Reason,
            oldValue: oldState,
            newValue: SerializeState(question),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new CloseCommunityQuestionResponse
        {
            Id = question.Id,
            Status = question.Status.ToString(),
            ClosedAt = question.ClosedAt ?? now,
            ClosedBy = command.ModeratedByUserId
        };
    }

    public async Task<ReopenCommunityQuestionResponse> ReopenQuestionAsync(
        ReopenCommunityQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new BusinessException(
                ResponseMessages.CommunityQuestionReasonRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CommunityQuestionReasonRequired);

        var question = await _repo.GetTrackedByIdAsync(command.QuestionId, cancellationToken);

        if (question == null)
        {
            var exists = await _repo.ExistsAnyAsync(command.QuestionId, cancellationToken);
            if (exists)
                throw new BusinessException(
                    ResponseMessages.CommunityQuestionCannotBeModerated,
                    statusCode: HttpStatusCodes.Conflict,
                    responseCode: ResponseCodes.CommunityQuestionAlreadyDeleted);

            throw new BusinessException(
                ResponseMessages.CommunityQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CommunityQuestionNotFound);
        }

        if (question.Status == CommunityQuestionStatus.Open)
            throw new BusinessException(
                ResponseMessages.CommunityQuestionAlreadyOpen,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CommunityQuestionAlreadyOpen);

        var oldState = SerializeState(question);

        await ExecuteInTransactionAsync(async () =>
        {
            question.Status = CommunityQuestionStatus.Open;
            question.ClosedAt = null;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CommunityQuestionReopened,
            command.ModeratedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: question.Id,
            details: $"Community question '{question.Title}' (ID: {question.Id}) reopened by user {command.ModeratedByUserId}. Reason: {command.Reason}",
            reason: command.Reason,
            oldValue: oldState,
            newValue: SerializeState(question),
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new ReopenCommunityQuestionResponse
        {
            Id = question.Id,
            Status = question.Status.ToString(),
            ClosedAt = null,
            UpdatedAt = question.DateLastMaint
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
                entityType: nameof(CommunityQuestion),
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

    private static string SerializeState(CommunityQuestion q, bool? isLocked = null, bool? isDeleted = null) =>
        JsonSerializer.Serialize(new
        {
            q.Id,
            q.Title,
            q.Slug,
            Status = q.Status.ToString(),
            IsLocked = isLocked ?? (q.ClosedAt != null),
            IsDeleted = isDeleted ?? (q.DeletedAt != null),
            q.ClosedAt,
            q.DeletedAt
        });
}
