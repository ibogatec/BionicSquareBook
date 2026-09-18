using System.Security.Claims;
using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.Models.ViewModels;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BionicSquare.UnitTests.Controllers;

public class CartControllerTests
{
    private readonly Mock<IShoppingCartService> _mockCartService;
    private readonly Mock<IApplicationUserService> _mockUserService;
    private readonly CartController _controller;

    public CartControllerTests()
    {
        _mockCartService = new Mock<IShoppingCartService>();
        _mockUserService = new Mock<IApplicationUserService>();
        _controller = new CartController(_mockCartService.Object, _mockUserService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user-100")
        ], "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task IndexGetAsync_WhenUserIsAuthenticated_ShouldCalculateTotalAndReturnViewModel()
    {
        // Arrange
        var product = new Product { Id = 1, Price = 50, Price50 = 45, Price100 = 40 };
        var cartItems = new List<ShoppingCart>
        {
            new() { Id = 1, ProductId = 1, Product = product, Quantity = 2, ApplicationUserId = "user-100" }
        };
        var appUser = new ApplicationUser
        {
            Id = "user-100",
            Name = "John Doe",
            City = "Metropolis",
            PostalCode = "10001"
        };

        _mockCartService.Setup(s => s.GetUserCartItemsAsync("user-100")).ReturnsAsync(cartItems);
        _mockUserService.Setup(s => s.GetUserByIdAsync("user-100")).ReturnsAsync(appUser);

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ShoppingCartViewModel>().Subject;
        model.CartItems.Should().HaveCount(1);
        model.OrderHeader.OrderTotal.Should().Be(100.0); // 2 * 50
        model.OrderHeader.Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task IndexGetAsync_WhenUserHasAllProfileFields_ShouldPopulateOrderHeaderFully()
    {
        // Arrange
        var appUser = new ApplicationUser
        {
            Id = "user-100",
            Name = "Jane Doe",
            PhoneNumber = "555-1234",
            StreetAddress = "456 Elm St",
            City = "Gotham",
            State = "NY",
            PostalCode = "10002"
        };
        _mockCartService.Setup(s => s.GetUserCartItemsAsync("user-100")).ReturnsAsync(new List<ShoppingCart>());
        _mockUserService.Setup(s => s.GetUserByIdAsync("user-100")).ReturnsAsync(appUser);

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ShoppingCartViewModel>().Subject;
        model.OrderHeader.PhoneNumber.Should().Be("555-1234");
        model.OrderHeader.StreetAddress.Should().Be("456 Elm St");
        model.OrderHeader.City.Should().Be("Gotham");
        model.OrderHeader.State.Should().Be("NY");
        model.OrderHeader.PostalCode.Should().Be("10002");
    }

    [Fact]
    public async Task IndexGetAsync_WhenUserHasNullProfileFields_ShouldFallbackToNA()
    {
        // Arrange
        var appUser = new ApplicationUser
        {
            Id = "user-100",
            Name = "Jane Doe",
            PhoneNumber = null,
            StreetAddress = null,
            City = null,
            State = null,
            PostalCode = null
        };
        _mockCartService.Setup(s => s.GetUserCartItemsAsync("user-100")).ReturnsAsync(new List<ShoppingCart>());
        _mockUserService.Setup(s => s.GetUserByIdAsync("user-100")).ReturnsAsync(appUser);

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ShoppingCartViewModel>().Subject;
        model.OrderHeader.PhoneNumber.Should().Be("N/A");
        model.OrderHeader.StreetAddress.Should().Be("N/A");
        model.OrderHeader.City.Should().Be("N/A");
        model.OrderHeader.State.Should().Be("N/A");
        model.OrderHeader.PostalCode.Should().Be("N/A");
    }

    [Fact]
    public async Task IndexGetAsync_WhenUserIdIsMissing_ShouldReturnUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task IndexGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnViewWithNullModel()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetUserCartItemsAsync("user-100"))
            .ThrowsAsync(new Exception("Database disconnected"));

        // Act
        var result = await _controller.IndexGetAsync();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Database disconnected"));
    }

