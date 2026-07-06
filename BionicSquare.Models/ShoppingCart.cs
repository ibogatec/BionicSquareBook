using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BionicSquare.Models;

public class ShoppingCart
{
    [Key]
    public int Id { get; init; }
    
    [Required]
    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    [Display(Name = "Products Quantity")]
    public int Quantity { get; set; }
    
    [Required]
    [Display(Name = "User")]
    public string ApplicationUserId { get; init; } = string.Empty;
    
    [ForeignKey(nameof(ApplicationUserId))]
    [ValidateNever]
    public ApplicationUser? ApplicationUser { get; init; }
    
    [Display(Name = "Product")]
    public int ProductId { get; init; }
    
    [ForeignKey(nameof(ProductId))]
    [ValidateNever]
    public Product? Product { get; init; }

    [NotMapped]
    public double Price
    {
        get
        {
            if (Product is null)
            {
                return 0.0;
            }

            return Quantity switch
            {
                <= 50 => Product.Price,
                <= 100 => Product.Price50,
                _ => Product.Price100
            };
        }
    }
    
}
