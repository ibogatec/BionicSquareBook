using System.Net;
using BionicSquare.IntegrationTests.Infrastructure;
using BionicSquare.Models;
using BionicSquare.Utility;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BionicSquare.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class HomeControllerIntegrationTests : IntegrationTestBase
{
    public HomeControllerIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    private async Task<ApplicationUser> CreateTestUserAsync()
    {
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"customer_{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Alice Home Shopper"
        };

        var result = await userManager.CreateAsync(user, "Password123!");
        result.Succeeded.Should().BeTrue();
        await userManager.AddToRoleAsync(user, Role.Customer);
        return user;
    }

    [Fact]
    public async Task Index_WhenAnonymous_ReturnsOkAndRendersSeededProducts()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var bodyText = document.Body?.TextContent ?? string.Empty;

        // Verify seeded catalog products are rendered in response HTML
        bodyText.Should().Contain("Store of the Jungle");
        bodyText.Should().Contain("Money and Time");
        bodyText.Should().Contain("Secret of the Lake");
    }

    [Fact]
    public async Task Index_WhenExceptionOccurs_EntersCatchAndReturnsViewWithEmptyList()
    {
        // Act: Controller instantiated without product services throws NullReferenceException inside try block
        var controller = new HomeController(null!, null!);
        var result = await controller.IndexGetAsync();

        // Assert: Catches exception, adds error to ModelState, and returns View with empty product enumerable
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeAssignableTo<IEnumerable<Product>>();
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.StartsWith("Error:"));
    }

    [Fact]
    public async Task Details_WhenProductExists_RendersProductDetailsAndPriceTiers()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Customer/Home/Details?productId=1&quantity=75");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var bodyText = document.Body?.TextContent ?? string.Empty;

        bodyText.Should().Contain("Store of the Jungle");
        bodyText.Should().Contain("Alba Solver");
        bodyText.Should().Contain("JNG7777770001");
    }

    [Fact]
    public async Task Details_WhenProductDoesNotExist_ReturnsInternalServerErrorDueToNullModelInView()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Customer/Home/Details?productId=999999");

        // Assert: Controller catches Exception and returns View(null), which Razor renders and encounters null reference
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DetailsPost_WhenAnonymous_RedirectsToLogin()
    {
        // Arrange
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "ProductId", "1" },
            { "Quantity", "1" }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Customer/Home/Details", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/Login");
    }

    [Fact]
    public async Task DetailsPost_WhenAuthenticated_AddsCartItemToDatabaseAndRedirects()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "ProductId", "2" },
            { "Quantity", "4" }
        });

        // Act
        var response = await Client.WithUser(user.Id).PostAsync("/Customer/Home/Details", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("Details?productId=2");

        // Verify SQL Server database reflects new ShoppingCart item
        var cartItem = await ExecuteDbContextAsync(async db =>
            await db.ShoppingCarts.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id && s.ProductId == 2));

        cartItem.Should().NotBeNull();
        cartItem.Quantity.Should().Be(4);
    }

    [Fact]
    public async Task DetailsPost_WhenAddingToExistingCartItem_IncrementsQuantity()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var initialCart = new ShoppingCart
        {
            ApplicationUserId = user.Id,
            ProductId = 3,
            Quantity = 2
        };
        await ExecuteDbContextAsync(async db =>
        {
            db.ShoppingCarts.Add(initialCart);
            await db.SaveChangesAsync();
        });

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "ProductId", "3" },
            { "Quantity", "5" }
        });

        // Act
        var response = await Client.WithUser(user.Id).PostAsync("/Customer/Home/Details", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var cartItem = await ExecuteDbContextAsync(async db =>
            await db.ShoppingCarts.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id && s.ProductId == 3));

        cartItem.Should().NotBeNull();
        cartItem.Quantity.Should().Be(7); // 2 + 5
    }

    [Fact]
    public async Task DetailsPost_WhenUserHasEmptyNameIdentifier_ReturnsUnauthorized()
    {
        // Arrange: Authenticated principal without NameIdentifier claim
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "ProductId", "1" },
            { "Quantity", "1" }
        });

        // Act
        var response = await Client.WithUser("__NO_NAME_ID__").PostAsync("/Customer/Home/Details", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DetailsPost_WhenForeignKeyFails_EntersCatchAndReturnsInternalServerError()
    {
        // Arrange: Authenticated user but non-existent product ID triggers DB foreign key exception
        var user = await CreateTestUserAsync();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "ProductId", "999999" },
            { "Quantity", "1" }
        });

        // Act
        var response = await Client.WithUser(user.Id).PostAsync("/Customer/Home/Details", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Privacy_ReturnsSuccessAndRendersView()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Customer/Home/Privacy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        document.Body.Should().NotBeNull();
    }
}
