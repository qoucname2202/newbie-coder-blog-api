using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Extensions;

public static class RateLimitingExtensions
{
    public const string DefaultPolicyName = "default";

    /// <summary>
    /// Registers fixed-window rate limiting for all controller endpoints.
    /// Limits are read from RateLimiting:PermitLimit and RateLimiting:WindowSeconds in configuration.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = HttpStatusCodes.TooManyRequests;

            options.AddFixedWindowLimiter(DefaultPolicyName, limiter =>
            {
                limiter.PermitLimit = permitLimit;
                limiter.Window = TimeSpan.FromSeconds(windowSeconds);
                limiter.QueueLimit = 0;
            });

            // Return the standard JSON envelope when the client exceeds the rate limit.
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.ContentType = "application/json";

                var trace = httpContext.GetRequestTrace();
                var response = ApiResponse<string>.Fail(
                    trace,
                    ResponseCodes.TooManyRequests,
                    ResponseMessages.TooManyRequests);

                await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };
        });

        return services;
    }
}
