namespace NewbieCoder.Core.ViewModels;


public class ApiResponse<T>
{
    public string RequestTrace { get; init; } = default!;
    public string ResponseDateTime { get; init; } = default!;
    public T? ResponseData { get; init; }
    public ResponseStatus? ResponseStatus { get; private set; }

    public static ApiResponse<T> Ok(T data, string requestTrace) => new()
    {
        RequestTrace = Guid.NewGuid().ToString(),
        ResponseDateTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
        ResponseStatus = new ResponseStatus
        {
            ResponseCode = "000000",
            ResponseMessage = "Success",
            TracingMessage = null
        },
        ResponseData = data
    };
    public static ApiResponse<T> Ok(T data) => new()
    {

        ResponseDateTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
        ResponseStatus = new ResponseStatus
        {
            ResponseCode = "000000",
            ResponseMessage = "Success",
            TracingMessage = null
        },
        ResponseData = data
    };

    public static ApiResponse<T> Error(
        string requestTrace,
        string responseCode,
        string responseMessage,
        string? tracingMessage = null) => new()
        {
            RequestTrace = Guid.NewGuid().ToString(),
            ResponseDateTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            ResponseStatus = new ResponseStatus
            {
                ResponseCode = responseCode,
                ResponseMessage = responseMessage,
                TracingMessage = tracingMessage
            }
        };
}