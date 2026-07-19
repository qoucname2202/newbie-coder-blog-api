namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request to change an interview question's status.
/// </summary>
public sealed class ChangeInterviewQuestionStatusRequest
{
    /// <summary>
    /// The target status: Draft, Active, Inactive, or Archived.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Optional reason for the status change (e.g. "Question reviewed and approved").
    /// </summary>
    public string? Reason { get; init; }
}
