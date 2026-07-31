using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Filter, search, and pagination parameters for GET /api/v1/admin/community-answers.
/// </summary>
public sealed class GetCommunityAnswersRequest
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = PagingDefaults.DefaultPage;
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;

    /// <summary>
    /// Filter by visible/hidden state. true = hidden, false = visible.
    /// </summary>
    public bool? IsHidden { get; set; }

    /// <summary>
    /// Filter by accepted state. true = accepted, false = not accepted.
    /// </summary>
    public bool? IsAccepted { get; set; }

    /// <summary>
    /// Filter by question ID.
    /// </summary>
    public long? QuestionId { get; set; }

    /// <summary>
    /// Filter by author/user ID.
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// Include soft-deleted answers in results.
    /// </summary>
    public bool IncludeDeleted { get; set; }

    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}

/// <summary>
/// Request payload for hiding a community answer (PATCH /api/v1/admin/community-answers/{id}/hide).
/// </summary>
public sealed class HideCommunityAnswerRequest
{
    /// <summary>
    /// Optional reason for hiding the answer.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Request payload for creating a new community answer (POST /api/v1/admin/community-answers).
/// </summary>
public sealed class CreateCommunityAnswerRequest
{
    /// <summary>
    /// ID of the question this answer belongs to.
    /// </summary>
    public required long QuestionId { get; set; }

    /// <summary>
    /// ID of the user who authored this answer.
    /// </summary>
    public required long AuthorId { get; set; }

    /// <summary>
    /// Answer content (supports markdown).
    /// </summary>
    public required string Content { get; set; } = null!;
}

/// <summary>
/// Request payload for updating an existing community answer (PUT /api/v1/admin/community-answers/{id}).
/// </summary>
public sealed class UpdateCommunityAnswerRequest
{
    /// <summary>
    /// Updated answer content (supports markdown).
    /// </summary>
    public required string Content { get; set; } = null!;
}
