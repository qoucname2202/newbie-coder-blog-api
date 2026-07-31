using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Filter, search, and pagination parameters for GET /api/v1/admin/community-questions.
/// </summary>
public sealed class GetCommunityQuestionsRequest
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = PagingDefaults.DefaultPage;
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;
    public string? Status { get; set; }
    public long? AuthorId { get; set; }
    public long? TagId { get; set; }
    public bool? HasAnswers { get; set; }
    public bool IncludeDeleted { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}

/// <summary>
/// Filter parameters for GET /api/v1/admin/community-questions/{id} (if any extra query params needed).
/// </summary>
public sealed class GetCommunityQuestionDetailRequest
{
    /// <summary>
    /// When true, includes soft-deleted records.
    /// </summary>
    public bool IncludeDeleted { get; set; }
}

/// <summary>
/// Request payload for creating a new community question (POST /api/v1/admin/community-questions).
/// </summary>
public sealed class CreateCommunityQuestionRequest
{
    /// <summary>
    /// ID of the user who authored this question.
    /// </summary>
    public long AuthorId { get; set; }

    /// <summary>
    /// Question title.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Question body content (supports markdown).
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Optional list of tag IDs to associate with the question.
    /// </summary>
    public List<long> TagIds { get; set; } = [];
}

/// <summary>
/// Request payload for updating an existing community question (PUT /api/v1/admin/community-questions/{id}).
/// </summary>
public sealed class UpdateCommunityQuestionRequest
{
    /// <summary>
    /// New title for the question.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// New body content for the question.
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Updated list of tag IDs to associate with the question.
    /// </summary>
    public List<long> TagIds { get; set; } = [];

    /// <summary>
    /// Reason for the update (required for audit trail).
    /// </summary>
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Request payload for changing the status of a community question
/// (PATCH /api/v1/admin/community-questions/{id}/status).
/// </summary>
public sealed class ChangeCommunityQuestionStatusRequest
{
    /// <summary>
    /// Target status value: 1=Open, 2=Answered, 3=Resolved, 4=Closed, 5=Hidden.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Reason for the status change (required for audit trail).
    /// </summary>
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Request payload for hiding a community question
/// (PATCH /api/v1/admin/community-questions/{id}/hide).
/// </summary>
public sealed class HideCommunityQuestionRequest
{
    /// <summary>
    /// Reason for hiding the question (required for audit trail).
    /// </summary>
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Request payload for closing a community question
/// (PATCH /api/v1/admin/community-questions/{id}/close).
/// </summary>
public sealed class CloseCommunityQuestionRequest
{
    /// <summary>
    /// Reason for closing the question (required for audit trail).
    /// </summary>
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Request payload for reopening a community question
/// (PATCH /api/v1/admin/community-questions/{id}/reopen).
/// </summary>
public sealed class ReopenCommunityQuestionRequest
{
    /// <summary>
    /// Reason for reopening the question (required for audit trail).
    /// </summary>
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Request payload for soft-deleting a community question
/// (DELETE /api/v1/admin/community-questions/{id}).
/// </summary>
public sealed class DeleteCommunityQuestionRequest
{
    /// <summary>
    /// Reason for the deletion (required for audit trail).
    /// </summary>
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Request payload for restoring a soft-deleted community question
/// (PATCH /api/v1/admin/community-questions/{id}/restore).
/// </summary>
public sealed class RestoreCommunityQuestionRequest
{
    /// <summary>
    /// Reason for the restore (required for audit trail).
    /// </summary>
    public string Reason { get; set; } = null!;
}
