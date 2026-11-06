using System.Linq.Expressions;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Application.Products;

/// <summary>
/// Hand-written mapping between entities, DTOs and requests.
/// </summary>
/// <remarks>
/// <para><b>Why not AutoMapper?</b> Explicit code is compile-time checked, easy to
/// debug (step into it!), has zero reflection cost, and the library went commercial
/// in 2025. With modern C# records, a mapping is a few lines.</para>
/// </remarks>
public static class ProductMappings
{
    /// <summary>
    /// The entity → DTO projection as an EXPRESSION TREE (not a compiled delegate).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This matters for performance. Because it is an <c>Expression&lt;Func&lt;...&gt;&gt;</c>,
    /// EF Core can read it and translate it into SQL:
    /// </para>
    /// <code>
    /// SELECT p.ProductID, p.ProductName, s.CompanyName, c.CategoryName, ...
    /// FROM Products p LEFT JOIN Categories c ... LEFT JOIN Suppliers s ...
    /// </code>
    /// <para>
    /// Only the needed columns are fetched, the joins happen in the DB, and no entities
    /// are tracked. A plain <c>Func</c> would force EF to load full entities first.
    /// </para>
    /// <para>It lives in Application (no EF types in it) and Infrastructure reuses it.</para>
    /// </remarks>
    public static readonly Expression<Func<Product, ProductDto>> ToDto = p => new ProductDto(
        p.Id,
        p.Name,
        p.SupplierId,
        p.Supplier != null ? p.Supplier.CompanyName : null,
        p.CategoryId,
        p.Category != null ? p.Category.Name : null,
        p.QuantityPerUnit,
        p.UnitPrice,
        p.UnitsInStock,
        p.UnitsOnOrder,
        p.ReorderLevel,
        p.Discontinued,
        // Same rule as Product.NeedsReorder, written inline so EF can translate it to SQL.
        !p.Discontinued && p.UnitsInStock + p.UnitsOnOrder <= p.ReorderLevel,
        p.RowVersion);

    /// <summary>Request (create OR update, thanks to the shared interface) → domain parameter object.</summary>
    public static ProductDetails ToDetails(this IProductWriteModel request) => new(
        request.Name,
        request.SupplierId,
        request.CategoryId,
        request.QuantityPerUnit,
        request.UnitPrice,
        request.UnitsInStock,
        request.UnitsOnOrder,
        request.ReorderLevel,
        request.Discontinued);
}
