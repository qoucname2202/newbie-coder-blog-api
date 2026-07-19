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

public sealed class AdminPostService : IAdminPostService
{
    private readonly AppDbContext _db;
    private readonly IPostRepository _postRepo;
    private readonly IAuditLogService _auditLog;

    public AdminPostService(
        AppDbContext db,
        IPostRepository postRepo,
        IAuditLogService auditLog)
    {
        _db = db;
        _postRepo = postRepo;
        _auditLog = auditLog;
    }

    #region GetPostsAsync

    public async Task<PaginatedResponse<AdminPostListItemResponse>> GetPostsAsync(
        GetAdminPostsRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _postRepo.GetPagedAsync(filter, cancellationToken);
    }

    #endregion

    #region GetPostByIdAsync

    public async Task<AdminPostDetailResponse> GetPostByIdAsync(
        long postId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _postRepo.GetDetailByIdAsync(postId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.PostNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.PostNotFound);

        return detail;
    }

    #endregion

    #region CreatePostAsync

    public async Task<CreateAdminPostResponse> CreatePostAsync(
        CreateAdminPostRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        // Determine author ID based on author source rules
        long authorId = ResolveAuthorId(request, createdByUserId);

        // Validate and load Author
        var author = await ValidateAuthorAsync(authorId, cancellationToken);

        // Validate Category
        PostCategory? category = null;
        if (request.CategoryId.HasValue)
        {
            category = await _postRepo.GetActiveCategoryAsync(request.CategoryId.Value, cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.CategoryNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.CategoryNotFound);
        }

        // Validate Tags
        var tags = await ValidateAndLoadTagsAsync(request.TagIds, cancellationToken);

        // Parse and validate status
        var status = ParseAndValidateStatus(request.Status, isCreate: true);

        // Parse and validate visibility
        var visibility = ParseAndValidateVisibility(request.Visibility);

        // Generate unique slug
        var slug = await GenerateUniqueSlugAsync(request.Title, excludePostId: null, cancellationToken);

        var post = new Post
        {
            AuthorId = authorId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Slug = slug,
            Summary = request.Summary?.Trim(),
            Content = request.Content.Trim(),
            ThumbnailUrl = request.ThumbnailUrl?.Trim(),
            Status = status,
            Visibility = visibility,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };

        // Set published fields if publishing
        if (status == PostStatus.Published)
        {
            post.PublishedAt = DateTimeOffset.UtcNow;
        }

        // Determine audit action
        string auditAction;
        string auditDetails;

        if (authorId != createdByUserId)
        {
            auditAction = AuditActions.PostCreatedForAuthor;
            auditDetails = $"Post '{post.Title}' created by admin {createdByUserId} for author {authorId}.";
        }
        else
        {
            auditAction = AuditActions.PostCreatedByAdmin;
            auditDetails = $"Post '{post.Title}' created by admin {createdByUserId}.";
        }

        // Execute in transaction
        await ExecuteInTransactionAsync(async () =>
        {
            await _postRepo.AddPostAsync(post, cancellationToken);

            if (tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    await _db.PostTags.AddAsync(new PostTag
                    {
                        PostId = post.Id,
                        TagId = tag.Id,
                        EffDate = DateTimeOffset.UtcNow
                    }, cancellationToken);
                }
            }

            await _postRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            auditAction,
            createdByUserId,
            ipAddress,
            userAgent,
            entityId: post.Id,
            details: auditDetails,
            newValue: SerializePost(post),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new CreateAdminPostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            ThumbnailUrl = post.ThumbnailUrl,
            Status = post.Status.ToString(),
            Visibility = post.Visibility.ToString(),
            ViewCount = post.ViewCount,
            CommentCount = post.CommentCount,
            CreatedAt = post.EffDate,
            PublishedAt = post.PublishedAt,
            Author = author != null ? new AuthorSummaryResponse
            {
                Id = author.Id,
                Username = author.Username,
                FullName = author.FullName
            } : null,
            Category = category != null ? new CategorySummaryResponse
            {
                Id = category.Id,
                Name = category.Name
            } : null,
            Tags = tags.Select(t => new TagSummaryResponse
            {
                Id = t.Id,
                Name = t.Name
            }).ToList()
        };
    }

    #endregion

    #region UpdatePostAsync

