namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request to change a post's visibility/status.
/// </summary>
public sealed class ChangePostStatusRequest
{
    /// <summary>
    /// The target status: Draft, PendingReview, Published, Hidden, or Archived.
    /// </summary>
    public required string Status { get; init; }
}
