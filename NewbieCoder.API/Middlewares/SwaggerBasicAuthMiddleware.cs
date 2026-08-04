using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NewbieCoder.API.Options;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Protects /swagger endpoints with HTTP Basic Authentication using credentials resolved
/// at startup via <see cref="SwaggerAuthOptions"/>.
/// </summary>
public sealed class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _username;
    private readonly string _password;

    public SwaggerBasicAuthMiddleware(RequestDelegate next, IOptions<SwaggerAuthOptions> options)
    {
        _next = next;
        _username = options.Value.Username;
        _password = options.Value.Password;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only protect Swagger UI and OpenAPI JSON endpoints.
        if (!path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Authorization header is in format: "Basic <base64(username:password)>"
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            await Challenge(context);
            return;
        }

        try
        {
            var encoded = authHeader["Basic ".Length..].Trim();
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var separator = decoded.IndexOf(':');
            if (separator < 0)
            {
                await Challenge(context);
                return;
            }

            var user = decoded[..separator];
            var pass = decoded[(separator + 1)..];

            if (user != _username || pass != _password)
            {
                await Challenge(context);
                return;
            }

            await _next(context);
        }
        catch
        {
            await Challenge(context);
        }
    }

    private static async Task Challenge(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(@"
<!doctype html>
<html>
<head><title>401 Unauthorized</title></head>
<body style='font-family:sans-serif;padding:40px;text-align:center'>
  <h2>&#128274; Swagger — Authentication Required</h2>
  <p>Please enter your credentials to access the API documentation.</p>
  <p><em>This dialog may not appear in all browsers — use the Authorize button in the top-right corner instead.</em></p>
</body>
</html>");
    }
}
