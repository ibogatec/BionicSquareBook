using Microsoft.EntityFrameworkCore;
using BionicSquare.DataAccess;
using BionicSquare.Models;
using Microsoft.AspNetCore.Hosting;

namespace BionicSquare.Business.Services;

public class ProductServices : IProductServices
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    
    public ProductServices(
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }
    
    public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeCategory = false)
    {
        if (includeCategory)
        {
            return await _context.Products.Include(p => p.Category).ToListAsync();
        }
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
        if (product is null)
        {
            throw new Exception($"Product with id '{id}' not found in database");
        }
        if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            var fullImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl);
            if (File.Exists(fullImagePath))
            {
                File.Delete(fullImagePath);
            }
        }
        _context.Products.Remove(product);
        return await _context.SaveChangesAsync() > 0 ? product : throw new DbUpdateException($"Failed to delete product: '{product.Title}' and id: '{id}'");
    }
    
    private async Task ValidateProductAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
    }
    
}
