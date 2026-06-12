using System.Text.Json;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestTrace = context.TraceIdentifier;
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, logger, requestTrace);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ILogger logger,
        string requestTrace)
    {
        var (statusCode, responseCode, responseMessage) = exception switch
        {
            BusinessException bex => (bex.StatusCode, bex.ResponseCode, bex.Message),
            _ => (500, ResponseCodes.InternalError, "Internal Server Error")
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.Error(
            requestTrace,
            responseCode,
            responseMessage,
            tracingMessage: exception.Message);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}