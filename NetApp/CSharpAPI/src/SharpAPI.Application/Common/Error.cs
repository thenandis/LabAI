namespace SharpAPI.Application.Common;

/// <summary>
/// Category of failure. The API maps each one to an HTTP status code
/// (see ResultExtensions.ToProblem in the Api project):
/// Validation → 400, NotFound → 404, Conflict → 409, Failure → 500.
/// </summary>
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict
}

/// <summary>
/// Describes WHY an operation failed, without knowing anything about HTTP.
/// </summary>
/// <remarks>
/// Keeping HTTP out of the Application layer means the same service could sit behind
/// gRPC, a message queue consumer or a CLI, and only the translation layer changes.
/// </remarks>
/// <param name="Code">Stable, machine-readable code (e.g. "Product.NotFound"); clients can switch on it.</param>
/// <param name="Description">Human-readable message.</param>
/// <param name="Type">Failure category.</param>
/// <param name="ValidationErrors">Field → messages, only for <see cref="ErrorType.Validation"/>.</param>
public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation, errors);
}
