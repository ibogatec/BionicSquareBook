using System.Security.Claims;
using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BionicSquare.UnitTests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<IProductServices> _mockProductServices;
    private readonly Mock<IShoppingCartService> _mockShoppingCartService;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockProductServices = new Mock<IProductServices>();
        _mockShoppingCartService = new Mock<IShoppingCartService>();
        _controller = new HomeController(_mockProductServices.Object, _mockShoppingCartService.Object);
    }

    [Fact]
    public async Task IndexGetAsync_ShouldReturnViewWithProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Title = "Book A", Price = 25 },
            new() { Id = 2, Title = "Book B", Price = 30 }
        };
        _mockProductServices.Setup(s => s.GetAllProductsAsync(true)).ReturnsAsync(products);

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Product>>().Subject;
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task IndexGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnViewWithEmptyList()
    {
        // Arrange
        _mockProductServices.Setup(s => s.GetAllProductsAsync(true))
            .ThrowsAsync(new Exception("Database connection failure"));

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Product>>().Subject;
        model.Should().BeEmpty();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Database connection failure"));
    }

    [Fact]
    public async Task DetailsGetAsync_WithValidProduct_ShouldReturnViewWithShoppingCartModel()
    {
        // Arrange
        var product = new Product { Id = 10, Title = "Special Book", Price = 40 };
        _mockProductServices.Setup(s => s.GetProductByIdAsync(10, true)).ReturnsAsync(product);

        // Act
        var result = await _controller.DetailsGetAsync(10, quantity: 3);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ShoppingCart>().Subject;
        model.ProductId.Should().Be(10);
        model.Quantity.Should().Be(3);
        model.Product.Should().Be(product);
    }

    [Fact]
    public async Task DetailsGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnViewNull()
    {
        // Arrange
        _mockProductServices.Setup(s => s.GetProductByIdAsync(10, true))
            .ThrowsAsync(new Exception("Product not found"));

        // Act
        var result = await _controller.DetailsGetAsync(10);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Product not found"));
    }

    [Fact]
    public async Task DetailsPostAsync_WhenAuthenticatedUser_ShouldAddCartAndRedirect()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user-42")
        ], "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var cartInput = new ShoppingCart { ProductId = 7, Quantity = 2 };
        var savedCart = new ShoppingCart { Id = 1, ProductId = 7, Quantity = 2, ApplicationUserId = "user-42" };

        _mockShoppingCartService.Setup(s => s.AddToCartAsync(It.IsAny<ShoppingCart>())).ReturnsAsync(savedCart);

        // Act
        var result = await _controller.DetailsPostAsync(cartInput);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Details");
        redirect.RouteValues.Should().ContainKey("productId").WhoseValue.Should().Be(7);
        redirect.RouteValues.Should().ContainKey("quantity").WhoseValue.Should().Be(2);
        _mockShoppingCartService.Verify(s => s.AddToCartAsync(It.Is<ShoppingCart>(c => c.ApplicationUserId == "user-42")), Times.Once);
    }

    [Fact]
    public async Task DetailsPostAsync_WhenUserIdentifierMissing_ShouldReturnUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var cartInput = new ShoppingCart { ProductId = 7, Quantity = 2 };

        // Act
        var result = await _controller.DetailsPostAsync(cartInput);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        _mockShoppingCartService.Verify(s => s.AddToCartAsync(It.IsAny<ShoppingCart>()), Times.Never);
    }

    [Fact]
    public async Task DetailsPostAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnViewNull()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user-42")
        ], "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _mockShoppingCartService.Setup(s => s.AddToCartAsync(It.IsAny<ShoppingCart>()))
            .ThrowsAsync(new Exception("Cart failed"));

        var cartInput = new ShoppingCart { ProductId = 7, Quantity = 2 };

        // Act
        var result = await _controller.DetailsPostAsync(cartInput);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Cart failed"));
    }

    [Fact]
    public void Privacy_ShouldReturnView()
    {
        // Act
        var result = _controller.Privacy();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }
}
