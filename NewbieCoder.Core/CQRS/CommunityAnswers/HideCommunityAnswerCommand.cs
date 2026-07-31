namespace NewbieCoder.Core.CQRS.CommunityAnswers;

/// <summary>
/// CQRS command to hide a community answer, making it invisible to public queries.
/// </summary>
public sealed class HideCommunityAnswerCommand
{
    public required long AnswerId { get; init; }
    public required long ModeratedByUserId { get; init; }
    public string? Reason { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
