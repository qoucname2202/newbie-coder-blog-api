using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NewbieCoder.API.Middlewares;
using NewbieCoder.Core.Constants;

namespace NewbieCoder.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<RequestTraceMiddleware>();
        app.UseRateLimiter();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<HttpStatusResponseMiddleware>();

        // Serve uploaded files (avatars, etc.)
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
            RequestPath = "/uploads"
        });

        // Auth middleware must run before MVC to populate HttpContext.User from the Bearer token.
        app.UseMiddleware<AuthMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        // Role enforcement via [RequiresRole] attribute.
        app.UseApiAuthorization();

        // Protect ALL /swagger/* endpoints with HTTP Basic Auth — runs BEFORE UseSwagger()
        // so that unauthenticated requests are rejected at the gate, not after serving HTML.
        app.UseWhen(
            predicate: context => (context.Request.Path.Value ?? string.Empty)
                                      .StartsWith("/swagger", StringComparison.OrdinalIgnoreCase),
            configuration: appBuilder => appBuilder.UseMiddleware<SwaggerBasicAuthMiddleware>()
        );

        app.UseHttpsRedirection();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                $"/swagger/{AppConstants.ApiVersion}/swagger.json",
                $"{AppConstants.ApiTitle} {AppConstants.ApiVersion}");

            options.DocumentTitle = AppConstants.ApiTitle;
            options.DefaultModelsExpandDepth(2);
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.ShowCommonExtensions();
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        });
        app.MapControllers()
            .RequireRateLimiting(RateLimitingExtensions.DefaultPolicyName);

        return app;
    }
}