    [Fact]
    public async Task PlusGetAsync_WithValidCartId_ShouldIncrementQuantityAndRedirect()
    {
        // Arrange
        var cart = new ShoppingCart { Id = 10, Quantity = 2 };
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ReturnsAsync(cart);

        // Act
        var result = await _controller.PlusGetAsync(10);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        cart.Quantity.Should().Be(3);
        _mockCartService.Verify(s => s.UpdateCartAsync(cart), Times.Once);
    }

    [Fact]
    public async Task PlusGetAsync_WhenCartNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ReturnsAsync((ShoppingCart?)null);

        // Act
        var result = await _controller.PlusGetAsync(10);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PlusGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnViewNull()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ThrowsAsync(new Exception("DB failure"));

        // Act
        var result = await _controller.PlusGetAsync(10);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task MinusGetAsync_WithValidCartId_ShouldDecrementQuantityAndRedirect()
    {
        // Arrange
        var cart = new ShoppingCart { Id = 10, Quantity = 3 };
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ReturnsAsync(cart);

        // Act
        var result = await _controller.MinusGetAsync(10);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        cart.Quantity.Should().Be(2);
        _mockCartService.Verify(s => s.UpdateCartAsync(cart), Times.Once);
    }

    [Fact]
    public async Task MinusGetAsync_WhenCartNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ReturnsAsync((ShoppingCart?)null);

        // Act
        var result = await _controller.MinusGetAsync(10);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MinusGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnViewNull()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ThrowsAsync(new Exception("DB failure"));

        // Act
        var result = await _controller.MinusGetAsync(10);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveGetAsync_WithValidCartId_ShouldSetQuantityToZeroAndRedirect()
    {
        // Arrange
        var cart = new ShoppingCart { Id = 10, Quantity = 5 };
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ReturnsAsync(cart);

        // Act
        var result = await _controller.RemoveGetAsync(10);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        cart.Quantity.Should().Be(0);
        _mockCartService.Verify(s => s.UpdateCartAsync(cart), Times.Once);
    }

    [Fact]
    public async Task RemoveGetAsync_WhenCartNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ReturnsAsync((ShoppingCart?)null);

        // Act
        var result = await _controller.RemoveGetAsync(10);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RemoveGetAsync_WhenServiceThrows_ShouldAddModelStateErrorAndReturnViewNull()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(10)).ThrowsAsync(new Exception("DB failure"));

        // Act
        var result = await _controller.RemoveGetAsync(10);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCartPostAsync_WhenCartNotFound_ShouldReturnNotFoundObjectResult()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(15)).ReturnsAsync((ShoppingCart?)null);

        // Act
        var result = await _controller.UpdateCartPostAsync(15, 4);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var messageProp = notFoundResult.Value?.GetType().GetProperty("message")?.GetValue(notFoundResult.Value);
        messageProp.Should().Be("Cart not found");
    }

    [Fact]
    public async Task UpdateCartPostAsync_WhenCartExists_ShouldUpdateQuantityAndReturnOk()
    {
        // Arrange
        var cart = new ShoppingCart { Id = 15, Quantity = 1 };
        _mockCartService.Setup(s => s.GetCartByIdAsync(15)).ReturnsAsync(cart);

        // Act
        var result = await _controller.UpdateCartPostAsync(15, 7);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var messageProp = okResult.Value?.GetType().GetProperty("message")?.GetValue(okResult.Value);
        messageProp.Should().Be("Cart updated successfully");
        cart.Quantity.Should().Be(7);
        _mockCartService.Verify(s => s.UpdateCartAsync(cart), Times.Once);
    }

    [Fact]
    public async Task UpdateCartPostAsync_WhenServiceThrows_ShouldReturnStatusCode500()
    {
        // Arrange
        _mockCartService.Setup(s => s.GetCartByIdAsync(15)).ThrowsAsync(new Exception("Write failure"));

        // Act
        var result = await _controller.UpdateCartPostAsync(15, 3);

        // Assert
        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var messageProp = objResult.Value?.GetType().GetProperty("message")?.GetValue(objResult.Value);
        messageProp.Should().Be("Error: Write failure");
    }
}
