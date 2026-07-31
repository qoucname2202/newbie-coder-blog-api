namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to hide a community question, making it invisible to public queries.
/// The question is not soft-deleted and remains accessible in Admin APIs.
/// </summary>
public sealed class HideCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required string Reason { get; init; }
    public required long ModeratedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
