namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// A single interview question returned in the paginated admin list response.
/// </summary>
public sealed class InterviewQuestionListItemResponse
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string QuestionContent { get; init; } = null!;
    public string DifficultyLevel { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? Technology { get; init; }
    public string? Topic { get; init; }
    public IReadOnlyList<TagSummaryResponse> Tags { get; init; } = [];
    public int AnswerCount { get; init; }
    public AuthorSummaryResponse? CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Full interview question details returned by GET /api/v1/admin/interview-questions/{questionId}.
/// </summary>
public sealed class InterviewQuestionDetailResponse
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string QuestionContent { get; init; } = null!;
    public string? Explanation { get; init; }
    public string DifficultyLevel { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? Technology { get; init; }
    public string? Topic { get; init; }
    public IReadOnlyList<TagSummaryResponse> Tags { get; init; } = [];
    public IReadOnlyList<InterviewAnswerResponse> Answers { get; init; } = [];
    public AuthorSummaryResponse? CreatedBy { get; init; }
    public AuthorSummaryResponse? UpdatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public long? DeletedBy { get; init; }
}

/// <summary>
/// Response returned after successfully creating an interview question.
/// </summary>
public sealed class CreateInterviewQuestionResponse
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string DifficultyLevel { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? Technology { get; init; }
    public string? Topic { get; init; }
    public int AnswerCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully updating an interview question.
/// </summary>
public sealed class UpdateInterviewQuestionResponse
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string DifficultyLevel { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? Technology { get; init; }
    public string? Topic { get; init; }
    public int AnswerCount { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully deleting an interview question.
/// </summary>
public sealed class DeleteInterviewQuestionResponse
{
    public long Id { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset? DeletedAt { get; init; }
}

/// <summary>
/// Response returned after successfully restoring a soft-deleted interview question.
/// </summary>
public sealed class RestoreInterviewQuestionResponse
{
    public long Id { get; init; }
    public string Status { get; init; } = null!;
    public bool IsDeleted { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully changing an interview question's status.
/// </summary>
public sealed class ChangeInterviewQuestionStatusResponse
{
    /// <summary>The question's status before the change.</summary>
    public required string PreviousStatus { get; init; }

    /// <summary>The question's status after the change.</summary>
    public required string CurrentStatus { get; init; }

    /// <summary>The ID of the user who performed the change.</summary>
    public required long UpdatedBy { get; init; }
}

/// <summary>
/// A single suggested answer embedded in question detail responses.
/// </summary>
public sealed class InterviewAnswerResponse
{
    public long Id { get; init; }
    public string Content { get; init; } = null!;
    public string? Explanation { get; init; }
    public string? Example { get; init; }
    public bool IsOfficial { get; init; }
}
