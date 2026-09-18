using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BionicSquare.UnitTests.Services;

public class CategoryServicesTests
{
    [Fact]
    public async Task GetAllCategoriesAsync_ShouldReturnAllCategories()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);

        // Act
        var result = (await service.GetAllCategoriesAsync()).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // Seeding has 3 categories
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WithValidId_ShouldReturnCategory()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);
        var existingCategory = await context.Categories.FirstAsync();

        // Act
        var result = await service.GetCategoryByIdAsync(existingCategory.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingCategory.Id);
        result.Name.Should().Be(existingCategory.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task GetCategoryByIdAsync_WithNullOrZeroId_ShouldThrowArgumentNullException(int? invalidId)
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);

        // Act
        var act = async () => await service.GetCategoryByIdAsync(invalidId);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WithNonExistentId_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);

        // Act
        var act = async () => await service.GetCategoryByIdAsync(9999);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*not found in database*");
    }

    [Fact]
    public async Task CreateCategoryAsync_WithNullCategory_ShouldThrowArgumentNullException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);

        // Act
        var act = async () => await service.CreateCategoryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateCategoryAsync_WithValidCategory_ShouldAddAndReturnCategory()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);
        var newCategory = new Category { Name = "Science & Nature", DisplayOrder = 500 };

        // Act
        var result = await service.CreateCategoryAsync(newCategory);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        context.Categories.Should().Contain(c => c.Name == "Science & Nature");
    }

    [Fact]
    public async Task CreateCategoryAsync_WithDuplicateName_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);
        var existing = await context.Categories.FirstAsync();

        var duplicateCategory = new Category { Name = existing.Name.ToLower(), DisplayOrder = 200 };

        // Act
        var act = async () => await service.CreateCategoryAsync(duplicateCategory);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateCategoryAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new CategoryServices(context);
        var newCategory = new Category { Name = "Zero Save Category", DisplayOrder = 550 };

        // Act
        var act = async () => await service.CreateCategoryAsync(newCategory);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to create category*");
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithNullCategory_ShouldThrowArgumentNullException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);

        // Act
        var act = async () => await service.UpdateCategoryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithValidChanges_ShouldUpdateSuccessfully()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);
        var existing = await context.Categories.FirstAsync();
        context.Entry(existing).State = EntityState.Detached;

        var updatedCategory = new Category
        {
            Id = existing.Id,
            Name = "Updated Action",
            DisplayOrder = 999
        };

        // Act
        var result = await service.UpdateCategoryAsync(updatedCategory);

        // Assert
        result.Name.Should().Be("Updated Action");
        result.DisplayOrder.Should().Be(999);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new CategoryServices(context);
        var existing = await context.Categories.FirstAsync();
        context.Entry(existing).State = EntityState.Detached;

        var updatedCategory = new Category
        {
            Id = existing.Id,
            Name = "Unsaved Updated Category",
            DisplayOrder = 999
        };

        // Act
        var act = async () => await service.UpdateCategoryAsync(updatedCategory);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to update category*");
    }

    [Fact]
    public async Task DeleteCategoryByIdAsync_WithValidId_ShouldRemoveCategory()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryServices(context);
        var newCategory = new Category { Name = "Temporary Category", DisplayOrder = 300 };
        await context.Categories.AddAsync(newCategory);
        await context.SaveChangesAsync();

        // Act
        var deleted = await service.DeleteCategoryByIdAsync(newCategory.Id);

        // Assert
        deleted.Id.Should().Be(newCategory.Id);
        context.Categories.Should().NotContain(c => c.Id == newCategory.Id);
    }

    [Fact]
    public async Task DeleteCategoryByIdAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new CategoryServices(context);
        var existing = await context.Categories.FirstAsync();

        // Act
        var act = async () => await service.DeleteCategoryByIdAsync(existing.Id);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to delete category*");
    }
}
