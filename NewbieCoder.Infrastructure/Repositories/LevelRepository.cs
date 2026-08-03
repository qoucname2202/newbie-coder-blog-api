using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Level data access operations.
/// </summary>
public sealed class LevelRepository : ILevelRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "name",
        "code",
        "displayOrder",
        "createdAt"
    ];

    private readonly AppDbContext _context;

    public LevelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<LevelListItemResponse>> GetPagedAsync(
        LevelFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Levels
            .AsNoTracking()
            .Include(l => l.InterviewQuestions.Where(q => q.DeletedAt == null))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim().ToLowerInvariant();
            var isNpgsql = !(_context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isNpgsql)
            {
                query = query.Where(l =>
                    EF.Functions.ILike(l.Code, $"%{term}%") ||
                    EF.Functions.ILike(l.Name, $"%{term}%") ||
                    (l.Description != null && EF.Functions.ILike(l.Description, $"%{term}%")));
            }
            else
            {
                query = query.Where(l =>
                    l.Code.ToLower().Contains(term) ||
                    l.Name.ToLower().Contains(term) ||
                    (l.Description != null && l.Description.ToLower().Contains(term)));
            }
        }

        if (filter.IsActive.HasValue)
            query = query.Where(l => l.IsActive == filter.IsActive.Value);

        query = query.Where(l => l.DeletedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LevelListItemResponse
            {
                Id = l.Id,
                Code = l.Code,
                Name = l.Name,
                Description = l.Description,
                DisplayOrder = l.DisplayOrder,
                IsActive = l.IsActive,
                QuestionCount = l.InterviewQuestions.Count,
                CreatedAt = l.EffDate,
                UpdatedAt = l.DateLastMaint
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<LevelListItemResponse>
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

    public async Task<LevelDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var level = await _context.Levels
            .AsNoTracking()
            .Include(l => l.InterviewQuestions.Where(q => q.DeletedAt == null))
            .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null, cancellationToken);

        if (level is null)
            return null;

        return new LevelDetailResponse
        {
            Id = level.Id,
            Code = level.Code,
            Name = level.Name,
            Description = level.Description,
            DisplayOrder = level.DisplayOrder,
            IsActive = level.IsActive,
            QuestionCount = level.InterviewQuestions.Count,
            CreatedAt = level.EffDate,
            UpdatedAt = level.DateLastMaint,
            CreatedBy = null,
            UpdatedBy = null,
            DeletedAt = level.DeletedAt,
            DeletedBy = level.DeletedBy
        };
    }

    public async Task<Level?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Levels
            .Include(l => l.InterviewQuestions)
            .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null, cancellationToken);
    }

    public async Task<Level?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Levels
            .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt != null, cancellationToken);
    }

    public async Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.Levels
            .AsNoTracking()
            .AnyAsync(l =>
                l.Code.ToUpper() == normalizedCode &&
                l.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> ExistsCodeExcludingIdAsync(
        long id,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.Levels
            .AsNoTracking()
            .AnyAsync(l =>
                l.Id != id &&
                l.Code.ToUpper() == normalizedCode &&
                l.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> ExistsNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        return await _context.Levels
            .AsNoTracking()
            .AnyAsync(l =>
                l.Name.ToLower() == normalizedName &&
                l.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> ExistsNameExcludingIdAsync(
        long id,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        return await _context.Levels
            .AsNoTracking()
            .AnyAsync(l =>
                l.Id != id &&
                l.Name.ToLower() == normalizedName &&
                l.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> HasInterviewQuestionsAsync(long levelId, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewQuestions
            .AsNoTracking()
            .AnyAsync(q => q.LevelId == levelId && q.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(Level level, CancellationToken cancellationToken = default)
    {
        await _context.Levels.AddAsync(level, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<Level> ApplySorting(
        IQueryable<Level> query,
        string? sortBy,
        string? sortDirection)
    {
        var field = (sortBy ?? "createdAt").ToLowerInvariant();
        var isAsc = sortDirection?.ToLowerInvariant() == "asc";

        if (!AllowedSortFields.Contains(field))
            field = "createdAt";

        query = field switch
        {
            "name" => isAsc ? query.OrderBy(l => l.Name) : query.OrderByDescending(l => l.Name),
            "code" => isAsc ? query.OrderBy(l => l.Code) : query.OrderByDescending(l => l.Code),
            "displayOrder" => isAsc ? query.OrderBy(l => l.DisplayOrder) : query.OrderByDescending(l => l.DisplayOrder),
            _ => isAsc ? query.OrderBy(l => l.EffDate) : query.OrderByDescending(l => l.EffDate)
        };

        return query;
    }
}
