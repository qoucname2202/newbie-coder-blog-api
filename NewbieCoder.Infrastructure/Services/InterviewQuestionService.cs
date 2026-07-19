using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
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

namespace NewbieCoder.Infrastructure.Services;

public sealed class InterviewQuestionService : IInterviewQuestionService
{
    private readonly AppDbContext _db;
    private readonly IInterviewQuestionRepository _iqRepo;
    private readonly IAuditLogService _auditLog;

    private static readonly HashSet<PostStatus> SupportedStatuses =
    [
        PostStatus.Draft,
        PostStatus.Published,
        PostStatus.Hidden,
        PostStatus.Archived
    ];

    private static readonly Dictionary<PostStatus, HashSet<PostStatus>> AllowedTransitions = new()
    {
        [PostStatus.Draft] = [PostStatus.Published, PostStatus.Hidden, PostStatus.Archived],
        [PostStatus.Published] = [PostStatus.Hidden, PostStatus.Archived],
        [PostStatus.Hidden] = [PostStatus.Published, PostStatus.Archived],
        [PostStatus.Archived] = []
    };

    public InterviewQuestionService(
        AppDbContext db,
        IInterviewQuestionRepository iqRepo,
        IAuditLogService auditLog)
    {
        _db = db;
        _iqRepo = iqRepo;
        _auditLog = auditLog;
    }

    #region GetQuestionsAsync

    public async Task<PaginatedResponse<InterviewQuestionListItemResponse>> GetQuestionsAsync(
        GetInterviewQuestionsRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _iqRepo.GetPagedAsync(filter, cancellationToken);
    }

    #endregion

    #region GetQuestionByIdAsync

    public async Task<InterviewQuestionDetailResponse> GetQuestionByIdAsync(
        long questionId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _iqRepo.GetDetailByIdAsync(questionId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.InterviewQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.InterviewQuestionNotFound);

        return detail;
    }

    #endregion

    #region CreateQuestionAsync

