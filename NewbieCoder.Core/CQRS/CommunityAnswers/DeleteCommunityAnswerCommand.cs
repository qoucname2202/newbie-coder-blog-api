namespace NewbieCoder.Core.CQRS.CommunityAnswers;

/// <summary>
/// CQRS command to soft-delete a community answer.
/// </summary>
public sealed class DeleteCommunityAnswerCommand
{
    public required long AnswerId { get; init; }
    public required long DeletedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? TraceId { get; init; }
}
