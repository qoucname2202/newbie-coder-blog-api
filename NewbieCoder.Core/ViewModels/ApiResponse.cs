using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.ViewModels;

public class ApiResponse<T>
{
    public required string RequestTrace { get; init; }
    public required string ResponseDateTime { get; init; }
    public T? ResponseData { get; init; }
    public required ResponseStatus ResponseStatus { get; init; }

    public static ApiResponse<T> Success(
        T data,
        string requestTrace,
        string responseMessage = ResponseMessages.Success,
        DateTimeOffset? responseDateTime = null) =>
        new()
        {
            RequestTrace = requestTrace,
            ResponseDateTime = FormatDateTime(responseDateTime ?? DateTimeOffset.Now),
            ResponseData = data,
            ResponseStatus = new ResponseStatus
            {
                ResponseCode = ResponseCodes.Success,
                ResponseMessage = responseMessage,
                TracingMessage = null
            }
        };

    public static ApiResponse<string> Fail(
        string requestTrace,
        string responseCode,
        string responseMessage,
        string? tracingMessage = null,
        DateTimeOffset? responseDateTime = null) =>
        new()
        {
            RequestTrace = requestTrace,
            ResponseDateTime = FormatDateTime(responseDateTime ?? DateTimeOffset.Now),
            ResponseData = string.Empty,
            ResponseStatus = new ResponseStatus
            {
                ResponseCode = responseCode,
                ResponseMessage = responseMessage,
                TracingMessage = tracingMessage
            }
        };

    internal static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd'T'HH:mm:sszzz");
}
