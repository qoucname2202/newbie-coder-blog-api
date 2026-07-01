using Microsoft.AspNetCore.Authorization;
using NewbieCoder.API.Attributes;
using NewbieCoder.Core.Constants;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Intercepts requests before they reach the action and enforces
/// <c>[RequiresRole]</c> attributes found on the controller or action method.
/// </summary>
public sealed class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // [RequiresRole] — OR: user must have ANY of the listed roles.
        var roleAttr = endpoint.Metadata.GetMetadata<RequiresRoleAttribute>();
        if (roleAttr != null)
        {
            var hasRole = roleAttr.Roles.Any(r => context.User.IsInRole(r));
            if (!hasRole)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    responseStatus = new
                    {
                        responseCode = ResponseCodes.Forbidden,
                        responseMessage = "You do not have the required role to access this resource."
                    }
                });
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to add the middleware to the pipeline.
/// </summary>
public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthorizationMiddleware>();
    }
}
