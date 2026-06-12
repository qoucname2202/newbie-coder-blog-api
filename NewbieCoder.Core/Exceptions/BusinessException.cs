using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.Exceptions;

public class BusinessException : Exception
{
    public int StatusCode { get; }
    public string ResponseCode { get; }
    public BusinessException(string message, string responseCode = ResponseCodes.BadRequest, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
        ResponseCode = responseCode;
    }
}