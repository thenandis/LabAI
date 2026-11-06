namespace SharpAPI.Application.Products;

/// <summary>
/// Query-string parameters for <c>GET /api/v1/products</c>.
/// e.g. <c>?search=chai&amp;categoryId=1&amp;sortBy=unitPrice&amp;sortDir=desc&amp;page=2&amp;pageSize=20</c>
/// </summary>
/// <remarks>
/// The endpoint binds this with <c>[AsParameters]</c>. The constructor parameters
/// have default values, so every query-string key is optional.
/// </remarks>
public sealed record ProductSearchQuery(
    string? Search = null,
    int? CategoryId = null,
    int? SupplierId = null,
    bool IncludeDiscontinued = true,
    string SortBy = ProductSortFields.Name,
    string SortDir = "asc",
    int Page = 1,
    int PageSize = 10)
{
    public const int MaxPageSize = 100;

    public bool IsDescending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Whitelist of sortable fields.
/// </summary>
/// <remarks>
/// SECURITY: never build ORDER BY from raw user input (SQL injection, or a DoS
/// from sorting on an unindexed column). The client picks a KEY from this list, and
/// Infrastructure maps each key to a strongly-typed expression.
/// </remarks>
public static class ProductSortFields
{
    public const string Id = "id";
    public const string Name = "name";
    public const string Category = "category";
    public const string UnitPrice = "unitPrice";
    public const string UnitsInStock = "unitsInStock";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>([Id, Name, Category, UnitPrice, UnitsInStock], StringComparer.OrdinalIgnoreCase);
}
