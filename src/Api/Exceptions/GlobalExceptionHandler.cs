using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Presentation.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = exception switch
        {
            ApplicationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = exception.GetType().Name,
                Title = "An exception occurred",
                Detail = exception.Message
            }
        };

        logger.LogGlobalExceptionError(
            JsonSerializer.Serialize(problemDetailsContext.ProblemDetails), DateTime.UtcNow);

        if (!await problemDetailsService.TryWriteAsync(problemDetailsContext))
        {
            await httpContext.Response.WriteAsJsonAsync(problemDetailsContext.ProblemDetails, cancellationToken);
        }

        return true;
    }
}

public static partial class GlobalExceptionHandlerLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred: {ProblemDetails} at {OccurredAt:F}")]
    public static partial void LogGlobalExceptionError(
        this ILogger logger,
        string problemDetails,
        DateTime occurredAt);
}
