using SharpAPI.Domain.Entities;

namespace SharpAPI.Application.Abstractions;

/// <summary>
/// WRITE-side access to Product aggregates (the "C" in CQRS: Commands).
/// </summary>
/// <remarks>
/// <para><b>CQRS-lite: why separate reads from writes?</b></para>
/// <para>
/// Writes need full, change-TRACKED entities so business rules run (Product.Update)
/// and EF can detect what changed. Reads need flat DTOs, fast, with no tracking
/// overhead. Forcing both through one generic IRepository&lt;T&gt; either makes reads
/// slow (loading whole entities and mapping in memory) or leaks IQueryable everywhere.
/// So writes go through this interface and reads go through <see cref="IProductQueries"/>.
/// </para>
/// <para>
/// <b>Interface Segregation Principle (the "I" in SOLID):</b> the interface is small
/// and specific. Nothing here forces an implementer to support methods it doesn't need.
/// </para>
/// </remarks>
public interface IProductRepository
{
    /// <summary>Loads a tracked entity for modification, or null when it doesn't exist.</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Stages a new product. It is persisted on <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Add(Product product);

    /// <summary>Stages a deletion.</summary>
    void Remove(Product product);

    /// <summary>
    /// Records the RowVersion the CLIENT last saw, so the save fails with a conflict
    /// if the row has changed since then (optimistic concurrency).
    /// </summary>
    void SetExpectedRowVersion(Product product, byte[] rowVersion);
}
