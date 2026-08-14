using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.Exceptions;

/// <summary>
/// Thrown when a database operation fails due to connectivity issues,
/// timeouts, or other infrastructure problems. Returns HTTP 503 / ServiceUnavailable
/// without leaking sensitive database information.
/// </summary>
public class DatabaseException : Exception
{
    public int StatusCode { get; }
    public string ResponseCode { get; }

    /// <summary>
    /// Creates a DatabaseException with a safe public message and optional custom response code.
    /// </summary>
    /// <param name="publicMessage">
    /// Safe message shown to the client. Should not contain DB connection strings,
    /// table names, or SQL details.
    /// </param>
    /// <param name="responseCode">
    /// Custom business response code (defaults to ServiceUnavailable = "00000503").
    /// </param>
    /// <param name="innerException">
    /// The original exception (logged server-side, never exposed to the client).
    /// </param>
    public DatabaseException(
        string? publicMessage = null,
        string? responseCode = null,
        Exception? innerException = null)
        : base(publicMessage ?? ResponseMessages.ServiceUnavailable, innerException)
    {
        StatusCode = HttpStatusCodes.ServiceUnavailable;
        ResponseCode = responseCode ?? ResponseCodes.ServiceUnavailable;
    }
}
