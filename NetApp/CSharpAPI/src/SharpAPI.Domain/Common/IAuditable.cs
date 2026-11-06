namespace SharpAPI.Domain.Common;

/// <summary>
/// Marker for entities that record when they were created and last modified.
/// </summary>
/// <remarks>
/// HOW THIS FITS: no service or endpoint sets these values by hand. The
/// Infrastructure layer's <c>AuditableEntityInterceptor</c> (an EF Core SaveChanges
/// interceptor) finds every IAuditable entity being saved and stamps the times.
/// That is a cross-cutting concern handled once rather than repeated in every use case.
/// The properties are read-only here, so application code can't tamper with them;
/// EF Core writes them through its change tracker.
/// </remarks>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; }
    DateTime? ModifiedAtUtc { get; }
}
