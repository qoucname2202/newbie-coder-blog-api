using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.Exceptions;

public class BusinessException : Exception
{
    public int StatusCode { get; }
    public string ResponseCode { get; }

    public BusinessException(
        string message,
        int statusCode = HttpStatusCodes.BadRequest,
        string? responseCode = null,
        string? tracingMessage = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseCode = responseCode ?? ResponseCodes.FromHttpStatus(statusCode);
        TracingMessage = tracingMessage;
    }

    public string? TracingMessage { get; }
}
