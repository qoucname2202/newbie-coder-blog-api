using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;

namespace NewbieCoder.Core.CQRS.Posts;

/// <summary>
/// CQRS command to change a post's visibility/status.
/// </summary>
public sealed class ChangePostStatusCommand
{
    /// <summary>
    /// The ID of the post to update.
    /// </summary>
    public required long PostId { get; init; }

    /// <summary>
    /// The target status payload.
    /// </summary>
    public required ChangePostStatusRequest Request { get; init; }

    /// <summary>
    /// The ID of the user performing the change.
    /// </summary>
    public required long ChangedByUserId { get; init; }

    /// <summary>
    /// The client's IP address for audit logging.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// The client's User-Agent for audit logging.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Trace/correlation ID for audit logging.
    /// </summary>
    public string? TraceId { get; init; }
}
