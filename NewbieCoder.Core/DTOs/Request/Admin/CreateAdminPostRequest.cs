using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/v1/admin/posts.
/// </summary>
public sealed class CreateAdminPostRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 500 characters.")]
    public string Title { get; init; } = null!;

    [StringLength(500, ErrorMessage = "Summary must not exceed 500 characters.")]
    public string? Summary { get; init; }

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; init; } = null!;

    [Url(ErrorMessage = "Thumbnail URL must be a valid URL.")]
    public string? ThumbnailUrl { get; init; }

    /// <summary>The category ID for the post. Must exist, be active, and not deleted.</summary>
    public long? CategoryId { get; init; }

    /// <summary>Tag IDs to associate with the post. All must exist, be active, and not deleted.</summary>
    public IReadOnlyList<long>? TagIds { get; init; }

    /// <summary>
    /// The author ID to create the post for.
    /// If null, the current admin user is set as the author.
    /// </summary>
    public long? AuthorId { get; init; }

    /// <summary>
    /// Post visibility: Public, Private, or LinkOnly.
    /// </summary>
    public string Visibility { get; init; } = "Public";

    /// <summary>Post status: Draft, Pending, Published, Hidden, Archived, Deleted.</summary>
    public string Status { get; init; } = "Draft";
}
