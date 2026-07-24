using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

public sealed class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "name",
        "createdAt",
        "updatedAt"
    ];

    public async Task<PaginatedResponse<CategoryListItemResponse>> GetPagedAsync(
        CategoryFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.PostCategories
            .AsNoTracking()
            .Include(c => c.Parent)
            .Include(c => c.Children.Where(child => child.DeletedAt == null))
            .Include(c => c.Posts.Where(p => p.DeletedAt == null))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim().ToLowerInvariant();
            var isNpgsql = !(context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isNpgsql)
            {
                query = query.Where(c =>
                    EF.Functions.Like(c.Name.ToLower(), $"%{term}%") ||
                    (c.Description != null && EF.Functions.Like(c.Description.ToLower(), $"%{term}%")));
            }
            else
            {
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)));
            }
        }

        if (filter.IsActive.HasValue)
        {
            var targetStatus = filter.IsActive.Value ? EntityStatus.Active : EntityStatus.Inactive;
            query = query.Where(c => c.Status == targetStatus);
        }

        if (filter.ParentId.HasValue)
        {
            if (filter.ParentId.Value == 0)
                query = query.Where(c => c.ParentId == null);
            else
                query = query.Where(c => c.ParentId == filter.ParentId.Value);
        }

        query = query.Where(c => c.DeletedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryListItemResponse
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ParentId = c.ParentId,
                ParentName = c.Parent != null ? c.Parent.Name : null,
                IsActive = c.Status == EntityStatus.Active,
                PostCount = c.Posts.Count,
                ChildCount = c.Children.Count,
                CreatedAt = c.EffDate,
                UpdatedAt = c.DateLastMaint
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<CategoryListItemResponse>
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

    public async Task<CategoryDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var category = await context.PostCategories
            .AsNoTracking()
            .Include(c => c.Parent)
            .Include(c => c.Children.Where(child => child.DeletedAt == null && child.Status == EntityStatus.Active))
            .Include(c => c.Posts.Where(p => p.DeletedAt == null))
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);

        if (category is null)
            return null;

        return new CategoryDetailResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            ParentName = category.Parent?.Name,
            IsActive = category.Status == EntityStatus.Active,
            PostCount = category.Posts.Count,
            ChildCount = category.Children.Count,
            CreatedAt = category.EffDate,
            UpdatedAt = category.DateLastMaint,
            Children = category.Children
                .OrderBy(c => c.Name)
                .Select(c => new CategoryItemResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    IsActive = c.Status == EntityStatus.Active
                })
                .ToList()
        };
    }

    public async Task<PostCategory?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.PostCategories
            .Include(c => c.Children.Where(child => child.DeletedAt == null))
            .Include(c => c.Posts.Where(p => p.DeletedAt == null))
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
    }

    public async Task<PostCategory?> GetActiveByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.PostCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.DeletedAt == null &&
                c.Status == EntityStatus.Active,
                cancellationToken);
    }

    public async Task<bool> ExistsSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await context.PostCategories
            .AsNoTracking()
            .AnyAsync(c =>
                c.Slug.ToLower() == normalizedSlug &&
                c.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> ExistsSlugExcludingIdAsync(
        long id,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await context.PostCategories
            .AsNoTracking()
            .AnyAsync(c =>
                c.Id != id &&
                c.Slug.ToLower() == normalizedSlug &&
                c.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.PostCategories
            .AsNoTracking()
            .AnyAsync(c =>
                c.ParentId == id &&
                c.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> HasPostsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Posts
            .AsNoTracking()
            .AnyAsync(p =>
                p.CategoryId == id &&
                p.DeletedAt == null,
                cancellationToken);
    }

    public async Task AddAsync(PostCategory category, CancellationToken cancellationToken = default)
    {
        await context.PostCategories.AddAsync(category, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    private static IQueryable<PostCategory> ApplySorting(
        IQueryable<PostCategory> query,
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
                ? query.OrderBy(c => c.Name)
                : query.OrderByDescending(c => c.Name),
            "updatedAt" => direction == SortDirection.Asc
                ? query.OrderBy(c => c.DateLastMaint)
                : query.OrderByDescending(c => c.DateLastMaint),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(c => c.EffDate)
                : query.OrderByDescending(c => c.EffDate)
        };

        return query;
    }
}
