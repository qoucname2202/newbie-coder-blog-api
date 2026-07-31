namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to update an existing community question.
/// </summary>
public sealed class UpdateCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public List<long> TagIds { get; init; } = [];
    public required string Reason { get; init; }
    public required long UpdatedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
