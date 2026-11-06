using Microsoft.AspNetCore.Http.HttpResults;
using SharpAPI.Api.Caching;
using SharpAPI.Application.Abstractions;
using SharpAPI.Application.Lookups;

namespace SharpAPI.Api.Endpoints;

/// <summary>
/// Read-only reference data for the product form's dropdowns.
/// </summary>
/// <remarks>
/// CQRS-lite: a simple read with no business rules calls the query interface
/// directly. A pass-through "LookupService" would add a layer with no value (YAGNI).
/// The 5-minute output cache means most calls never touch the database.
/// </remarks>
internal static class LookupEndpoints
{
    public static IEndpointRouteBuilder MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/lookups")
            .WithTags("Lookups")
            .CacheOutput(CachePolicies.Lookups);

        group.MapGet("/categories", GetCategories).WithName("GetCategories");
        group.MapGet("/suppliers", GetSuppliers).WithName("GetSuppliers");

        return app;
    }

    internal static async Task<Ok<IReadOnlyList<LookupDto>>> GetCategories(
        ILookupQueries lookups, CancellationToken cancellationToken) =>
        TypedResults.Ok(await lookups.GetCategoriesAsync(cancellationToken));

    internal static async Task<Ok<IReadOnlyList<LookupDto>>> GetSuppliers(
        ILookupQueries lookups, CancellationToken cancellationToken) =>
        TypedResults.Ok(await lookups.GetSuppliersAsync(cancellationToken));
}
