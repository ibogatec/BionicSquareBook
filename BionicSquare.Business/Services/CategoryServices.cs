using Microsoft.EntityFrameworkCore;
using BionicSquare.DataAccess;
using BionicSquare.Models;

namespace BionicSquare.Business.Services;

public class CategoryServices : ICategoryServices
{
    private readonly ApplicationDbContext _context;
    
    public CategoryServices(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories.ToListAsync();
    }
    
    public async Task<Category> CreateCategoryAsync(Category newCategory)
    {
        ArgumentNullException.ThrowIfNull(newCategory);
        await ValidateCategoryAsync(newCategory);
        _context.Categories.Add(newCategory);
        return await _context.SaveChangesAsync() > 0 ? newCategory : throw new DbUpdateException($"Failed to create category with name: '{newCategory.Name}' and id: '{newCategory.Id}'");
    }

    public async Task<Category> GetCategoryByIdAsync(int? id)
    {
        if (id is null or 0)
        {
            throw new ArgumentNullException(nameof(id));
        }
        return await _context.Categories.FindAsync(id) ?? throw new Exception($"Category with id '{id}' not found in database");
    }
    
    public async Task<Category> UpdateCategoryAsync(Category newCategory)
    {
        ArgumentNullException.ThrowIfNull(newCategory);
        await ValidateCategoryAsync(newCategory);
        _context.Categories.Update(newCategory);
        return await _context.SaveChangesAsync() > 0 ? newCategory : throw new DbUpdateException($"Failed to update category with name: '{newCategory.Name}' and id: '{newCategory.Id}'");
    }

    public async Task<Category> DeleteCategoryByIdAsync(int? id)
    {
        var category = await GetCategoryByIdAsync(id);
        _context.Categories.Remove(category);
        return await _context.SaveChangesAsync() > 0 ? category : throw new DbUpdateException($"Failed to delete category with name: '{category.Name}' and id: '{id}'");
    }
    
    private async Task ValidateCategoryAsync(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        bool sameNameCategoryExist = await _context.Categories.AnyAsync(c => c.Id != category.Id && c.Name.ToLower() == category.Name.ToLower());
        if (sameNameCategoryExist)
        {
            throw new DbUpdateException($"Category with name '{category.Name}' already exists");
        }
    }
    
}
