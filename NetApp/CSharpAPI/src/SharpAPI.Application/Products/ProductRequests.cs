namespace SharpAPI.Application.Products;

/// <summary>
/// The fields shared by create and update requests.
/// </summary>
/// <remarks>
/// WHY AN INTERFACE: both request records implement it, so ONE validator
/// (<see cref="Validators.ProductWriteModelValidator"/>) and ONE mapping method
/// (<see cref="ProductMappings.ToDetails"/>) serve both, which keeps the code DRY.
/// </remarks>
public interface IProductWriteModel
{
    string Name { get; }
    int? SupplierId { get; }
    int? CategoryId { get; }
    string? QuantityPerUnit { get; }
    decimal UnitPrice { get; }
    short UnitsInStock { get; }
    short UnitsOnOrder { get; }
    short ReorderLevel { get; }
    bool Discontinued { get; }
}

/// <summary>Body of <c>POST /api/v1/products</c>.</summary>
/// <remarks>
/// Note what is NOT here: Id, CreatedAtUtc, RowVersion. The client can't set them
/// (protection against "mass assignment" / over-posting).
/// </remarks>
public sealed record CreateProductRequest(
    string Name,
    int? SupplierId,
    int? CategoryId,
    string? QuantityPerUnit,
    decimal UnitPrice,
    short UnitsInStock,
    short UnitsOnOrder,
    short ReorderLevel,
    bool Discontinued) : IProductWriteModel;

/// <summary>Body of <c>PUT /api/v1/products/{id}</c>.</summary>
/// <param name="RowVersion">
/// The concurrency token the client received with the product. If it no longer matches
/// the database, someone else saved first and the API returns 409 Conflict.
/// </param>
public sealed record UpdateProductRequest(
    string Name,
    int? SupplierId,
    int? CategoryId,
    string? QuantityPerUnit,
    decimal UnitPrice,
    short UnitsInStock,
    short UnitsOnOrder,
    short ReorderLevel,
    bool Discontinued,
    byte[] RowVersion) : IProductWriteModel;
