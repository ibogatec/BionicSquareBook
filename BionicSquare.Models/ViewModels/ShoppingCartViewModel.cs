namespace BionicSquare.Models.ViewModels;

public class ShoppingCartViewModel
{
    public IEnumerable<ShoppingCart> CartItems { get; init; } = Array.Empty<ShoppingCart>();

    public OrderHeader OrderHeader { get; init; } = new();

}