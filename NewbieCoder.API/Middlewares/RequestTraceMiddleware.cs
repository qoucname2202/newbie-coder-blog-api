using NewbieCoder.Core.Constants;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Assigns a correlation ID (request trace) to every request.
/// Reads X-Request-Trace from the client when valid; otherwise generates a new GUID.
/// </summary>
public class RequestTraceMiddleware(RequestDelegate next)
{
    public const string RequestTraceHeaderName = "X-Request-Trace";

    public async Task InvokeAsync(HttpContext context)
    {
        var trace = context.Request.Headers[RequestTraceHeaderName].FirstOrDefault();

        // Reuse client trace only when it is a valid GUID.
        if (string.IsNullOrWhiteSpace(trace) || !Guid.TryParse(trace, out _))
            trace = Guid.NewGuid().ToString();

        context.Items[HttpContextItemKeys.RequestTrace] = trace;
        context.Response.Headers[RequestTraceHeaderName] = trace;

        await next(context);
    }
}
