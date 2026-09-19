using System.Net;
using BionicSquare.IntegrationTests.Infrastructure;
using BionicSquare.Utility;
using FluentAssertions;

namespace BionicSquare.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class DashboardControllerIntegrationTests : IntegrationTestBase
{
    public DashboardControllerIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Index_UnauthenticatedUser_RedirectsToLogin()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Admin/Dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/Login");
    }

    [Fact]
    public async Task Index_CustomerRole_RedirectsToAccessDenied()
    {
        // Act
        var response = await Client.WithUser("customer-user").GetAsync("/Admin/Dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Identity/Account/AccessDenied");
    }

    [Fact]
    public async Task Index_EmployeeRole_ReturnsSuccessAndRendersView()
    {
        // Act
        var response = await Client.WithUser("emp-user", Role.Employee).GetAsync("/Admin/Dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Index_AdminRole_ReturnsSuccessAndRendersView()
    {
        // Act
        var response = await Client.WithAdmin().GetAsync("/Admin/Dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }
}
