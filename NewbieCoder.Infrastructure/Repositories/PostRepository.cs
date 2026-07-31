using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

public sealed class PostRepository(AppDbContext context) : IPostRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "title",
        "createdAt",
        "updatedAt",
        "publishedAt",
        "viewCount",
        "commentCount"
    ];

    public async Task<PaginatedResponse<AdminPostListItemResponse>> GetPagedAsync(
        GetAdminPostsRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Category)
            .AsQueryable();

        // Keyword search on title, slug, summary, and author name
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim().ToLowerInvariant();
            query = query.Where(p =>
                EF.Functions.Like(p.Title.ToLower(), $"%{term}%") ||
                EF.Functions.Like(p.Slug.ToLower(), $"%{term}%") ||
                (p.Summary != null && EF.Functions.Like(p.Summary.ToLower(), $"%{term}%")) ||
                EF.Functions.Like(p.Author.Username.ToLower(), $"%{term}%") ||
                EF.Functions.Like(p.Author.FullName.ToLower(), $"%{term}%"));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<PostStatus>(filter.Status, ignoreCase: true, out var status))
                query = query.Where(p => p.Status == status);
        }

        // Author filter
        if (filter.AuthorId.HasValue)
            query = query.Where(p => p.AuthorId == filter.AuthorId.Value);

        // Category filter
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        // Tag filter
        if (filter.TagId.HasValue)
            query = query.Where(p => p.PostTags.Any(pt => pt.TagId == filter.TagId.Value));

        // Featured filter
        if (filter.IsFeatured.HasValue)
            query = query.Where(p => p.ViewCount > 0 == filter.IsFeatured.Value);

        // Created date range
        if (filter.CreatedFrom.HasValue)
            query = query.Where(p => p.EffDate >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue)
            query = query.Where(p => p.EffDate <= filter.CreatedTo.Value);

        // Published date range
        if (filter.PublishedFrom.HasValue)
            query = query.Where(p => p.PublishedAt >= filter.PublishedFrom.Value);
        if (filter.PublishedTo.HasValue)
            query = query.Where(p => p.PublishedAt <= filter.PublishedTo.Value);

        // Deleted filter — default excludes deleted unless includeDeleted=true
        if (!filter.IncludeDeleted)
            query = query.Where(p => p.DeletedAt == null);
        else
            query = query.Where(p => p.DeletedAt != null);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var page = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPostListItemResponse
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary,
                ThumbnailUrl = p.ThumbnailUrl,
                Status = p.Status.ToString(),
                Visibility = p.Visibility.ToString(),
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                VoteScore = p.VoteScore,
                BookmarkCount = p.BookmarkCount,
                CreatedAt = p.EffDate,
                UpdatedAt = p.DateLastMaint,
                PublishedAt = p.PublishedAt,
                Author = p.Author != null ? new AuthorSummaryResponse
                {
                    Id = p.Author.Id,
                    Username = p.Author.Username,
                    FullName = p.Author.FullName
                } : null,
                Category = p.Category != null ? new CategorySummaryResponse
                {
                    Id = p.Category.Id,
                    Name = p.Category.Name
                } : null
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<AdminPostListItemResponse>
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

    public async Task<AdminPostDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var post = await context.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (post is null)
            return null;

        return new AdminPostDetailResponse
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Content = post.Content,
            ThumbnailUrl = post.ThumbnailUrl,
            Status = post.Status.ToString(),
            Visibility = post.Visibility.ToString(),
            ViewCount = post.ViewCount,
            CommentCount = post.CommentCount,
            VoteScore = post.VoteScore,
            BookmarkCount = post.BookmarkCount,
            CreatedAt = post.EffDate,
            UpdatedAt = post.DateLastMaint,
            PublishedAt = post.PublishedAt,
            DeletedAt = post.DeletedAt,
            Author = post.Author != null ? new AuthorSummaryResponse
            {
                Id = post.Author.Id,
                Username = post.Author.Username,
                FullName = post.Author.FullName
            } : null,
            Category = post.Category != null ? new CategorySummaryResponse
            {
                Id = post.Category.Id,
                Name = post.Category.Name
            } : null,
            Tags = post.PostTags
                .Select(pt => new TagSummaryResponse
                {
                    Id = pt.Tag.Id,
                    Name = pt.Tag.Name
                })
                .ToList()
        };
    }

    public async Task<Post?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Posts
            .Include(p => p.PostTags)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);
    }

    public async Task<Post?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt != null, cancellationToken);
    }

    public async Task<User?> GetActiveAuthorAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.Status == UserStatus.Active &&
                u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetAuthorByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.DeletedAt == null, cancellationToken);
    }

    public async Task<PostCategory?> GetActiveCategoryAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        return await context.PostCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.Status == EntityStatus.Active &&
                c.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetActiveTagsAsync(
        IReadOnlyList<long> tagIds,
        CancellationToken cancellationToken = default)
    {
        return await context.Tags
            .AsNoTracking()
            .Where(t =>
                tagIds.Contains(t.Id) &&
                t.Status == EntityStatus.Active &&
                t.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return await context.Posts
            .AsNoTracking()
            .AnyAsync(p => p.Slug.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsSlugExcludingIdAsync(
        long id,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return await context.Posts
            .AsNoTracking()
            .AnyAsync(p => p.Id != id && p.Slug.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsPostAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Posts
            .AsNoTracking()
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddPostAsync(Post post, CancellationToken cancellationToken = default)
    {
        await context.Posts.AddAsync(post, cancellationToken);
    }

    public async Task ReplacePostTagsAsync(
        Post post,
        IReadOnlyList<Tag> tags,
        CancellationToken cancellationToken = default)
    {
        // Remove existing tags
        var existingTags = post.PostTags.ToList();
        foreach (var et in existingTags)
            context.PostTags.Remove(et);

        // Add new tags
        foreach (var tag in tags)
        {
            await context.PostTags.AddAsync(new PostTag
            {
                PostId = post.Id,
                TagId = tag.Id,
                EffDate = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    private static IQueryable<Post> ApplySorting(
        IQueryable<Post> query,
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
            "title" => direction == SortDirection.Asc
                ? query.OrderBy(p => p.Title)
                : query.OrderByDescending(p => p.Title),
            "updatedAt" => direction == SortDirection.Asc
                ? query.OrderBy(p => p.DateLastMaint)
                : query.OrderByDescending(p => p.DateLastMaint),
            "publishedAt" => direction == SortDirection.Asc
                ? query.OrderBy(p => p.PublishedAt)
                : query.OrderByDescending(p => p.PublishedAt),
            "viewCount" => direction == SortDirection.Asc
                ? query.OrderBy(p => p.ViewCount)
                : query.OrderByDescending(p => p.ViewCount),
            "commentCount" => direction == SortDirection.Asc
                ? query.OrderBy(p => p.CommentCount)
                : query.OrderByDescending(p => p.CommentCount),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(p => p.EffDate)
                : query.OrderByDescending(p => p.EffDate)
        };

        return query;
    }
}
