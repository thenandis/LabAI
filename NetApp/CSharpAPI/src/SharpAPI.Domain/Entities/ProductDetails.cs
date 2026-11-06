namespace SharpAPI.Domain.Entities;

/// <summary>
/// Parameter object holding every editable field of a <see cref="Product"/>.
/// </summary>
/// <remarks>
/// WHY: without it, <c>Product.Create(...)</c> and <c>Product.Update(...)</c> would
/// take nine positional parameters each, which is easy to mix up (two shorts next
/// to each other!). A record gives named fields, value equality and immutability.
/// </remarks>
public sealed record ProductDetails(
    string Name,
    int? SupplierId,
    int? CategoryId,
    string? QuantityPerUnit,
    decimal UnitPrice,
    short UnitsInStock,
    short UnitsOnOrder,
    short ReorderLevel,
    bool Discontinued);
