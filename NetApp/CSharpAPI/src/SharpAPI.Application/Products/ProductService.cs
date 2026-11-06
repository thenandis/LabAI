using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using SharpAPI.Application.Abstractions;
using SharpAPI.Application.Common;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Application.Products;

/// <summary>
/// Orchestrates the product use cases: validate → load → apply domain logic → save → return DTO.
/// </summary>
/// <remarks>
/// <para><b>SOLID in this one class:</b></para>
/// <list type="bullet">
///   <item><b>S</b>ingle Responsibility: it ORCHESTRATES. Validation rules live in
///         validators, business rules in the Product entity, SQL in Infrastructure, HTTP in endpoints.</item>
///   <item><b>O</b>pen/Closed: new behaviour (caching, auditing) can wrap IProductService as a
///         decorator without editing this class.</item>
///   <item><b>L</b>iskov: any IProductRepository implementation (EF, Dapper, in-memory fake)
///         can be substituted and the service still works; the unit tests do exactly that.</item>
///   <item><b>I</b>nterface Segregation: it depends on small, focused interfaces (repository vs. queries).</item>
///   <item><b>D</b>ependency Inversion: every dependency is an abstraction injected through
///         the constructor (a C# 12 primary constructor).</item>
/// </list>
/// <para>
/// <c>internal sealed</c>: outside code can only reach it through IProductService
/// (registered in DependencyInjection.cs), and sealed lets the JIT devirtualise calls.
/// </para>
/// </remarks>
internal sealed class ProductService(
    IProductRepository repository,
    IProductQueries queries,
    IUnitOfWork unitOfWork,
    IValidator<ProductSearchQuery> searchValidator,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<Result<PagedResult<ProductDto>>> SearchAsync(
        ProductSearchQuery query, CancellationToken cancellationToken = default)
    {
        var validation = await searchValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return ToValidationError(validation);

        return await queries.SearchAsync(query, cancellationToken);
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await queries.GetByIdAsync(id, cancellationToken);

        // Implicit conversions on Result<T> make both branches read naturally.
        return dto is null ? ProductErrors.NotFound(id) : dto;
    }

    public async Task<Result<ProductDto>> CreateAsync(
        CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validate input (cheap checks first; fail fast before any write).
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ToValidationError(validation);

        // 2. Domain: the factory method enforces the invariants.
        var product = Product.Create(request.ToDetails());

        // 3. Persist: the repository stages the insert, and the unit of work commits it.
        repository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Structured logging: {ProductId} becomes a searchable field in Seq / App Insights / ELK,
        // not just text inside a string.
        logger.LogInformation("Product {ProductId} '{ProductName}' created", product.Id, product.Name);

        // 4. Re-read through the query side, so the response has the joined
        //    CategoryName/SupplierName and the DB-generated RowVersion.
        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<Result<ProductDto>> UpdateAsync(
        int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ToValidationError(validation);

        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return ProductErrors.NotFound(id);

        // OPTIMISTIC CONCURRENCY: "only save if the row still has the version I loaded".
        // EF adds `WHERE RowVersion = @clientVersion` to the UPDATE. Zero rows affected means
        // someone else changed it, and EF throws, which we translate to 409 below.
        repository.SetExpectedRowVersion(product, request.RowVersion);

        product.Update(request.ToDetails());

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            logger.LogWarning("Concurrency conflict updating product {ProductId}", id);
            return ProductErrors.ConcurrencyConflict;
        }

        logger.LogInformation("Product {ProductId} updated", id);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return ProductErrors.NotFound(id);

        repository.Remove(product);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Deleted by someone else between our SELECT and DELETE.
            return ProductErrors.ConcurrencyConflict;
        }

        logger.LogInformation("Product {ProductId} deleted", id);
        return Result.Success();
    }

    /// <summary>FluentValidation failures → { "Name": ["msg1", "msg2"], ... }.</summary>
    private static Error ToValidationError(ValidationResult validation) =>
        Error.Validation(validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()));
}
