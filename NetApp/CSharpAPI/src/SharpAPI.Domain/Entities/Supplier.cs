namespace SharpAPI.Domain.Entities;

/// <summary>
/// A company that supplies products. Maps to <c>dbo.Suppliers</c>.
/// Read-only in this app; it fills the "Supplier" dropdown.
/// </summary>
public sealed class Supplier
{
    public const int CompanyNameMaxLength = 40;

    private Supplier() { }

    public Supplier(string companyName, string? contactName, string? city, string? country, string? phone)
    {
        CompanyName = companyName;
        ContactName = contactName;
        City = city;
        Country = country;
        Phone = phone;
    }

    public int Id { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? Phone { get; private set; }
}
