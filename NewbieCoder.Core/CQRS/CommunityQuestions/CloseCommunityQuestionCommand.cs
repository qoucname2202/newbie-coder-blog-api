namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to close a community question, preventing new community answers.
/// Sets ClosedAt timestamp and locks the question.
/// </summary>
public sealed class CloseCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required string Reason { get; init; }
    public required long ModeratedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
