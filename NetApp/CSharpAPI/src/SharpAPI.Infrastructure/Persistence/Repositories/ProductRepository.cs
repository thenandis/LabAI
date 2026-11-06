using Microsoft.EntityFrameworkCore;
using SharpAPI.Application.Abstractions;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the write-side product repository.
/// </summary>
/// <remarks>
/// Note what it does NOT do: call SaveChanges. Committing is the Unit of Work's job,
/// so a use case can stage several changes and commit them atomically.
/// </remarks>
internal sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        // FindAsync checks the change tracker first (no DB hit if already loaded), then
        // queries by primary key. The entity comes back TRACKED, ready for modification.
        dbContext.Products.FindAsync([id], cancellationToken).AsTask();

    public void Add(Product product) => dbContext.Products.Add(product);

    public void Remove(Product product) => dbContext.Products.Remove(product);

    public void SetExpectedRowVersion(Product product, byte[] rowVersion) =>
        // EF compares ORIGINAL values in the WHERE clause. By overwriting the original with
        // the client's token, the UPDATE becomes:
        //   UPDATE Products SET ... WHERE ProductID = @id AND RowVersion = @clientToken
        dbContext.Entry(product).Property(p => p.RowVersion).OriginalValue = rowVersion;
}
