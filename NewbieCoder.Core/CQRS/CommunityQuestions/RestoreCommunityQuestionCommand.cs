namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to restore a soft-deleted community question.
/// </summary>
public sealed class RestoreCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required string Reason { get; init; }
    public required long RestoredByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
