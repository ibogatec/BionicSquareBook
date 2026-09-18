using BionicSquare.Models;
using FluentAssertions;

namespace BionicSquare.UnitTests.Models;

public class ShoppingCartTests
{
    private readonly Product _sampleProduct = new()
    {
        Id = 1,
        Title = "C# Mastery in Depth",
        Author = "Tech Author",
        Description = "Comprehensive .NET book",
        Isbn = "ISBN-12345",
        ListPrice = 100,
        Price = 90,      // 1 - 50 items
        Price50 = 80,    // 51 - 100 items
        Price100 = 70    // 101+ items
    };

    [Fact]
    public void ShoppingCart_PropertiesCanBeSetAndRetrieved()
    {
        // Arrange
        var user = new ApplicationUser { Id = "u-1", Name = "Alice" };
        var cart = new ShoppingCart
        {
            Id = 5,
            Quantity = 10,
            ApplicationUserId = "u-1",
            ApplicationUser = user,
            ProductId = 1,
            Product = _sampleProduct
        };

        // Assert
        cart.Id.Should().Be(5);
        cart.Quantity.Should().Be(10);
        cart.ApplicationUserId.Should().Be("u-1");
        cart.ApplicationUser.Should().Be(user);
        cart.ProductId.Should().Be(1);
        cart.Product.Should().Be(_sampleProduct);
    }

    [Fact]
    public void Price_WhenProductIsNull_ShouldReturnZero()
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Quantity = 10,
            Product = null
        };

        // Act & Assert
        cart.Price.Should().Be(0.0);
    }

    [Theory]
    [InlineData(1, 90)]
    [InlineData(25, 90)]
    [InlineData(50, 90)]
    public void Price_WhenQuantityIsUpTo50_ShouldUseBasePrice(int quantity, double expectedPrice)
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Quantity = quantity,
            Product = _sampleProduct
        };

        // Act & Assert
        cart.Price.Should().Be(expectedPrice);
    }

    [Theory]
    [InlineData(51, 80)]
    [InlineData(75, 80)]
    [InlineData(100, 80)]
    public void Price_WhenQuantityIsBetween51And100_ShouldUsePrice50(int quantity, double expectedPrice)
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Quantity = quantity,
            Product = _sampleProduct
        };

        // Act & Assert
        cart.Price.Should().Be(expectedPrice);
    }

    [Theory]
    [InlineData(101, 70)]
    [InlineData(500, 70)]
    [InlineData(1000, 70)]
    public void Price_WhenQuantityIsAbove100_ShouldUsePrice100(int quantity, double expectedPrice)
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Quantity = quantity,
            Product = _sampleProduct
        };

        // Act & Assert
        cart.Price.Should().Be(expectedPrice);
    }
}
