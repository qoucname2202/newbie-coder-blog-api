namespace NewbieCoder.Core.CQRS.CommunityQuestions;

/// <summary>
/// CQRS command to soft-delete a community question.
/// </summary>
public sealed class DeleteCommunityQuestionCommand
{
    public required long QuestionId { get; init; }
    public required string Reason { get; init; }
    public required long DeletedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
