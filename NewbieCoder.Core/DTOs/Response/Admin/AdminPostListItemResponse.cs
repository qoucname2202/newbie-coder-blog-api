namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// A single post item returned in the admin paginated list response.
/// Maps directly to the posts table columns.
/// </summary>
public sealed class AdminPostListItemResponse
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Summary { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string Status { get; init; } = null!;
    public string Visibility { get; init; } = null!;
    public int ViewCount { get; init; }
    public int CommentCount { get; init; }
    public int VoteScore { get; init; }
    public int BookmarkCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public AuthorSummaryResponse? Author { get; init; }
    public CategorySummaryResponse? Category { get; init; }
}
