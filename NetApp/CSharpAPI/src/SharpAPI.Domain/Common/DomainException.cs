namespace SharpAPI.Domain.Common;

/// <summary>
/// Thrown when code tries to put an entity into an invalid state (a broken invariant).
/// </summary>
/// <remarks>
/// <para>
/// HOW THIS FITS: the Application layer validates requests first (FluentValidation)
/// and returns friendly 400 responses, so in normal use this exception should never
/// fire. It is the LAST line of defence: whoever calls the domain (a background job,
/// a future gRPC endpoint, a buggy refactor), the entity protects itself.
/// </para>
/// <para>
/// The API's GlobalExceptionHandler turns it into a 400 ProblemDetails response.
/// </para>
/// </remarks>
public sealed class DomainException(string message) : Exception(message);
