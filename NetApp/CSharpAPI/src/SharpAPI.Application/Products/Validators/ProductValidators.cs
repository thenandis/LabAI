using FluentValidation;
using SharpAPI.Application.Abstractions;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Application.Products.Validators;

/// <summary>
/// Rules shared by create and update requests (anything implementing <see cref="IProductWriteModel"/>).
/// </summary>
/// <remarks>
/// <para><b>Why FluentValidation rather than [Required] attributes?</b></para>
/// <list type="bullet">
///   <item>Rules live OUTSIDE the DTO (Single Responsibility: the DTO carries data, the validator validates).</item>
///   <item>Supports async rules that hit the database (e.g. "does CategoryId exist?").</item>
///   <item>Easy to unit test in isolation.</item>
/// </list>
/// <para>
/// HOW THIS FITS: ProductService runs the validator before touching the domain.
/// Failures become <c>Error.Validation</c>, then a 400 ValidationProblemDetails whose
/// "errors" dictionary the Angular form shows next to each field.
/// </para>
/// </remarks>
public sealed class ProductWriteModelValidator : AbstractValidator<IProductWriteModel>
{
    public ProductWriteModelValidator(ILookupQueries lookups)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(Product.NameMaxLength);

        RuleFor(x => x.QuantityPerUnit)
            .MaximumLength(Product.QuantityPerUnitMaxLength);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1_000_000)
            .PrecisionScale(19, 4, ignoreTrailingZeros: true); // matches DECIMAL(19,4)

        RuleFor(x => x.UnitsInStock).GreaterThanOrEqualTo((short)0);
        RuleFor(x => x.UnitsOnOrder).GreaterThanOrEqualTo((short)0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo((short)0);

        // Async rules: check that the foreign keys exist, so the client gets a clean 400
        // ("Category 99 does not exist") rather than a 500 from an FK violation in SQL.
        RuleFor(x => x.CategoryId)
            .MustAsync(async (id, ct) => await lookups.CategoryExistsAsync(id!.Value, ct))
            .When(x => x.CategoryId.HasValue)
            .WithMessage(x => $"Category {x.CategoryId} does not exist.");

        RuleFor(x => x.SupplierId)
            .MustAsync(async (id, ct) => await lookups.SupplierExistsAsync(id!.Value, ct))
            .When(x => x.SupplierId.HasValue)
            .WithMessage(x => $"Supplier {x.SupplierId} does not exist.");
    }
}

/// <summary>Create = the shared rules only.</summary>
public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    // Include() works because IValidator<in T> is CONTRAVARIANT: a validator for the
    // interface can validate any class implementing it.
    public CreateProductRequestValidator(ILookupQueries lookups) =>
        Include(new ProductWriteModelValidator(lookups));
}

/// <summary>Update = the shared rules + a concurrency token is mandatory.</summary>
public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator(ILookupQueries lookups)
    {
        Include(new ProductWriteModelValidator(lookups));

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required for updates (optimistic concurrency).");
    }
}

/// <summary>Guards the list endpoint against silly or abusive query strings.</summary>
public sealed class ProductSearchQueryValidator : AbstractValidator<ProductSearchQuery>
{
    public ProductSearchQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        // Capping the page size stops ?pageSize=1000000 from dumping the whole table.
        RuleFor(x => x.PageSize).InclusiveBetween(1, ProductSearchQuery.MaxPageSize);

        RuleFor(x => x.Search).MaximumLength(100);

        RuleFor(x => x.SortBy)
            .Must(ProductSortFields.All.Contains)
            .WithMessage($"SortBy must be one of: {string.Join(", ", ProductSortFields.All)}.");

        RuleFor(x => x.SortDir)
            .Must(d => d.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                       d.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDir must be 'asc' or 'desc'.");
    }
}
