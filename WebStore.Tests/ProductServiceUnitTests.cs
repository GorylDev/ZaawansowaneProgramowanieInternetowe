using Xunit;

namespace WebStore.Tests;

public class ProductServiceUnitTests : BaseUnitTests
{
    [Theory][InlineData("pl-PL")][InlineData("en-US")]
    public void DecimalValidationWorksInBothCultures(string culture)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new(culture);
            var input = ProductInput();
            input.Price = 123.45m;
            Assert.Equal(123.45m, Products.AddOrUpdateProduct(input).Price);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Fact] public void GetProductTest() => Assert.Equal(1, Products.GetProduct(p => p.Name == "Monitor Dell 32")?.Id);
    [Fact] public void GetMultipleProductsTest() => Assert.Equal(2, Products.GetProducts(p => p.Id >= 1 && p.Id <= 2).Count());
    [Fact] public void GetAllProductsTest() => Assert.Equal(2, Products.GetProducts().Count());

    [Fact]
    public void AddNewProductTest()
    {
        var result = Products.AddOrUpdateProduct(ProductInput());
        Assert.Equal("Macbook Pro", result.Name);
        DbContext.ChangeTracker.Clear();
        Assert.Equal(6000, Products.GetProduct(p => p.Id == result.Id)?.Price);
        Assert.Equal(3, Products.GetProducts().Count());
    }

    [Fact]
    public void UpdateProductTest()
    {
        var input = ProductInput(1);
        input.Name = "Monitor Dell 32";
        input.Price = 2000;
        Products.AddOrUpdateProduct(input);
        DbContext.ChangeTracker.Clear();
        Assert.Equal(2000, Products.GetProduct(p => p.Id == 1)?.Price);
        Assert.Equal(2, Products.GetProducts().Count());
    }

    [Fact]
    public void QuantityIsSumOfStocksAndZeroWithoutStocks()
    {
        DbContext.ProductStocks.AddRange(new() { ProductId = 1, Quantity = 2 }, new() { ProductId = 1, Quantity = 3 });
        DbContext.SaveChanges();
        Assert.Equal(5, Products.GetProduct(p => p.Id == 1)?.Quantity);
        Assert.Equal(0, Products.GetProduct(p => p.Id == 2)?.Quantity);
    }

    [Theory]
    [InlineData("price")][InlineData("weight")][InlineData("name")][InlineData("description")]
    [InlineData("image")][InlineData("category")][InlineData("supplier")][InlineData("infinite")]
    public void InvalidProductDoesNotChangeStoredData(string field)
    {
        var input = ProductInput(1);
        switch (field)
        {
            case "price": input.Price = -1; break;
            case "weight": input.Weight = -1; break;
            case "name": input.Name = " "; break;
            case "description": input.Description = ""; break;
            case "image": input.ImageBytes = null!; break;
            case "category": input.CategoryId = 99; break;
            case "supplier": input.SupplierId = 2; break;
            case "infinite": input.Weight = float.PositiveInfinity; break;
        }
        Assert.Throws<ArgumentException>(() => Products.AddOrUpdateProduct(input));
        DbContext.ChangeTracker.Clear();
        Assert.Equal("Monitor Dell 32", Products.GetProduct(p => p.Id == 1)?.Name);
    }
}
