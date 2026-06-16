using NewbieCoder.API.Middlewares;

namespace NewbieCoder.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<RequestTraceMiddleware>();
        app.UseRateLimiter();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers()
            .RequireRateLimiting(RateLimitingExtensions.DefaultPolicyName);
        app.MapHealthChecks("/health");

        return app;
    }
}
