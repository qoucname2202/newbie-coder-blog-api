namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response returned after successfully creating a post.
/// </summary>
public sealed class CreateAdminPostResponse
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
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public AuthorSummaryResponse? Author { get; init; }
    public CategorySummaryResponse? Category { get; init; }
    public IReadOnlyList<TagSummaryResponse> Tags { get; init; } = [];
}
