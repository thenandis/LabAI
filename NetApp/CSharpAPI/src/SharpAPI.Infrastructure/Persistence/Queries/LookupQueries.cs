using Microsoft.EntityFrameworkCore;
using SharpAPI.Application.Abstractions;
using SharpAPI.Application.Lookups;

namespace SharpAPI.Infrastructure.Persistence.Queries;

/// <summary>
/// Reference-data queries for dropdowns and FK validation.
/// </summary>
/// <remarks>
/// This data rarely changes, so the API puts a 5-minute output cache in front of the
/// GET endpoints (see LookupEndpoints). Most dropdown loads never reach this class.
/// </remarks>
internal sealed class LookupQueries(AppDbContext dbContext) : ILookupQueries
{
    public async Task<IReadOnlyList<LookupDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LookupDto>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.CompanyName)
            .Select(s => new LookupDto(s.Id, s.CompanyName))
            .ToListAsync(cancellationToken);

    // AnyAsync → SELECT CASE WHEN EXISTS(...): the DB stops at the first match.
    public Task<bool> CategoryExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Categories.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> SupplierExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.AnyAsync(s => s.Id == id, cancellationToken);
}
