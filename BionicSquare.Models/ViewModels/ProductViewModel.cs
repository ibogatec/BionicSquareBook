using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BionicSquare.Models.ViewModels;

public class ProductViewModel
{
    public Product Product { get; init; } = new();
    
    [ValidateNever]
    public IEnumerable<SelectListItem> CategoryList { get; init; } = Array.Empty<SelectListItem>();
    
}