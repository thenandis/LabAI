using SharpAPI.Application.Common;

namespace SharpAPI.Application.Products;

/// <summary>
/// The product use cases: the application's public API for this feature.
/// </summary>
/// <remarks>
/// <para>
/// The endpoints depend on this INTERFACE, not on <see cref="ProductService"/>. That is
/// the Dependency Inversion Principle, and it is what makes the endpoints testable and
/// the implementation replaceable (e.g. wrapping it in a caching decorator later
/// without touching any caller: the Open/Closed Principle).
/// </para>
/// <para>
/// Every method takes a <see cref="CancellationToken"/>. If the browser navigates
/// away, ASP.NET Core cancels the token, and EF Core aborts the SQL query rather than
/// wasting DB time on a response no one will read.
/// </para>
/// </remarks>
public interface IProductService
{
    Task<Result<PagedResult<ProductDto>>> SearchAsync(ProductSearchQuery query, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
