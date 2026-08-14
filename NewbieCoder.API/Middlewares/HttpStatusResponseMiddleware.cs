using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Intercepts responses with non-success status codes (4xx) that were not handled
/// by ExceptionHandlingMiddleware, and rewrites them to the standard ApiResponse envelope.
/// Covers: 415 Unsupported Media Type, 405 Method Not Allowed, 404 Not Found, etc.
/// </summary>
public sealed class HttpStatusResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Only rewrite 4xx responses that don't already have our standard envelope.
        // Guard: if the response has already started, do not touch headers.
        if (context.Response.HasStarted)
        {
            return;
        }

        var is4xxWithNonJsonBody =
            context.Response.StatusCode >= 400
            && context.Response.StatusCode < 500
            && (context.Response.ContentType == null
                || !context.Response.ContentType.Contains(
                    "application/json", StringComparison.OrdinalIgnoreCase));

        if (!is4xxWithNonJsonBody)
        {
            return;
        }

        var requestTrace = context.GetRequestTrace();
        (string responseCode, string responseMessage) = context.Response.StatusCode switch
        {
            StatusCodes.Status415UnsupportedMediaType => (
                ResponseCodes.ValidationError,
                "Content-Type header is required. Expected 'application/json'."),
            StatusCodes.Status405MethodNotAllowed => (
                ResponseCodes.ValidationError,
                $"The HTTP method '{context.Request.Method}' is not allowed for this endpoint."),
            StatusCodes.Status404NotFound => (
                ResponseCodes.NotFound,
                "The requested resource was not found."),
            StatusCodes.Status401Unauthorized => (
                ResponseCodes.Unauthorized,
                ResponseMessages.Unauthenticated),
            StatusCodes.Status403Forbidden => (
                ResponseCodes.Forbidden,
                "You do not have permission to access this resource."),
            _ => (
                ResponseCodes.ValidationError,
                "The request could not be processed.")
        };

        context.Response.ContentType = "application/json";

        var envelope = new
        {
            RequestTrace = requestTrace,
            ResponseDateTime = DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ResponseData = (string?)null,
            ResponseStatus = new
            {
                ResponseCode = responseCode,
                ResponseMessage = responseMessage,
                TracingMessage = (string?)null
            }
        };

        await context.Response.WriteAsJsonAsync(envelope);
    }
}
