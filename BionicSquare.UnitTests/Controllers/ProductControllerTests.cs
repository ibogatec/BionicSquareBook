using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.Models.ViewModels;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace BionicSquare.UnitTests.Controllers;

public class ProductControllerTests : IDisposable
{
    private readonly Mock<IProductServices> _mockProductServices;
    private readonly Mock<ICategoryServices> _mockCategoryServices;
    private readonly ProductController _controller;
    private readonly string _tempWebRoot;

    public ProductControllerTests()
    {
        _mockProductServices = new Mock<IProductServices>();
        _mockCategoryServices = new Mock<ICategoryServices>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        _tempWebRoot = Path.Combine(Path.GetTempPath(), "ProductControllerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);
        mockEnvironment.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _controller = new ProductController(
            _mockProductServices.Object,
            _mockCategoryServices.Object,
            mockEnvironment.Object);

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWebRoot))
        {
            try
            {
                Directory.Delete(_tempWebRoot, recursive: true);
            }
            catch
            {
                // ignored
            }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void IndexGet_ShouldReturnViewResult()
    {
        // Act
        var result = _controller.IndexGet();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task CreateGetAsync_ShouldReturnUpdateViewWithCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Fiction" },
            new() { Id = 2, Name = "History" }
        };
        _mockCategoryServices.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.CreateGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("Update");
        var model = viewResult.Model.Should().BeOfType<ProductViewModel>().Subject;
        model.CategoryList.Should().HaveCount(2);
        model.CategoryList.First().Text.Should().Be("Fiction");
    }

    [Fact]
    public async Task CreateGetAsync_WhenCategoryServiceThrows_ShouldAddModelStateError()
    {
        // Arrange
        _mockCategoryServices.Setup(s => s.GetAllCategoriesAsync())
            .ThrowsAsync(new Exception("Database offline"));

        // Act
        var result = await _controller.CreateGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("Update");
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Database offline"));
    }

