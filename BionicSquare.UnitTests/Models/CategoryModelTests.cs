using System.ComponentModel.DataAnnotations;
using BionicSquare.Models;
using FluentAssertions;

namespace BionicSquare.UnitTests.Models;

public class CategoryModelTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void Category_WithValidProperties_ShouldPassValidation()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Science Fiction",
            DisplayOrder = 150
        };

        // Act
        var results = ValidateModel(category);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Category_WithEmptyOrWhitespaceName_ShouldFailValidation(string invalidName)
    {
        // Arrange
        var category = new Category
        {
            Name = invalidName,
            DisplayOrder = 200
        };

        // Act
        var results = ValidateModel(category);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Category.Name)));
    }

    [Fact]
    public void Category_WithNameShorterThanTwoCharacters_ShouldFailMinLengthValidation()
    {
        // Arrange
        var category = new Category
        {
            Name = "A",
            DisplayOrder = 200
        };

        // Act
        var results = ValidateModel(category);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Category.Name)));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1001)]
    [InlineData(5000)]
    public void Category_WithDisplayOrderOutOfRange_ShouldFailRangeValidation(int invalidDisplayOrder)
    {
        // Arrange
        var category = new Category
        {
            Name = "Valid Name",
            DisplayOrder = invalidDisplayOrder
        };

        // Act
        var results = ValidateModel(category);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Category.DisplayOrder)) &&
                                     r.ErrorMessage!.Contains("Display order must be between 100 and 1000"));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Category_WithDisplayOrderOnBoundaries_ShouldPassValidation(int validDisplayOrder)
    {
        // Arrange
        var category = new Category
        {
            Name = "Valid Category",
            DisplayOrder = validDisplayOrder
        };

        // Act
        var results = ValidateModel(category);

        // Assert
        results.Should().BeEmpty();
    }
}
