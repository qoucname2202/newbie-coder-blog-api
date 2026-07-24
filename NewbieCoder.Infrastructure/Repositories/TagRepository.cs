using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

public sealed class TagRepository(AppDbContext context) : ITagRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "name",
        "createdAt",
        "updatedAt",
        "postCount",
        "questionCount"
    ];

    public async Task<PaginatedResponse<TagListItemResponse>> GetPagedAsync(
        TagFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.Tags
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim().ToLowerInvariant();
            var isNpgsql = !(context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isNpgsql)
            {
                query = query.Where(t =>
                    EF.Functions.Like(t.Name.ToLower(), $"%{term}%") ||
                    (t.Description != null && EF.Functions.Like(t.Description.ToLower(), $"%{term}%")));
            }
            else
            {
                query = query.Where(t =>
                    t.Name.ToLower().Contains(term) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)));
            }
        }

        if (filter.IsActive.HasValue)
        {
            var targetStatus = filter.IsActive.Value ? EntityStatus.Active : EntityStatus.Inactive;
            query = query.Where(t => t.Status == targetStatus);
        }

        query = query.Where(t => t.DeletedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TagListItemResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description,
                IsActive = t.Status == EntityStatus.Active,
                PostCount = t.PostCount,
                QuestionCount = t.QuestionCount,
                CreatedAt = t.EffDate,
                UpdatedAt = t.DateLastMaint
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<TagListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<TagDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var tag = await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);

        if (tag is null)
            return null;

        return new TagDetailResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            Description = tag.Description,
            IsActive = tag.Status == EntityStatus.Active,
            PostCount = tag.PostCount,
            QuestionCount = tag.QuestionCount,
            FollowerCount = tag.FollowerCount,
            CreatedAt = tag.EffDate,
            UpdatedAt = tag.DateLastMaint
        };
    }

    public async Task<Tag?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);
    }

    public async Task<Tag?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt != null, cancellationToken);
    }

    public async Task<Tag?> GetActiveByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.DeletedAt == null &&
                t.Status == EntityStatus.Active,
                cancellationToken);
    }

    public async Task<bool> ExistsSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await context.Tags
            .AsNoTracking()
            .AnyAsync(t =>
                t.Slug.ToLower() == normalizedSlug &&
                t.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> ExistsSlugExcludingIdAsync(
        long id,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await context.Tags
            .AsNoTracking()
            .AnyAsync(t =>
                t.Id != id &&
                t.Slug.ToLower() == normalizedSlug &&
                t.DeletedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PostTag>> GetPostTagsByTagIdAsync(long tagId, CancellationToken cancellationToken = default)
    {
        return await context.PostTags
            .Where(pt => pt.TagId == tagId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewQuestionTag>> GetInterviewQuestionTagsByTagIdAsync(long tagId, CancellationToken cancellationToken = default)
    {
        return await context.InterviewQuestionTags
            .Where(iqt => iqt.TagId == tagId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityQuestionTag>> GetCommunityQuestionTagsByTagIdAsync(long tagId, CancellationToken cancellationToken = default)
    {
        return await context.CommunityQuestionTags
            .Where(cqt => cqt.TagId == tagId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPostTagAsync(long tagId, long postId, CancellationToken cancellationToken = default)
    {
        return await context.PostTags
            .AsNoTracking()
            .AnyAsync(pt => pt.TagId == tagId && pt.PostId == postId, cancellationToken);
    }

    public async Task<bool> HasInterviewQuestionTagAsync(long tagId, long questionId, CancellationToken cancellationToken = default)
    {
        return await context.InterviewQuestionTags
            .AsNoTracking()
            .AnyAsync(iqt => iqt.TagId == tagId && iqt.QuestionId == questionId, cancellationToken);
    }

    public async Task<bool> HasCommunityQuestionTagAsync(long tagId, long questionId, CancellationToken cancellationToken = default)
    {
        return await context.CommunityQuestionTags
            .AsNoTracking()
            .AnyAsync(cqt => cqt.TagId == tagId && cqt.QuestionId == questionId, cancellationToken);
    }

    public Task AddPostTagAsync(PostTag postTag, CancellationToken cancellationToken = default)
    {
        context.PostTags.Add(postTag);
        return Task.CompletedTask;
    }

    public Task AddInterviewQuestionTagAsync(InterviewQuestionTag iqt, CancellationToken cancellationToken = default)
    {
        context.InterviewQuestionTags.Add(iqt);
        return Task.CompletedTask;
    }

    public Task AddCommunityQuestionTagAsync(CommunityQuestionTag cqt, CancellationToken cancellationToken = default)
    {
        context.CommunityQuestionTags.Add(cqt);
        return Task.CompletedTask;
    }

    public void RemovePostTag(PostTag postTag) => context.PostTags.Remove(postTag);
    public void RemoveInterviewQuestionTag(InterviewQuestionTag iqt) => context.InterviewQuestionTags.Remove(iqt);
    public void RemoveCommunityQuestionTag(CommunityQuestionTag cqt) => context.CommunityQuestionTags.Remove(cqt);

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await context.Tags.AddAsync(tag, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    private static IQueryable<Tag> ApplySorting(
        IQueryable<Tag> query,
        string? sortBy,
        string? sortDirection)
    {
        var field = (sortBy ?? "createdAt").ToLowerInvariant();
        var direction = sortDirection?.ToLowerInvariant() == "asc"
            ? SortDirection.Asc
            : SortDirection.Desc;

        if (!AllowedSortFields.Contains(field))
            field = "createdAt";

        query = field switch
        {
            "name" => direction == SortDirection.Asc
                ? query.OrderBy(t => t.Name)
                : query.OrderByDescending(t => t.Name),
            "updatedAt" => direction == SortDirection.Asc
                ? query.OrderBy(t => t.DateLastMaint)
                : query.OrderByDescending(t => t.DateLastMaint),
            "postCount" => direction == SortDirection.Asc
                ? query.OrderBy(t => t.PostCount)
                : query.OrderByDescending(t => t.PostCount),
            "questionCount" => direction == SortDirection.Asc
                ? query.OrderBy(t => t.QuestionCount)
                : query.OrderByDescending(t => t.QuestionCount),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(t => t.EffDate)
                : query.OrderByDescending(t => t.EffDate)
        };

        return query;
    }
}
