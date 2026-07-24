using System.Text.Json;
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

public sealed class TagService : ITagService
{
    private readonly AppDbContext _db;
    private readonly ITagRepository _tagRepo;
    private readonly IAuditLogService _auditLog;

    public TagService(
        AppDbContext db,
        ITagRepository tagRepo,
        IAuditLogService auditLog)
    {
        _db = db;
        _tagRepo = tagRepo;
        _auditLog = auditLog;
    }

    #region CreateTagAsync

    public async Task<TagResponse> CreateTagAsync(
        CreateTagRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name!.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(trimmedName)
            : request.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.TagNameRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.TagNameRequired);

        if (await _tagRepo.ExistsSlugAsync(slug, cancellationToken))
            throw new BusinessException(
                ResponseMessages.TagSlugAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.TagSlugAlreadyExists);

        var tag = new Tag
        {
            Name = trimmedName,
            Slug = slug,
            Description = request.Description?.Trim(),
            Status = EntityStatus.Active,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };

        await _tagRepo.AddAsync(tag, cancellationToken);
        await _tagRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.TagCreated,
            createdByUserId,
            ipAddress,
            userAgent,
            entityId: tag.Id,
            details: $"Tag '{tag.Name}' created.",
            newValue: SerializeTag(tag),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(tag);
    }

    #endregion

    #region GetTagsAsync

    public async Task<PaginatedResponse<TagListItemResponse>> GetTagsAsync(
        TagFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _tagRepo.GetPagedAsync(filter, cancellationToken);
    }

    #endregion

    #region GetTagByIdAsync

    public async Task<TagDetailResponse> GetTagByIdAsync(
        long tagId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _tagRepo.GetDetailByIdAsync(tagId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.TagNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.TagNotFound);

        return detail;
    }

    #endregion

    #region UpdateTagAsync

    public async Task<TagResponse> UpdateTagAsync(
        long tagId,
        UpdateTagRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepo.GetTrackedByIdAsync(tagId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.TagNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.TagNotFound);

        var trimmedName = request.Name!.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.TagNameRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.TagNameRequired);

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(trimmedName)
            : request.Slug.Trim().ToLowerInvariant();

        if (await _tagRepo.ExistsSlugExcludingIdAsync(tagId, slug, cancellationToken))
            throw new BusinessException(
                ResponseMessages.TagSlugAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.TagSlugAlreadyExists);

        var oldValue = SerializeTag(tag);

        tag.Name = trimmedName;
        tag.Slug = slug;
        tag.Description = request.Description?.Trim();
        tag.DateLastMaint = DateTimeOffset.UtcNow;

        if (request.IsActive.HasValue)
            tag.Status = request.IsActive.Value ? EntityStatus.Active : EntityStatus.Inactive;

        await _tagRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.TagUpdated,
            updatedByUserId,
            ipAddress,
            userAgent,
            entityId: tag.Id,
            details: $"Tag '{tag.Name}' updated.",
            oldValue: oldValue,
            newValue: SerializeTag(tag),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(tag);
    }

    #endregion

    #region ChangeStatusAsync

    public async Task<TagResponse> ChangeStatusAsync(
        long tagId,
        ChangeTagStatusRequest request,
        long changedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepo.GetTrackedByIdAsync(tagId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.TagNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.TagNotFound);

        if (!Enum.TryParse<EntityStatus>(request.Status, ignoreCase: true, out var newStatus))
            throw new BusinessException(
                ResponseMessages.TagInvalidStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.TagInvalidStatus);

        if (tag.Status == newStatus)
            throw new BusinessException(
                ResponseMessages.TagAlreadyInTargetStatus,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.TagAlreadyInTargetStatus);

        var oldStatus = tag.Status;
        tag.Status = newStatus;
        tag.DateLastMaint = DateTimeOffset.UtcNow;

        await _tagRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.TagStatusChanged,
            changedByUserId,
            ipAddress,
            userAgent,
            entityId: tag.Id,
            details: $"Tag '{tag.Name}' status changed from '{oldStatus}' to '{newStatus}'.",
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(tag);
    }

    #endregion

    #region DeleteTagAsync

    public async Task DeleteTagAsync(
        long tagId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepo.GetTrackedByIdAsync(tagId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.TagNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.TagNotFound);

        var oldValue = SerializeTag(tag);

        tag.DeletedAt = DateTimeOffset.UtcNow;
        tag.DeletedBy = deletedByUserId;
        tag.Status = EntityStatus.Inactive;
        tag.DateLastMaint = DateTimeOffset.UtcNow;

        await _tagRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.TagDeleted,
            deletedByUserId,
            ipAddress,
            userAgent,
            entityId: tag.Id,
            details: $"Tag '{tag.Name}' deleted.",
            oldValue: oldValue,
            traceId: traceId,
            cancellationToken: cancellationToken);
    }

    #endregion

    #region MergeTagsAsync

    public async Task<MergeTagResponse> MergeTagsAsync(
        MergeTagRequest request,
        long mergedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        if (request.SourceTagId == request.TargetTagId)
            throw new BusinessException(
                ResponseMessages.TagNotMergeableWithSelf,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.TagNotMergeableWithSelf);

        var sourceTag = await _tagRepo.GetTrackedByIdAsync(request.SourceTagId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.TagSourceNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.TagSourceNotFound);

        var targetTag = await _tagRepo.GetTrackedByIdAsync(request.TargetTagId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.TagTargetNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.TagTargetNotFound);

        int postsMerged = 0;
        int questionsMerged = 0;

        // Migrate post associations
        var postTags = await _tagRepo.GetPostTagsByTagIdAsync(sourceTag.Id, cancellationToken);
        foreach (var pt in postTags)
        {
            var alreadyExists = await _tagRepo.HasPostTagAsync(targetTag.Id, pt.PostId, cancellationToken);
            if (!alreadyExists)
            {
                await _tagRepo.AddPostTagAsync(new PostTag
                {
                    PostId = pt.PostId,
                    TagId = targetTag.Id,
                    EffDate = DateTimeOffset.UtcNow
                }, cancellationToken);
                postsMerged++;
            }
            _tagRepo.RemovePostTag(pt);
        }

        // Migrate interview question associations
        var iqTags = await _tagRepo.GetInterviewQuestionTagsByTagIdAsync(sourceTag.Id, cancellationToken);
        foreach (var iqt in iqTags)
        {
            var alreadyExists = await _tagRepo.HasInterviewQuestionTagAsync(targetTag.Id, iqt.QuestionId, cancellationToken);
            if (!alreadyExists)
            {
                await _tagRepo.AddInterviewQuestionTagAsync(new InterviewQuestionTag
                {
                    QuestionId = iqt.QuestionId,
                    TagId = targetTag.Id,
                    EffDate = DateTimeOffset.UtcNow
                }, cancellationToken);
                questionsMerged++;
            }
            _tagRepo.RemoveInterviewQuestionTag(iqt);
        }

        // Migrate community question associations
        var cqTags = await _tagRepo.GetCommunityQuestionTagsByTagIdAsync(sourceTag.Id, cancellationToken);
        foreach (var cqt in cqTags)
        {
            var alreadyExists = await _tagRepo.HasCommunityQuestionTagAsync(targetTag.Id, cqt.QuestionId, cancellationToken);
            if (!alreadyExists)
            {
                await _tagRepo.AddCommunityQuestionTagAsync(new CommunityQuestionTag
                {
                    QuestionId = cqt.QuestionId,
                    TagId = targetTag.Id,
                    EffDate = DateTimeOffset.UtcNow
                }, cancellationToken);
                questionsMerged++;
            }
            _tagRepo.RemoveCommunityQuestionTag(cqt);
        }

        // Update target tag counts
        targetTag.PostCount += postsMerged;
        targetTag.QuestionCount += questionsMerged;
        targetTag.DateLastMaint = DateTimeOffset.UtcNow;

        // Soft-delete source tag
        var sourceOldValue = SerializeTag(sourceTag);
        sourceTag.DeletedAt = DateTimeOffset.UtcNow;
        sourceTag.DeletedBy = mergedByUserId;
        sourceTag.Status = EntityStatus.Inactive;
        sourceTag.DateLastMaint = DateTimeOffset.UtcNow;

        await _tagRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.TagMerged,
            mergedByUserId,
            ipAddress,
            userAgent,
            entityId: targetTag.Id,
            details: $"Tag '{sourceTag.Name}' (ID:{sourceTag.Id}) merged into '{targetTag.Name}' (ID:{targetTag.Id}). " +
                      $"Posts moved: {postsMerged}, Questions moved: {questionsMerged}.",
            oldValue: sourceOldValue,
            newValue: SerializeTag(targetTag),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new MergeTagResponse
        {
            SourceTagId = sourceTag.Id,
            SourceTagName = sourceTag.Name,
            TargetTagId = targetTag.Id,
            TargetTagName = targetTag.Name,
            PostsMerged = postsMerged,
            QuestionsMerged = questionsMerged
        };
    }

    #endregion

    #region Private Helpers

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Trim()
            .Replace(" ", "-");

        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (slug.Length > 80)
            slug = slug[..80].Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "tag" : slug;
    }

    private static TagResponse ToResponse(Tag tag) =>
        new()
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            Description = tag.Description,
            IsActive = tag.Status == EntityStatus.Active,
            PostCount = tag.PostCount,
            QuestionCount = tag.QuestionCount,
            CreatedAt = tag.EffDate,
            UpdatedAt = tag.DateLastMaint
        };

    private static string SerializeTag(Tag tag) =>
        JsonSerializer.Serialize(tag, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

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
                entityType: nameof(Tag),
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
