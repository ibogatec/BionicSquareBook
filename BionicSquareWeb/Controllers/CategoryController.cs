using BionicSquareWeb.Data;
using BionicSquareWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BionicSquareWeb.Controllers;

public class CategoryController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public CategoryController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public IActionResult Index()
    {
        var categories = _context.Categories.ToList();
        return View("Index", categories);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Create")]
    public IActionResult CreatePost(Category category)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        bool isDuplicate = _context.Categories.Any(c => c.Name.ToLower() == category.Name.ToLower());
        if (isDuplicate)
        {
            ModelState.AddModelError("Name", $"Category with the name '{category.Name}' already exists");
            return View();
        }
        
        _context.Categories.Add(category);
        _context.SaveChanges();
        return RedirectToAction("Index", "Category");
    }
    
    public IActionResult Update(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }
        
        var category = _context.Categories.Find(id);
        if (category == null)
        {
            return NotFound();
        }
        
        return View(category);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Update")]
    public IActionResult UpdatePost(Category category)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }
        
        bool isDuplicate = _context.Categories.Any(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);
        if (isDuplicate)
        {
            ModelState.AddModelError("Name", $"Category with the name '{category.Name}' already exists");
            return View();
        }
        
        _context.Categories.Update(category);
        _context.SaveChanges();
        return RedirectToAction("Index", "Category");
    }
    

}
