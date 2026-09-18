using BionicSquare.Models;
using FluentAssertions;

namespace BionicSquare.UnitTests.Models;

public class ErrorViewModelTests
{
    [Theory]
    [InlineData("req-12345", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ShowRequestId_ShouldReturnTrueOnlyWhenRequestIdIsNotEmpty(string? requestId, bool expected)
    {
        // Arrange
        var model = new ErrorViewModel
        {
            RequestId = requestId
        };

        // Act & Assert
        model.ShowRequestId.Should().Be(expected);
    }
}
