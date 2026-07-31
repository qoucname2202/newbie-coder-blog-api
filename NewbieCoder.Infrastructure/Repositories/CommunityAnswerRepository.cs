using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

public sealed class CommunityAnswerRepository : ICommunityAnswerRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "createdAt",
        "updatedAt",
        "voteScore"
    ];

    private const int ContentPreviewMaxLength = 200;

    private readonly AppDbContext _context;

    public CommunityAnswerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<CommunityAnswerListItemResponse>> GetPagedAsync(
        GetCommunityAnswersRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CommunityAnswers
            .AsNoTracking()
            .Include(a => a.Question)
            .Include(a => a.Author)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim();
            var isNpgsql = !(_context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isNpgsql)
            {
                query = query.Where(a =>
                    EF.Functions.ILike(a.Content, $"%{term}%") ||
                    EF.Functions.ILike(a.Question.Title, $"%{term}%") ||
                    EF.Functions.ILike(a.Author.FullName, $"%{term}%") ||
                    EF.Functions.ILike(a.Author.Email, $"%{term}%"));
            }
            else
            {
                var lowerTerm = term.ToLowerInvariant();
                query = query.Where(a =>
                    a.Content.ToLower().Contains(lowerTerm) ||
                    a.Question.Title.ToLower().Contains(lowerTerm) ||
                    a.Author.FullName.ToLower().Contains(lowerTerm) ||
                    a.Author.Email.ToLower().Contains(lowerTerm));
            }
        }

        if (filter.QuestionId.HasValue)
            query = query.Where(a => a.QuestionId == filter.QuestionId.Value);

        if (filter.UserId.HasValue)
            query = query.Where(a => a.AuthorId == filter.UserId.Value);

        if (filter.IsHidden.HasValue)
            query = query.Where(a => a.IsHidden == filter.IsHidden.Value);

        if (filter.IsAccepted.HasValue)
            query = query.Where(a => a.IsAccepted == filter.IsAccepted.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.EffDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(a => a.EffDate <= filter.ToDate.Value);

        if (!filter.IncludeDeleted)
            query = query.Where(a => a.DeletedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var page = filter.Page < 1 ? PagingDefaults.DefaultPage : filter.Page;
        var pageSize = filter.PageSize < 1 ? PagingDefaults.DefaultPageSize
            : filter.PageSize > PagingDefaults.MaxPageSize ? PagingDefaults.MaxPageSize
            : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new CommunityAnswerListItemResponse
            {
                Id = a.Id,
                ContentPreview = TruncateContent(a.Content, ContentPreviewMaxLength),
                Question = new CommunityAnswerQuestionSummaryResponse
                {
                    Id = a.Question.Id,
                    Title = a.Question.Title,
                    Status = a.Question.Status.ToString()
                },
                Author = new CommunityAnswerAuthorResponse
                {
                    Id = a.Author.Id,
                    FullName = a.Author.FullName,
                    AvatarUrl = a.Author.AvatarUrl
                },
                IsHidden = a.IsHidden,
                IsAccepted = a.IsAccepted,
                VoteScore = a.VoteScore,
                CreatedAt = a.EffDate,
                UpdatedAt = a.DateLastMaint,
                DeletedAt = a.DeletedAt
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<CommunityAnswerListItemResponse>
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

    public async Task<CommunityAnswerDetailResponse?> GetDetailByIdAsync(
        long id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CommunityAnswers
            .AsNoTracking()
            .Include(a => a.Question)
            .Include(a => a.Author)
            .AsQueryable();

        if (!includeDeleted)
            query = query.Where(a => a.DeletedAt == null);

        var answer = await query.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (answer is null)
            return null;

        return new CommunityAnswerDetailResponse
        {
            Id = answer.Id,
            Content = answer.Content,
            Question = new CommunityAnswerQuestionDetailResponse
            {
                Id = answer.Question.Id,
                Title = answer.Question.Title,
                Status = answer.Question.Status.ToString(),
                IsLocked = answer.Question.ClosedAt != null,
                DeletedAt = answer.Question.DeletedAt
            },
            Author = new CommunityAnswerAuthorDetailResponse
            {
                Id = answer.Author.Id,
                FullName = answer.Author.FullName,
                Email = answer.Author.Email,
                AvatarUrl = answer.Author.AvatarUrl,
                IsActive = answer.Author.Status == UserStatus.Active
            },
            IsHidden = answer.IsHidden,
            IsAccepted = answer.IsAccepted,
            VoteScore = answer.VoteScore,
            CreatedAt = answer.EffDate,
            UpdatedAt = answer.DateLastMaint,
            DeletedAt = answer.DeletedAt,
            DeletedBy = answer.DeletedBy
        };
    }

    public async Task<CommunityAnswer?> GetTrackedByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityAnswers
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, cancellationToken);
    }

    public async Task<CommunityAnswer?> GetTrackedDeletedByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityAnswers
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt != null, cancellationToken);
    }

    public async Task<CommunityAnswer?> GetTrackedAnyByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityAnswers
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAnyAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityAnswers
            .AsNoTracking()
            .AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> QuestionExistsAsync(
        long questionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityQuestions
            .AsNoTracking()
            .AnyAsync(q => q.Id == questionId && q.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> AuthorExistsAsync(
        long authorId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == authorId, cancellationToken);
    }

    public async Task<CommunityAnswer> CreateAsync(
        CommunityAnswer answer,
        CancellationToken cancellationToken = default)
    {
        _context.CommunityAnswers.Add(answer);
        await _context.SaveChangesAsync(cancellationToken);
        return answer;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<CommunityAnswer> ApplySorting(
        IQueryable<CommunityAnswer> query,
        string? sortBy,
        string? sortDirection)
    {
        var field = (sortBy ?? "createdAt").ToLowerInvariant();
        var direction = sortDirection?.ToLowerInvariant() == "asc"
            ? SortDirection.Asc
            : SortDirection.Desc;

        if (!AllowedSortFields.Contains(field))
            field = "createdAt";

        return field switch
        {
            "createdAt" => direction == SortDirection.Asc
                ? query.OrderBy(a => a.EffDate)
                : query.OrderByDescending(a => a.EffDate),
            "updatedAt" => direction == SortDirection.Asc
                ? query.OrderBy(a => a.DateLastMaint)
                : query.OrderByDescending(a => a.DateLastMaint),
            "voteScore" => direction == SortDirection.Asc
                ? query.OrderBy(a => a.VoteScore)
                : query.OrderByDescending(a => a.VoteScore),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(a => a.EffDate)
                : query.OrderByDescending(a => a.EffDate)
        };
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        var lastSpace = content.LastIndexOf(' ', maxLength - 1, maxLength);
        var cutPoint = lastSpace > 0 ? lastSpace : maxLength;
        return content[..cutPoint] + "...";
    }
}
