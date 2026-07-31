namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to create a new community question.
/// </summary>
public sealed class CreateCommunityQuestionCommand
{
    public required long AuthorId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public List<long> TagIds { get; init; } = [];
    public required long CreatedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
