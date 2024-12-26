using Microsoft.AspNetCore.Mvc;
using BionicSquare.Business.Services;
using BionicSquare.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BionicSquare.Web.Controllers;

[Area("Customer")]
public class ProductController : Controller
{
    private readonly IProductServices _productServices;
    private readonly ICategoryServices _categoryServices;
    
    public ProductController(IProductServices productServices, ICategoryServices categoryServices)
    {
        _productServices = productServices;
        _categoryServices = categoryServices;
    }
    
    #region UI CALLS
    
    [HttpGet]
    [ActionName("Index")]
    public IActionResult IndexGet()
    {
        return View();
    }
    
    [HttpGet]
    [ActionName("Create")]
    public async Task<IActionResult> CreateGetAsync()
    {
        try
        {
            var categories = await _categoryServices.GetAllCategoriesAsync();
            IEnumerable<SelectListItem> categoryListItems = categories
                .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
                .ToList();
            ViewData["categoryListItems"] = categoryListItems;
        }
        catch (Exception)
        {
            // ignored
        }
        return View("Update");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Create")]
    public async Task<IActionResult> CreatePostAsync(Product product)
    {
        if (!ModelState.IsValid)
        {
            return View("Update");
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
            return View("Update");
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
    
    #endregion
    
    # region API CALLS
    
    [HttpGet]
    [Route("api/products")]
    [ActionName("GetAll")]
    public async Task<IActionResult> IndexGetJsonAsync()
    {
        IEnumerable<Product> products = new List<Product>();
        try
        {
            products = await _productServices.GetAllProductsAsync(includeCategory: true);
        }
        catch
        {
            // ignored
        }
        return Json(new { data = products });
    }
    
    #endregion
    
}