namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Query parameters for GET /api/v1/admin/interview-questions.
/// </summary>
public sealed class GetInterviewQuestionsRequest
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "pageNumber must be at least 1.")]
    public int PageNumber { get; init; } = 1;

    [System.ComponentModel.DataAnnotations.Range(1, 100, ErrorMessage = "pageSize must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;

    /// <summary>Search by title, question content, explanation, technology, topic, or tag name.</summary>
    public string? Keyword { get; init; }

    /// <summary>Filter by status string (Draft, Active, Inactive, Archived).</summary>
    public string? Status { get; init; }

    /// <summary>Filter by difficulty level (Entry, Junior, Middle, Senior, Expert).</summary>
    public string? DifficultyLevel { get; init; }

    /// <summary>Filter by technology field.</summary>
    public string? Technology { get; init; }

    /// <summary>Filter by topic field.</summary>
    public string? Topic { get; init; }

    /// <summary>Filter by category ID.</summary>
    public long? CategoryId { get; init; }

    /// <summary>Filter by tag ID.</summary>
    public long? TagId { get; init; }

    /// <summary>Filter by creator ID.</summary>
    public long? CreatedByUserId { get; init; }

    /// <summary>Filter questions created on or after this date.</summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>Filter questions created on or before this date.</summary>
    public DateTimeOffset? CreatedTo { get; init; }

    /// <summary>Filter questions updated on or after this date.</summary>
    public DateTimeOffset? UpdatedFrom { get; init; }

    /// <summary>Filter questions updated on or before this date.</summary>
    public DateTimeOffset? UpdatedTo { get; init; }

    /// <summary>
    /// Sort field. Allowed: title, difficultyLevel, status, technology, topic, createdAt, updatedAt, answerCount.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>Sort direction: asc or desc.</summary>
    public string? SortDirection { get; init; }

    /// <summary>When true, includes soft-deleted questions.</summary>
    public bool IncludeDeleted { get; init; }
}
