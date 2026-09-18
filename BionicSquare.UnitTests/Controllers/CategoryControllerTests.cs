using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace BionicSquare.UnitTests.Controllers;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryServices> _mockCategoryServices;
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _mockCategoryServices = new Mock<ICategoryServices>();
        _controller = new CategoryController(_mockCategoryServices.Object);

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
    }

    [Fact]
    public async Task IndexGetAsync_ShouldReturnViewWithListOfCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Fiction", DisplayOrder = 100 },
            new() { Id = 2, Name = "Non-Fiction", DisplayOrder = 200 }
        };
        _mockCategoryServices.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Category>>().Subject;
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task IndexGetAsync_WhenServiceThrows_ShouldReturnViewWithEmptyList()
    {
        // Arrange
        _mockCategoryServices.Setup(s => s.GetAllCategoriesAsync())
            .ThrowsAsync(new Exception("Database connection failure"));

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Category>>().Subject;
        model.Should().BeEmpty();
    }

    [Fact]
    public void CreateGet_ShouldReturnViewResult()
    {
        // Act
        var result = _controller.CreateGet();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task CreatePostAsync_WhenModelStateIsInvalid_ShouldReturnViewWithSameModel()
    {
        // Arrange
        var category = new Category { Name = "" };
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreatePostAsync(category);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _mockCategoryServices.Verify(s => s.CreateCategoryAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreatePostAsync_WhenModelStateIsValid_ShouldCallServiceAndRedirectToIndex()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Thriller", DisplayOrder = 300 };
        _mockCategoryServices.Setup(s => s.CreateCategoryAsync(category)).ReturnsAsync(category);

        // Act
        var result = await _controller.CreatePostAsync(category);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        _mockCategoryServices.Verify(s => s.CreateCategoryAsync(category), Times.Once);
        _controller.TempData["success"].Should().Be("Category 'Thriller' created successfully");
    }

    [Fact]
    public async Task CreatePostAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        var category = new Category { Name = "Drama", DisplayOrder = 50 };
        _mockCategoryServices.Setup(s => s.CreateCategoryAsync(category))
            .ThrowsAsync(new Exception("Unique constraint violated"));

        // Act
        var result = await _controller.CreatePostAsync(category);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Unique constraint violated"));
    }

    [Fact]
    public async Task UpdateGetAsync_WithValidId_ShouldReturnViewWithCategory()
    {
        // Arrange
        var category = new Category { Id = 3, Name = "Horror", DisplayOrder = 400 };
        _mockCategoryServices.Setup(s => s.GetCategoryByIdAsync(3)).ReturnsAsync(category);

        // Act
        var result = await _controller.UpdateGetAsync(3);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Category>().Subject;
        model.Id.Should().Be(3);
        model.Name.Should().Be("Horror");
    }

    [Fact]
    public async Task UpdateGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnFallbackCategory()
    {
        // Arrange
        _mockCategoryServices.Setup(s => s.GetCategoryByIdAsync(99))
            .ThrowsAsync(new Exception("Category not found"));

        // Act
        var result = await _controller.UpdateGetAsync(99);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Category>().Subject;
        model.Id.Should().Be(0);
        model.Name.Should().Be("Unknown");
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Category not found"));
    }

    [Fact]
    public async Task UpdatePostAsync_WhenModelStateIsInvalid_ShouldReturnViewWithoutCallingService()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "" };
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.UpdatePostAsync(category);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _mockCategoryServices.Verify(s => s.UpdateCategoryAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePostAsync_WhenValid_ShouldCallUpdateAndRedirectToIndex()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Sci-Fi", DisplayOrder = 150 };
        _mockCategoryServices.Setup(s => s.UpdateCategoryAsync(category)).ReturnsAsync(category);

        // Act
        var result = await _controller.UpdatePostAsync(category);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        _mockCategoryServices.Verify(s => s.UpdateCategoryAsync(category), Times.Once);
        _controller.TempData["success"].Should().Be("Category 'Sci-Fi' updated successfully");
    }

    [Fact]
    public async Task UpdatePostAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Sci-Fi", DisplayOrder = 150 };
        _mockCategoryServices.Setup(s => s.UpdateCategoryAsync(category))
            .ThrowsAsync(new Exception("Concurrency error"));

        // Act
        var result = await _controller.UpdatePostAsync(category);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Concurrency error"));
    }

    [Fact]
    public async Task DeleteGetAsync_WithValidId_ShouldReturnViewWithCategory()
    {
        // Arrange
        var category = new Category { Id = 4, Name = "History", DisplayOrder = 250 };
        _mockCategoryServices.Setup(s => s.GetCategoryByIdAsync(4)).ReturnsAsync(category);

        // Act
        var result = await _controller.DeleteGetAsync(4);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Category>().Subject;
        model.Id.Should().Be(4);
        model.Name.Should().Be("History");
    }

    [Fact]
    public async Task DeleteGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnFallbackCategory()
    {
        // Arrange
        _mockCategoryServices.Setup(s => s.GetCategoryByIdAsync(99))
            .ThrowsAsync(new Exception("Category not found"));

        // Act
        var result = await _controller.DeleteGetAsync(99);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Category>().Subject;
        model.Id.Should().Be(0);
        model.Name.Should().Be("Unknown");
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Category not found"));
    }

    [Fact]
    public async Task DeletePostAsync_WhenModelStateIsInvalid_ShouldReturnViewWithoutCallingService()
    {
        // Arrange
        _controller.ModelState.AddModelError("Id", "Invalid Id");

        // Act
        var result = await _controller.DeletePostAsync(5);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _mockCategoryServices.Verify(s => s.DeleteCategoryByIdAsync(It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task DeletePostAsync_WithValidId_ShouldCallDeleteAndRedirectToIndex()
    {
        // Arrange
        var category = new Category { Id = 5, Name = "Comics" };
        _mockCategoryServices.Setup(s => s.DeleteCategoryByIdAsync(5)).ReturnsAsync(category);

        // Act
        var result = await _controller.DeletePostAsync(5);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        _mockCategoryServices.Verify(s => s.DeleteCategoryByIdAsync(5), Times.Once);
        _controller.TempData["success"].Should().Be("Category 'Comics' deleted successfully");
    }

    [Fact]
    public async Task DeletePostAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        _mockCategoryServices.Setup(s => s.DeleteCategoryByIdAsync(5))
            .ThrowsAsync(new Exception("Cannot delete category with active products"));

        // Act
        var result = await _controller.DeletePostAsync(5);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Cannot delete category with active products"));
    }
}
