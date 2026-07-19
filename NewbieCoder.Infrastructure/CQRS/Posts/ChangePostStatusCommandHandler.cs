using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.CQRS.Posts;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.CQRS.Posts;

/// <summary>
/// Handler for <see cref="ChangePostStatusCommand"/>.
/// </summary>
public sealed class ChangePostStatusCommandHandler
{
    private readonly AppDbContext _db;
    private readonly IPostRepository _postRepo;
    private readonly IAuditLogService _auditLog;

    private static readonly HashSet<PostStatus> SupportedStatuses = new()
    {
        PostStatus.Draft,
        PostStatus.Pending,
        PostStatus.Published,
        PostStatus.Hidden,
        PostStatus.Archived
    };

    private static readonly Dictionary<PostStatus, HashSet<PostStatus>> AllowedTransitions = new()
    {
        [PostStatus.Published] = [PostStatus.Hidden, PostStatus.Archived],
        [PostStatus.Hidden] = [PostStatus.Published],
        [PostStatus.Draft] = [PostStatus.Published],
    };

    public ChangePostStatusCommandHandler(
        AppDbContext db,
        IPostRepository postRepo,
        IAuditLogService auditLog)
    {
        _db = db;
        _postRepo = postRepo;
        _auditLog = auditLog;
    }

    public async Task<ChangePostStatusResponse> HandleAsync(
        ChangePostStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepo.GetTrackedByIdAsync(command.PostId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.PostNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.PostNotFound);

        if (post.DeletedAt != null)
            throw new BusinessException(
                ResponseMessages.PostNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.PostNotFound);

        if (!Enum.TryParse<PostStatus>(command.Request.Status, ignoreCase: true, out var targetStatus))
            throw new BusinessException(
                ResponseMessages.InvalidPostStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidPostStatus);

        if (!SupportedStatuses.Contains(targetStatus))
            throw new BusinessException(
                ResponseMessages.InvalidPostStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidPostStatus);

        if (post.Status == targetStatus)
            throw new BusinessException(
                ResponseMessages.PostAlreadyInTargetStatus,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.PostAlreadyInTargetStatus);

        if (!IsValidTransition(post.Status, targetStatus))
            throw new BusinessException(
                ResponseMessages.InvalidPostStatusTransition,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.InvalidPostStatusTransition);

        var previousStatus = post.Status;
        var oldData = SerializePost(post);

        await ExecuteInTransactionAsync(async () =>
        {
            post.Status = targetStatus;
            post.DateLastMaint = DateTimeOffset.UtcNow;

            if (targetStatus == PostStatus.Published && previousStatus != PostStatus.Published)
                post.PublishedAt = DateTimeOffset.UtcNow;
            else if (previousStatus == PostStatus.Published && targetStatus != PostStatus.Published)
                post.PublishedAt = null;

            await _postRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var newData = SerializePost(post);

        await SafeAuditLogAsync(
            AuditActions.PostVisibilityChanged,
            command.ChangedByUserId,
            command.IpAddress,
            command.UserAgent,
            entityId: post.Id,
            details: $"Post '{post.Title}' status changed from {previousStatus} to {targetStatus} by admin {command.ChangedByUserId}.",
            oldValue: oldData,
            newValue: newData,
            traceId: command.TraceId,
            cancellationToken: cancellationToken);

        return new ChangePostStatusResponse
        {
            PreviousStatus = previousStatus.ToString(),
            CurrentStatus = targetStatus.ToString(),
            UpdatedBy = command.ChangedByUserId
        };
    }

    private static bool IsValidTransition(PostStatus from, PostStatus to)
    {
        if (AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to))
            return true;

        return false;
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
                entityType: nameof(Post),
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

    private static string SerializePost(Post post) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            post.Id,
            post.Title,
            post.Slug,
            Status = post.Status.ToString()
        });
}
