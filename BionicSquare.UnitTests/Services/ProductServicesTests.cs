using System.Reflection;
using System.Runtime.CompilerServices;
using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BionicSquare.UnitTests.Services;

public class ProductServicesTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;

    public ProductServicesTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
    }

    [Fact]
    public async Task GetAllProductsAsync_WithoutCategory_ShouldReturnProducts()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var products = (await service.GetAllProductsAsync(includeCategory: false)).ToList();

        // Assert
        products.Should().NotBeNull();
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllProductsAsync_WithCategory_ShouldIncludeCategory()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var products = (await service.GetAllProductsAsync(includeCategory: true)).ToList();

        // Assert
        products.Should().NotBeEmpty();
        products.All(p => p.Category != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetProductByIdAsync_WithValidId_ShouldReturnProduct()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var existing = await context.Products.FirstAsync();

        // Act
        var result = await service.GetProductByIdAsync(existing.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existing.Id);
        result.Title.Should().Be(existing.Title);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithIncludeCategory_ShouldIncludeCategory()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var existing = await context.Products.FirstAsync();

        // Act
        var result = await service.GetProductByIdAsync(existing.Id, includeCategory: true);

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProductByIdAsync_WithIncludeCategory_WhenNonExistent_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var act = async () => await service.GetProductByIdAsync(9999, includeCategory: true);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*not found in database*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task GetProductByIdAsync_WithNullOrZeroId_ShouldThrowArgumentNullException(int? id)
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var act = async () => await service.GetProductByIdAsync(id);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistentId_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var act = async () => await service.GetProductByIdAsync(9999);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*not found in database*");
    }

    [Fact]
    public async Task CreateProductAsync_WithValidProduct_ShouldAddSuccessfully()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var category = await context.Categories.FirstAsync();

        var newProduct = new Product
        {
            Title = "Unit Testing in .NET",
            Author = "Quality Engineer",
            Description = "Guide to solid testing",
            Isbn = "TST1234567890",
            ListPrice = 50,
            Price = 45,
            Price50 = 40,
            Price100 = 35,
            CategoryId = category.Id
        };

        // Act
        var created = await service.CreateProductAsync(newProduct);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().BeGreaterThan(0);
        context.Products.Should().Contain(p => p.Title == "Unit Testing in .NET");
    }

    [Fact]
    public async Task CreateProductAsync_WithNullProduct_ShouldThrowArgumentNullException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var act = async () => await service.CreateProductAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateProductAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var category = await context.Categories.FirstAsync();

        var newProduct = new Product
        {
            Title = "Zero Save Product",
            Author = "Author",
            Description = "Desc",
            Isbn = "0000000000000",
            ListPrice = 50,
            Price = 45,
            Price50 = 40,
            Price100 = 35,
            CategoryId = category.Id
        };

        // Act
        var act = async () => await service.CreateProductAsync(newProduct);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to create product*");
    }

    [Fact]
    public async Task UpdateProductAsync_WithValidChanges_ShouldUpdateProduct()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var product = await context.Products.FirstAsync();
        context.Entry(product).State = EntityState.Detached;

        var productToUpdate = new Product
        {
            Id = product.Id,
            Title = "Updated Title for Testing",
            Author = product.Author,
            Description = product.Description,
            Isbn = product.Isbn,
            ListPrice = product.ListPrice,
            Price = product.Price,
            Price50 = product.Price50,
            Price100 = product.Price100,
            CategoryId = product.CategoryId
        };

        // Act
        var updated = await service.UpdateProductAsync(productToUpdate);

        // Assert
        updated.Title.Should().Be("Updated Title for Testing");
    }

    [Fact]
    public async Task UpdateProductAsync_WithNullProduct_ShouldThrowArgumentNullException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var act = async () => await service.UpdateProductAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateProductAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var product = await context.Products.FirstAsync();
        context.Entry(product).State = EntityState.Detached;

        var productToUpdate = new Product
        {
            Id = product.Id,
            Title = "Unsaved Product",
            Author = product.Author,
            Description = product.Description,
            Isbn = product.Isbn,
            ListPrice = product.ListPrice,
            Price = product.Price,
            Price50 = product.Price50,
            Price100 = product.Price100,
            CategoryId = product.CategoryId
        };

        // Act
        var act = async () => await service.UpdateProductAsync(productToUpdate);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to update product*");
    }

    [Fact]
    public async Task DeleteProductByIdAsync_WithValidId_ShouldRemoveProduct()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var category = await context.Categories.FirstAsync();

        var productToDelete = new Product
        {
            Title = "To Be Deleted",
            Author = "Temp Author",
            Description = "Short-lived product",
            Isbn = "DEL0000000001",
            ListPrice = 20,
            Price = 18,
            Price50 = 16,
            Price100 = 14,
            CategoryId = category.Id
        };
        await context.Products.AddAsync(productToDelete);
        await context.SaveChangesAsync();

        // Act
        var deleted = await service.DeleteProductByIdAsync(productToDelete.Id);

        // Assert
        deleted.Id.Should().Be(productToDelete.Id);
        context.Products.Should().NotContain(p => p.Id == productToDelete.Id);
    }

    [Fact]
    public async Task DeleteProductByIdAsync_WithExistingImage_ShouldDeleteImageFileFromDisk()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "ProductServicesTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(tempFolder);

        try
        {
            var relPath = Path.Combine("images", "test.jpg");
            var absPath = Path.Combine(tempFolder, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
            await File.WriteAllTextAsync(absPath, "dummy image");

            await using var context = TestDbContextFactory.Create();
            var service = new ProductServices(context, envMock.Object);
            var category = await context.Categories.FirstAsync();

            var product = new Product
            {
                Title = "Product with Image",
                Author = "Author",
                Description = "Desc",
                Isbn = "IMG1234567890",
                ListPrice = 20,
                Price = 18,
                Price50 = 16,
                Price100 = 14,
                CategoryId = category.Id,
                ImageUrl = relPath
            };
            await context.Products.AddAsync(product);
            await context.SaveChangesAsync();

            // Act
            await service.DeleteProductByIdAsync(product.Id);

            // Assert
            File.Exists(absPath).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }

    [Fact]
    public async Task DeleteProductByIdAsync_WithImageFileMissingOnDisk_ShouldSucceedWithoutThrowing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var category = await context.Categories.FirstAsync();

        var product = new Product
        {
            Title = "Missing Image Product",
            Author = "Author",
            Description = "Desc",
            Isbn = "NONEXIST1234",
            ListPrice = 20,
            Price = 18,
            Price50 = 16,
            Price100 = 14,
            CategoryId = category.Id,
            ImageUrl = "images/does-not-exist.jpg"
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        var deleted = await service.DeleteProductByIdAsync(product.Id);

        // Assert
        deleted.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task DeleteProductByIdAsync_WithNullImageUrl_ShouldSucceed()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var category = await context.Categories.FirstAsync();

        var product = new Product
        {
            Title = "No Image Product",
            Author = "Author",
            Description = "Desc",
            Isbn = "NOIMG1234567",
            ListPrice = 20,
            Price = 18,
            Price50 = 16,
            Price100 = 14,
            CategoryId = category.Id,
            ImageUrl = null
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        var deleted = await service.DeleteProductByIdAsync(product.Id);

        // Assert
        deleted.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task DeleteProductByIdAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new ProductServices(context, _mockEnvironment.Object);
        var existing = await context.Products.FirstAsync();

        // Act
        var act = async () => await service.DeleteProductByIdAsync(existing.Id);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to delete product*");
    }

    [Fact]
    public async Task DeleteProductByIdAsync_WithNonExistentId_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductServices(context, _mockEnvironment.Object);

        // Act
        var act = async () => await service.DeleteProductByIdAsync(9999);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void DeleteProductByIdAsync_WhenStateMachineYieldsNullProduct_ShouldThrowProductNotFoundException()
    {
        // Arrange
        var nestedType = typeof(ProductServices).GetNestedType("<DeleteProductByIdAsync>d__7", BindingFlags.NonPublic | BindingFlags.Public);
        nestedType.Should().NotBeNull();

        // Boxed struct
        object stateMachine = Activator.CreateInstance(nestedType!)!;

        // Find fields
        var stateField = nestedType!.GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var builderField = nestedType.GetField("<>t__builder", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var thisField = nestedType.GetField("<>4__this", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var idField = nestedType.GetField("id", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var awaiterField = nestedType.GetField("<>u__1", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        var service = new ProductServices(TestDbContextFactory.Create(), _mockEnvironment.Object);
        thisField?.SetValue(stateMachine, service);
        idField?.SetValue(stateMachine, (int?)123);
        stateField?.SetValue(stateMachine, 0); // state 0 means resumption after await GetProductByIdAsync
        awaiterField?.SetValue(stateMachine, Task.FromResult<Product>(null!).GetAwaiter());

        // Act
        var moveNextMethod = nestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        moveNextMethod.Should().NotBeNull();
        moveNextMethod!.Invoke(stateMachine, null);

        // Extract Task from builder to verify exception thrown
        var builder = builderField!.GetValue(stateMachine);
        var taskProperty = builder!.GetType().GetProperty("Task", BindingFlags.Public | BindingFlags.Instance);
        var task = (Task<Product>)taskProperty!.GetValue(builder)!;

        // Assert
        task.IsFaulted.Should().BeTrue();
        task.Exception!.InnerException.Should().BeOfType<Exception>()
            .Which.Message.Should().Be("Product with id '123' not found in database");
    }
}
