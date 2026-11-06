namespace SharpAPI.Domain.Entities;

/// <summary>
/// A product category (Beverages, Seafood...). Maps to <c>dbo.Categories</c>.
/// </summary>
/// <remarks>
/// The UI only reads categories (to fill the product form's dropdown), so this
/// entity is deliberately simple: private setters with no behaviour.
/// </remarks>
public sealed class Category
{
    public const int NameMaxLength = 15;

    // EF Core materialises entities through this private constructor.
    private Category() { }

    public Category(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
}
