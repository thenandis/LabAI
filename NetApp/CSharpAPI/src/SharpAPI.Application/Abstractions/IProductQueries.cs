using SharpAPI.Application.Common;
using SharpAPI.Application.Products;

namespace SharpAPI.Application.Abstractions;

/// <summary>
/// READ-side access to products (the "Q" in CQRS: Queries).
/// </summary>
/// <remarks>
/// Returns DTOs projected directly in SQL (SELECT only the needed columns, with
/// no change tracking). This is the fastest way to read with EF Core, and the
/// Application layer still never sees EF types.
/// </remarks>
public interface IProductQueries
{
    Task<PagedResult<ProductDto>> SearchAsync(ProductSearchQuery query, CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
