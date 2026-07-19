namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Full post details returned by GET /api/v1/admin/posts/{postId}.
/// Maps directly to the posts table columns.
/// </summary>
public sealed class AdminPostDetailResponse
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Summary { get; init; }
    public string Content { get; init; } = null!;
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
    public DateTimeOffset? DeletedAt { get; init; }
    public AuthorSummaryResponse? Author { get; init; }
    public CategorySummaryResponse? Category { get; init; }
    public IReadOnlyList<TagSummaryResponse> Tags { get; init; } = [];
}
