namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to change the status of a community question.
/// Supports transitions to Open, Answered, Resolved, Closed, and Hidden.
/// </summary>
public sealed class ChangeCommunityQuestionStatusCommand
{
    public required long QuestionId { get; init; }
    public required int Status { get; init; }
    public required string Reason { get; init; }
    public required long ModeratedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
