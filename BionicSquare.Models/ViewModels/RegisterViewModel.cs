using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BionicSquare.Models.ViewModels;

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^\+?[0-9\s\-]{7,15}$", ErrorMessage = "Invalid phone number format")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
    
    [Display(Name = "Street Address")]
    public string? StreetAddress { get; set; }
    
    [Display(Name = "City")]
    public string? City { get; set; }
    
    [Display(Name = "State")]
    public string? State { get; set; }
    
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }
    
    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [ValidateNever]
    public IEnumerable<SelectListItem> RoleList { get; init; } = Array.Empty<SelectListItem>();
    
}
