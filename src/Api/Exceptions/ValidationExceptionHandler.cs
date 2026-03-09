using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Presentation.Exceptions;

public sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ValidationExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Detail = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
            }
        };

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        context.ProblemDetails.Extensions["errors"] = errors;

        logger.LogValidationExceptionWarning(
            JsonSerializer.Serialize(context.ProblemDetails), DateTime.UtcNow);

        if (!await problemDetailsService.TryWriteAsync(context))
        {
            await httpContext.Response.WriteAsJsonAsync(context.ProblemDetails, cancellationToken);
        }

        return true;
    }
}

public static partial class ValidationExceptionHandlerLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Validation failed: {ProblemDetails} at {OccurredAt:F}")]
    public static partial void LogValidationExceptionWarning(
        this ILogger logger,
        string problemDetails,
        DateTime occurredAt);
}
