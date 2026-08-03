using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// A single community question returned in the paginated admin list response.
/// </summary>
public sealed class CommunityQuestionListItemResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("contentPreview")]
    public required string ContentPreview { get; init; }

    [JsonPropertyName("status")]
    public required StatusValueResponse Status { get; init; }

    [JsonPropertyName("isLocked")]
    public required bool IsLocked { get; init; }

    [JsonPropertyName("viewCount")]
    public required int ViewCount { get; init; }

    [JsonPropertyName("answerCount")]
    public required int AnswerCount { get; init; }

    [JsonPropertyName("voteScore")]
    public required int VoteScore { get; init; }

    [JsonPropertyName("bookmarkCount")]
    public required int BookmarkCount { get; init; }

    [JsonPropertyName("acceptedAnswerId")]
    public long? AcceptedAnswerId { get; init; }

    [JsonPropertyName("author")]
    public required AuthorSummaryResponse Author { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<CommunityQuestionTagResponse> Tags { get; init; } = [];

    [JsonPropertyName("closedAt")]
    public DateTimeOffset? ClosedAt { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("isDeleted")]
    public required bool IsDeleted { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }
}

/// <summary>
/// Status value/name pair returned in list and detail responses.
/// </summary>
public sealed class StatusValueResponse
{
    [JsonPropertyName("value")]
    public required int Value { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

/// <summary>
/// Full community question details returned by GET /api/v1/admin/community-questions/{id}.
/// </summary>
public sealed class CommunityQuestionDetailResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("status")]
    public required StatusValueResponse Status { get; init; }

    [JsonPropertyName("isLocked")]
    public required bool IsLocked { get; init; }

    [JsonPropertyName("viewCount")]
    public required int ViewCount { get; init; }

    [JsonPropertyName("answerCount")]
    public required int AnswerCount { get; init; }

    [JsonPropertyName("voteScore")]
    public required int VoteScore { get; init; }

    [JsonPropertyName("bookmarkCount")]
    public required int BookmarkCount { get; init; }

    [JsonPropertyName("acceptedAnswerId")]
    public long? AcceptedAnswerId { get; init; }

    [JsonPropertyName("closedAt")]
    public DateTimeOffset? ClosedAt { get; init; }

    [JsonPropertyName("author")]
    public required AuthorDetailResponse Author { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<CommunityQuestionTagResponse> Tags { get; init; } = [];

    [JsonPropertyName("answers")]
    public IReadOnlyList<CommunityAnswerDetailItemResponse> Answers { get; init; } = [];

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("isDeleted")]
    public required bool IsDeleted { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }

    [JsonPropertyName("deletedBy")]
    public long? DeletedBy { get; init; }
}

/// <summary>
/// Author detail response embedded in question detail.
/// </summary>
public sealed class AuthorDetailResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// Tag summary embedded in question responses (includes slug).
/// </summary>
public sealed class CommunityQuestionTagResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }
}

/// <summary>
/// A single answer embedded in community question detail responses.
/// Includes IsHidden and UpdatedAt per the spec.
/// </summary>
public sealed class CommunityAnswerDetailItemResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("voteScore")]
    public required int VoteScore { get; init; }

    [JsonPropertyName("isAccepted")]
    public required bool IsAccepted { get; init; }

    [JsonPropertyName("isHidden")]
    public required bool IsHidden { get; init; }

    [JsonPropertyName("author")]
    public required AuthorSummaryResponse Author { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully locking a community question.
/// </summary>
public sealed class LockCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isLocked")]
    public required bool IsLocked { get; init; }

    [JsonPropertyName("closedAt")]
    public DateTimeOffset? ClosedAt { get; init; }

    [JsonPropertyName("lockedBy")]
    public long? LockedBy { get; init; }
}

/// <summary>
/// Response returned after successfully unlocking a community question.
/// </summary>
public sealed class UnlockCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isLocked")]
    public required bool IsLocked { get; init; }

    [JsonPropertyName("closedAt")]
    public DateTimeOffset? ClosedAt { get; init; }
}

/// <summary>
/// Response returned after successfully soft-deleting a community question.
/// </summary>
public sealed class DeleteCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isDeleted")]
    public required bool IsDeleted { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }
}

/// <summary>
/// Response returned after successfully restoring a soft-deleted community question.
/// </summary>
public sealed class RestoreCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isDeleted")]
    public required bool IsDeleted { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully creating a new community question.
/// </summary>
public sealed class CreateCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("authorId")]
    public required long AuthorId { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully updating a community question.
/// </summary>
public sealed class UpdateCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully changing a community question status.
/// </summary>
public sealed class ChangeCommunityQuestionStatusResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("previousStatus")]
    public required string PreviousStatus { get; init; }

    [JsonPropertyName("currentStatus")]
    public required string CurrentStatus { get; init; }

    [JsonPropertyName("closedAt")]
    public DateTimeOffset? ClosedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully hiding a community question.
/// </summary>
public sealed class HideCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("hiddenAt")]
    public required DateTimeOffset HiddenAt { get; init; }

    [JsonPropertyName("hiddenBy")]
    public long? HiddenBy { get; init; }
}

/// <summary>
/// Response returned after successfully closing a community question.
/// </summary>
public sealed class CloseCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("closedAt")]
    public required DateTimeOffset ClosedAt { get; init; }

    [JsonPropertyName("closedBy")]
    public long? ClosedBy { get; init; }
}

/// <summary>
/// Response returned after successfully reopening a community question.
/// </summary>
public sealed class ReopenCommunityQuestionResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("closedAt")]
    public DateTimeOffset? ClosedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
