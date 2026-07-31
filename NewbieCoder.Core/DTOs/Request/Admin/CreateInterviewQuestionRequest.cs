using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Request body for POST /api/v1/admin/interview-questions.
/// </summary>
public sealed class CreateInterviewQuestionRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 500 characters.")]
    public string Title { get; init; } = null!;

    [Required(ErrorMessage = "Question content is required.")]
    public string QuestionContent { get; init; } = null!;

    /// <summary>Explanation or notes about the answer.</summary>
    public string? Explanation { get; init; }

    /// <summary>Difficulty level: Entry, Junior, Middle, Senior, Expert. Defaults to Entry.</summary>
    public string DifficultyLevel { get; init; } = "Entry";

    /// <summary>Status: Draft, Active, Inactive, Archived. Defaults to Draft.</summary>
    public string Status { get; init; } = "Draft";

    /// <summary>Technology or framework the question relates to (e.g. ASP.NET Core).</summary>
    [StringLength(100, ErrorMessage = "Technology must not exceed 100 characters.")]
    public string? Technology { get; init; }

    /// <summary>Topic or concept the question covers (e.g. Dependency Injection).</summary>
    [StringLength(100, ErrorMessage = "Topic must not exceed 100 characters.")]
    public string? Topic { get; init; }

    /// <summary>The category ID for the question. Must exist, be active, and not deleted.</summary>
    public long? CategoryId { get; init; }

    /// <summary>Tag IDs to associate with the question. All must exist, be active, and not deleted.</summary>
    public IReadOnlyList<long>? TagIds { get; init; }

    /// <summary>Suggested answers. At least one is required when status is Active.</summary>
    public IReadOnlyList<InterviewAnswerRequest>? Answers { get; init; }
}

/// <summary>
/// A single suggested answer within a question create or update request.
/// </summary>
public sealed class InterviewAnswerRequest
{
    [Required(ErrorMessage = "Answer content is required.")]
    public string Content { get; init; } = null!;

    /// <summary>Brief explanation of why this answer is correct.</summary>
    public string? Explanation { get; init; }

    /// <summary>Code example or additional reference.</summary>
    public string? Example { get; init; }

    /// <summary>Whether this is the official/preferred answer. Only one answer should be marked preferred.</summary>
    public bool IsOfficial { get; init; } = true;
}
