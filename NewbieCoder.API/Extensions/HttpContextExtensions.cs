using NewbieCoder.Core.Constants;

namespace NewbieCoder.API.Extensions;

public static class HttpContextExtensions
{
    public static string GetRequestTrace(this HttpContext context)
    {
        if (context.Items.TryGetValue(HttpContextItemKeys.RequestTrace, out var value)
            && value is string trace
            && !string.IsNullOrWhiteSpace(trace))
        {
            return trace;
        }

        return Guid.NewGuid().ToString();
    }
}
