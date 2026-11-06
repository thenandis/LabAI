using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SharpAPI.Application.Abstractions;
using SharpAPI.Application.Common;
using SharpAPI.Application.Products;
using SharpAPI.Domain.Entities;

namespace SharpAPI.UnitTests.Application;

/// <summary>
/// ProductService orchestration tests. Every dependency is an interface, so each one is
/// replaced with a fake. This is the PAYOFF of Dependency Inversion: no database, no web
/// server, and each test runs in about a millisecond.
/// </summary>
/// <remarks>Structure: Arrange / Act / Assert (AAA).</remarks>
public sealed class ProductServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IProductQueries _queries = Substitute.For<IProductQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<ProductSearchQuery> _searchValidator = PassingValidator<ProductSearchQuery>();
    private readonly IValidator<CreateProductRequest> _createValidator = PassingValidator<CreateProductRequest>();
    private readonly IValidator<UpdateProductRequest> _updateValidator = PassingValidator<UpdateProductRequest>();

    private ProductService CreateSut() => new( // SUT = "system under test"
        _repository, _queries, _unitOfWork,
        _searchValidator, _createValidator, _updateValidator,
        NullLogger<ProductService>.Instance);

    private static IValidator<T> PassingValidator<T>()
    {
        var validator = Substitute.For<IValidator<T>>();
        validator.ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        return validator;
    }

    private static CreateProductRequest NewRequest() => new("Chai", 1, 1, null, 18m, 39, 0, 10, false);

    private static UpdateProductRequest UpdateRequest() => new("Chai v2", 1, 1, null, 19m, 39, 0, 10, false, [1, 2, 3]);

    private static ProductDto Dto(int id) =>
        new(id, "Chai", 1, "Exotic Liquids", 1, "Beverages", null, 18m, 39, 0, 10, false, false, [1]);

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        _queries.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((ProductDto?)null);

        var result = await CreateSut().GetByIdAsync(42, Ct);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Create_WhenInvalid_ReturnsValidationError_AndDoesNotSave()
    {
        _createValidator.ValidateAsync(Arg.Any<CreateProductRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Product name is required.")]));

        var result = await CreateSut().CreateAsync(NewRequest() with { Name = "" }, Ct);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.True(result.Error.ValidationErrors!.ContainsKey("Name"));
        // Verify behaviour: nothing was staged or committed.
        _repository.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WhenValid_AddsSavesAndReturnsDto()
    {
        _queries.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Dto(0));

        var result = await CreateSut().CreateAsync(NewRequest(), Ct);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Add(Arg.Is<Product>(p => p.Name == "Chai"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await CreateSut().UpdateAsync(7, UpdateRequest(), Ct);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Update_SetsExpectedRowVersion_FromClient()
    {
        var product = Product.Create(NewRequest().ToDetails());
        var request = UpdateRequest();
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);
        _queries.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Dto(1));

        var result = await CreateSut().UpdateAsync(1, request, Ct);

        Assert.True(result.IsSuccess);
        _repository.Received(1).SetExpectedRowVersion(product, request.RowVersion);
        Assert.Equal("Chai v2", product.Name);
    }

    [Fact]
    public async Task Update_WhenConcurrencyConflict_ReturnsConflict()
    {
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(Product.Create(NewRequest().ToDetails()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyConflictException("stale"));

        var result = await CreateSut().UpdateAsync(1, UpdateRequest(), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal(ProductErrors.ConcurrencyConflict.Code, result.Error.Code);
    }

    [Fact]
    public async Task Delete_WhenFound_RemovesAndSaves()
    {
        var product = Product.Create(NewRequest().ToDetails());
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        var result = await CreateSut().DeleteAsync(1, Ct);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Remove(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
