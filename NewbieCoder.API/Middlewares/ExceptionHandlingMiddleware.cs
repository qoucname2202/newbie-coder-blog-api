using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Catches unhandled exceptions and returns the standard JSON failure envelope.
/// Maps BusinessException to the correct HTTP status and business responseCode.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestTrace = context.GetRequestTrace();

        var (statusCode, responseCode, responseMessage, tracingMessage) = exception switch
        {
            BusinessException business => (
                business.StatusCode,
                business.ResponseCode,
                business.Message,
                business.TracingMessage),
            _ => (
                HttpStatusCodes.InternalServerError,
                ResponseCodes.InternalError,
                ResponseMessages.InternalError,
                environment.IsDevelopment() ? exception.ToString() : null)
        };

        if (statusCode >= HttpStatusCodes.InternalServerError)
            logger.LogError(exception, "Unhandled exception. RequestTrace={RequestTrace}", requestTrace);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse<string>.Fail(
            requestTrace,
            responseCode,
            responseMessage,
            tracingMessage);

        await context.Response.WriteAsJsonAsync(response);
    }
}