    [Fact]
    public async Task CreatePostAsync_WhenModelStateIsInvalid_ShouldReturnUpdateViewWithoutCallingService()
    {
        // Arrange
        var viewModel = new ProductViewModel
        {
            Product = new Product { Title = "" }
        };
        _controller.ModelState.AddModelError("Product.Title", "Title is required");

        // Act
        var result = await _controller.CreatePostAsync(viewModel, file: null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("Update");
        viewResult.Model.Should().Be(viewModel);
        _mockProductServices.Verify(s => s.CreateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreatePostAsync_WhenValidWithoutFile_ShouldCreateProductAndRedirectToIndex()
    {
        // Arrange
        var product = new Product { Id = 1, Title = "Clean Architecture" };
        var viewModel = new ProductViewModel { Product = product };
        _mockProductServices.Setup(s => s.CreateProductAsync(product)).ReturnsAsync(product);

        // Act
        var result = await _controller.CreatePostAsync(viewModel, file: null);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        product.ImageUrl.Should().BeNull();
        _mockProductServices.Verify(s => s.CreateProductAsync(product), Times.Once);
        _controller.TempData["success"].Should().Be("Product 'Clean Architecture' created successfully");
    }

    [Fact]
    public async Task CreatePostAsync_WhenValidWithFile_ShouldSaveFileAndUpdateImageUrl()
    {
        // Arrange
        var product = new Product { Id = 1, Title = "Refactoring" };
        var viewModel = new ProductViewModel { Product = product };
        _mockProductServices.Setup(s => s.CreateProductAsync(product)).ReturnsAsync(product);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("cover.jpg");
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreatePostAsync(viewModel, mockFile.Object);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        product.ImageUrl.Should().NotBeNull();
        product.ImageUrl.Should().StartWith(Path.Combine("images", "uploads", "products"));
        product.ImageUrl.Should().EndWith(".jpg");
        _mockProductServices.Verify(s => s.CreateProductAsync(product), Times.Once);
    }

    [Fact]
    public async Task CreatePostAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        var product = new Product { Id = 1, Title = "Domain Driven Design" };
        var viewModel = new ProductViewModel { Product = product };
        _mockProductServices.Setup(s => s.CreateProductAsync(product))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreatePostAsync(viewModel, file: null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("Update");
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Database error"));
    }

    [Fact]
    public async Task UpdateGetAsync_WithValidId_ShouldReturnViewWithProductAndCategories()
    {
        // Arrange
        var product = new Product { Id = 5, Title = "Test Product" };
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Sci-Fi" }
        };
        _mockProductServices.Setup(s => s.GetProductByIdAsync(5, false)).ReturnsAsync(product);
        _mockCategoryServices.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.UpdateGetAsync(5);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ProductViewModel>().Subject;
        model.Action.Should().Be("Update Product");
        model.Product.Should().Be(product);
        model.CategoryList.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateGetAsync_WhenServiceThrows_ShouldAddModelStateError()
    {
        // Arrange
        _mockProductServices.Setup(s => s.GetProductByIdAsync(5, false))
            .ThrowsAsync(new Exception("Product not found"));

        // Act
        var result = await _controller.UpdateGetAsync(5);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Product not found"));
    }

    [Fact]
    public async Task UpdatePostAsync_WhenModelStateIsInvalid_ShouldReturnViewWithSameModel()
    {
        // Arrange
        var viewModel = new ProductViewModel { Product = new Product { Title = "" } };
        _controller.ModelState.AddModelError("Product.Title", "Title required");

        // Act
        var result = await _controller.UpdatePostAsync(viewModel, file: null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(viewModel);
        _mockProductServices.Verify(s => s.UpdateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePostAsync_WhenValidWithoutFile_ShouldUpdateProductAndRedirectToIndex()
    {
        // Arrange
        var product = new Product { Id = 2, Title = "Updated Product" };
        var viewModel = new ProductViewModel { Product = product };
        _mockProductServices.Setup(s => s.UpdateProductAsync(product)).ReturnsAsync(product);

        // Act
        var result = await _controller.UpdatePostAsync(viewModel, file: null);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        _mockProductServices.Verify(s => s.UpdateProductAsync(product), Times.Once);
        _controller.TempData["success"].Should().Be("Product 'Updated Product' updated successfully");
    }

    [Fact]
    public async Task UpdatePostAsync_WhenValidWithNewFileAndExistingOldImage_ShouldDeleteOldFileAndSaveNew()
    {
        // Arrange
        var oldRelPath = Path.Combine("images", "uploads", "products", "old-image.png");
        var oldAbsPath = Path.Combine(_tempWebRoot, oldRelPath);
        Directory.CreateDirectory(Path.GetDirectoryName(oldAbsPath)!);
        await File.WriteAllTextAsync(oldAbsPath, "old content");

        var product = new Product { Id = 3, Title = "Product with Image", ImageUrl = oldRelPath };
        var viewModel = new ProductViewModel { Product = product };
        _mockProductServices.Setup(s => s.UpdateProductAsync(product)).ReturnsAsync(product);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("new-image.png");
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdatePostAsync(viewModel, mockFile.Object);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        File.Exists(oldAbsPath).Should().BeFalse();
        product.ImageUrl.Should().NotBe(oldRelPath);
        product.ImageUrl.Should().EndWith(".png");
        _mockProductServices.Verify(s => s.UpdateProductAsync(product), Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_WhenValidWithNewFileAndNoExistingOldImage_ShouldSaveNewFile()
    {
        // Arrange
        var product = new Product { Id = 3, Title = "Product without prior Image", ImageUrl = null };
        var viewModel = new ProductViewModel { Product = product };
        _mockProductServices.Setup(s => s.UpdateProductAsync(product)).ReturnsAsync(product);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("fresh-image.png");
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdatePostAsync(viewModel, mockFile.Object);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        product.ImageUrl.Should().NotBeNull();
        product.ImageUrl.Should().EndWith(".png");
        _mockProductServices.Verify(s => s.UpdateProductAsync(product), Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        var product = new Product { Id = 4, Title = "Failing Update" };
        var viewModel = new ProductViewModel { Product = product };
        _mockProductServices.Setup(s => s.UpdateProductAsync(product))
            .ThrowsAsync(new Exception("Database locked"));

        // Act
        var result = await _controller.UpdatePostAsync(viewModel, file: null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(viewModel);
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Database locked"));
    }

    [Fact]
    public async Task IndexGetJsonAsync_ShouldReturnJsonWithProductData()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Title = "P1" },
            new() { Id = 2, Title = "P2" }
        };
        _mockProductServices.Setup(s => s.GetAllProductsAsync(true)).ReturnsAsync(products);

        // Act
        var result = await _controller.IndexGetJsonAsync();

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var dataProp = jsonResult.Value?.GetType().GetProperty("data")?.GetValue(jsonResult.Value);
        dataProp.Should().BeAssignableTo<IEnumerable<Product>>();
        ((IEnumerable<Product>)dataProp).Should().HaveCount(2);
    }

    [Fact]
    public async Task IndexGetJsonAsync_WhenServiceThrows_ShouldReturnJsonWithEmptyData()
    {
        // Arrange
        _mockProductServices.Setup(s => s.GetAllProductsAsync(true))
            .ThrowsAsync(new Exception("DB connection timeout"));

        // Act
        var result = await _controller.IndexGetJsonAsync();

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var dataProp = jsonResult.Value?.GetType().GetProperty("data")?.GetValue(jsonResult.Value);
        dataProp.Should().BeAssignableTo<IEnumerable<Product>>();
        ((IEnumerable<Product>)dataProp).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task DeleteAsync_WhenIdIsNullOrZero_ShouldReturnJsonError(int? id)
    {
        // Act
        var result = await _controller.DeleteAsync(id);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var successProp = jsonResult.Value?.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        var errorProp = jsonResult.Value?.GetType().GetProperty("error")?.GetValue(jsonResult.Value);
        successProp.Should().Be(false);
        errorProp.Should().Be($"Invalid product id: {id}");
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldCallServiceAndReturnJsonSuccess()
    {
        // Arrange
        var deleted = new Product { Id = 10, Title = "Removed Book" };
        _mockProductServices.Setup(s => s.DeleteProductByIdAsync(10)).ReturnsAsync(deleted);

        // Act
        var result = await _controller.DeleteAsync(10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var successProp = jsonResult.Value?.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        var dataProp = jsonResult.Value?.GetType().GetProperty("data")?.GetValue(jsonResult.Value);
        successProp.Should().Be(true);
        dataProp.Should().Be("Product 'Removed Book' deleted successfully");
        _mockProductServices.Verify(s => s.DeleteProductByIdAsync(10), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenServiceThrows_ShouldReturnJsonError()
    {
        // Arrange
        _mockProductServices.Setup(s => s.DeleteProductByIdAsync(10))
            .ThrowsAsync(new Exception("Cannot delete item in order"));

        // Act
        var result = await _controller.DeleteAsync(10);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var successProp = jsonResult.Value?.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        var errorProp = jsonResult.Value?.GetType().GetProperty("error")?.GetValue(jsonResult.Value);
        successProp.Should().Be(false);
        errorProp.Should().Be("Error: Cannot delete item in order");
    }
}
