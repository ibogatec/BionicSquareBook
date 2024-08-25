using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BionicSquare.Models;

public class Product
{
    [Key]
    public int Id { get; init; }
    
    [Required]
    public string Title { get; init; } = string.Empty;
    
    public string Description { get; init; } = string.Empty;
    
    [Required]
    public string Isbn { get; init; } = string.Empty;
    
    [Required]
    public string Author { get; init; } = string.Empty;
    
    [Required]
    [Range(1, 1000, ErrorMessage = "Price must be between 1 and 1000")]
    [Display(Name = "List Price")]
    public double ListPrice { get; init; }
    
    [Required]
    [Range(1, 1000, ErrorMessage = "Price must be between 1 and 1000")]
    [Display(Name = "Price for 1-50")]
    public double Price { get; init; }
    
    [Required]
    [Range(1, 1000, ErrorMessage = "Price must be between 1 and 1000")]
    [Display(Name = "Price for 50+")]
    public double Price50 { get; init; }
    
    [Required]
    [Range(1, 1000, ErrorMessage = "Price must be between 1 and 1000")]
    [Display(Name = "Price for 100+")]
    public double Price100 { get; init; }
    
    [ValidateNever]
    [Display(Name = "Product Image")]
    public string? ImageUrl { get; init; }
    
    public int CategoryId { get; init; }
    
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; init; }
}
