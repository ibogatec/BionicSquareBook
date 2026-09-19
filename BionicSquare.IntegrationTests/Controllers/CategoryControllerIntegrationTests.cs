using System.Net;
using AngleSharp.Html.Dom;
using BionicSquare.IntegrationTests.Infrastructure;
using BionicSquare.Models;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BionicSquare.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class CategoryControllerIntegrationTests : IntegrationTestBase
{
    public CategoryControllerIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Index_WhenAnonymous_RedirectsToLogin()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Admin/Category");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/Login");
    }

    [Fact]
    public async Task Index_WhenNonAdminUser_RedirectsToAccessDenied()
    {
        // Act
        var response = await Client.WithUser("customer-user-id").GetAsync("/Admin/Category");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/AccessDenied");
    }

    [Fact]
    public async Task Index_WhenAdminUser_ReturnsOkWithSeededCategories()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Category");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var pageText = document.Body?.TextContent ?? string.Empty;

        // Verify seeded categories from ApplicationDbContext are rendered
        pageText.Should().Contain("Action");
        pageText.Should().Contain("SciFi");
        pageText.Should().Contain("History");
    }

    [Fact]
    public async Task Index_WhenExceptionOccurs_EntersCatchAndReturnsView()
    {
        // Act: CategoryController instantiated without service throws NullReferenceException inside try block
        var controller = new CategoryController(null!);
        var result = await controller.IndexGetAsync();

        // Assert: Catches exception and returns empty View()
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Get_WhenAdminUser_ReturnsSuccessAndRendersView()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Category/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        document.Body.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WhenValidCategorySubmitted_PersistsToDatabaseAndRedirects()
    {
        // Arrange - DisplayOrder must be between 100 and 1000 per model validation
        var newCategoryName = "Cyberpunk Adventures " + Guid.NewGuid().ToString("N")[..8];
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Name", newCategoryName },
            { "DisplayOrder", "250" }
        });

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Create", formContent);

        // Assert response redirect
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Admin/Category");

        // Assert SQL Server database state directly via DbContext
        var savedCategory = await ExecuteDbContextAsync(async db =>
            await db.Categories.FirstOrDefaultAsync(c => c.Name == newCategoryName));

        savedCategory.Should().NotBeNull();
        savedCategory.DisplayOrder.Should().Be(250);
    }

    [Fact]
    public async Task Create_WhenInvalidDataSubmitted_DoesNotPersistAndRendersValidation()
    {
        // Arrange - Name is empty and DisplayOrder 5 is outside [100, 1000]
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Name", "" },
            { "DisplayOrder", "5" }
        });

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Create", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var validationErrors = document.QuerySelectorAll(".field-validation-error, .validation-summary-errors");
        validationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WhenDuplicateNameSubmitted_ReturnsViewWithModelError()
    {
        // Arrange: Category name that already exists (e.g. Action)
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Name", "Action" },
            { "DisplayOrder", "200" }
        });

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Create", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("error:");
    }

    [Fact]
    public async Task Update_Get_WhenValidId_ReturnsSuccessAndRendersCategory()
    {
        // Arrange
        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());

        // Act
        var response = await Client.WithAdmin().GetAsync($"/Admin/Category/Update?id={category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var input = document.QuerySelector("input[name='Name']") as IHtmlInputElement;
        input?.Value.Should().Be(category.Name);
    }

    [Fact]
    public async Task Update_Get_WhenNonExistentId_ReturnsViewWithModelError()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Category/Update?id=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Error:");
    }

    [Fact]
    public async Task Update_WhenValidDataSubmitted_UpdatesDatabaseAndRedirects()
    {
        // Arrange: Insert a category into SQL Server directly (DisplayOrder within 100-1000)
        var category = new Category { Name = "Old Category " + Guid.NewGuid().ToString("N")[..6], DisplayOrder = 200 };
        await ExecuteDbContextAsync(async db =>
        {
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        });

        var updatedName = "Updated Category " + Guid.NewGuid().ToString("N")[..6];
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Id", category.Id.ToString() },
            { "Name", updatedName },
            { "DisplayOrder", "300" }
        });

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Update", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Admin/Category");

        // Verify update in SQL Server database
        var refreshedCategory = await ExecuteDbContextAsync(async db =>
            await db.Categories.FindAsync(category.Id));

        refreshedCategory.Should().NotBeNull();
        refreshedCategory.Name.Should().Be(updatedName);
        refreshedCategory.DisplayOrder.Should().Be(300);
    }

    [Fact]
    public async Task Update_WhenInvalidDataSubmitted_ReturnsView()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Id", "1" },
            { "Name", "" },
            { "DisplayOrder", "5" }
        });

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Update", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_WhenDuplicateNameSubmitted_ReturnsViewWithModelError()
    {
        // Arrange: Category name that matches another existing category
        var category = new Category { Name = "To Update " + Guid.NewGuid().ToString("N")[..6], DisplayOrder = 200 };
        await ExecuteDbContextAsync(async db =>
        {
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        });

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Id", category.Id.ToString() },
            { "Name", "Action" }, // Already exists
            { "DisplayOrder", "200" }
        });

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Update", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Error:");
    }

    [Fact]
    public async Task Delete_Get_WhenValidId_ReturnsSuccessAndRendersCategory()
    {
        // Arrange
        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());

        // Act
        var response = await Client.WithAdmin().GetAsync($"/Admin/Category/Delete?id={category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var input = document.QuerySelector("input[name='Name']") as IHtmlInputElement;
        input?.Value.Should().Be(category.Name);
    }

    [Fact]
    public async Task Delete_Get_WhenNonExistentId_ReturnsViewWithModelError()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Category/Delete?id=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Error:");
    }

    [Fact]
    public async Task Delete_WhenCategoryExists_RemovesFromDatabaseAndRedirects()
    {
        // Arrange: Insert a category to delete
        var category = new Category { Name = "To Delete " + Guid.NewGuid().ToString("N")[..6], DisplayOrder = 250 };
        await ExecuteDbContextAsync(async db =>
        {
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithAdmin().PostAsync($"/Admin/Category/Delete?id={category.Id}", new FormUrlEncodedContent([]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Admin/Category");

        // Verify removed from SQL Server database
        var deletedCategory = await ExecuteDbContextAsync(async db =>
            await db.Categories.FindAsync(category.Id));

        deletedCategory.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WhenNonExistentId_ReturnsViewWithModelError()
    {
        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Delete?id=999999", new FormUrlEncodedContent([]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Error:");
    }

    [Fact]
    public async Task Delete_WhenInvalidModelSubmitted_ReturnsView()
    {
        // Act: id is non-integer string, causing ModelState.IsValid = false
        var response = await Client.WithAdmin().PostAsync("/Admin/Category/Delete?id=invalid_id", new FormUrlEncodedContent([]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
