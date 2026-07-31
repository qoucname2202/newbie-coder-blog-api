namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to reopen a previously closed or hidden community question.
/// Clears ClosedAt and resets status to Open.
/// </summary>
public sealed class ReopenCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required string Reason { get; init; }
    public required long ModeratedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
