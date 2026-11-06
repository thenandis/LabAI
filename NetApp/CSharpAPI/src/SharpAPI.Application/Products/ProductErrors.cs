using SharpAPI.Application.Common;

namespace SharpAPI.Application.Products;

/// <summary>
/// Catalogue of the errors the product use cases can return.
/// </summary>
/// <remarks>
/// Centralising them gives consistent codes and messages, makes them easy to assert in
/// tests, and documents every way the feature can fail in one place.
/// </remarks>
public static class ProductErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Product.NotFound", $"Product with id {id} was not found.");

    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "Product.ConcurrencyConflict",
        "This product was changed or deleted by someone else after you loaded it. Reload and try again.");
}
