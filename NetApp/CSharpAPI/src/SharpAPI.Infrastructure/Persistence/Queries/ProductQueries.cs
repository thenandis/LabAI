using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharpAPI.Application.Abstractions;
using SharpAPI.Application.Common;
using SharpAPI.Application.Products;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Infrastructure.Persistence.Queries;

/// <summary>
/// Read side for products, optimised for speed.
/// </summary>
/// <remarks>
/// <para><b>EF Core read-performance checklist (all applied here):</b></para>
/// <list type="number">
///   <item><c>AsNoTracking()</c>: skip change-tracker bookkeeping; the results are read-only.</item>
///   <item>Projection (<c>Select(ProductMappings.ToDto)</c>): SELECT only the needed columns, joins done in SQL.</item>
///   <item>Server-side filtering, sorting and paging: only one page leaves the database.</item>
///   <item>Parameterised queries: EF always parameterises, so SQL injection is impossible
///         and SQL Server reuses cached execution plans.</item>
///   <item>A stable sort (ThenBy Id), so rows don't jump between pages when values tie.</item>
/// </list>
/// </remarks>
internal sealed class ProductQueries(AppDbContext dbContext) : IProductQueries
{
    // Whitelisted sort keys → strongly-typed ORDER BY. Unknown keys never reach SQL
    // (the validator rejects them first; this falls back to Name as a safety net).
    private static readonly Dictionary<string, Func<IQueryable<Product>, bool, IOrderedQueryable<Product>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [ProductSortFields.Id] = (q, desc) => Order(q, p => p.Id, desc),
            [ProductSortFields.Name] = (q, desc) => Order(q, p => p.Name, desc),
            [ProductSortFields.Category] = (q, desc) => Order(q, p => p.Category!.Name, desc),
            [ProductSortFields.UnitPrice] = (q, desc) => Order(q, p => p.UnitPrice, desc),
            [ProductSortFields.UnitsInStock] = (q, desc) => Order(q, p => p.UnitsInStock, desc),
        };

    public async Task<PagedResult<ProductDto>> SearchAsync(
        ProductSearchQuery query, CancellationToken cancellationToken = default)
    {
        // IQueryable composes lazily: these Where calls build ONE SQL statement;
        // nothing executes until CountAsync/ToListAsync.
        IQueryable<Product> products = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // LIKE '%term%' is case-insensitive under SQL Server's default collation and
            // SQLite's ASCII rules. Wildcards in the user's text are escaped so "50%" matches literally.
            // At scale, a leading-wildcard LIKE can't use an index: switch to Full-Text Search
            // (CONTAINS) or Azure AI Search.
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            products = products.Where(p => EF.Functions.Like(p.Name, pattern, "\\"));
        }

        if (query.CategoryId is { } categoryId)
            products = products.Where(p => p.CategoryId == categoryId);

        if (query.SupplierId is { } supplierId)
            products = products.Where(p => p.SupplierId == supplierId);

        if (!query.IncludeDiscontinued)
            products = products.Where(p => !p.Discontinued);

        // Query 1: total count, for the pager (SELECT COUNT(*) ... WHERE ...)
        var totalCount = await products.CountAsync(cancellationToken);

        // Query 2: one page (SELECT cols ... ORDER BY ... OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY)
        var sort = Sorts.GetValueOrDefault(query.SortBy) ?? Sorts[ProductSortFields.Name];
        var items = await sort(products, query.IsDescending)
            .ThenBy(p => p.Id) // tie-breaker for deterministic paging
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ProductMappings.ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ProductMappings.ToDto)
            .FirstOrDefaultAsync(cancellationToken);

    private static IOrderedQueryable<Product> Order<TKey>(
        IQueryable<Product> query, Expression<Func<Product, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
