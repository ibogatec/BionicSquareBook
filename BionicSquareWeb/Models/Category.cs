using System.ComponentModel.DataAnnotations;

namespace BionicSquareWeb.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    [Range(100, 1000, ErrorMessage = "Display order must be between 100 and 1000")]
    public int DisplayOrder { get; set; }
}
