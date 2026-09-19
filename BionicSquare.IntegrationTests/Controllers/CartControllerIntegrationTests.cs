using System.Net;
using System.Text.Json;
using BionicSquare.Business.Services;
using BionicSquare.IntegrationTests.Infrastructure;
using BionicSquare.Models;
using BionicSquare.Utility;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BionicSquare.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class CartControllerIntegrationTests : IntegrationTestBase
{
    public CartControllerIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    private class NullCartService : IShoppingCartService
    {
        public Task<ShoppingCart?> GetCartByIdAsync(int cartId) => Task.FromResult<ShoppingCart?>(null);
        public Task<IEnumerable<ShoppingCart>> GetUserCartItemsAsync(string userId) => Task.FromResult(Enumerable.Empty<ShoppingCart>());
        public Task<int> GetCartCountAsync(string userId) => Task.FromResult(0);
        public Task<ShoppingCart> AddToCartAsync(ShoppingCart shoppingCart) => Task.FromResult(shoppingCart);
        public Task<ShoppingCart> UpdateCartAsync(ShoppingCart shoppingCart) => Task.FromResult(shoppingCart);
        public Task ClearCartAsync(string userId) => Task.CompletedTask;
    }

    private async Task<ApplicationUser> CreateTestUserAsync(string role = Role.Customer, bool withAddress = true)
    {
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"user_{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "John Cart Tester",
            StreetAddress = withAddress ? "123 Test Avenue" : null,
            City = withAddress ? "Tech City" : null,
            State = withAddress ? "CA" : null,
            PostalCode = withAddress ? "94016" : null,
            PhoneNumber = withAddress ? "555-123-4567" : null
        };

        var result = await userManager.CreateAsync(user, "TestP@ssword123!");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    [Fact]
    public async Task Index_WhenAnonymous_RedirectsToLogin()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Customer/Cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/Login");
    }

    [Fact]
    public async Task Index_WhenUserHasEmptyNameIdentifier_ReturnsUnauthorized()
    {
        // Act: Authenticated principal without NameIdentifier claim
        var response = await Client.WithUser("__NO_NAME_ID__").GetAsync("/Customer/Cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Index_WhenUserNotFoundInDatabase_EntersCatchAndReturnsInternalServerError()
    {
        // Act: Valid Guid userId not registered in AspNetUsers
        var nonExistentUserId = Guid.NewGuid().ToString();
        var response = await Client.WithUser(nonExistentUserId).GetAsync("/Customer/Cart");

        // Assert: Throws in GetUserByIdAsync and enters catch block returning View(null) -> 500
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Index_WhenAuthenticatedUserHasItems_RendersCartAndCalculatesTierTotal()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Product 1: "Store of the Jungle", Price = 40 (<=50 qty)
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 2
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).GetAsync("/Customer/Cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var bodyText = document.Body?.TextContent ?? string.Empty;

        bodyText.Should().Contain("Store of the Jungle");
        // 2 * 40 = 80 total
        bodyText.Should().Contain("80.00");

        // Verify pre-populated customer checkout inputs via AngleSharp DOM query
        var nameInput = document.QuerySelector("input[name='OrderHeader.Name']")?.GetAttribute("value");
        nameInput.Should().Be("John Cart Tester");

        var addressInput = document.QuerySelector("input[name='OrderHeader.StreetAddress']")?.GetAttribute("value");
        addressInput.Should().Be("123 Test Avenue");
    }

    [Fact]
    public async Task Index_WhenUserAddressFieldsAreNull_CoalescesToNA()
    {
        // Arrange
        var user = await CreateTestUserAsync(withAddress: false);
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 75 // exercises Price50 tier
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).GetAsync("/Customer/Cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);

        var addressInput = document.QuerySelector("input[name='OrderHeader.StreetAddress']")?.GetAttribute("value");
        addressInput.Should().Be("N/A");

        var phoneInput = document.QuerySelector("input[name='OrderHeader.PhoneNumber']")?.GetAttribute("value");
        phoneInput.Should().Be("N/A");
    }

    [Fact]
    public async Task Index_WhenUserHasOver100Quantity_ExercisesPrice100Tier()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 120 // exercises Price100 tier
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).GetAsync("/Customer/Cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Plus_WhenCartItemExists_IncrementsQuantityInDatabaseAndRedirects()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 2
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).GetAsync($"/Customer/Cart/Plus?cartId={cart.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer/Cart");

        // Verify in SQL Server database
        var updatedCart = await ExecuteDbContextAsync(async db =>
            await db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == cart.Id));

        updatedCart.Should().NotBeNull();
        updatedCart.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Plus_WhenCartNotFound_ReturnsInternalServerErrorDueToMissingView()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act: Controller catches not found exception and calls View(null), which fails as 'Plus' view does not exist
        var response = await Client.WithUser(user.Id).GetAsync("/Customer/Cart/Plus?cartId=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Plus_WhenCartReturnsNull_ReturnsNotFound()
    {
        // Arrange
        var controller = new CartController(new NullCartService(), null!);

        // Act
        var result = await controller.PlusGetAsync(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Minus_WhenQuantityGreaterThanOne_DecrementsQuantityInDatabase()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 3
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).GetAsync($"/Customer/Cart/Minus?cartId={cart.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer/Cart");

        // Verify in SQL Server database
        var updatedCart = await ExecuteDbContextAsync(async db =>
            await db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == cart.Id));

        updatedCart.Should().NotBeNull();
        updatedCart.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Minus_WhenQuantityIsOne_RemovesFromDatabase()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 1
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).GetAsync($"/Customer/Cart/Minus?cartId={cart.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var inDb = await ExecuteDbContextAsync(async db =>
            await db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == cart.Id));
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task Minus_WhenCartNotFound_ReturnsInternalServerErrorDueToMissingView()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act: Controller catches not found exception and calls View(null), which fails as 'Minus' view does not exist
        var response = await Client.WithUser(user.Id).GetAsync("/Customer/Cart/Minus?cartId=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Minus_WhenCartReturnsNull_ReturnsNotFound()
    {
        // Arrange
        var controller = new CartController(new NullCartService(), null!);

        // Act
        var result = await controller.MinusGetAsync(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Remove_WhenCartItemExists_DeletesCartItemFromDatabase()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 2
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).GetAsync($"/Customer/Cart/Remove?cartId={cart.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer/Cart");

        // Verify in SQL Server database
        var removedCart = await ExecuteDbContextAsync(async db =>
            await db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == cart.Id));

        removedCart.Should().BeNull();
    }

    [Fact]
    public async Task Remove_WhenCartNotFound_ReturnsInternalServerErrorDueToMissingView()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act: Controller catches not found exception and calls View(null), which fails as 'Remove' view does not exist
        var response = await Client.WithUser(user.Id).GetAsync("/Customer/Cart/Remove?cartId=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Remove_WhenCartReturnsNull_ReturnsNotFound()
    {
        // Arrange
        var controller = new CartController(new NullCartService(), null!);

        // Act
        var result = await controller.RemoveGetAsync(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateCartPost_WhenAuthorized_UpdatesQuantityInDatabaseViaApi()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var cart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 1,
            Quantity = 1
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithUser(user.Id).PostAsync($"/api/cart/update?cartId={cart.Id}&quantity=15", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("Cart updated successfully");

        // Verify in SQL Server database
        var updatedCart = await ExecuteDbContextAsync(async db =>
            await db.ShoppingCarts.FirstOrDefaultAsync(s => s.Id == cart.Id));

        updatedCart.Should().NotBeNull();
        updatedCart.Quantity.Should().Be(15);
    }

    [Fact]
    public async Task UpdateCartPost_WhenCartNotFound_ReturnsServerErrorDueToServiceException()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act - Cart ID 999999 does not exist, ShoppingCartService throws exception which controller catches
        var response = await Client.WithUser(user.Id).PostAsync("/api/cart/update?cartId=999999&quantity=5", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found in database");
    }

    [Fact]
    public async Task UpdateCartPost_WhenCartReturnsNull_ReturnsNotFoundJson()
    {
        // Arrange
        var controller = new CartController(new NullCartService(), null!);

        // Act
        var result = await controller.UpdateCartPostAsync(1, 5);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
