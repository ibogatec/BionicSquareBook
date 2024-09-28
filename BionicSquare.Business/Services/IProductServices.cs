using BionicSquare.Models;

namespace BionicSquare.Business.Services;

public interface IProductServices
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    
    Task<Product> CreateProductAsync(Product newProduct);

    Task<Product> GetProductByIdAsync(int? id);

    Task<Product> UpdateProductAsync(Product newProduct);
    
    Task<Product> DeleteProductByIdAsync(int? id);
    
}
