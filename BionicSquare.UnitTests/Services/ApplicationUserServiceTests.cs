using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.UnitTests.Helpers;
using FluentAssertions;

namespace BionicSquare.UnitTests.Services;

public class ApplicationUserServiceTests
{
    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ApplicationUserService(context);

        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = "ivan@example.com",
            Email = "ivan@example.com",
            Name = "Ivan Developer",
            StreetAddress = "Main St 123",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701"
        };
        await context.ApplicationUsers.AddAsync(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserByIdAsync("user-123");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("user-123");
        result.Name.Should().Be("Ivan Developer");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ApplicationUserService(context);

        // Act
        var act = async () => await service.GetUserByIdAsync("non-existent-user");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*not found in database*");
    }
}
