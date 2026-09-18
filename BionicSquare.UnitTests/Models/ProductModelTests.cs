using System.ComponentModel.DataAnnotations;
using BionicSquare.Models;
using FluentAssertions;

namespace BionicSquare.UnitTests.Models;

public class ProductModelTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    private static Product CreateValidProduct()
    {
        return new Product
        {
            Id = 1,
            Title = "Valid Book Title",
            Author = "Valid Author",
            Description = "A well-written description of the book",
            Isbn = "978-3-16-148410-0",
            ListPrice = 100,
            Price = 90,
            Price50 = 80,
            Price100 = 70,
            CategoryId = 1
        };
    }

    [Fact]
    public void Product_WithAllValidProperties_ShouldPassValidation()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        var results = ValidateModel(product);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Product_WithMissingTitle_ShouldFailRequiredValidation()
    {
        // Arrange
        var product = new Product
        {
            Title = "",
            Author = "Author",
            Isbn = "ISBN-123",
            ListPrice = 50,
            Price = 45,
            Price50 = 40,
            Price100 = 35
        };

        // Act
        var results = ValidateModel(product);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Product.Title)));
    }

    [Fact]
    public void Product_WithMissingAuthor_ShouldFailRequiredValidation()
    {
        // Arrange
        var product = new Product
        {
            Title = "Some Title",
            Author = "",
            Isbn = "ISBN-123",
            ListPrice = 50,
            Price = 45,
            Price50 = 40,
            Price100 = 35
        };

        // Act
        var results = ValidateModel(product);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Product.Author)));
    }

    [Fact]
    public void Product_WithMissingIsbn_ShouldFailRequiredValidation()
    {
        // Arrange
        var product = new Product
        {
            Title = "Some Title",
            Author = "Author",
            Isbn = "",
            ListPrice = 50,
            Price = 45,
            Price50 = 40,
            Price100 = 35
        };

        // Act
        var results = ValidateModel(product);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Product.Isbn)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(1001)]
    public void Product_WithPriceOutOfRange_ShouldFailRangeValidation(double invalidPrice)
    {
        // Arrange
        var product = new Product
        {
            Title = "Valid Title",
            Author = "Valid Author",
            Isbn = "ISBN-999",
            ListPrice = 50,
            Price = invalidPrice,
            Price50 = 40,
            Price100 = 35
        };

        // Act
        var results = ValidateModel(product);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Product.Price)) &&
                                     r.ErrorMessage!.Contains("Price must be between 1 and 1000"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Product_WithPriceOnBoundaries_ShouldPassValidation(double validPrice)
    {
        // Arrange
        var product = new Product
        {
            Title = "Valid Title",
            Author = "Valid Author",
            Isbn = "ISBN-999",
            ListPrice = validPrice,
            Price = validPrice,
            Price50 = validPrice,
            Price100 = validPrice
        };

        // Act
        var results = ValidateModel(product);

        // Assert
        results.Should().BeEmpty();
    }
}
