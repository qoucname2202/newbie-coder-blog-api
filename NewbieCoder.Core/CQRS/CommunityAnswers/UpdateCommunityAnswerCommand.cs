namespace NewbieCoder.Core.CQRS.CommunityAnswers;

/// <summary>
/// CQRS command to update an existing community answer.
/// </summary>
public sealed class UpdateCommunityAnswerCommand
{
    public required long AnswerId { get; init; }
    public required string Content { get; init; }
    public required long UpdatedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
