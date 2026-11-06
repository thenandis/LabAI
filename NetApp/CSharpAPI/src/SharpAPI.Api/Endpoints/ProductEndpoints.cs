using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;
using SharpAPI.Api.Caching;
using SharpAPI.Api.ErrorHandling;
using SharpAPI.Application.Common;
using SharpAPI.Application.Products;

namespace SharpAPI.Api.Endpoints;

/// <summary>
/// The REST endpoints for products: the "C R U D" of the app.
/// </summary>
/// <remarks>
/// <para><b>Minimal APIs vs. Controllers:</b> Minimal APIs have less ceremony and are
/// faster (no MVC filter pipeline or model-binding reflection), and they're Microsoft's
/// recommended default for new APIs. Grouping routes in a static class keeps them as
/// organised as controllers.</para>
///
/// <para><b>REST conventions used:</b></para>
/// <code>
///  GET    /api/v1/products          → 200 + page        (list / search / sort / page)
///  GET    /api/v1/products/{id}     → 200 | 404         (read one)
///  POST   /api/v1/products          → 201 + Location    (create)    | 400
///  PUT    /api/v1/products/{id}     → 200               (replace)   | 400 | 404 | 409
///  DELETE /api/v1/products/{id}     → 204               (delete)    | 404
/// </code>
///
/// <para><b>TypedResults + Results&lt;T1,T2&gt;</b>: the return type DECLARES every possible
/// response. That gives compile-time safety, makes OpenAPI docs accurate automatically,
/// and lets unit tests assert on concrete result types.</para>
///
/// <para>Handlers are thin: bind input → call IProductService → map Result to HTTP.
/// There is no business logic here.</para>
/// </remarks>
internal static class ProductEndpoints
{
    private const string GetByIdRouteName = "GetProductById";

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // Route group: a shared prefix + shared metadata. URL versioning (/v1) lets us ship
        // a breaking /v2 later while existing clients keep working.
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapGet("/", SearchProducts)
            .WithName("SearchProducts")
            .WithSummary("Search, sort and page products")
            .CacheOutput(CachePolicies.Products);

        // {id:int} is a route CONSTRAINT: /products/abc never matches (404), so there's no parsing code.
        group.MapGet("/{id:int}", GetProductById)
            .WithName(GetByIdRouteName)
            .WithSummary("Get a product by id")
            .CacheOutput(CachePolicies.Products);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a product");

        group.MapPut("/{id:int}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithSummary("Update a product (requires rowVersion for optimistic concurrency)");

        group.MapDelete("/{id:int}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product");

        return app;
    }

    // [AsParameters] binds each property of ProductSearchQuery from the query string.
    // The services (IProductService, CancellationToken...) are injected per request,
    // which is method injection, Minimal API style.
    internal static async Task<Results<Ok<PagedResult<ProductDto>>, ProblemHttpResult>> SearchProducts(
        [AsParameters] ProductSearchQuery query,
        IProductService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchAsync(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    internal static async Task<Results<Ok<ProductDto>, ProblemHttpResult>> GetProductById(
        int id,
        IProductService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    internal static async Task<Results<CreatedAtRoute<ProductDto>, ProblemHttpResult>> CreateProduct(
        CreateProductRequest request,
        IProductService service,
        IOutputCacheStore cache,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();

        // The data changed, so purge every cached product response.
        await cache.EvictByTagAsync(CachePolicies.ProductsTag, cancellationToken);

        // 201 Created + "Location: /api/v1/products/78" header, the REST convention for creation.
        return TypedResults.CreatedAtRoute(result.Value, GetByIdRouteName, new { id = result.Value.Id });
    }

    internal static async Task<Results<Ok<ProductDto>, ProblemHttpResult>> UpdateProduct(
        int id,
        UpdateProductRequest request,
        IProductService service,
        IOutputCacheStore cache,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();

        await cache.EvictByTagAsync(CachePolicies.ProductsTag, cancellationToken);

        // Return the updated product: the client needs the NEW rowVersion for its next edit.
        return TypedResults.Ok(result.Value);
    }

    internal static async Task<Results<NoContent, ProblemHttpResult>> DeleteProduct(
        int id,
        IProductService service,
        IOutputCacheStore cache,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();

        await cache.EvictByTagAsync(CachePolicies.ProductsTag, cancellationToken);
        return TypedResults.NoContent();
    }
}
