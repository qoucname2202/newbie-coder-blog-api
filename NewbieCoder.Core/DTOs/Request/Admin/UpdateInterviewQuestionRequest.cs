using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for PUT /api/v1/admin/interview-questions/{questionId}.
/// </summary>
public sealed class UpdateInterviewQuestionRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 500 characters.")]
    public string Title { get; init; } = null!;

    [Required(ErrorMessage = "Question content is required.")]
    public string QuestionContent { get; init; } = null!;

    /// <summary>Explanation or notes about the answer.</summary>
    public string? Explanation { get; init; }

    /// <summary>Difficulty level: Entry, Junior, Middle, Senior, Expert.</summary>
    public string DifficultyLevel { get; init; } = "Entry";

    /// <summary>Status: Draft, Active, Inactive, Archived.</summary>
    public string Status { get; init; } = "Draft";

    /// <summary>Technology or framework the question relates to.</summary>
    [StringLength(100, ErrorMessage = "Technology must not exceed 100 characters.")]
    public string? Technology { get; init; }

    /// <summary>Topic or concept the question covers.</summary>
    [StringLength(100, ErrorMessage = "Topic must not exceed 100 characters.")]
    public string? Topic { get; init; }

    /// <summary>The category ID for the question. Must exist, be active, and not deleted.</summary>
    public long? CategoryId { get; init; }

    /// <summary>Tag IDs to associate with the question. All must exist, be active, and not deleted.</summary>
    public IReadOnlyList<long>? TagIds { get; init; }

    /// <summary>
    /// Suggested answers. If provided, replaces all existing answers.
    /// Answers with Id = 0 are treated as new; answers with existing Id are updated.
    /// Answers previously associated but not included here will be soft-deleted.
    /// </summary>
    public IReadOnlyList<UpdateInterviewAnswerRequest>? Answers { get; init; }
}

/// <summary>
/// A single suggested answer within an update request.
/// Answers with no Id (or 0) are treated as new; answers with Id are updated.
/// </summary>
public sealed class UpdateInterviewAnswerRequest
{
    /// <summary>
    /// The ID of an existing answer to update. 0 means "create new".
    /// </summary>
    public long Id { get; init; }

    [Required(ErrorMessage = "Answer content is required.")]
    public string Content { get; init; } = null!;

    /// <summary>Brief explanation of why this answer is correct.</summary>
    public string? Explanation { get; init; }

    /// <summary>Code example or additional reference.</summary>
    public string? Example { get; init; }

    /// <summary>Whether this is the official/preferred answer.</summary>
    public bool IsOfficial { get; init; } = true;
}