    public async Task<UpdateAdminPostResponse> UpdatePostAsync(
        long postId,
        UpdateAdminPostRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var post = await _postRepo.GetTrackedByIdAsync(postId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.PostNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.PostNotFound);

        var oldData = SerializePost(post);

        // Resolve and validate author
        long authorId = ResolveUpdateAuthorId(request, post.AuthorId, updatedByUserId);
        var author = await ValidateAuthorAsync(authorId, cancellationToken);

        // Validate Category
        PostCategory? category = null;
        if (request.CategoryId.HasValue)
        {
            category = await _postRepo.GetActiveCategoryAsync(request.CategoryId.Value, cancellationToken)
                ?? throw new BusinessException(
                    ResponseMessages.CategoryNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.CategoryNotFound);
        }

        // Validate Tags
        var tags = await ValidateAndLoadTagsAsync(request.TagIds, cancellationToken);

        // Parse and validate status
        var newStatus = ParseAndValidateStatus(request.Status, isCreate: false);

        // Parse and validate visibility
        var newVisibility = ParseAndValidateVisibility(request.Visibility);

        // Validate status transition
        ValidateStatusTransition(post.Status, newStatus);

        // Generate new slug if title changed
        var newSlug = post.Slug;
        if (!string.Equals(post.Title.Trim(), request.Title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            newSlug = await GenerateUniqueSlugAsync(request.Title, postId, cancellationToken);
        }

        var oldStatus = post.Status;

        // Update post fields
        post.AuthorId = authorId;
        post.CategoryId = request.CategoryId;
        post.Title = request.Title.Trim();
        post.Slug = newSlug;
        post.Summary = request.Summary?.Trim();
        post.Content = request.Content.Trim();
        post.ThumbnailUrl = request.ThumbnailUrl?.Trim();
        post.Status = newStatus;
        post.Visibility = newVisibility;
        post.DateLastMaint = DateTimeOffset.UtcNow;

        // Handle published transition
        if (newStatus == PostStatus.Published && oldStatus != PostStatus.Published)
        {
            post.PublishedAt = DateTimeOffset.UtcNow;
        }
        else if (newStatus != PostStatus.Published && oldStatus == PostStatus.Published)
        {
            post.PublishedAt = null;
        }

        // Execute in transaction: update tags + save
        await ExecuteInTransactionAsync(async () =>
        {
            await _postRepo.ReplacePostTagsAsync(post, tags, cancellationToken);
            await _postRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var newData = SerializePost(post);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.PostUpdatedByAdmin,
            updatedByUserId,
            ipAddress,
            userAgent,
            entityId: post.Id,
            details: $"Post '{post.Title}' updated by admin {updatedByUserId}.",
            oldValue: oldData,
            newValue: newData,
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new UpdateAdminPostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            ThumbnailUrl = post.ThumbnailUrl,
            Status = post.Status.ToString(),
            Visibility = post.Visibility.ToString(),
            ViewCount = post.ViewCount,
            CommentCount = post.CommentCount,
            UpdatedAt = post.DateLastMaint
        };
    }

    #endregion

    #region DeletePostAsync

    public async Task<DeleteAdminPostResponse> DeletePostAsync(
        long postId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        // Check if post exists at all
        var exists = await _postRepo.ExistsPostAsync(postId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.PostNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.PostNotFound);

        var post = await _postRepo.GetTrackedByIdAsync(postId, cancellationToken);

        if (post == null)
            throw new BusinessException(
                ResponseMessages.PostAlreadyDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.PostAlreadyDeleted);

        var oldData = SerializePost(post);

        await ExecuteInTransactionAsync(async () =>
        {
            post.DeletedAt = DateTimeOffset.UtcNow;
            post.DeletedBy = deletedByUserId;
            post.Status = PostStatus.Deleted;
            post.DateLastMaint = DateTimeOffset.UtcNow;
            await _postRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.PostDeletedByAdmin,
            deletedByUserId,
            ipAddress,
            userAgent,
            entityId: post.Id,
            details: $"Post '{post.Title}' soft-deleted by admin {deletedByUserId}.",
            oldValue: oldData,
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new DeleteAdminPostResponse
        {
            Id = post.Id,
            Status = post.Status.ToString(),
            DeletedAt = post.DeletedAt
        };
    }

    #endregion

    #region RestorePostAsync

    public async Task<RestoreAdminPostResponse> RestorePostAsync(
        long postId,
        long restoredByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _postRepo.ExistsPostAsync(postId, cancellationToken);
        if (!exists)
            throw new BusinessException(
                ResponseMessages.PostNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.PostNotFound);

        var post = await _postRepo.GetTrackedByIdAsync(postId, cancellationToken);

        if (post != null && post.DeletedAt == null)
            throw new BusinessException(
                ResponseMessages.PostNotDeleted,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.PostNotDeleted);

        // Post exists but is not found via GetTrackedByIdAsync — it must be deleted
        post = await _postRepo.GetTrackedDeletedByIdAsync(postId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.PostNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.PostNotFound);

        var oldData = SerializePost(post);

        await ExecuteInTransactionAsync(async () =>
        {
            post.DeletedAt = null;
            post.DeletedBy = null;
            post.Status = PostStatus.Draft;
            post.DateLastMaint = DateTimeOffset.UtcNow;
            await _postRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.PostRestoredByAdmin,
            restoredByUserId,
            ipAddress,
            userAgent,
            entityId: post.Id,
            details: $"Post '{post.Title}' restored by admin {restoredByUserId}.",
            oldValue: oldData,
            newValue: SerializePost(post),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new RestoreAdminPostResponse
        {
            Id = post.Id,
            Status = post.Status.ToString(),
            IsDeleted = post.DeletedAt != null,
            UpdatedAt = post.DateLastMaint
        };
    }

    #endregion

    #region Private Helpers

    private static long ResolveAuthorId(CreateAdminPostRequest request, long adminUserId)
    {
        return request.AuthorId ?? adminUserId;
    }

    private static long ResolveUpdateAuthorId(
        UpdateAdminPostRequest request,
        long currentAuthorId,
        long adminUserId)
    {
        return request.AuthorId ?? currentAuthorId;
    }

    private async Task<User> ValidateAuthorAsync(
        long authorId,
        CancellationToken cancellationToken)
    {
        var author = await _postRepo.GetAuthorByIdAsync(authorId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.AuthorNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.AuthorNotFound);

        if (author.Status == UserStatus.Inactive)
            throw new BusinessException(
                ResponseMessages.AuthorInactive,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.AuthorInactive);

        if (author.Status == UserStatus.Locked)
            throw new BusinessException(
                ResponseMessages.AuthorLocked,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.AuthorLocked);

        return author;
    }

    private async Task<IReadOnlyList<Tag>> ValidateAndLoadTagsAsync(
        IReadOnlyList<long>? tagIds,
        CancellationToken cancellationToken)
    {
        if (tagIds is null || tagIds.Count == 0)
            return [];

        // Check for duplicates
        if (tagIds.Distinct().Count() != tagIds.Count)
            throw new BusinessException(
                ResponseMessages.InvalidTagIds,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidTagIds);

        var tags = await _postRepo.GetActiveTagsAsync(tagIds, cancellationToken);

        if (tags.Count != tagIds.Count)
        {
            var foundIds = tags.Select(t => t.Id).ToHashSet();
            var missingIds = tagIds.Where(id => !foundIds.Contains(id)).ToList();

            throw new BusinessException(
                missingIds.All(id => !foundIds.Contains(id))
                    ? ResponseMessages.TagNotFound
                    : ResponseMessages.TagNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.TagNotFound);
        }

        return tags;
    }

    private static PostStatus ParseAndValidateStatus(string statusString, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(statusString))
            return PostStatus.Draft;

        if (!Enum.TryParse<PostStatus>(statusString, ignoreCase: true, out var status))
            throw new BusinessException(
                ResponseMessages.InvalidPostStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidPostStatus);

        return status;
    }

    private static PostVisibility ParseAndValidateVisibility(string visibilityString)
    {
        if (string.IsNullOrWhiteSpace(visibilityString))
            return PostVisibility.Public;

        if (!Enum.TryParse<PostVisibility>(visibilityString, ignoreCase: true, out var visibility))
            throw new BusinessException(
                ResponseMessages.InvalidPostVisibility,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.InvalidPostVisibility);

        return visibility;
    }

    private static void ValidateStatusTransition(PostStatus currentStatus, PostStatus newStatus)
    {
        // All transitions are allowed for admin
        _ = currentStatus;
        _ = newStatus;
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string title,
        long? excludePostId = null,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = GenerateSlug(title);
        var slug = baseSlug;
        var counter = 1;

        bool exists;
        if (excludePostId.HasValue)
            exists = await _postRepo.ExistsSlugExcludingIdAsync(excludePostId.Value, slug, cancellationToken);
        else
            exists = await _postRepo.ExistsSlugAsync(slug, cancellationToken);

        while (exists)
        {
            slug = $"{baseSlug}-{counter}";
            counter++;

            if (excludePostId.HasValue)
                exists = await _postRepo.ExistsSlugExcludingIdAsync(excludePostId.Value, slug, cancellationToken);
            else
                exists = await _postRepo.ExistsSlugAsync(slug, cancellationToken);
        }

        return slug;
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "post";

        // Convert to lowercase
        var slug = title.ToLowerInvariant().Trim();

        // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"\s+", "-");

        // Remove invalid URL characters (keep only alphanumeric, hyphens, and underscores)
        slug = Regex.Replace(slug, @"[^a-z0-9\-_]", "");

        // Replace multiple hyphens/underscores with single hyphen
        slug = Regex.Replace(slug, @"-+", "-");
        slug = Regex.Replace(slug, @"_+", "_");
        slug = slug.Trim('-', '_');

        // Limit length
        if (slug.Length > 200)
            slug = slug[..200].Trim('-', '_');

        return string.IsNullOrWhiteSpace(slug) ? "post" : slug;
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
            post.Status,
            AuthorId = post.AuthorId,
            CategoryId = post.CategoryId,
            IsFeatured = post.ViewCount > 0,
            post.PublishedAt,
            post.EffDate
        });

    #endregion
}
