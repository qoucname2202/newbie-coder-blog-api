namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to unlock a previously locked community question.
/// </summary>
public sealed class UnlockCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required long UnlockedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
