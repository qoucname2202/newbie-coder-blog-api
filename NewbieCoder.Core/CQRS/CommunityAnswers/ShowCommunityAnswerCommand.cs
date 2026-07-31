namespace NewbieCoder.Core.CQRS.CommunityAnswers;

/// <summary>
/// CQRS command to show (unhide) a previously hidden community answer.
/// </summary>
public sealed class ShowCommunityAnswerCommand
{
    public required long AnswerId { get; init; }
    public required long ModeratedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