    public async Task<CreateInterviewQuestionResponse> CreateQuestionAsync(
        CreateInterviewQuestionRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        // Validate and parse difficulty
        var level = ParseAndValidateLevel(request.DifficultyLevel);

        // Parse and validate status
        var status = ParseAndValidateStatus(request.Status, isCreate: true);

        // Validate answers before going into transaction
        ValidateAnswers(request.Answers, status, isCreate: true);

        // Generate unique slug
        var slug = await GenerateUniqueSlugAsync(request.Title, null, cancellationToken);

        var question = new InterviewQuestion
        {
            AuthorId = createdByUserId,
            Title = request.Title.Trim(),
            Slug = slug,
            QuestionContent = request.QuestionContent.Trim(),
            Explanation = request.Explanation?.Trim(),
            Level = level,
            Status = status,
            Technology = request.Technology?.Trim(),
            Topic = request.Topic?.Trim(),
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };

        await ExecuteInTransactionAsync(async () =>
        {
            await _iqRepo.AddQuestionAsync(question, cancellationToken);

            if (request.TagIds is { Count: > 0 })
            {
                var tags = await _iqRepo.GetActiveTagsAsync(request.TagIds, cancellationToken);
                if (tags.Count != request.TagIds.Count)
                    throw new BusinessException(
                        ResponseMessages.InterviewQuestionTagNotFound,
                        statusCode: HttpStatusCodes.NotFound,
                        responseCode: ResponseCodes.InterviewQuestionTagNotFound);

                await _iqRepo.ReplaceTagsAsync(question, tags, cancellationToken);
            }

            if (request.Answers is { Count: > 0 })
            {
                var answerOrder = 1;
                foreach (var answerReq in request.Answers)
                {
                    await _iqRepo.AddAnswerAsync(new InterviewAnswer
                    {
                        Question = question,
                        AnswerContent = answerReq.Content.Trim(),
                        Explanation = answerReq.Explanation?.Trim(),
                        Example = answerReq.Example?.Trim(),
                        IsOfficial = answerReq.IsOfficial,
                        EffDate = DateTimeOffset.UtcNow,
                        DateLastMaint = DateTimeOffset.UtcNow
                    }, cancellationToken);
                    answerOrder++;
                }
            }

            await _iqRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.InterviewQuestionCreated,
            createdByUserId,
            ipAddress,
            userAgent,
            entityId: question.Id,
            details: $"Interview question '{question.Title}' created by user {createdByUserId}.",
            newValue: SerializeQuestion(question),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new CreateInterviewQuestionResponse
        {
            Id = question.Id,
            Title = question.Title,
            DifficultyLevel = question.Level.ToString(),
            Status = question.Status.ToString(),
            Technology = question.Technology,
            Topic = question.Topic,
            AnswerCount = request.Answers?.Count ?? 0,
            CreatedAt = question.EffDate
        };
    }

    #endregion

    #region UpdateQuestionAsync

    public async Task<UpdateInterviewQuestionResponse> UpdateQuestionAsync(
        long questionId,
        UpdateInterviewQuestionRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var question = await _iqRepo.GetTrackedByIdAsync(questionId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.InterviewQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.InterviewQuestionNotFound);

        var oldData = SerializeQuestion(question);

        // Validate and parse difficulty
        var level = ParseAndValidateLevel(request.DifficultyLevel);

        // Parse and validate status
        var newStatus = ParseAndValidateStatus(request.Status, isCreate: false);

        // Validate answers
        ValidateUpdateAnswers(request.Answers, newStatus);

        // Generate new slug if title changed
        var newSlug = question.Slug;
        if (!string.Equals(question.Title.Trim(), request.Title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            newSlug = await GenerateUniqueSlugAsync(request.Title, questionId, cancellationToken);
        }

        // Update question fields
        question.Title = request.Title.Trim();
        question.Slug = newSlug;
        question.QuestionContent = request.QuestionContent.Trim();
        question.Explanation = request.Explanation?.Trim();
        question.Level = level;
        question.Status = newStatus;
        question.Technology = request.Technology?.Trim();
        question.Topic = request.Topic?.Trim();
        question.DateLastMaint = DateTimeOffset.UtcNow;

        await ExecuteInTransactionAsync(async () =>
        {
            // Replace tags
            if (request.TagIds is { Count: > 0 })
            {
                var tags = await _iqRepo.GetActiveTagsAsync(request.TagIds, cancellationToken);
                if (tags.Count != request.TagIds.Count)
                    throw new BusinessException(
                        ResponseMessages.InterviewQuestionTagNotFound,
                        statusCode: HttpStatusCodes.NotFound,
                        responseCode: ResponseCodes.InterviewQuestionTagNotFound);

                await _iqRepo.ReplaceTagsAsync(question, tags, cancellationToken);
            }
            else
            {
                await _iqRepo.ReplaceTagsAsync(question, [], cancellationToken);
            }

            // Synchronize answers
            if (request.Answers is { Count: > 0 })
            {
                var existingAnswerIds = question.Answers.Select(a => a.Id).ToHashSet();
                var submittedAnswerIds = request.Answers
                    .Where(a => a.Id > 0)
                    .Select(a => a.Id)
                    .ToHashSet();

                // Soft-delete answers not in the submitted list
                foreach (var existing in question.Answers)
                {
                    if (!submittedAnswerIds.Contains(existing.Id))
                    {
                        await _iqRepo.SoftDeleteAnswerAsync(existing, updatedByUserId, cancellationToken);
                    }
                }

                // Upsert answers
                foreach (var answerReq in request.Answers)
                {
                    if (answerReq.Id > 0 && existingAnswerIds.Contains(answerReq.Id))
                    {
                        // Update existing
                        var existingAnswer = question.Answers.First(a => a.Id == answerReq.Id);
                        existingAnswer.AnswerContent = answerReq.Content.Trim();
                        existingAnswer.Explanation = answerReq.Explanation?.Trim();
                        existingAnswer.Example = answerReq.Example?.Trim();
                        existingAnswer.IsOfficial = answerReq.IsOfficial;
                        existingAnswer.DateLastMaint = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        // Create new
                        await _iqRepo.AddAnswerAsync(new InterviewAnswer
                        {
                            QuestionId = question.Id,
                            AnswerContent = answerReq.Content.Trim(),
                            Explanation = answerReq.Explanation?.Trim(),
                            Example = answerReq.Example?.Trim(),
                            IsOfficial = answerReq.IsOfficial,
                            EffDate = DateTimeOffset.UtcNow,
                            DateLastMaint = DateTimeOffset.UtcNow
                        }, cancellationToken);
                    }
                }
            }
            else
            {
                // Remove all existing answers
                foreach (var existing in question.Answers)
                {
                    await _iqRepo.SoftDeleteAnswerAsync(existing, updatedByUserId, cancellationToken);
                }
            }

            await _iqRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var newData = SerializeQuestion(question);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.InterviewQuestionUpdated,
            updatedByUserId,
            ipAddress,
            userAgent,
            entityId: question.Id,
            details: $"Interview question '{question.Title}' updated by user {updatedByUserId}.",
            oldValue: oldData,
            newValue: newData,
            traceId: traceId,
            cancellationToken: cancellationToken);

        var answerCount = question.Answers.Count(a => a.DeletedAt == null);

        return new UpdateInterviewQuestionResponse
        {
            Id = question.Id,
            Title = question.Title,
            DifficultyLevel = question.Level.ToString(),
            Status = question.Status.ToString(),
            Technology = question.Technology,
            Topic = question.Topic,
            AnswerCount = answerCount,
            UpdatedAt = question.DateLastMaint
        };
    }

    #endregion

    #region DeleteQuestionAsync

    public async Task<DeleteInterviewQuestionResponse> DeleteQuestionAsync(
        long questionId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _iqRepo.ExistsQuestionAsync(questionId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.InterviewQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.InterviewQuestionNotFound);

        var question = await _iqRepo.GetTrackedByIdAsync(questionId, cancellationToken);

        if (question == null)
            throw new BusinessException(
                ResponseMessages.InterviewQuestionAlreadyDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.InterviewQuestionAlreadyDeleted);

        var oldData = SerializeQuestion(question);

        await ExecuteInTransactionAsync(async () =>
        {
            question.DeletedAt = DateTimeOffset.UtcNow;
            question.DeletedBy = deletedByUserId;
            question.Status = PostStatus.Archived;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _iqRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.InterviewQuestionDeleted,
            deletedByUserId,
            ipAddress,
            userAgent,
            entityId: question.Id,
            details: $"Interview question '{question.Title}' soft-deleted by user {deletedByUserId}.",
            oldValue: oldData,
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new DeleteInterviewQuestionResponse
        {
            Id = question.Id,
            Status = question.Status.ToString(),
            DeletedAt = question.DeletedAt
        };
    }

    #endregion

    #region RestoreQuestionAsync

    public async Task<RestoreInterviewQuestionResponse> RestoreQuestionAsync(
        long questionId,
        long restoredByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _iqRepo.ExistsQuestionAsync(questionId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.InterviewQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.InterviewQuestionNotFound);

        var question = await _iqRepo.GetTrackedByIdAsync(questionId, cancellationToken);

        if (question != null && question.DeletedAt == null)
            throw new BusinessException(
                ResponseMessages.InterviewQuestionNotDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.InterviewQuestionNotDeleted);

        question = await _iqRepo.GetTrackedDeletedByIdAsync(questionId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.InterviewQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.InterviewQuestionNotFound);

        var oldData = SerializeQuestion(question);

        await ExecuteInTransactionAsync(async () =>
        {
            question.DeletedAt = null;
            question.DeletedBy = null;
            question.Status = PostStatus.Draft;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _iqRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.InterviewQuestionRestored,
            restoredByUserId,
            ipAddress,
            userAgent,
            entityId: question.Id,
            details: $"Interview question '{question.Title}' restored by user {restoredByUserId}.",
            oldValue: oldData,
            newValue: SerializeQuestion(question),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new RestoreInterviewQuestionResponse
        {
            Id = question.Id,
            Status = question.Status.ToString(),
            IsDeleted = question.DeletedAt != null,
            UpdatedAt = question.DateLastMaint
        };
    }

    #endregion

    #region ChangeQuestionStatusAsync

    public async Task<ChangeInterviewQuestionStatusResponse> ChangeQuestionStatusAsync(
        long questionId,
        ChangeInterviewQuestionStatusRequest request,
        long changedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var question = await _iqRepo.GetTrackedByIdAsync(questionId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.InterviewQuestionNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.InterviewQuestionNotFound);

        if (!Enum.TryParse<PostStatus>(request.Status, ignoreCase: true, out var targetStatus))
            throw new BusinessException(
                ResponseMessages.InterviewQuestionInvalidStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InterviewQuestionInvalidStatus);

        if (!SupportedStatuses.Contains(targetStatus))
            throw new BusinessException(
                ResponseMessages.InterviewQuestionInvalidStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InterviewQuestionInvalidStatus);

        if (question.Status == targetStatus)
            throw new BusinessException(
                ResponseMessages.InterviewQuestionAlreadyInTargetStatus,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.InterviewQuestionAlreadyInTargetStatus);

        if (!IsValidTransition(question.Status, targetStatus))
            throw new BusinessException(
                ResponseMessages.InterviewQuestionInvalidStatusTransition,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.InterviewQuestionInvalidStatusTransition);

        var previousStatus = question.Status;
        var oldData = SerializeQuestion(question);

        await ExecuteInTransactionAsync(async () =>
        {
            question.Status = targetStatus;
            question.DateLastMaint = DateTimeOffset.UtcNow;
            await _iqRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var newData = SerializeQuestion(question);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.InterviewQuestionStatusChanged,
            changedByUserId,
            ipAddress,
            userAgent,
            entityId: question.Id,
            details: $"Interview question '{question.Title}' status changed from {previousStatus} to {targetStatus} by user {changedByUserId}. Reason: {request.Reason ?? "N/A"}",
            oldValue: oldData,
            newValue: newData,
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new ChangeInterviewQuestionStatusResponse
        {
            PreviousStatus = previousStatus.ToString(),
            CurrentStatus = targetStatus.ToString(),
            UpdatedBy = changedByUserId
        };
    }

    #endregion

    #region Private Helpers

    private static InterviewLevel ParseAndValidateLevel(string levelString)
    {
        if (string.IsNullOrWhiteSpace(levelString))
            return InterviewLevel.Entry;

        if (!Enum.TryParse<InterviewLevel>(levelString, ignoreCase: true, out var level))
            throw new BusinessException(
                ResponseMessages.InterviewQuestionInvalidDifficulty,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InterviewQuestionInvalidDifficulty);

        return level;
    }

    private static PostStatus ParseAndValidateStatus(string statusString, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(statusString))
            return PostStatus.Draft;

        if (!Enum.TryParse<PostStatus>(statusString, ignoreCase: true, out var status))
            throw new BusinessException(
                ResponseMessages.InterviewQuestionInvalidStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InterviewQuestionInvalidStatus);

        if (!SupportedStatuses.Contains(status))
            throw new BusinessException(
                ResponseMessages.InterviewQuestionInvalidStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InterviewQuestionInvalidStatus);

        return status;
    }

    private static void ValidateAnswers(
        IReadOnlyList<InterviewAnswerRequest>? answers,
        PostStatus targetStatus,
        bool isCreate)
    {
        if (targetStatus == PostStatus.Published)
        {
            if (answers is null || answers.Count == 0)
                throw new BusinessException(
                    ResponseMessages.InterviewQuestionAnswerRequired,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.InterviewQuestionAnswerRequired);
        }

        if (answers is { Count: > 0 })
        {
            var preferredCount = answers.Count(a => a.IsOfficial);
            if (preferredCount > 1)
                throw new BusinessException(
                    ResponseMessages.InterviewQuestionMultiplePreferredAnswers,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.InterviewQuestionMultiplePreferredAnswers);

            for (var i = 0; i < answers.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(answers[i].Content))
                    throw new BusinessException(
                        ResponseMessages.InterviewQuestionInvalidAnswers,
                        statusCode: HttpStatusCodes.BadRequest,
                        responseCode: ResponseCodes.InterviewQuestionInvalidAnswers);
            }
        }
    }

    private static void ValidateUpdateAnswers(
        IReadOnlyList<UpdateInterviewAnswerRequest>? answers,
        PostStatus targetStatus)
    {
        if (targetStatus == PostStatus.Published)
        {
            if (answers is null || answers.Count == 0)
                throw new BusinessException(
                    ResponseMessages.InterviewQuestionAnswerRequired,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.InterviewQuestionAnswerRequired);
        }

        if (answers is { Count: > 0 })
        {
            var preferredCount = answers.Count(a => a.IsOfficial);
            if (preferredCount > 1)
                throw new BusinessException(
                    ResponseMessages.InterviewQuestionMultiplePreferredAnswers,
                    statusCode: HttpStatusCodes.BadRequest,
                    responseCode: ResponseCodes.InterviewQuestionMultiplePreferredAnswers);

            for (var i = 0; i < answers.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(answers[i].Content))
                    throw new BusinessException(
                        ResponseMessages.InterviewQuestionInvalidAnswers,
                        statusCode: HttpStatusCodes.BadRequest,
                        responseCode: ResponseCodes.InterviewQuestionInvalidAnswers);
            }
        }
    }

    private static bool IsValidTransition(PostStatus from, PostStatus to)
    {
        if (AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to))
            return true;

        return false;
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string title,
        long? excludeQuestionId = null,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = GenerateSlug(title);
        var slug = baseSlug;
        var counter = 1;

        bool exists;
        if (excludeQuestionId.HasValue)
            exists = await _db.InterviewQuestions
                .AsNoTracking()
                .AnyAsync(q => q.Id != excludeQuestionId.Value && q.Slug == slug, cancellationToken);
        else
            exists = await _db.InterviewQuestions
                .AsNoTracking()
                .AnyAsync(q => q.Slug == slug, cancellationToken);

        while (exists)
        {
            slug = $"{baseSlug}-{counter}";
            counter++;

            if (excludeQuestionId.HasValue)
                exists = await _db.InterviewQuestions
                    .AsNoTracking()
                    .AnyAsync(q => q.Id != excludeQuestionId.Value && q.Slug == slug, cancellationToken);
            else
                exists = await _db.InterviewQuestions
                    .AsNoTracking()
                    .AnyAsync(q => q.Slug == slug, cancellationToken);
        }

        return slug;
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "question";

        var slug = title.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9\-_]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = Regex.Replace(slug, @"_+", "_");
        slug = slug.Trim('-', '_');

        if (slug.Length > 200)
            slug = slug[..200].Trim('-', '_');

        return string.IsNullOrWhiteSpace(slug) ? "question" : slug;
    }

    private async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
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
        });
    }

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
                entityType: nameof(InterviewQuestion),
                entityId: entityId,
                details: details,
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

    private static string SerializeQuestion(InterviewQuestion question) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            question.Id,
            question.Title,
            question.Slug,
            Level = question.Level.ToString(),
            Status = question.Status.ToString(),
            question.Technology,
            question.Topic,
            question.EffDate,
            question.DateLastMaint
        });

    #endregion
}
