using Microsoft.AspNetCore.Mvc;
using BionicSquare.Business.Services;
using BionicSquare.Models;

namespace BionicSquare.Web.Controllers;

[Area("Admin")]
public class CategoryController : Controller
{
    private readonly ICategoryServices _categoryServices;
    
    public CategoryController(ICategoryServices categoryServices)
    {
        _categoryServices = categoryServices;
    }
    
    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexGetAsync()
    {
        IEnumerable<Category> categories = new List<Category>();
        try
        {
            categories = await _categoryServices.GetAllCategoriesAsync();
        }
        catch
        {
            // ignored
        }
        return View(categories);
    }
    
    [HttpGet]
    [ActionName("Create")]
    public IActionResult CreateGet()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Create")]
    public async Task<IActionResult> CreatePostAsync(Category category)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }
        
        try
        {
            var createdCategory = await _categoryServices.CreateCategoryAsync(category);
            TempData["success"] = $"Category '{createdCategory.Name}' created successfully";
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"error: {e.Message}");
            return View();
        }
    }
    
    [HttpGet]
    [ActionName("Update")]
    public async Task<IActionResult> UpdateGetAsync(int? id)
    {
        var category = new Category { Id = 0, Name = "Unknown", DisplayOrder = 0 };
        try
        {
            category = await _categoryServices.GetCategoryByIdAsync(id);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Update")]
    public async Task<IActionResult> UpdatePostAsync(Category category)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }
        
        try
        {
            var updatedCategory = await _categoryServices.UpdateCategoryAsync(category);
            TempData["success"] = $"Category '{updatedCategory.Name}' updated successfully";
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View();
        }
    }

    [HttpGet]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteGetAsync(int? id)
    {
        var category = new Category { Id = 0, Name = "Unknown", DisplayOrder = 0 };
        try
        {
            category = await _categoryServices.GetCategoryByIdAsync(id);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeletePostAsync(int? id)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }
        
        try
        {
            var updatedCategory = await _categoryServices.DeleteCategoryByIdAsync(id);
            TempData["success"] = $"Category '{updatedCategory.Name}' deleted successfully";
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View();
        }

    }
    
}
