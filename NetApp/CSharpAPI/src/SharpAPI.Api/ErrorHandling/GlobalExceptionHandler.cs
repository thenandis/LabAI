using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SharpAPI.Domain.Common;

namespace SharpAPI.Api.ErrorHandling;

/// <summary>
/// Last-resort handler for UNEXPECTED exceptions (bugs, database down...).
/// </summary>
/// <remarks>
/// <para>
/// <b>IExceptionHandler</b> (.NET 8+) is the modern replacement for custom
/// try/catch middleware. It is registered in DI, so it can take dependencies.
/// </para>
/// <para>
/// SECURITY: in production the client gets a generic message plus a traceId, never the
/// stack trace or exception text, which could leak table names or connection details.
/// The full details go to the logs, where the traceId finds them.
/// </para>
/// </remarks>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            // Invariant violated in the domain (should normally be caught earlier by validators).
            DomainException => (StatusCodes.Status400BadRequest, "A business rule was violated."),

            // Malformed JSON, wrong content type, etc.
            BadHttpRequestException bad => (bad.StatusCode, "The request was malformed."),

            // The client disconnected or navigated away. Not a server error.
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested
                => (499, "Client closed request."),

            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Request {Method} {Path} failed with {Status}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, status, exception.Message);

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Client errors can safely echo the message; server errors only in Development.
                Detail = status < 500 || environment.IsDevelopment() ? exception.Message : null
            }
        });
    }
}
