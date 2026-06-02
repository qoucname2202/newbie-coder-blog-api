using System.Net;
using System.Text.Json;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, logger);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        var statusCode = exception switch
        {
            BusinessException business => business.StatusCode,
            _ => (int)HttpStatusCode.InternalServerError
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.Fail(exception.Message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
