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
        _context.Categories.Add(category);
        _context.SaveChanges();
        return RedirectToAction("Index", "Category");
    }
    
}
