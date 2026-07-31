namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to lock a community question, preventing new answers.
/// </summary>
public sealed class LockCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required long LockedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
