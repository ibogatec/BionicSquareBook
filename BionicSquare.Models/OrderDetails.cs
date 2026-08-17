using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BionicSquare.Models;

public class OrderDetails
{
    [Key]
    public int Id { get; init; }
    
    [Display(Name = "Total Price")]
    public double Price { get; init; }
    
    [Display(Name = "Order Header")]
    public int OrderHeaderId { get; init; }
    
    [ValidateNever]
    [ForeignKey(nameof(OrderHeaderId))]
    public OrderHeader? OrderHeader { get; init; }
    
    [Display(Name = "Product")]
    public int ProductId { get; init; }
    
    [ValidateNever]
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; init; }
    
    [Display(Name = "Quantity")]
    public int Quantity { get; init; }
    
}
