using SharpAPI.Domain.Common;

namespace SharpAPI.Domain.Entities;

/// <summary>
/// The aggregate root of this app: a sellable product. Maps to <c>dbo.Products</c>.
/// </summary>
/// <remarks>
/// <para><b>Rich domain model vs. anaemic model</b></para>
/// <para>
/// An "anaemic" entity is a bag of public get/set properties, and any code anywhere
/// can put it into a nonsense state (negative price, empty name). Here:
/// <list type="bullet">
///   <item>All setters are <c>private</c>. State changes ONLY through the methods
///         <see cref="Create"/> and <see cref="Update"/>.</item>
///   <item>Those methods enforce the invariants (encapsulation, one of the OOP pillars).</item>
///   <item>Business logic such as <see cref="NeedsReorder"/> lives WITH the data it
///         uses, not in a scattered "helper" class.</item>
/// </list>
/// </para>
/// <para>
/// HOW THIS FITS: <c>ProductService</c> (Application) calls <c>Product.Create</c> /
/// <c>product.Update</c>; EF Core (Infrastructure) persists the entity through its
/// private setters. The entity knows nothing about either of them.
/// </para>
/// </remarks>
public sealed class Product : IAuditable
{
    // Limits match the SQL column sizes (NVARCHAR(40), NVARCHAR(20)). They are
    // shared constants, so the validator, the EF mapping and the domain guard can't drift apart.
    public const int NameMaxLength = 40;
    public const int QuantityPerUnitMaxLength = 20;

    /// <summary>EF Core needs a parameterless constructor; private keeps application code from using it.</summary>
    private Product() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int? SupplierId { get; private set; }
    public int? CategoryId { get; private set; }
    public string? QuantityPerUnit { get; private set; }
    public decimal UnitPrice { get; private set; }
    public short UnitsInStock { get; private set; }
    public short UnitsOnOrder { get; private set; }
    public short ReorderLevel { get; private set; }
    public bool Discontinued { get; private set; }

    // ----- IAuditable (stamped by AuditableEntityInterceptor) -----
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. SQL Server changes this ROWVERSION value
    /// automatically on every UPDATE. If two users edit the same product, the second
    /// save sees a different RowVersion than it loaded, and the API returns 409 Conflict
    /// instead of silently overwriting the first user's change (a "lost update").
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    // ----- Navigation properties (loaded by EF Core when needed) -----
    public Category? Category { get; private set; }
    public Supplier? Supplier { get; private set; }

    /// <summary>
    /// Business rule: an active product needs reordering when stock plus units
    /// already on order has dropped to the reorder level.
    /// </summary>
    public bool NeedsReorder => !Discontinued && UnitsInStock + UnitsOnOrder <= ReorderLevel;

    /// <summary>
    /// Factory method: the ONLY way to create a valid Product.
    /// </summary>
    public static Product Create(ProductDetails details)
    {
        var product = new Product();
        product.Apply(details);
        return product;
    }

    /// <summary>Changes the editable fields, enforcing the same invariants as creation.</summary>
    public void Update(ProductDetails details) => Apply(details);

    // One private method keeps the validation rules for Create and Update identical (DRY).
    private void Apply(ProductDetails d)
    {
        if (string.IsNullOrWhiteSpace(d.Name))
            throw new DomainException("Product name is required.");
        if (d.Name.Trim().Length > NameMaxLength)
            throw new DomainException($"Product name cannot exceed {NameMaxLength} characters.");
        if (d.QuantityPerUnit?.Trim().Length > QuantityPerUnitMaxLength)
            throw new DomainException($"Quantity per unit cannot exceed {QuantityPerUnitMaxLength} characters.");
        if (d.UnitPrice < 0)
            throw new DomainException("Unit price cannot be negative.");
        if (d.UnitsInStock < 0 || d.UnitsOnOrder < 0 || d.ReorderLevel < 0)
            throw new DomainException("Stock quantities cannot be negative.");

        Name = d.Name.Trim();
        SupplierId = d.SupplierId;
        CategoryId = d.CategoryId;
        QuantityPerUnit = string.IsNullOrWhiteSpace(d.QuantityPerUnit) ? null : d.QuantityPerUnit.Trim();
        UnitPrice = decimal.Round(d.UnitPrice, 4); // matches DECIMAL(19,4)
        UnitsInStock = d.UnitsInStock;
        UnitsOnOrder = d.UnitsOnOrder;
        ReorderLevel = d.ReorderLevel;
        Discontinued = d.Discontinued;
    }
}
