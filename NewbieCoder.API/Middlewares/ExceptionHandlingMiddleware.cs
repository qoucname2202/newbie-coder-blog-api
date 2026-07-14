using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Catches unhandled exceptions and returns the standard JSON failure envelope.
/// Maps BusinessException to the correct HTTP status and business responseCode.
/// Also handles UpdateValidationException to return per-field validation errors.
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

        // Handle UpdateValidationException separately — uses custom response envelope.
        if (exception is UpdateValidationException validation)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = HttpStatusCodes.BadRequest;
            var validationResponse = new UpdateProfileValidationErrorResponse
            {
                Success = false,
                Message = ResponseMessages.UpdateValidationFailed,
                Errors = validation.Errors
            };
            await context.Response.WriteAsJsonAsync(validationResponse);
            return;
        }

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
