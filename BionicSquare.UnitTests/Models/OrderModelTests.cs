using System.ComponentModel.DataAnnotations;
using BionicSquare.Models;
using FluentAssertions;

namespace BionicSquare.UnitTests.Models;

public class OrderModelTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void OrderHeader_WithAllRequiredFields_ShouldPassValidation()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-1", Name = "Alice" };
        var orderDate = DateTime.UtcNow;
        var shippingDate = DateTime.UtcNow.AddDays(2);
        var header = new OrderHeader
        {
            Id = 1,
            OrderDate = orderDate,
            ShippingDate = shippingDate,
            OrderTotal = 150.0,
            Name = "Alice Smith",
            PhoneNumber = "555-9876",
            StreetAddress = "123 High St",
            City = "Metropolis",
            State = "NY",
            PostalCode = "10001",
            ApplicationUserId = "user-1",
            OrderStatus = "Approved",
            TrackingNumber = "TRACK123",
            Carrier = "FedEx",
            SessionId = "sess_123",
            PaymentIntentId = "pi_123",
            ApplicationUser = user
        };

        // Act
        var results = ValidateModel(header);

        // Assert
        results.Should().BeEmpty();
        header.OrderStatus.Should().Be("Approved");
        header.TrackingNumber.Should().Be("TRACK123");
        header.Carrier.Should().Be("FedEx");
        header.SessionId.Should().Be("sess_123");
        header.PaymentIntentId.Should().Be("pi_123");
        header.ApplicationUser.Should().Be(user);
    }

    [Theory]
    [InlineData(nameof(OrderHeader.PhoneNumber))]
    [InlineData(nameof(OrderHeader.StreetAddress))]
    [InlineData(nameof(OrderHeader.City))]
    [InlineData(nameof(OrderHeader.State))]
    [InlineData(nameof(OrderHeader.PostalCode))]
    [InlineData(nameof(OrderHeader.Name))]
    [InlineData(nameof(OrderHeader.ApplicationUserId))]
    public void OrderHeader_WithMissingRequiredField_ShouldFailValidation(string fieldName)
    {
        // Arrange
        var header = new OrderHeader
        {
            Name = fieldName == nameof(OrderHeader.Name) ? "" : "Alice",
            PhoneNumber = fieldName == nameof(OrderHeader.PhoneNumber) ? "" : "555-0000",
            StreetAddress = fieldName == nameof(OrderHeader.StreetAddress) ? "" : "Main St",
            City = fieldName == nameof(OrderHeader.City) ? "" : "City",
            State = fieldName == nameof(OrderHeader.State) ? "" : "State",
            PostalCode = fieldName == nameof(OrderHeader.PostalCode) ? "" : "12345",
            ApplicationUserId = fieldName == nameof(OrderHeader.ApplicationUserId) ? "" : "user-1"
        };

        // Act
        var results = ValidateModel(header);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(fieldName));
    }

    [Fact]
    public void OrderDetails_CanBeInstantiatedWithProperties()
    {
        // Arrange
        var header = new OrderHeader { Id = 10 };
        var product = new Product { Id = 20, Title = "Book" };
        var details = new OrderDetails
        {
            Id = 1,
            OrderHeaderId = 10,
            OrderHeader = header,
            ProductId = 20,
            Product = product,
            Quantity = 3,
            Price = 90.0
        };

        // Assert
        details.Id.Should().Be(1);
        details.OrderHeaderId.Should().Be(10);
        details.OrderHeader.Should().Be(header);
        details.ProductId.Should().Be(20);
        details.Product.Should().Be(product);
        details.Quantity.Should().Be(3);
        details.Price.Should().Be(90.0);
    }
}
