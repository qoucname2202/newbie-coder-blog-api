namespace NewbieCoder.Core.CQRS.CommunityAnswers;

/// <summary>
/// CQRS command to restore a soft-deleted community answer.
/// </summary>
public sealed class RestoreCommunityAnswerCommand
{
    public required long AnswerId { get; init; }
    public required long RestoredByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
