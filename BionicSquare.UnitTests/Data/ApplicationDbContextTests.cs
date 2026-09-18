using BionicSquare.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BionicSquare.UnitTests.Data;

public class ApplicationDbContextTests
{
    [Fact]
    public async Task ApplicationDbContext_ShouldHaveDbSetsAndInitialSeedData()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        // Act
        var categories = await context.Categories.ToListAsync();
        var products = await context.Products.ToListAsync();

        // Assert
        context.Categories.Should().NotBeNull();
        context.Products.Should().NotBeNull();
        context.ShoppingCarts.Should().NotBeNull();
        context.OrderHeaders.Should().NotBeNull();
        context.OrderDetails.Should().NotBeNull();
        context.ApplicationUsers.Should().NotBeNull();

        categories.Count.Should().BeGreaterThanOrEqualTo(3);
        categories.Should().Contain(c => c.Name == "Action" && c.Id == 1);
        categories.Should().Contain(c => c.Name == "SciFi" && c.Id == 2);
        categories.Should().Contain(c => c.Name == "History" && c.Id == 3);

        products.Count.Should().BeGreaterThanOrEqualTo(8);
        products.Should().Contain(p => p.Title == "Store of the Jungle" && p.Id == 1);
        products.Should().Contain(p => p.Title == "Money and Time" && p.Id == 2);
    }
}
