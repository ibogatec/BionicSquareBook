using BionicSquare.DataAccess;
using BionicSquare.Models;
using Microsoft.EntityFrameworkCore;

namespace BionicSquare.Business.Services;

public class ShoppingCartService : IShoppingCartService
{
    private readonly ApplicationDbContext _context;
    
    public ShoppingCartService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ShoppingCart?> GetCartByIdAsync(int cartId)
    {
        if (cartId == 0)
        {
            throw new ArgumentNullException(nameof(cartId));
        }
        return await _context.ShoppingCarts.Include(s => s.Product).FirstOrDefaultAsync(s => s.Id == cartId) ?? throw new Exception($"ShoppingCart with id '{cartId}' not found in database");
    }

    public async Task<IEnumerable<ShoppingCart>> GetUserCartItemsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException(nameof(userId));
        }
        return await _context.ShoppingCarts.Include(s => s.Product).Where(s => EF.Functions.Like(s.ApplicationUserId, userId)).ToArrayAsync() ?? throw new Exception($"ShoppingCart with id '{userId}' not found in database");
    }

    public async Task<int> GetCartCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException(nameof(userId));
        }
        return await _context.ShoppingCarts.Where(s => EF.Functions.Like(s.ApplicationUserId, userId)).SumAsync(s => s.Quantity);
    }

    public async Task<ShoppingCart> AddToCartAsync(ShoppingCart shoppingCart)
    {
        if (shoppingCart is null)
        {
            throw new ArgumentNullException(nameof(shoppingCart));
        }
        
        var existingCart = await _context.ShoppingCarts
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.ProductId == shoppingCart.ProductId && EF.Functions.Like(s.ApplicationUserId, shoppingCart.ApplicationUserId));
        if (existingCart is null)
        {
            existingCart = _context.ShoppingCarts.Add(shoppingCart).Entity;
        }
        else
        {
            existingCart.Quantity += shoppingCart.Quantity;
        }
        return await _context.SaveChangesAsync() > 0 ? existingCart : throw new DbUpdateException($"Failed to add product to cart");
    }

    public async Task<ShoppingCart> UpdateCartAsync(ShoppingCart shoppingCart)
    {
        if (shoppingCart is null)
        {
            throw new ArgumentNullException(nameof(shoppingCart));
        }
        
        var existingCart = await _context.ShoppingCarts.FindAsync(shoppingCart.Id);
        if (existingCart is not null && shoppingCart.Quantity <= 0)
        { 
            _context.ShoppingCarts.Remove(existingCart);
        }
        else if (existingCart is not null)
        {
            _context.Entry(existingCart).CurrentValues.SetValues(shoppingCart);
        }
        return await _context.SaveChangesAsync() > 0 ? shoppingCart : throw new DbUpdateException($"Failed to update shopping cart");
    }

    public async Task ClearCartAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException(nameof(userId));
        }
        await _context.ShoppingCarts.Where(s => EF.Functions.Like(s.ApplicationUserId, userId)).ExecuteDeleteAsync();
    }
}
