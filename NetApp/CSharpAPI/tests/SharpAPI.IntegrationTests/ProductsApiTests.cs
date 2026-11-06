using System.Net;
using System.Net.Http.Json;
using SharpAPI.Application.Common;
using SharpAPI.Application.Products;

namespace SharpAPI.IntegrationTests;

/// <summary>
/// End-to-end HTTP tests: real routing, validation, EF Core, SQL, ProblemDetails and output cache.
/// </summary>
/// <remarks>
/// IClassFixture shares ONE factory (one app + one DB) across the tests in this class, so
/// startup cost is paid once. Tests create their own data rather than relying on
/// each other, so they stay order-independent.
/// </remarks>
public sealed class ProductsApiTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private const string BaseUrl = "/api/v1/products";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClient();

    private static CreateProductRequest NewProduct(string name = "Integration Tea") =>
        new(name, SupplierId: 1, CategoryId: 1, "20 bags", 9.99m, 100, 0, 10, false);

    [Fact]
    public async Task Search_ReturnsSeededNorthwindProducts_WithJoinedNames()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>($"{BaseUrl}?search=chai", Ct);

        var chai = Assert.Single(page!.Items);
        Assert.Equal("Chai", chai.Name);
        Assert.Equal("Beverages", chai.CategoryName);
        Assert.Equal("Exotic Liquids", chai.SupplierName);
    }

    [Fact]
    public async Task Search_SortsByPriceDescending_AndPages()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"{BaseUrl}?sortBy=unitPrice&sortDir=desc&pageSize=5", Ct);

        Assert.Equal(5, page!.Items.Count);
        Assert.Equal("Côte de Blaye", page.Items[0].Name); // the most expensive Northwind product
        Assert.True(page.TotalCount >= 77);
        Assert.Equal(page.Items.OrderByDescending(p => p.UnitPrice).Select(p => p.Id), page.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Search_InvalidPageSize_Returns400ProblemDetails()
    {
        var response = await _client.GetAsync($"{BaseUrl}?pageSize=5000", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Crud_FullLifecycle_Works()
    {
        // CREATE → 201 + Location header
        var create = await _client.PostAsJsonAsync(BaseUrl, NewProduct(), Ct);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location);
        var created = await create.Content.ReadFromJsonAsync<ProductDto>(Ct);
        Assert.Equal("Beverages", created!.CategoryName);
        Assert.NotEmpty(created.RowVersion);

        // READ (this also populates the output cache)
        var fetched = await _client.GetFromJsonAsync<ProductDto>($"{BaseUrl}/{created.Id}", Ct);
        Assert.Equal(created.Name, fetched!.Name);

        // UPDATE with the current rowVersion → 200 + a NEW rowVersion
        var update = new UpdateProductRequest("Integration Tea (Large)", 1, 1, "40 bags", 14.5m, 80, 0, 10, false, created.RowVersion);
        var updateResponse = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", update, Ct);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDto>(Ct);
        Assert.NotEqual(created.RowVersion, updated!.RowVersion);

        // READ AGAIN: the output cache must have been evicted, so we see the new name
        var refetched = await _client.GetFromJsonAsync<ProductDto>($"{BaseUrl}/{created.Id}", Ct);
        Assert.Equal("Integration Tea (Large)", refetched!.Name);

        // DELETE → 204, then 404
        var delete = await _client.DeleteAsync($"{BaseUrl}/{created.Id}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var gone = await _client.GetAsync($"{BaseUrl}/{created.Id}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409Conflict()
    {
        var created = await (await _client.PostAsJsonAsync(BaseUrl, NewProduct("Concurrency Coffee"), Ct))
            .Content.ReadFromJsonAsync<ProductDto>(Ct);

        // User A saves first...
        var userA = new UpdateProductRequest("Edited by A", 1, 1, null, 10m, 1, 0, 0, false, created!.RowVersion);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", userA, Ct)).StatusCode);

        // ...user B still holds the OLD rowVersion, so their save must be rejected (no lost update).
        var userB = userA with { Name = "Edited by B" };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", userB, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400WithFieldErrors()
    {
        var invalid = NewProduct() with { Name = "", CategoryId = 999, UnitPrice = -1 };

        var response = await _client.PostAsJsonAsync(BaseUrl, invalid, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains("\"name\"", body, StringComparison.Ordinal);       // camelCase keys match the Angular form fields
        Assert.Contains("\"categoryId\"", body, StringComparison.Ordinal);
        Assert.Contains("\"unitPrice\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/987654", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var response = await _client.GetAsync($"{BaseUrl}/1", Ct);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task HealthReady_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/ready", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
