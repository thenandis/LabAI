namespace SharpAPI.Application.Products;

/// <summary>
/// What the API RETURNS for a product. A DTO (Data Transfer Object) is the public
/// contract with the Angular UI.
/// </summary>
/// <remarks>
/// <para><b>Why not return the Product entity directly?</b></para>
/// <list type="bullet">
///   <item><b>Decoupling:</b> renaming a DB column or entity property won't break clients.</item>
///   <item><b>Security:</b> internal fields are never exposed by accident (over-posting / over-sharing).</item>
///   <item><b>Shape:</b> the UI wants CategoryName and SupplierName flattened in; the entity has navigation objects.</item>
///   <item><b>Serialization safety:</b> no lazy-loading surprises or circular references.</item>
/// </list>
/// <para>
/// <c>RowVersion</c> is a byte[]; System.Text.Json sends it as a Base64 string. The UI
/// holds it and sends it back on update, which enables optimistic concurrency.
/// </para>
/// </remarks>
public sealed record ProductDto(
    int Id,
    string Name,
    int? SupplierId,
    string? SupplierName,
    int? CategoryId,
    string? CategoryName,
    string? QuantityPerUnit,
    decimal UnitPrice,
    short UnitsInStock,
    short UnitsOnOrder,
    short ReorderLevel,
    bool Discontinued,
    bool NeedsReorder,
    byte[] RowVersion);
