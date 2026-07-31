using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Tag data access operations used by the admin module.
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Returns a paginated list of tags matching the filter criteria.
    /// </summary>
    Task<PaginatedResponse<TagListItemResponse>> GetPagedAsync(
        TagFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full tag details (no tracking) by ID, including association counts.
    /// Returns null if not found or deleted.
    /// </summary>
    Task<TagDetailResponse?> GetDetailByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked Tag entity by ID (for updates). Returns null if not found or deleted.
    /// </summary>
    Task<Tag?> GetTrackedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked Tag entity by ID if it is soft-deleted. Returns null otherwise.
    /// </summary>
    Task<Tag?> GetTrackedDeletedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an active (non-deleted) tag by ID, or null.
    /// </summary>
    Task<Tag?> GetActiveByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a tag with the given slug exists (case-insensitive).
    /// </summary>
    Task<bool> ExistsSlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a tag with the given slug exists, excluding the specified tag ID.
    /// </summary>
    Task<bool> ExistsSlugExcludingIdAsync(
        long id,
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all PostTag associations for a source tag (for merge operation).
    /// </summary>
    Task<IReadOnlyList<PostTag>> GetPostTagsByTagIdAsync(long tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all InterviewQuestionTag associations for a source tag (for merge operation).
    /// </summary>
    Task<IReadOnlyList<InterviewQuestionTag>> GetInterviewQuestionTagsByTagIdAsync(long tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all CommunityQuestionTag associations for a source tag (for merge operation).
    /// </summary>
    Task<IReadOnlyList<CommunityQuestionTag>> GetCommunityQuestionTagsByTagIdAsync(long tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the target tag already has a PostTag for the given postId.
    /// </summary>
    Task<bool> HasPostTagAsync(long tagId, long postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the target tag already has an InterviewQuestionTag for the given questionId.
    /// </summary>
    Task<bool> HasInterviewQuestionTagAsync(long tagId, long questionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the target tag already has a CommunityQuestionTag for the given questionId.
    /// </summary>
    Task<bool> HasCommunityQuestionTagAsync(long tagId, long questionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new PostTag association.
    /// </summary>
    Task AddPostTagAsync(PostTag postTag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new InterviewQuestionTag association.
    /// </summary>
    Task AddInterviewQuestionTagAsync(InterviewQuestionTag iqt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new CommunityQuestionTag association.
    /// </summary>
    Task AddCommunityQuestionTagAsync(CommunityQuestionTag cqt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a PostTag association.
    /// </summary>
    void RemovePostTag(PostTag postTag);

    /// <summary>
    /// Removes an InterviewQuestionTag association.
    /// </summary>
    void RemoveInterviewQuestionTag(InterviewQuestionTag iqt);

    /// <summary>
    /// Removes a CommunityQuestionTag association.
    /// </summary>
    void RemoveCommunityQuestionTag(CommunityQuestionTag cqt);

    /// <summary>
    /// Adds a new tag entity.
    /// </summary>
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
