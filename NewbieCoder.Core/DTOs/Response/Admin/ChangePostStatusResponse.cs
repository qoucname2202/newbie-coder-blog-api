namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Response returned after successfully changing a post's status.
/// </summary>
public sealed class ChangePostStatusResponse
{
    /// <summary>
    /// The post's status before the change.
    /// </summary>
    public required string PreviousStatus { get; init; }

    /// <summary>
    /// The post's status after the change.
    /// </summary>
    public required string CurrentStatus { get; init; }

    /// <summary>
    /// The ID of the user who performed the change.
    /// </summary>
    public required long UpdatedBy { get; init; }
}
