using Microsoft.AspNetCore.Mvc;
using BionicSquare.Business.Services;
using BionicSquare.Models;

namespace BionicSquare.Web.Controllers;

[Area("Customer")]
public class ProductController : Controller
{
    private readonly IProductServices _productServices;
    
    public ProductController(IProductServices productServices)
    {
        _productServices = productServices;
    }
    
    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexGetAsync()
    {
        IEnumerable<Product> products = new List<Product>();
        try
        {
            products = await _productServices.GetAllProductsAsync();
        }
        catch
        {
            // ignored
        }
        return View(products);
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
    public async Task<IActionResult> CreatePostAsync(Product product)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }
        
        try
        {
            var createdProduct = await _productServices.CreateProductAsync(product);
            TempData["success"] = $"Product '{createdProduct.Title}' created successfully";
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
        var product = new Product { Id = 0, Title = "Unknown", Description = "Unknown", Price = 0 };
        try
        {
            product = await _productServices.GetProductByIdAsync(id);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Update")]
    public async Task<IActionResult> UpdatePostAsync(Product product)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }
        
        try
        {
            var updatedProduct = await _productServices.UpdateProductAsync(product);
            TempData["success"] = $"Product '{updatedProduct.Title}' updated successfully";
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
        var product = new Product { Id = 0, Title = "Unknown", Description = "Unknown", Price = 0 };
        try
        {
            product = await _productServices.GetProductByIdAsync(id);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View(product);
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
            var updatedProduct = await _productServices.DeleteProductByIdAsync(id);
            TempData["success"] = $"Product '{updatedProduct.Title}' deleted successfully";
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View();
        }
    }
    
}