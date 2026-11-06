using NSubstitute;
using SharpAPI.Application.Abstractions;
using SharpAPI.Application.Products;
using SharpAPI.Application.Products.Validators;

namespace SharpAPI.UnitTests.Application;

/// <summary>
/// Validators tested in isolation; the DB-backed lookup is faked with NSubstitute.
/// </summary>
public sealed class ProductValidatorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ILookupQueries _lookups = Substitute.For<ILookupQueries>();

    public ProductValidatorTests()
    {
        // Arrange the fake: only category 1 and supplier 1 "exist".
        _lookups.CategoryExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<int>(0) == 1);
        _lookups.SupplierExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<int>(0) == 1);
    }

    private static CreateProductRequest ValidCreate() =>
        new("Chai", SupplierId: 1, CategoryId: 1, "10 boxes", 18m, 39, 0, 10, false);

    [Fact]
    public async Task Create_ValidRequest_Passes()
    {
        var result = await new CreateProductRequestValidator(_lookups).ValidateAsync(ValidCreate(), Ct);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_UnknownCategory_FailsOnCategoryId()
    {
        var request = ValidCreate() with { CategoryId = 999 };

        var result = await new CreateProductRequestValidator(_lookups).ValidateAsync(request, Ct);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.CategoryId));
    }

    [Fact]
    public async Task Create_EmptyNameAndNegativePrice_ReportsBothErrors()
    {
        var request = ValidCreate() with { Name = "", UnitPrice = -5m };

        var result = await new CreateProductRequestValidator(_lookups).ValidateAsync(request, Ct);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.UnitPrice));
    }

    [Fact]
    public async Task Update_MissingRowVersion_Fails()
    {
        var request = new UpdateProductRequest("Chai", 1, 1, null, 18m, 39, 0, 10, false, RowVersion: []);

        var result = await new UpdateProductRequestValidator(_lookups).ValidateAsync(request, Ct);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductRequest.RowVersion));
    }

    [Theory]
    [InlineData(0, 10, "name", "asc", false)]
    [InlineData(1, 0, "name", "asc", false)]
    [InlineData(1, 101, "name", "asc", false)]
    [InlineData(1, 10, "password", "asc", false)] // non-whitelisted sort column
    [InlineData(1, 10, "unitPrice", "sideways", false)]
    [InlineData(1, 10, "UNITPRICE", "DESC", true)] // case-insensitive
    public void SearchQuery_Rules(int page, int pageSize, string sortBy, string sortDir, bool expectedValid)
    {
        var query = new ProductSearchQuery(SortBy: sortBy, SortDir: sortDir, Page: page, PageSize: pageSize);

        var result = new ProductSearchQueryValidator().Validate(query);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
