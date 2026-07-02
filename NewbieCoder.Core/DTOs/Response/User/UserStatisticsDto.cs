using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.User;

/// <summary>
/// Personal statistics returned alongside the user profile.
/// </summary>
public sealed class UserStatisticsDto
{
    [JsonPropertyName("total_bookmarked_questions")]
    public required int TotalBookmarkedQuestions { get; init; }

    [JsonPropertyName("total_mastered_questions")]
    public required int TotalMasteredQuestions { get; init; }

    [JsonPropertyName("total_learning_questions")]
    public required int TotalLearningQuestions { get; init; }

    [JsonPropertyName("total_created_questions")]
    public required int TotalCreatedQuestions { get; init; }

    [JsonPropertyName("total_published_questions")]
    public required int TotalPublishedQuestions { get; init; }

    [JsonPropertyName("total_comments")]
    public required int TotalComments { get; init; }
}
