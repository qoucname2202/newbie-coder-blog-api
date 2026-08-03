using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.Exceptions;

/// <summary>
/// Exception thrown when image upload operations fail.
/// </summary>
public sealed class ImageUploadException : Exception
{
    /// <summary>
    /// Business response code for this exception.
    /// </summary>
    public string ResponseCode { get; }

    /// <summary>
    /// HTTP status code for this exception.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Creates a new instance of ImageUploadException.
    /// </summary>
    /// <param name="message">User-friendly error message.</param>
    /// <param name="responseCode">Business response code.</param>
    /// <param name="statusCode">HTTP status code.</param>
    public ImageUploadException(
        string message,
        string responseCode = ResponseCodes.InternalError,
        int statusCode = HttpStatusCodes.InternalServerError)
        : base(message)
    {
        ResponseCode = responseCode;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a new instance of ImageUploadException with an inner exception.
    /// </summary>
    /// <param name="message">User-friendly error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="responseCode">Business response code.</param>
    /// <param name="statusCode">HTTP status code.</param>
    public ImageUploadException(
        string message,
        Exception innerException,
        string responseCode = ResponseCodes.InternalError,
        int statusCode = HttpStatusCodes.InternalServerError)
        : base(message, innerException)
    {
        ResponseCode = responseCode;
        StatusCode = statusCode;
    }
}
