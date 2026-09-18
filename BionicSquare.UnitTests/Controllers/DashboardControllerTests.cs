using BionicSquare.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace BionicSquare.UnitTests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public void IndexGet_ShouldReturnViewResult()
    {
        // Arrange
        var controller = new DashboardController();

        // Act
        var result = controller.IndexGet();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }
}
