using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

public sealed class InterviewQuestionRepository : IInterviewQuestionRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "title",
        "difficultyLevel",
        "status",
        "technology",
        "topic",
        "createdAt",
        "updatedAt",
        "answerCount"
    ];

    private readonly AppDbContext _context;

    public InterviewQuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<InterviewQuestionListItemResponse>> GetPagedAsync(
        GetInterviewQuestionsRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InterviewQuestions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.InterviewQuestionTags)
                .ThenInclude(iqt => iqt.Tag)
            .AsQueryable();

        // Keyword search
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim().ToLowerInvariant();
            var isNpgsql = !(_context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isNpgsql)
            {
                query = query.Where(q =>
                    EF.Functions.Like(q.Title.ToLower(), $"%{term}%") ||
                    EF.Functions.Like(q.QuestionContent.ToLower(), $"%{term}%") ||
                    (q.Explanation != null && EF.Functions.Like(q.Explanation.ToLower(), $"%{term}%")) ||
                    (q.Technology != null && EF.Functions.Like(q.Technology.ToLower(), $"%{term}%")) ||
                    (q.Topic != null && EF.Functions.Like(q.Topic.ToLower(), $"%{term}%")) ||
                    q.InterviewQuestionTags.Any(iqt => EF.Functions.Like(iqt.Tag.Name.ToLower(), $"%{term}%")));
            }
            else
            {
                query = query.Where(q =>
                    q.Title.ToLower().Contains(term) ||
                    q.QuestionContent.ToLower().Contains(term) ||
                    (q.Explanation != null && q.Explanation.ToLower().Contains(term)) ||
                    (q.Technology != null && q.Technology.ToLower().Contains(term)) ||
                    (q.Topic != null && q.Topic.ToLower().Contains(term)) ||
                    q.InterviewQuestionTags.Any(iqt => iqt.Tag.Name.ToLower().Contains(term)));
            }
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<PostStatus>(filter.Status, ignoreCase: true, out var status))
                query = query.Where(q => q.Status == status);
        }

        // Difficulty filter
        if (!string.IsNullOrWhiteSpace(filter.DifficultyLevel))
        {
            if (Enum.TryParse<InterviewLevel>(filter.DifficultyLevel, ignoreCase: true, out var level))
                query = query.Where(q => q.Level == level);
        }

        // Technology filter
        if (!string.IsNullOrWhiteSpace(filter.Technology))
        {
            var tech = filter.Technology.Trim().ToLowerInvariant();
            query = query.Where(q => q.Technology != null && q.Technology.ToLower() == tech);
        }

        // Topic filter
        if (!string.IsNullOrWhiteSpace(filter.Topic))
        {
            var topic = filter.Topic.Trim().ToLowerInvariant();
            query = query.Where(q => q.Topic != null && q.Topic.ToLower() == topic);
        }

        // Creator filter
        if (filter.CreatedByUserId.HasValue)
            query = query.Where(q => q.AuthorId == filter.CreatedByUserId.Value);

        // Created date range
        if (filter.CreatedFrom.HasValue)
            query = query.Where(q => q.EffDate >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue)
            query = query.Where(q => q.EffDate <= filter.CreatedTo.Value);

        // Updated date range
        if (filter.UpdatedFrom.HasValue)
            query = query.Where(q => q.DateLastMaint >= filter.UpdatedFrom.Value);
        if (filter.UpdatedTo.HasValue)
            query = query.Where(q => q.DateLastMaint <= filter.UpdatedTo.Value);

        // Deleted filter
        if (!filter.IncludeDeleted)
            query = query.Where(q => q.DeletedAt == null);
        // When IncludeDeleted=true, show ALL questions (include both deleted and non-deleted)

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var page = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize > 100 ? 100 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new InterviewQuestionListItemResponse
            {
                Id = q.Id,
                Title = q.Title,
                // Truncate question content in the projection
                QuestionContent = q.QuestionContent,
                DifficultyLevel = q.Level.ToString(),
                Status = q.Status.ToString(),
                Technology = q.Technology,
                Topic = q.Topic,
                AnswerCount = q.Answers.Count(a => a.DeletedAt == null),
                CreatedAt = q.EffDate,
                UpdatedAt = q.DateLastMaint,
                CreatedBy = q.Author != null ? new AuthorSummaryResponse
                {
                    Id = q.Author.Id,
                    Username = q.Author.Username,
                    FullName = q.Author.FullName
                } : null,
                Tags = q.InterviewQuestionTags
                    .Where(iqt => iqt.Tag.DeletedAt == null)
                    .Select(iqt => new TagSummaryResponse
                    {
                        Id = iqt.Tag.Id,
                        Name = iqt.Tag.Name
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<InterviewQuestionListItemResponse>
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

    public async Task<InterviewQuestionDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var question = await _context.InterviewQuestions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.InterviewQuestionTags)
                .ThenInclude(iqt => iqt.Tag)
            .Include(q => q.Answers.Where(a => a.DeletedAt == null))
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (question is null)
            return null;

        return new InterviewQuestionDetailResponse
        {
            Id = question.Id,
            Title = question.Title,
            QuestionContent = question.QuestionContent,
            Explanation = question.Explanation,
            DifficultyLevel = question.Level.ToString(),
            Status = question.Status.ToString(),
            Technology = question.Technology,
            Topic = question.Topic,
            CreatedAt = question.EffDate,
            UpdatedAt = question.DateLastMaint,
            DeletedAt = question.DeletedAt,
            DeletedBy = question.DeletedBy,
            CreatedBy = question.Author != null ? new AuthorSummaryResponse
            {
                Id = question.Author.Id,
                Username = question.Author.Username,
                FullName = question.Author.FullName
            } : null,
            Tags = question.InterviewQuestionTags
                .Where(iqt => iqt.Tag.DeletedAt == null)
                .Select(iqt => new TagSummaryResponse
                {
                    Id = iqt.Tag.Id,
                    Name = iqt.Tag.Name
                }).ToList(),
            Answers = question.Answers
                .OrderBy(a => a.Id)
                .Select(a => new InterviewAnswerResponse
                {
                    Id = a.Id,
                    Content = a.AnswerContent,
                    Explanation = a.Explanation,
                    Example = a.Example,
                    IsOfficial = a.IsOfficial
                }).ToList()
        };
    }

    public async Task<InterviewQuestion?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewQuestions
            .Include(q => q.InterviewQuestionTags)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id && q.DeletedAt == null, cancellationToken);
    }

    public async Task<InterviewQuestion?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewQuestions
            .Include(q => q.InterviewQuestionTags)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id && q.DeletedAt != null, cancellationToken);
    }

    public async Task<PostCategory?> GetActiveCategoryAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PostCategories
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
        return await _context.Tags
            .AsNoTracking()
            .Where(t =>
                tagIds.Contains(t.Id) &&
                t.Status == EntityStatus.Active &&
                t.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<InterviewAnswer?> GetActiveAnswerAsync(
        long answerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.InterviewAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Id == answerId &&
                a.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> ExistsQuestionAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewQuestions
            .AsNoTracking()
            .AnyAsync(q => q.Id == id, cancellationToken);
    }

    public async Task AddQuestionAsync(InterviewQuestion question, CancellationToken cancellationToken = default)
    {
        await _context.InterviewQuestions.AddAsync(question, cancellationToken);
    }

    public async Task AddAnswerAsync(InterviewAnswer answer, CancellationToken cancellationToken = default)
    {
        await _context.InterviewAnswers.AddAsync(answer, cancellationToken);
    }

    public async Task ReplaceTagsAsync(
        InterviewQuestion question,
        IReadOnlyList<Tag> tags,
        CancellationToken cancellationToken = default)
    {
        // Remove existing tags
        var existingTags = question.InterviewQuestionTags.ToList();
        foreach (var et in existingTags)
            _context.InterviewQuestionTags.Remove(et);

        // Add new tags
        foreach (var tag in tags)
        {
            await _context.InterviewQuestionTags.AddAsync(new InterviewQuestionTag
            {
                Question = question,
                TagId = tag.Id,
                EffDate = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    public Task SoftDeleteAnswerAsync(InterviewAnswer answer, long deletedBy, CancellationToken cancellationToken = default)
    {
        answer.DeletedAt = DateTimeOffset.UtcNow;
        answer.DeletedBy = deletedBy;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<InterviewQuestion> ApplySorting(
        IQueryable<InterviewQuestion> query,
        string? sortBy,
        string? sortDirection)
    {
        var field = (sortBy ?? "createdAt").ToLowerInvariant();
        var direction = sortDirection?.ToLowerInvariant() == "asc"
            ? SortDirection.Asc
            : SortDirection.Desc;

        if (!AllowedSortFields.Contains(field))
            field = "createdAt";

        if (field == "answerCount")
        {
            // Sort by count of non-deleted answers
            query = direction == SortDirection.Asc
                ? query.OrderBy(q => q.Answers.Count(a => a.DeletedAt == null))
                : query.OrderByDescending(q => q.Answers.Count(a => a.DeletedAt == null));
            return query;
        }

        query = field switch
        {
            "title" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.Title)
                : query.OrderByDescending(q => q.Title),
            "difficultyLevel" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.Level)
                : query.OrderByDescending(q => q.Level),
            "status" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.Status)
                : query.OrderByDescending(q => q.Status),
            "technology" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.Technology)
                : query.OrderByDescending(q => q.Technology),
            "topic" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.Topic)
                : query.OrderByDescending(q => q.Topic),
            "updatedAt" => direction == SortDirection.Asc
                ? query.OrderBy(q => q.DateLastMaint)
                : query.OrderByDescending(q => q.DateLastMaint),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(q => q.EffDate)
                : query.OrderByDescending(q => q.EffDate)
        };

        return query;
    }
}
