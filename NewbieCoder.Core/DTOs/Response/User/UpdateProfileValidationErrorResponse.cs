using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.User;

/// <summary>
/// Per-field validation error returned when update validation fails.
/// </summary>
public sealed class FieldValidationError
{
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

/// <summary>
/// Response body when PATCH /api/v1/users/me fails due to validation errors.
/// </summary>
public sealed class UpdateProfileValidationErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("errors")]
    public required IReadOnlyList<FieldValidationError> Errors { get; init; }
}
