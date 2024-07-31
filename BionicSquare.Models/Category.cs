using System.ComponentModel.DataAnnotations;

namespace BionicSquare.Models;

public class Category
{
    public int Id { get; init; }
    
    [Required]
    [MinLength(2)]
    [Display(Name = "Category Name")]
    public string Name { get; init; } = string.Empty;

    [Range(100, 1000, ErrorMessage = "Display order must be between 100 and 1000")]
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; init; }
}
