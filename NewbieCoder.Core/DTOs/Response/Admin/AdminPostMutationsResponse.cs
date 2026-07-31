namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response returned after successfully updating a post.
/// </summary>
public sealed class UpdateAdminPostResponse
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
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully deleting a post.
/// </summary>
public sealed class DeleteAdminPostResponse
{
    public long Id { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset? DeletedAt { get; init; }
}

/// <summary>
/// Response returned after successfully restoring a soft-deleted post.
/// </summary>
public sealed class RestoreAdminPostResponse
{
    public long Id { get; init; }
    public string Status { get; init; } = null!;
    public bool IsDeleted { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
