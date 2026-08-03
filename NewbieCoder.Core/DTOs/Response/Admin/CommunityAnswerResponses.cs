using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// A single community answer returned in the paginated admin list response.
/// </summary>
public sealed class CommunityAnswerListItemResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("contentPreview")]
    public required string ContentPreview { get; init; }

    [JsonPropertyName("question")]
    public required CommunityAnswerQuestionSummaryResponse Question { get; init; }

    [JsonPropertyName("author")]
    public required CommunityAnswerAuthorResponse Author { get; init; }

    [JsonPropertyName("isHidden")]
    public required bool IsHidden { get; init; }

    [JsonPropertyName("isAccepted")]
    public required bool IsAccepted { get; init; }

    [JsonPropertyName("voteScore")]
    public required int VoteScore { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }
}

/// <summary>
/// Full community answer details returned by GET /api/v1/admin/community-answers/{id}.
/// </summary>
public sealed class CommunityAnswerDetailResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("question")]
    public required CommunityAnswerQuestionDetailResponse Question { get; init; }

    [JsonPropertyName("author")]
    public required CommunityAnswerAuthorDetailResponse Author { get; init; }

    [JsonPropertyName("isHidden")]
    public required bool IsHidden { get; init; }

    [JsonPropertyName("isAccepted")]
    public required bool IsAccepted { get; init; }

    [JsonPropertyName("voteScore")]
    public required int VoteScore { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }

    [JsonPropertyName("deletedBy")]
    public long? DeletedBy { get; init; }
}

/// <summary>
/// Question summary embedded in community answer list item responses.
/// </summary>
public sealed class CommunityAnswerQuestionSummaryResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}

/// <summary>
/// Extended question details embedded in community answer detail responses.
/// </summary>
public sealed class CommunityAnswerQuestionDetailResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("isLocked")]
    public required bool IsLocked { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }
}

/// <summary>
/// Author summary embedded in community answer list item responses.
/// </summary>
public sealed class CommunityAnswerAuthorResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// Extended author details embedded in community answer detail responses.
/// </summary>
public sealed class CommunityAnswerAuthorDetailResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }
}

/// <summary>
/// Response returned after successfully hiding a community answer.
/// </summary>
public sealed class HideCommunityAnswerResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isHidden")]
    public required bool IsHidden { get; init; }

    [JsonPropertyName("moderatedAt")]
    public required DateTimeOffset ModeratedAt { get; init; }

    [JsonPropertyName("moderatedBy")]
    public long? ModeratedBy { get; init; }
}

/// <summary>
/// Response returned after successfully showing (unhiding) a community answer.
/// </summary>
public sealed class ShowCommunityAnswerResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isHidden")]
    public required bool IsHidden { get; init; }

    [JsonPropertyName("moderatedAt")]
    public required DateTimeOffset ModeratedAt { get; init; }

    [JsonPropertyName("moderatedBy")]
    public long? ModeratedBy { get; init; }
}

/// <summary>
/// Response returned after successfully soft-deleting a community answer.
/// </summary>
public sealed class DeleteCommunityAnswerResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isDeleted")]
    public required bool IsDeleted { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }

    [JsonPropertyName("deletedBy")]
    public long? DeletedBy { get; init; }
}

/// <summary>
/// Response returned after successfully restoring a soft-deleted community answer.
/// </summary>
public sealed class RestoreCommunityAnswerResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("isDeleted")]
    public required bool IsDeleted { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; init; }

    [JsonPropertyName("deletedBy")]
    public long? DeletedBy { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully creating a new community answer.
/// </summary>
public sealed class CreateCommunityAnswerResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("questionId")]
    public required long QuestionId { get; init; }

    [JsonPropertyName("authorId")]
    public required long AuthorId { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Response returned after successfully updating a community answer.
/// </summary>
public sealed class UpdateCommunityAnswerResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
