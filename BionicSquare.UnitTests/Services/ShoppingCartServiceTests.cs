using System.Reflection;
using System.Runtime.CompilerServices;
using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BionicSquare.UnitTests.Services;

public class ShoppingCartServiceTests
{
    [Fact]
    public async Task GetCartByIdAsync_WithValidId_ShouldReturnCartWithProduct()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);
        var product = await context.Products.FirstAsync();
        var cart = await service.AddToCartAsync(new ShoppingCart
        {
            ApplicationUserId = "user-get-cart",
            ProductId = product.Id,
            Quantity = 3
        });

        // Act
        var result = await service.GetCartByIdAsync(cart.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(cart.Id);
        result.Product.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCartByIdAsync_WithZeroId_ShouldThrowArgumentNullException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        // Act
        var act = async () => await service.GetCartByIdAsync(0);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCartByIdAsync_WithNonExistentId_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        // Act
        var act = async () => await service.GetCartByIdAsync(99999);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*not found in database*");
    }

    [Fact]
    public async Task AddToCartAsync_WhenNewItem_ShouldCreateCartEntry()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);
        var product = await context.Products.FirstAsync();

        var cart = new ShoppingCart
        {
            ApplicationUserId = "user-test-1",
            ProductId = product.Id,
            Quantity = 2
        };

        // Act
        var result = await service.AddToCartAsync(cart);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddToCartAsync_WhenItemAlreadyInCart_ShouldIncrementQuantity()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);
        var product = await context.Products.FirstAsync();

        var cart1 = new ShoppingCart
        {
            ApplicationUserId = "user-test-2",
            ProductId = product.Id,
            Quantity = 2
        };
        await service.AddToCartAsync(cart1);

        var cart2 = new ShoppingCart
        {
            ApplicationUserId = "user-test-2",
            ProductId = product.Id,
            Quantity = 3
        };

        // Act
        var result = await service.AddToCartAsync(cart2);

        // Assert
        result.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task AddToCartAsync_WhenCartIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        // Act
        var act = async () => await service.AddToCartAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddToCartAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new ShoppingCartService(context);
        var product = await context.Products.FirstAsync();

        var cart = new ShoppingCart
        {
            ApplicationUserId = "user-zero-save",
            ProductId = product.Id,
            Quantity = 2
        };

        // Act
        var act = async () => await service.AddToCartAsync(cart);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to add product to cart*");
    }

    [Fact]
    public async Task GetUserCartItemsAsync_ShouldReturnAllUserItems()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);
        var products = await context.Products.Take(2).ToListAsync();

        await service.AddToCartAsync(new ShoppingCart { ApplicationUserId = "user-list", ProductId = products[0].Id, Quantity = 1 });
        await service.AddToCartAsync(new ShoppingCart { ApplicationUserId = "user-list", ProductId = products[1].Id, Quantity = 2 });

        // Act
        var items = (await service.GetUserCartItemsAsync("user-list")).ToList();

        // Assert
        items.Should().HaveCount(2);
        items.Sum(i => i.Quantity).Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetUserCartItemsAsync_WithNullOrEmptyUserId_ShouldThrowArgumentNullException(string? userId)
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        // Act
        var act = async () => await service.GetUserCartItemsAsync(userId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GetUserCartItemsAsync_WhenQueryYieldsNull_ShouldThrowShoppingCartNotFoundException()
    {
        // Arrange
        var nestedType = typeof(ShoppingCartService).GetNestedType("<GetUserCartItemsAsync>d__3", BindingFlags.NonPublic | BindingFlags.Public);
        nestedType.Should().NotBeNull();

        object stateMachine = Activator.CreateInstance(nestedType!)!;

        var stateField = nestedType!.GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var builderField = nestedType.GetField("<>t__builder", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var thisField = nestedType.GetField("<>4__this", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var userIdField = nestedType.GetField("userId", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var displayClassField = nestedType.GetField("<>8__1", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var awaiterField = nestedType.GetField("<>u__1", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        var service = new ShoppingCartService(TestDbContextFactory.Create());
        thisField?.SetValue(stateMachine, service);
        userIdField?.SetValue(stateMachine, "test-user-123");

        if (displayClassField != null)
        {
            var displayClass = Activator.CreateInstance(displayClassField.FieldType);
            var displayUserIdField = displayClassField.FieldType.GetField("userId", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            displayUserIdField?.SetValue(displayClass, "test-user-123");
            displayClassField.SetValue(stateMachine, displayClass);
        }

        stateField?.SetValue(stateMachine, 0); // state 0 is resumption after ToArrayAsync
        awaiterField?.SetValue(stateMachine, Task.FromResult<ShoppingCart[]>(null!).GetAwaiter());

        // Act
        var moveNextMethod = nestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        moveNextMethod.Should().NotBeNull();
        moveNextMethod!.Invoke(stateMachine, null);

        // Assert
        var builder = builderField!.GetValue(stateMachine);
        var taskProperty = builder!.GetType().GetProperty("Task", BindingFlags.Public | BindingFlags.Instance);
        var task = (Task<IEnumerable<ShoppingCart>>)taskProperty!.GetValue(builder)!;

        task.IsFaulted.Should().BeTrue();
        task.Exception!.InnerException.Should().BeOfType<Exception>()
            .Which.Message.Should().Be("ShoppingCart with id 'test-user-123' not found in database");
    }

    [Fact]
    public async Task GetCartCountAsync_ShouldReturnTotalItemCount()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);
        var products = await context.Products.Take(2).ToListAsync();

        await service.AddToCartAsync(new ShoppingCart { ApplicationUserId = "user-count", ProductId = products[0].Id, Quantity = 4 });
        await service.AddToCartAsync(new ShoppingCart { ApplicationUserId = "user-count", ProductId = products[1].Id, Quantity = 6 });

        // Act
        var count = await service.GetCartCountAsync("user-count");

        // Assert
        count.Should().Be(10);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetCartCountAsync_WithNullOrEmptyUserId_ShouldThrowArgumentNullException(string? userId)
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        // Act
        var act = async () => await service.GetCartCountAsync(userId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateCartAsync_WhenPositiveQuantity_ShouldUpdateExistingCart()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);
        var product = await context.Products.FirstAsync();

        var cart = await service.AddToCartAsync(new ShoppingCart
        {
            ApplicationUserId = "user-update",
            ProductId = product.Id,
            Quantity = 2
        });

        cart.Quantity = 5;

        // Act
        var result = await service.UpdateCartAsync(cart);

        // Assert
        result.Quantity.Should().Be(5);
        var updated = await service.GetCartByIdAsync(cart.Id);
        updated!.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCartAsync_WhenQuantityIsZeroOrNegative_ShouldRemoveCartItem()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);
        var product = await context.Products.FirstAsync();

        var cart = await service.AddToCartAsync(new ShoppingCart
        {
            ApplicationUserId = "user-remove",
            ProductId = product.Id,
            Quantity = 2
        });

        cart.Quantity = 0;

        // Act
        var result = await service.UpdateCartAsync(cart);

        // Assert
        var remaining = await context.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cart.Id);
        remaining.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCartAsync_WhenCartDoesNotExistInDb_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        var nonExistentCart = new ShoppingCart
        {
            Id = 99999,
            ApplicationUserId = "user-unknown",
            ProductId = 1,
            Quantity = 2
        };

        // Act
        var act = async () => await service.UpdateCartAsync(nonExistentCart);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to update shopping cart*");
    }

    [Fact]
    public async Task UpdateCartAsync_WhenSaveChangesReturnsZero_ShouldThrowDbUpdateException()
    {
        // Arrange
        await using var context = TestDbContextFactory.CreateWithZeroSave();
        var service = new ShoppingCartService(context);
        var product = await context.Products.FirstAsync();

        var cart = new ShoppingCart
        {
            Id = 100,
            ApplicationUserId = "user-update-zero",
            ProductId = product.Id,
            Quantity = 3
        };
        await context.ShoppingCarts.AddAsync(cart);

        var updatedCart = new ShoppingCart
        {
            Id = 100,
            ApplicationUserId = "user-update-zero",
            ProductId = product.Id,
            Quantity = 5
        };

        // Act
        var act = async () => await service.UpdateCartAsync(updatedCart);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Failed to update shopping cart*");
    }

    [Fact]
    public async Task UpdateCartAsync_WhenCartIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        // Act
        var act = async () => await service.UpdateCartAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ClearCartAsync_WithValidUserId_ShouldCallExecuteDelete()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateSqlite();
        await using (connection)
        await using (context)
        {
            var user = new ApplicationUser { Id = "user-clear-test", UserName = "test@user.com", Email = "test@user.com" };
            await context.Users.AddAsync(user);
            var service = new ShoppingCartService(context);
            var product = await context.Products.FirstAsync();
            await context.ShoppingCarts.AddAsync(new ShoppingCart
            {
                ApplicationUserId = "user-clear-test",
                ProductId = product.Id,
                Quantity = 2
            });
            await context.SaveChangesAsync();

            // Act
            await service.ClearCartAsync("user-clear-test");

            // Assert
            var remaining = await context.ShoppingCarts.Where(s => s.ApplicationUserId == "user-clear-test").ToListAsync();
            remaining.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ClearCartAsync_WithNullOrEmptyUserId_ShouldThrowArgumentNullException(string? userId)
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ShoppingCartService(context);

        // Act
        var act = async () => await service.ClearCartAsync(userId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
