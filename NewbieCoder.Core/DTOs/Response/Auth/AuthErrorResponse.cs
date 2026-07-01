using System.Text.Json.Serialization;

namespace NewbieCoder.Core.DTOs.Response.Auth;

/// <summary>
/// Single field-level validation error, used inside <see cref="AuthErrorStatus"/>.
/// </summary>
public sealed class ValidationErrorItem
{
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

/// <summary>
/// Error response body for auth-related 4xx responses.
/// </summary>
public sealed class AuthErrorResponse
{
    [JsonPropertyName("requestTrace")]
    public required string RequestTrace { get; init; }

    [JsonPropertyName("responseDateTime")]
    public required string ResponseDateTime { get; init; }

    [JsonPropertyName("responseData")]
    public required string ResponseData { get; init; }

    [JsonPropertyName("responseStatus")]
    public required AuthErrorStatus ResponseStatus { get; init; }
}

/// <summary>
/// Contains the error code, message, and field-level errors returned in an auth error response.
/// </summary>
public sealed class AuthErrorStatus
{
    [JsonPropertyName("responseCode")]
    public required string ResponseCode { get; init; }

    [JsonPropertyName("responseMessage")]
    public required string ResponseMessage { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<ValidationErrorItem>? Errors { get; init; }

    [JsonPropertyName("tracingMessage")]
    public string? TracingMessage { get; init; }
}
