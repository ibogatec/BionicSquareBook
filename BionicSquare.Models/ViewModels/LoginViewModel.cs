using System.ComponentModel.DataAnnotations;

namespace BionicSquare.Models.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string Password { get; set; } = string.Empty;
    
    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
    
}
