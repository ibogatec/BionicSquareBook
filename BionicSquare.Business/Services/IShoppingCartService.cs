using BionicSquare.Models;

namespace BionicSquare.Business.Services;

public interface IShoppingCartService
{
    Task<ShoppingCart?> GetCartByIdAsync(int cartId);
    
    Task<IEnumerable<ShoppingCart>> GetUserCartItemsAsync(string userId);
    
    Task<int> GetCartCountAsync(string userId);
    
    Task<ShoppingCart> AddToCartAsync(ShoppingCart shoppingCart);
    
    Task<ShoppingCart> UpdateCartAsync(ShoppingCart shoppingCart);
    
    Task ClearCartAsync(string userId);
    
}
