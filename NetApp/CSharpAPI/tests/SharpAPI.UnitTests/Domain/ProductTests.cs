using SharpAPI.Domain.Common;
using SharpAPI.Domain.Entities;

namespace SharpAPI.UnitTests.Domain;

/// <summary>
/// Tests for the Product entity's invariants and business rules.
/// Pure C# with no mocks needed: one benefit of a framework-free Domain layer.
/// </summary>
public sealed class ProductTests
{
    private static ProductDetails ValidDetails(
        string name = "Chai", decimal price = 18m, short stock = 39, short onOrder = 0, short reorder = 10,
        bool discontinued = false) =>
        new(name, SupplierId: 1, CategoryId: 1, "10 boxes", price, stock, onOrder, reorder, discontinued);

    [Fact]
    public void Create_WithValidDetails_TrimsAndAssignsValues()
    {
        var product = Product.Create(ValidDetails(name: "  Chai  "));

        Assert.Equal("Chai", product.Name);
        Assert.Equal(18m, product.UnitPrice);
        Assert.Equal((short)39, product.UnitsInStock);
    }

    // [Theory] + [InlineData] = one test method, many cases (data-driven testing).
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => Product.Create(ValidDetails(name: name)));
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Product.Create(ValidDetails(price: -0.01m)));
    }

    [Fact]
    public void Create_WithNameTooLong_ThrowsDomainException()
    {
        var tooLong = new string('x', Product.NameMaxLength + 1);
        Assert.Throws<DomainException>(() => Product.Create(ValidDetails(name: tooLong)));
    }

    [Theory]
    [InlineData(5, 0, 10, false, true)]   // low stock → reorder
    [InlineData(5, 10, 10, false, false)] // enough on order → no reorder
    [InlineData(50, 0, 10, false, false)] // plenty of stock
    [InlineData(0, 0, 10, true, false)]   // discontinued products are never reordered
    public void NeedsReorder_FollowsBusinessRule(short stock, short onOrder, short reorder, bool discontinued, bool expected)
    {
        var product = Product.Create(ValidDetails(stock: stock, onOrder: onOrder, reorder: reorder, discontinued: discontinued));

        Assert.Equal(expected, product.NeedsReorder);
    }

    [Fact]
    public void Update_ChangesValues_AndEnforcesSameRules()
    {
        var product = Product.Create(ValidDetails());

        product.Update(ValidDetails(name: "Chai Latte", price: 20m));

        Assert.Equal("Chai Latte", product.Name);
        Assert.Equal(20m, product.UnitPrice);
        Assert.Throws<DomainException>(() => product.Update(ValidDetails(price: -1m)));
    }
}
