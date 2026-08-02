using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BionicSquare.Models;

public class OrderHeader
{
    [Key]
    public int Id { get; init; }
    
    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; init; }
    
    [Display(Name = "Shipping Date")]
    public DateTime ShippingDate { get; init; }
    
    [Display(Name = "Order Total")]
    public double OrderTotal { get; init; }
    
    [Display(Name = "Order Status")]
    public string? OrderStatus { get; init; }
    
    [Display(Name = "Tracking Number")]
    public string? TrackingNumber { get; init; }
    
    [Display(Name = "Carrier")]
    public string? Carrier { get; init; }
    
    [Display(Name = "Session ID")]
    public string? SessionId { get; init; }
    
    [Display(Name = "Payment Intent ID")]
    public string? PaymentIntentId { get; init; }
    
    [Required]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; init; } = string.Empty;
    
    [Required]
    [Display(Name = "Street Address")]
    public string StreetAddress { get; init; } = string.Empty;

    [Required]
    [Display(Name = "City")]
    public string City { get; init; } = string.Empty;

    [Required]
    [Display(Name = "State")]
    public string State { get; init; } = string.Empty;

    [Required]
    [Display(Name = "Postal Code")]
    public string PostalCode { get; init; } = string.Empty;

    [Required]
    [Display(Name = "Name")]
    public string Name { get; init; } = string.Empty;
    
    [Required]
    [Display(Name = "User")]
    public string ApplicationUserId { get; init; } = string.Empty;
    
    [ForeignKey(nameof(ApplicationUserId))]
    [ValidateNever]
    public ApplicationUser? ApplicationUser { get; init; }
    
}
