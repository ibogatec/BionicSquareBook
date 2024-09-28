using Microsoft.EntityFrameworkCore;
using BionicSquare.DataAccess;
using BionicSquare.Models;

namespace BionicSquare.Business.Services;

public class ProductServices : IProductServices
{
    private readonly ApplicationDbContext _context;
    
    public ProductServices(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }
    
    public async Task<Product> CreateProductAsync(Product newProduct)
    {
        await ValidateProductAsync(newProduct);
        _context.Products.Add(newProduct);
        return await _context.SaveChangesAsync() > 0 ? newProduct : throw new DbUpdateException($"Failed to create product: '{newProduct.Title}' and id: '{newProduct.Id}'");
    }

    public async Task<Product> GetProductByIdAsync(int? id)
    {
        if (id is null or 0)
        {
            throw new ArgumentNullException(nameof(id));
        }
        return await _context.Products.FindAsync(id) ?? throw new Exception($"Product with id '{id}' not found in database");
    }
    
    public async Task<Product> UpdateProductAsync(Product newProduct)
    {
        await ValidateProductAsync(newProduct);
        _context.Products.Update(newProduct);
        return await _context.SaveChangesAsync() > 0 ? newProduct : throw new DbUpdateException($"Failed to update product: '{newProduct.Title}' and id: '{newProduct.Id}'");
    }

    public async Task<Product> DeleteProductByIdAsync(int? id)
    {
        var product = await GetProductByIdAsync(id);
        _context.Products.Remove(product);
        return await _context.SaveChangesAsync() > 0 ? product : throw new DbUpdateException($"Failed to delete product: '{product.Title}' and id: '{id}'");
    }
    
    private async Task ValidateProductAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
    }
    
}
