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
        // Guard: if the response has already started, we cannot write headers or body.
        // Re-throw so the host can handle it (e.g. 502 Bad Gateway from Kestrel).
        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "Response has already started; cannot write exception response. " +
                "ExceptionType={ExceptionType}, RequestTrace={RequestTrace}",
                exception.GetType().FullName,
                context.GetRequestTrace());
            throw exception;
        }

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

        // Handle ValidationException from data annotations (e.g. [RegularExpression] on DTOs).
        if (exception is System.ComponentModel.DataAnnotations.ValidationException validationEx)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = HttpStatusCodes.BadRequest;
            var validationResponse = ApiResponse<string>.Fail(
                requestTrace,
                ResponseCodes.ValidationError,
                validationEx.ValidationResult?.ErrorMessage ?? ResponseMessages.ValidationError);
            await context.Response.WriteAsJsonAsync(validationResponse);
            return;
        }

        // Handle JsonException from unknown fields rejected by StrictJsonConverterFactory.
        if (exception is System.Text.Json.JsonException jsonEx)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = HttpStatusCodes.BadRequest;
            var jsonResponse = ApiResponse<string>.Fail(
                requestTrace,
                ResponseCodes.ValidationError,
                jsonEx.Message);
            await context.Response.WriteAsJsonAsync(jsonResponse);
            return;
        }

        // Handle DatabaseException — already a safe, structured response; just pass through.
        if (exception is DatabaseException dbEx)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = dbEx.StatusCode;
            logger.LogError(dbEx,
                "Database exception. RequestTrace={RequestTrace}. ExceptionType={ExceptionType}. ExceptionMessage={ExceptionMessage}",
                requestTrace,
                dbEx.GetType().FullName,
                dbEx.Message);
            var dbResponse = ApiResponse<string>.Fail(
                requestTrace,
                dbEx.ResponseCode,
                dbEx.Message);
            await context.Response.WriteAsJsonAsync(dbResponse);
            return;
        }

        // Catch raw database / infrastructure exceptions from Npgsql and EF Core.
        //
        // Strategy:
        //   1. Explicitly catch NpgsqlException (base class) — covers PostgresException,
        //      NpgsqlConnectionException, NpgsqlTimeoutException, etc.
        //   2. Catch any exception whose type name or inner-exception chain contains
        //      known DB keywords (handles EF RetryLimitExceeded, TransactionAborted, etc.
        //      which are internal/embedded and may not be directly referenceable).
        //
        // None of these messages are ever exposed to the client.
        var ex = exception;
        var isDbError = exception is Npgsql.NpgsqlException
            || exception.GetType().FullName?.Contains("DbException",
                StringComparison.OrdinalIgnoreCase) == true
            || exception.GetType().FullName?.Contains("RetryLimitExceeded",
                StringComparison.OrdinalIgnoreCase) == true
            || exception.GetType().FullName?.Contains("TransactionAborted",
                StringComparison.OrdinalIgnoreCase) == true
            || exception.GetType().FullName?.Contains("Npgsql",
                StringComparison.OrdinalIgnoreCase) == true
            || exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("database", StringComparison.OrdinalIgnoreCase);

        // Walk inner exceptions to catch wrapped DB errors.
        while (!isDbError && ex.InnerException != null)
        {
            ex = ex.InnerException;
            isDbError = ex.GetType().FullName?.Contains("Exception",
                               StringComparison.OrdinalIgnoreCase) == true
                && (ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("database", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("Postgres", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase));
        }

        if (isDbError)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = HttpStatusCodes.ServiceUnavailable;
            logger.LogError(exception,
                "Database infrastructure exception. RequestTrace={RequestTrace}. " +
                "ExceptionType={ExceptionType}. ExceptionMessage={ExceptionMessage}",
                requestTrace,
                exception.GetType().FullName,
                exception.Message);
            var dbResponse = ApiResponse<string>.Fail(
                requestTrace,
                ResponseCodes.ServiceUnavailable,
                ResponseMessages.ServiceUnavailable);
            await context.Response.WriteAsJsonAsync(dbResponse);
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
        {
            logger.LogError(exception,
                "Unhandled exception. RequestTrace={RequestTrace}. " +
                "ExceptionType={ExceptionType}. ExceptionMessage={ExceptionMessage}",
                requestTrace,
                exception.GetType().FullName,
                exception.Message);
        }

        // Second HasStarted check before writing response (covers edge cases where
        // status code logic above triggers a write before the final response body).
        if (context.Response.HasStarted)
        {
            throw exception;
        }

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
