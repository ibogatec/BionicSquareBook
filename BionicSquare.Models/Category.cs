using System.ComponentModel.DataAnnotations;

namespace BionicSquare.Models;

public class Category
{
    public int Id { get; init; }
    
    [Required]
    [MinLength(2)]
    public string Name { get; init; } = string.Empty;

    [Range(100, 1000, ErrorMessage = "Display order must be between 100 and 1000")]
    public int DisplayOrder { get; init; }
}
