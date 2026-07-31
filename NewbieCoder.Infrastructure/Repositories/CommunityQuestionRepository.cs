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

public sealed class CommunityQuestionRepository : ICommunityQuestionRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "title",
        "status",
        "createdAt",
        "updatedAt",
        "viewCount",
        "answerCount",
        "voteScore",
        "bookmarkCount"
    ];

    private const int ContentPreviewMaxLength = 200;

    private readonly AppDbContext _context;

    public CommunityQuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<CommunityQuestionListItemResponse>> GetPagedAsync(
        GetCommunityQuestionsRequest filter,
        CancellationToken cancellationToken = default)
    {
        // Status filter must be applied first so the enum comparison never gets
        // mixed into a string ILIKE expression in PostgreSQL (Npgsql bug/limitation).
        // Apply keyword separately after so the enum stays in its own Where() clause.
        var query = _context.CommunityQuestions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.CommunityQuestionTags)
                .ThenInclude(cqt => cqt.Tag)
            .AsQueryable();

        // Status filter — separate Where() so enum never touches ILIKE in SQL
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<CommunityQuestionStatus>(filter.Status, ignoreCase: true, out var status))
                query = query.Where(q => q.Status == status);
        }

        // Keyword search: title, content, author username
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim();
            var isNpgsql = !(_context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isNpgsql)
            {
                query = query.Where(q =>
                    EF.Functions.ILike(q.Title, $"%{term}%") ||
                    EF.Functions.ILike(q.Content, $"%{term}%") ||
                    EF.Functions.ILike(q.Author.Username, $"%{term}%") ||
                    q.CommunityQuestionTags.Any(cqt => EF.Functions.ILike(cqt.Tag.Name, $"%{term}%")));
            }
            else
            {
                var lowerTerm = term.ToLowerInvariant();
                query = query.Where(q =>
                    q.Title.ToLower().Contains(lowerTerm) ||
                    q.Content.ToLower().Contains(lowerTerm) ||
                    q.Author.Username.ToLower().Contains(lowerTerm) ||
                    q.CommunityQuestionTags.Any(cqt => cqt.Tag.Name.ToLower().Contains(lowerTerm)));
            }
        }

        // Author filter
        if (filter.AuthorId.HasValue)
            query = query.Where(q => q.AuthorId == filter.AuthorId.Value);

        // Tag filter
        if (filter.TagId.HasValue)
            query = query.Where(q => q.CommunityQuestionTags.Any(cqt => cqt.TagId == filter.TagId.Value));

        // Has answers filter
        if (filter.HasAnswers.HasValue)
        {
            if (filter.HasAnswers.Value)
                query = query.Where(q => q.AnswerCount > 0);
            else
                query = query.Where(q => q.AnswerCount == 0);
        }

        // Created date range
        if (filter.FromDate.HasValue)
            query = query.Where(q => q.EffDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(q => q.EffDate <= filter.ToDate.Value);

        // Deleted filter
        if (!filter.IncludeDeleted)
            query = query.Where(q => q.DeletedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var page = filter.Page < 1 ? PagingDefaults.DefaultPage : filter.Page;
        var pageSize = filter.PageSize < 1 ? PagingDefaults.DefaultPageSize
            : filter.PageSize > PagingDefaults.MaxPageSize ? PagingDefaults.MaxPageSize
            : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new CommunityQuestionListItemResponse
            {
                Id = q.Id,
                Title = q.Title,
                Slug = q.Slug,
                ContentPreview = TruncateContent(q.Content, ContentPreviewMaxLength),
                Status = new StatusValueResponse
                {
                    Value = (int)q.Status,
                    Name = q.Status.ToString()
                },
                IsLocked = q.ClosedAt != null,
                ViewCount = q.ViewCount,
                AnswerCount = q.AnswerCount,
                VoteScore = q.VoteScore,
                BookmarkCount = q.BookmarkCount,
                AcceptedAnswerId = q.AcceptedAnswerId,
                ClosedAt = q.ClosedAt,
                CreatedAt = q.EffDate,
                UpdatedAt = q.DateLastMaint,
                DeletedAt = q.DeletedAt,
                IsDeleted = q.DeletedAt != null,
                Author = new AuthorSummaryResponse
                {
                    Id = q.Author.Id,
                    Username = q.Author.Username,
                    FullName = q.Author.FullName
                },
                Tags = q.CommunityQuestionTags
                    .Where(cqt => cqt.Tag.DeletedAt == null)
                    .Select(cqt => new CommunityQuestionTagResponse
                    {
                        Id = cqt.Tag.Id,
                        Name = cqt.Tag.Name,
                        Slug = cqt.Tag.Slug
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<CommunityQuestionListItemResponse>
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

    public async Task<CommunityQuestionDetailResponse?> GetDetailByIdAsync(
        long id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CommunityQuestions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.CommunityQuestionTags)
                .ThenInclude(cqt => cqt.Tag)
            .Include(q => q.Answers.Where(a => a.DeletedAt == null))
                .ThenInclude(a => a.Author)
            .AsQueryable();

        if (!includeDeleted)
            query = query.Where(q => q.DeletedAt == null);

        var question = await query.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (question is null)
            return null;

        return new CommunityQuestionDetailResponse
        {
            Id = question.Id,
            Title = question.Title,
            Slug = question.Slug,
            Content = question.Content,
            Status = new StatusValueResponse
            {
                Value = (int)question.Status,
                Name = question.Status.ToString()
            },
            IsLocked = question.ClosedAt != null,
            ViewCount = question.ViewCount,
            AnswerCount = question.AnswerCount,
            VoteScore = question.VoteScore,
            BookmarkCount = question.BookmarkCount,
            AcceptedAnswerId = question.AcceptedAnswerId,
            ClosedAt = question.ClosedAt,
            CreatedAt = question.EffDate,
            UpdatedAt = question.DateLastMaint,
            IsDeleted = question.DeletedAt != null,
            DeletedAt = question.DeletedAt,
            DeletedBy = question.DeletedBy,
            Author = new AuthorDetailResponse
            {
                Id = question.Author.Id,
                DisplayName = question.Author.FullName,
                Email = question.Author.Email,
                AvatarUrl = question.Author.AvatarUrl
            },
            Tags = question.CommunityQuestionTags
                .Where(cqt => cqt.Tag.DeletedAt == null)
                .Select(cqt => new CommunityQuestionTagResponse
                {
                    Id = cqt.Tag.Id,
                    Name = cqt.Tag.Name,
                    Slug = cqt.Tag.Slug
                }).ToList(),
            Answers = question.Answers
                .OrderBy(a => a.Id)
                .Select(a => new CommunityAnswerDetailItemResponse
                {
                    Id = a.Id,
                    Content = a.Content,
                    VoteScore = a.VoteScore,
                    IsAccepted = a.IsAccepted,
                    IsHidden = a.IsHidden,
                    CreatedAt = a.EffDate,
                    UpdatedAt = a.DateLastMaint,
                    Author = new AuthorSummaryResponse
                    {
                        Id = a.Author.Id,
                        Username = a.Author.Username,
                        FullName = a.Author.FullName
                    }
                }).ToList()
        };
    }

    public async Task<CommunityQuestion?> GetTrackedByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityQuestions
            .FirstOrDefaultAsync(q => q.Id == id && q.DeletedAt == null, cancellationToken);
    }

    public async Task<CommunityQuestion?> GetTrackedDeletedByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityQuestions
            .FirstOrDefaultAsync(q => q.Id == id && q.DeletedAt != null, cancellationToken);
    }

    public async Task<CommunityQuestion?> GetTrackedAnyByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityQuestions
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAnyAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityQuestions
            .AsNoTracking()
            .AnyAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<bool> AuthorExistsAsync(
        long authorId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == authorId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<bool> HasActiveAnswerAsync(
        long questionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommunityAnswers
            .AsNoTracking()
            .AnyAsync(a =>
                a.QuestionId == questionId &&
                a.DeletedAt == null &&
                a.IsHidden == false,
                cancellationToken);
    }

    public async Task<CommunityQuestion> CreateAsync(
        CommunityQuestion question,
        IReadOnlyList<long> tagIds,
        CancellationToken cancellationToken = default)
    {
        _context.CommunityQuestions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        if (tagIds.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var tagId in tagIds)
            {
                _context.CommunityQuestionTags.Add(new CommunityQuestionTag
                {
                    QuestionId = question.Id,
                    TagId = tagId,
                    EffDate = now
                });
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        return question;
    }

    private static IQueryable<CommunityQuestion> ApplySorting(
        IQueryable<CommunityQuestion> query,
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
            "title" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.Title).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.Title).ThenByDescending(q => q.Id),
            "status" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.Status).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.Status).ThenByDescending(q => q.Id),
            "createdAt" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.EffDate).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.EffDate).ThenByDescending(q => q.Id),
            "updatedAt" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.DateLastMaint).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.DateLastMaint).ThenByDescending(q => q.Id),
            "viewCount" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.ViewCount).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.ViewCount).ThenByDescending(q => q.Id),
            "answerCount" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.AnswerCount).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.AnswerCount).ThenByDescending(q => q.Id),
            "voteScore" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.VoteScore).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.VoteScore).ThenByDescending(q => q.Id),
            "bookmarkCount" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.BookmarkCount).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.BookmarkCount).ThenByDescending(q => q.Id),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(q => q.EffDate).ThenBy(q => q.Id)
                : query.OrderByDescending(q => q.EffDate).ThenByDescending(q => q.Id)
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
