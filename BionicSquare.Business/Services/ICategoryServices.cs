using BionicSquare.Models;

namespace BionicSquare.Business.Services;

public interface ICategoryServices
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    
    Task<Category> CreateCategoryAsync(Category newCategory);

    Task<Category> GetCategoryByIdAsync(int? id);

    Task<Category> UpdateCategoryAsync(Category newCategory);
    
    Task<Category> DeleteCategoryByIdAsync(int? id);
    
}
