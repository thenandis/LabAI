using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharpAPI.Application.Common;

namespace SharpAPI.Api.ErrorHandling;

/// <summary>
/// Translates the Application layer's transport-agnostic <see cref="Error"/> into HTTP.
/// </summary>
/// <remarks>
/// This is the ONLY place that knows "NotFound means 404". Changing the HTTP mapping
/// never touches business code, and the services never see HTTP.
/// </remarks>
internal static class ResultExtensions
{
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var (status, title, type) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "One or more validation errors occurred.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "The resource was not found.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "The request conflicts with the current state.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred while processing the request.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        // Validation errors use the "errors" dictionary shape that ASP.NET Core and most
        // front-end libraries understand: { "errors": { "name": ["Product name is required."] } }
        ProblemDetails problem = error is { Type: ErrorType.Validation, ValidationErrors: not null }
            ? new HttpValidationProblemDetails(error.ValidationErrors.ToDictionary(
                e => JsonNamingPolicy.CamelCase.ConvertName(e.Key), // "UnitPrice" → "unitPrice", matching the JSON/Angular field names
                e => e.Value))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Type = type;
        problem.Detail = error.Description;
        problem.Extensions["code"] = error.Code; // stable, machine-readable error code for clients

        return TypedResults.Problem(problem);
    }
}
