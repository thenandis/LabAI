namespace SharpAPI.Application.Abstractions;

/// <summary>
/// Unit of Work: commits every change made during one business operation as a
/// single atomic transaction.
/// </summary>
/// <remarks>
/// <para>
/// Repositories only STAGE changes (Add/Remove/modify tracked entities). Nothing hits
/// the database until the use case calls <see cref="SaveChangesAsync"/>. A use case
/// touching three tables either fully succeeds or fully rolls back.
/// </para>
/// <para>
/// HOW THIS FITS: implemented by Infrastructure's UnitOfWork, which wraps
/// <c>DbContext.SaveChangesAsync</c> (EF Core's DbContext is itself a Unit of Work;
/// this interface just hides EF from the Application layer).
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <exception cref="Common.ConcurrencyConflictException">The row was changed by another user.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
