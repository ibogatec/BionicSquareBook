using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AngleSharp.Html.Dom;
using BionicSquare.IntegrationTests.Infrastructure;
using BionicSquare.Models;
using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BionicSquare.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class ProductControllerIntegrationTests : IntegrationTestBase
{
    public ProductControllerIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Index_UnauthenticatedUser_RedirectsToLogin()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Admin/Product");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/Login");
    }

    [Fact]
    public async Task Index_CustomerRole_RedirectsToAccessDenied()
    {
        // Act
        var response = await Client.WithUser("customer-user").GetAsync("/Admin/Product");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/AccessDenied");
    }

    [Fact]
    public async Task Index_AdminRole_ReturnsSuccessAndRendersView()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Product");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        document.Body.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Get_AdminRole_ReturnsSuccessAndRendersCategories()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Product/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var select = document.QuerySelector("select");
        select.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Get_WhenExceptionOccurs_EntersCatchAndReturnsViewWithModelError()
    {
        // Act: Controller instantiated without category services causes NullReferenceException inside try block
        var controller = new ProductController(null!, null!, null!);
        var result = await controller.CreateGetAsync();

        // Assert: Catches exception, adds error to ModelState, and returns empty View()
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.StartsWith("Error:"));
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange: Missing required Title, Isbn, etc.
        var postData = new Dictionary<string, string>
        {
            { "Product.Price", "-5" }
        };

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Create", new FormUrlEncodedContent(postData));

        // Assert: Returns view with validation errors (200 OK)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Post_ValidDataWithoutFile_PersistsProductAndRedirects()
    {
        // Arrange
        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var postData = new Dictionary<string, string>
        {
            { "Product.Title", "Integration Test Book 101" },
            { "Product.Description", "A great book for testing" },
            { "Product.Isbn", "978-0123456789" },
            { "Product.Author", "Integration Author" },
            { "Product.ListPrice", "99" },
            { "Product.Price", "90" },
            { "Product.Price50", "85" },
            { "Product.Price100", "80" },
            { "Product.CategoryId", category.Id.ToString(CultureInfo.InvariantCulture) }
        };

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Create", new FormUrlEncodedContent(postData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Admin/Product");

        var createdInDb = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Title == "Integration Test Book 101"));
        createdInDb.Should().NotBeNull();
        createdInDb.Author.Should().Be("Integration Author");
        createdInDb.Price.Should().Be(90);
    }

    [Fact]
    public async Task Create_Post_ValidDataWithFile_SavesFileToDiskPersistsProductAndRedirects()
    {
        // Arrange: Delete target directory so Directory.CreateDirectory branch is executed
        var testWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var uploadDir = Path.Combine(testWebRoot, "images", "uploads", "products");
        if (Directory.Exists(uploadDir))
        {
            Directory.Delete(uploadDir, recursive: true);
        }

        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var multipartContent = new MultipartFormDataContent
        {
            { new StringContent("Book With Cover Image"), "Product.Title" },
            { new StringContent("Has a cover image"), "Product.Description" },
            { new StringContent("978-9876543210"), "Product.Isbn" },
            { new StringContent("Cover Author"), "Product.Author" },
            { new StringContent("50"), "Product.ListPrice" },
            { new StringContent("45"), "Product.Price" },
            { new StringContent("40"), "Product.Price50" },
            { new StringContent("35"), "Product.Price100" },
            { new StringContent(category.Id.ToString(CultureInfo.InvariantCulture)), "Product.CategoryId" }
        };

        byte[] fileBytes = [.. "fake-image-bytes"u8];
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        multipartContent.Add(fileContent, "file", "test-cover.jpg");

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Create", multipartContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var createdInDb = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Title == "Book With Cover Image"));
        createdInDb.Should().NotBeNull();
        createdInDb.ImageUrl.Should().NotBeNullOrEmpty();
        createdInDb.ImageUrl.Should().Contain("images/uploads/products");
    }

    [Fact]
    public async Task Create_Post_WhenForeignKeyFails_EntersCatchAndReturnsView()
    {
        // Arrange: CategoryId 999999 triggers DB FK violation
        var postData = new Dictionary<string, string>
        {
            { "Product.Title", "FK Failing Book" },
            { "Product.Description", "Desc" },
            { "Product.Isbn", "978-9999999999" },
            { "Product.Author", "Author" },
            { "Product.ListPrice", "50" },
            { "Product.Price", "45" },
            { "Product.Price50", "40" },
            { "Product.Price100", "35" },
            { "Product.CategoryId", "999999" }
        };

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Create", new FormUrlEncodedContent(postData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var errors = document.QuerySelectorAll(".validation-summary-errors, .field-validation-error");
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_Get_ValidId_ReturnsSuccessAndRendersProduct()
    {
        // Arrange
        var product = await ExecuteDbContextAsync(db => db.Products.FirstAsync());

        // Act
        var response = await Client.WithAdmin().GetAsync($"/Admin/Product/Update?id={product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var titleInput = document.QuerySelector("input[name='Product.Title']") as IHtmlInputElement;
        titleInput?.Value.Should().Be(product.Title);
    }

    [Fact]
    public async Task Update_Get_NonExistentId_ReturnsViewWithModelError()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Product/Update?id=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Error");
    }

    [Fact]
    public async Task Update_Post_InvalidModel_ReturnsView()
    {
        // Arrange: Missing title and invalid price
        var postData = new Dictionary<string, string>
        {
            { "Product.Id", "1" },
            { "Product.Price", "-100" }
        };

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Update", new FormUrlEncodedContent(postData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_Post_ValidData_UpdatesProductAndRedirects()
    {
        // Arrange: Create a product to update
        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var product = new Product
        {
            Title = "Original Product Title",
            Description = "Desc",
            Isbn = "978-1111222233",
            Author = "Original Author",
            ListPrice = 100,
            Price = 90,
            Price50 = 85,
            Price100 = 80,
            CategoryId = category.Id
        };
        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        });

        var postData = new Dictionary<string, string>
        {
            { "Product.Id", product.Id.ToString(CultureInfo.InvariantCulture) },
            { "Product.Title", "Updated Product Title" },
            { "Product.Description", "Updated Description" },
            { "Product.Isbn", "978-1111222233" },
            { "Product.Author", "Original Author" },
            { "Product.ListPrice", "120" },
            { "Product.Price", "110" },
            { "Product.Price50", "105" },
            { "Product.Price100", "100" },
            { "Product.CategoryId", category.Id.ToString(CultureInfo.InvariantCulture) }
        };

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Update", new FormUrlEncodedContent(postData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var updatedInDb = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id));
        updatedInDb!.Title.Should().Be("Updated Product Title");
        updatedInDb.Price.Should().Be(110);
    }

    [Fact]
    public async Task Update_Post_WithNewImage_ReplacesOldImageFileOnDisk()
    {
        // Arrange: Delete products upload directory to test Directory.CreateDirectory
        var testWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var uploadDir = Path.Combine(testWebRoot, "images", "uploads", "products");
        if (Directory.Exists(uploadDir))
        {
            Directory.Delete(uploadDir, recursive: true);
        }

        // Store existing image in a parent folder so it's not deleted with uploadDir
        var oldImageRelPath = Path.Combine("images", "old-cover.jpg");
        var oldImageAbsPath = Path.Combine(testWebRoot, oldImageRelPath);
        Directory.CreateDirectory(Path.GetDirectoryName(oldImageAbsPath)!);
        await File.WriteAllTextAsync(oldImageAbsPath, "old image content");

        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var product = new Product
        {
            Title = "Product For Image Replace",
            Description = "Desc",
            Isbn = "978-4444555566",
            Author = "Author",
            ListPrice = 50,
            Price = 40,
            Price50 = 35,
            Price100 = 30,
            CategoryId = category.Id,
            ImageUrl = oldImageRelPath
        };
        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        });

        var multipartContent = new MultipartFormDataContent
        {
            { new StringContent(product.Id.ToString(CultureInfo.InvariantCulture)), "Product.Id" },
            { new StringContent("Product For Image Replace"), "Product.Title" },
            { new StringContent("Desc"), "Product.Description" },
            { new StringContent("978-4444555566"), "Product.Isbn" },
            { new StringContent("Author"), "Product.Author" },
            { new StringContent("50"), "Product.ListPrice" },
            { new StringContent("40"), "Product.Price" },
            { new StringContent("35"), "Product.Price50" },
            { new StringContent("30"), "Product.Price100" },
            { new StringContent(category.Id.ToString(CultureInfo.InvariantCulture)), "Product.CategoryId" },
            { new StringContent(product.ImageUrl), "Product.ImageUrl" }
        };

        var newImageContent = new ByteArrayContent([.. "new image content"u8]);
        newImageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        multipartContent.Add(newImageContent, "file", "new-cover.jpg");

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Update", multipartContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        File.Exists(oldImageAbsPath).Should().BeFalse(); // Old image was deleted

        var updated = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id));
        updated!.ImageUrl.Should().NotBeNull();
        updated.ImageUrl.Should().NotBe(product.ImageUrl);
    }

    [Fact]
    public async Task Update_Post_WhenForeignKeyFails_EntersCatchAndReturnsView()
    {
        // Arrange
        var product = await ExecuteDbContextAsync(db => db.Products.FirstAsync());
        var postData = new Dictionary<string, string>
        {
            { "Product.Id", product.Id.ToString(CultureInfo.InvariantCulture) },
            { "Product.Title", product.Title },
            { "Product.Description", product.Description },
            { "Product.Isbn", product.Isbn },
            { "Product.Author", product.Author },
            { "Product.ListPrice", product.ListPrice.ToString(CultureInfo.InvariantCulture) },
            { "Product.Price", product.Price.ToString(CultureInfo.InvariantCulture) },
            { "Product.Price50", product.Price50.ToString(CultureInfo.InvariantCulture) },
            { "Product.Price100", product.Price100.ToString(CultureInfo.InvariantCulture) },
            { "Product.CategoryId", "999999" } // Causes FK exception
        };

        // Act
        var response = await Client.WithAdmin().PostAsync("/Admin/Product/Update", new FormUrlEncodedContent(postData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var errors = document.QuerySelectorAll(".validation-summary-errors, .field-validation-error");
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Api_GetProducts_ReturnsJsonProductList()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var dataProp = jsonDoc.RootElement.GetProperty("data");
        dataProp.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Api_GetProducts_WhenExceptionOccurs_EntersCatchAndReturnsJson()
    {
        // Act: Controller instantiated without product services throws NullReferenceException inside try block
        var controller = new ProductController(null!, null!, null!);
        var result = await controller.IndexGetJsonAsync();

        // Assert: Catches exception and returns JsonResult with empty products
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        jsonResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Api_Delete_ValidId_DeletesProductAndReturnsSuccess()
    {
        // Arrange
        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var product = new Product
        {
            Title = "Product To Delete",
            Description = "Desc",
            Isbn = "978-7777888899",
            Author = "Author",
            ListPrice = 30,
            Price = 25,
            Price50 = 20,
            Price100 = 15,
            CategoryId = category.Id
        };
        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await Client.WithAdmin().DeleteAsync($"/api/products/delete?id={product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

        var inDb = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id));
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task Api_Delete_InvalidOrZeroId_ReturnsErrorJson()
    {
        // Act
        var response = await Client.WithAdmin().DeleteAsync("/api/products/delete?id=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid product id");
    }

    [Fact]
    public async Task Api_Delete_NonExistentId_ReturnsErrorJson()
    {
        // Act
        var response = await Client.WithAdmin().DeleteAsync("/api/products/delete?id=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Error");
    }
}
