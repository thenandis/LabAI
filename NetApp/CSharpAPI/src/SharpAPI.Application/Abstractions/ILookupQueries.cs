using SharpAPI.Application.Lookups;

namespace SharpAPI.Application.Abstractions;

/// <summary>
/// Read-only reference data: the categories and suppliers that fill the UI dropdowns
/// and that the validators check foreign keys against.
/// </summary>
public interface ILookupQueries
{
    Task<IReadOnlyList<LookupDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SupplierExistsAsync(int id, CancellationToken cancellationToken = default);
}
