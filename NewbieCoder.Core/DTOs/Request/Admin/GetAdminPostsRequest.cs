using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Query parameters for GET /api/v1/admin/posts.
/// </summary>
public sealed class GetAdminPostsRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "pageNumber must be at least 1.")]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "pageSize must be between 1 and 100.")]
    public int PageSize { get; init; } = 10;

    public string? Keyword { get; init; }

    /// <summary>Filter by post status string (Draft, Pending, Published, etc.).</summary>
    public string? Status { get; init; }

    /// <summary>Filter by author ID.</summary>
    public long? AuthorId { get; init; }

    /// <summary>Filter by category ID.</summary>
    public long? CategoryId { get; init; }

    /// <summary>Filter by tag ID.</summary>
    public long? TagId { get; init; }

    /// <summary>Filter by featured status.</summary>
    public bool? IsFeatured { get; init; }

    /// <summary>Filter posts created on or after this date.</summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>Filter posts created on or before this date.</summary>
    public DateTimeOffset? CreatedTo { get; init; }

    /// <summary>Filter posts published on or after this date.</summary>
    public DateTimeOffset? PublishedFrom { get; init; }

    /// <summary>Filter posts published on or before this date.</summary>
    public DateTimeOffset? PublishedTo { get; init; }

    /// <summary>
    /// Sort field. Allowed: title, createdAt, updatedAt, publishedAt, viewCount, commentCount.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>Sort direction: asc or desc.</summary>
    public string? SortDirection { get; init; }

    /// <summary>When true, includes soft-deleted posts.</summary>
    public bool IncludeDeleted { get; init; }
}
