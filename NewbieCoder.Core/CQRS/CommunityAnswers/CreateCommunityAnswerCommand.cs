namespace NewbieCoder.Core.CQRS.CommunityAnswers;

/// <summary>
/// CQRS command to create a new community answer by an admin or moderator.
/// </summary>
public sealed class CreateCommunityAnswerCommand
{
    public required long QuestionId { get; init; }
    public required long AuthorId { get; init; }
    public required string Content { get; init; }
    public required long CreatedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
