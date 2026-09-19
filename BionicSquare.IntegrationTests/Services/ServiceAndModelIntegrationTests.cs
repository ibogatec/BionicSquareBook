using BionicSquare.Business.Services;
using BionicSquare.DataAccess;
using BionicSquare.IntegrationTests.Infrastructure;
using BionicSquare.Models;
using BionicSquare.Models.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace BionicSquare.IntegrationTests.Services;

[Collection("IntegrationTests")]
public class ServiceAndModelIntegrationTests : IntegrationTestBase
{
    public ServiceAndModelIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    private async Task<ApplicationUser> CreateTestUserAsync()
    {
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"service_user_{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Service User"
        };
        var result = await userManager.CreateAsync(user, "Password123!");
        result.Succeeded.Should().BeTrue();
        return user;
    }

    [Fact]
    public async Task ShoppingCartService_CountAndClearCart_PersistsChangesInDatabase()
    {
        using var scope = CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<IShoppingCartService>();

        var user = await CreateTestUserAsync();

        // Add 2 items
        await cartService.AddToCartAsync(new ShoppingCart { ApplicationUserId = user.Id, ProductId = 1, Quantity = 3 });
        await cartService.AddToCartAsync(new ShoppingCart { ApplicationUserId = user.Id, ProductId = 2, Quantity = 4 });

        // Verify count in SQL Server
        var count = await cartService.GetCartCountAsync(user.Id);
        count.Should().Be(7);

        // Clear cart
        await cartService.ClearCartAsync(user.Id);

        // Verify count is 0
        var countAfterClear = await cartService.GetCartCountAsync(user.Id);
        countAfterClear.Should().Be(0);
    }

    [Fact]
    public async Task ShoppingCartService_InvalidArguments_ThrowsArgumentNullException()
    {
        using var scope = CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<IShoppingCartService>();

        var act1 = async () => await cartService.GetCartByIdAsync(0);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        var act2 = async () => await cartService.GetUserCartItemsAsync("");
        await act2.Should().ThrowAsync<ArgumentNullException>();

        var act3 = async () => await cartService.GetCartCountAsync("");
        await act3.Should().ThrowAsync<ArgumentNullException>();

        var act4 = async () => await cartService.ClearCartAsync("");
        await act4.Should().ThrowAsync<ArgumentNullException>();

        var act5 = async () => await cartService.AddToCartAsync(null!);
        await act5.Should().ThrowAsync<ArgumentNullException>();

        var act6 = async () => await cartService.UpdateCartAsync(null!);
        await act6.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ShoppingCartService_UpdateCartAsync_WhenCartNotFound_ThrowsDbUpdateException()
    {
        using var scope = CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<IShoppingCartService>();

        // Non-existent cart ID 999999 triggers: existingCart is null -> SaveChanges returns 0 -> DbUpdateException
        var nonExistentCart = new ShoppingCart
        {
            Id = 999999,
            ApplicationUserId = "user",
            ProductId = 1,
            Quantity = 5
        };

        var act = async () => await cartService.UpdateCartAsync(nonExistentCart);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ProductServices_DirectEdgeCases_BehaveExpectedly()
    {
        using var scope = CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductServices>();

        // GetAll without category
        var products = await productService.GetAllProductsAsync(includeCategory: false);
        products.Should().NotBeEmpty();

        // GetProductById with null/0
        var actZero = async () => await productService.GetProductByIdAsync(0);
        await actZero.Should().ThrowAsync<ArgumentNullException>();

        var actNull = async () => await productService.GetProductByIdAsync(null);
        await actNull.Should().ThrowAsync<ArgumentNullException>();

        // GetProductById non-existent
        var actNotFound1 = async () => await productService.GetProductByIdAsync(999999, includeCategory: false);
        await actNotFound1.Should().ThrowAsync<Exception>();

        var actNotFound2 = async () => await productService.GetProductByIdAsync(999999, includeCategory: true);
        await actNotFound2.Should().ThrowAsync<Exception>();

        // Null product validation
        var actNullProduct = async () => await productService.CreateProductAsync(null!);
        await actNullProduct.Should().ThrowAsync<ArgumentNullException>();

        var actNullUpdate = async () => await productService.UpdateProductAsync(null!);
        await actNullUpdate.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProductServices_DeleteProductByIdAsync_WithExistingImageOnDisk_DeletesFileAndRecord()
    {
        using var scope = CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductServices>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var relativeImagePath = Path.Combine("images", "uploads", "products", $"delete_me_{Guid.NewGuid():N}.jpg");
        var absoluteImagePath = Path.Combine(env.WebRootPath, relativeImagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteImagePath)!);
        await File.WriteAllTextAsync(absoluteImagePath, "dummy image binary");

        var product = new Product
        {
            Title = "Product To Delete Directly",
            Description = "Desc",
            Isbn = "978-0000111122",
            Author = "Author",
            ListPrice = 20,
            Price = 18,
            Price50 = 16,
            Price100 = 14,
            CategoryId = category.Id,
            ImageUrl = relativeImagePath
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        });

        // Act
        var deleted = await productService.DeleteProductByIdAsync(product.Id);

        // Assert
        deleted.Should().NotBeNull();
        deleted.Id.Should().Be(product.Id);
        File.Exists(absoluteImagePath).Should().BeFalse();

        var inDb = await ExecuteDbContextAsync(db => db.Products.FirstOrDefaultAsync(p => p.Id == product.Id));
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task ProductServices_DeleteProductByIdAsync_WhenImageFileDoesNotExistOnDisk_RemovesProductWithoutDeletingFile()
    {
        using var scope = CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductServices>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var relativeImagePath = Path.Combine("images", "uploads", "products", $"non_existent_{Guid.NewGuid():N}.jpg");
        var absoluteImagePath = Path.Combine(env.WebRootPath, relativeImagePath);
        if (File.Exists(absoluteImagePath))
        {
            File.Delete(absoluteImagePath);
        }

        var product = new Product
        {
            Title = "Product With Missing Image File",
            Description = "Desc",
            Isbn = "978-0000111199",
            Author = "Author",
            ListPrice = 20,
            Price = 18,
            Price50 = 16,
            Price100 = 14,
            CategoryId = category.Id,
            ImageUrl = relativeImagePath
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        });

        // Act: Delete product when ImageUrl is present but File.Exists is false
        var deleted = await productService.DeleteProductByIdAsync(product.Id);

        // Assert
        deleted.Should().NotBeNull();
        deleted.Id.Should().Be(product.Id);

        var inDb = await ExecuteDbContextAsync(db => db.Products.FirstOrDefaultAsync(p => p.Id == product.Id));
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task ProductServices_CreateProductAsync_WhenSaveChangesReturnsZero_ThrowsDbUpdateException()
    {
        using var scope = CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(Factory.ConnectionString)
            .AddInterceptors(new ZeroSaveChangesInterceptor())
            .Options;
        await using var zeroContext = new ApplicationDbContext(options);
        var productService = new ProductServices(zeroContext, env);

        var category = await ExecuteDbContextAsync(db => db.Categories.FirstAsync());
        var product = new Product
        {
            Title = "Zero Save Product",
            Description = "Desc",
            Isbn = "978-0000999901",
            Author = "Author",
            ListPrice = 20,
            Price = 18,
            Price50 = 16,
            Price100 = 14,
            CategoryId = category.Id
        };

        var act = async () => await productService.CreateProductAsync(product);
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to create product*");
    }

    [Fact]
    public async Task ProductServices_UpdateProductAsync_WhenSaveChangesReturnsZero_ThrowsDbUpdateException()
    {
        using var scope = CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(Factory.ConnectionString)
            .AddInterceptors(new ZeroSaveChangesInterceptor())
            .Options;
        await using var zeroContext = new ApplicationDbContext(options);
        var productService = new ProductServices(zeroContext, env);

        var existingProduct = await ExecuteDbContextAsync(db => db.Products.FirstAsync());

        var act = async () => await productService.UpdateProductAsync(existingProduct);
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to update product*");
    }

    [Fact]
    public async Task ProductServices_DeleteProductByIdAsync_WhenSaveChangesReturnsZero_ThrowsDbUpdateException()
    {
        using var scope = CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(Factory.ConnectionString)
            .AddInterceptors(new ZeroSaveChangesInterceptor())
            .Options;
        await using var zeroContext = new ApplicationDbContext(options);
        var productService = new ProductServices(zeroContext, env);

        var existingProduct = await ExecuteDbContextAsync(db => db.Products.FirstAsync());

        var act = async () => await productService.DeleteProductByIdAsync(existingProduct.Id);
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to delete product*");
    }

    [Fact]
    public async Task ProductServices_DeleteProductByIdAsync_InvalidOrNotFound_ThrowsException()
    {
        using var scope = CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductServices>();

        var actZero = async () => await productService.DeleteProductByIdAsync(0);
        await actZero.Should().ThrowAsync<ArgumentNullException>();

        var actNull = async () => await productService.DeleteProductByIdAsync(null);
        await actNull.Should().ThrowAsync<ArgumentNullException>();

        var actNotFound = async () => await productService.DeleteProductByIdAsync(999999);
        await actNotFound.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CategoryServices_DirectEdgeCases_BehaveExpectedly()
    {
        using var scope = CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryServices>();

        var actZero = async () => await categoryService.GetCategoryByIdAsync(0);
        await actZero.Should().ThrowAsync<ArgumentNullException>();

        var actNull = async () => await categoryService.GetCategoryByIdAsync(null);
        await actNull.Should().ThrowAsync<ArgumentNullException>();

        var actNotFound = async () => await categoryService.GetCategoryByIdAsync(999999);
        await actNotFound.Should().ThrowAsync<Exception>();

        var actNullCreate = async () => await categoryService.CreateCategoryAsync(null!);
        await actNullCreate.Should().ThrowAsync<ArgumentNullException>();

        var actNullUpdate = async () => await categoryService.UpdateCategoryAsync(null!);
        await actNullUpdate.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task OrderHeaderAndOrderDetails_DatabasePersistence_IntegratesCleanly()
    {
        var user = await CreateTestUserAsync();

        var orderHeader = new OrderHeader
        {
            ApplicationUserId = user.Id,
            OrderDate = DateTime.UtcNow,
            ShippingDate = DateTime.UtcNow.AddDays(2),
            OrderTotal = 150.0,
            OrderStatus = "Approved",
            TrackingNumber = "TRACK12345",
            Carrier = "UPS",
            SessionId = "sess_123",
            PaymentIntentId = "pi_123",
            PhoneNumber = "555-987-6543",
            StreetAddress = "456 Test Blvd",
            City = "Metropolis",
            State = "NY",
            PostalCode = "10001",
            Name = "Order Placer"
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.OrderHeaders.Add(orderHeader);
            await db.SaveChangesAsync();
        });

        var orderDetail = new OrderDetails
        {
            OrderHeaderId = orderHeader.Id,
            ProductId = 1,
            Quantity = 2,
            Price = 75.0
        };

        await ExecuteDbContextAsync(async db =>
        {
            db.OrderDetails.Add(orderDetail);
            await db.SaveChangesAsync();
        });

        var retrieved = await ExecuteDbContextAsync(async db =>
            await db.OrderDetails.Include(d => d.OrderHeader).Include(d => d.Product).FirstOrDefaultAsync(d => d.Id == orderDetail.Id));

        retrieved.Should().NotBeNull();
        retrieved.Price.Should().Be(75.0);
        retrieved.Quantity.Should().Be(2);
        retrieved.ProductId.Should().Be(1);
        retrieved.Product.Should().NotBeNull();
        retrieved.OrderHeader.Should().NotBeNull();
        retrieved.OrderHeader.OrderTotal.Should().Be(150.0);
        retrieved.OrderHeader.TrackingNumber.Should().Be("TRACK12345");
        retrieved.OrderHeader.Carrier.Should().Be("UPS");
    }

    [Fact]
    public void ViewModels_PropertiesAndBehaviors_Covered()
    {
        var errorModel = new ErrorViewModel { RequestId = "req-123" };
        errorModel.ShowRequestId.Should().BeTrue();
        errorModel.RequestId = null;
        errorModel.ShowRequestId.Should().BeFalse();

        var prodVm = new ProductViewModel
        {
            Action = "Create Product",
            Product = new Product { Title = "Sample" },
            CategoryList = []
        };
        prodVm.Action.Should().Be("Create Product");
        prodVm.Product.Title.Should().Be("Sample");
        prodVm.CategoryList.Should().BeEmpty();
    }

    private class ZeroSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
        }
    }
}
