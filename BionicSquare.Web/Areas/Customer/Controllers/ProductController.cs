using Microsoft.AspNetCore.Mvc;
using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BionicSquare.Web.Controllers;

[Area("Customer")]
public class ProductController : Controller
{
    private readonly IProductServices _productServices;
    private readonly ICategoryServices _categoryServices;
    private readonly IWebHostEnvironment _webHostEnvironment;
    
    public ProductController(
        IProductServices productServices,
        ICategoryServices categoryServices,
        IWebHostEnvironment webHostEnvironment)
    {
        _productServices = productServices;
        _categoryServices = categoryServices;
        _webHostEnvironment = webHostEnvironment;
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
        ProductViewModel productViewModel = new();
        try
        {
            productViewModel = new()
            {
                CategoryList = (await _categoryServices.GetAllCategoriesAsync())
                    .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
                    .ToArray()
            };
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View("Update", productViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Create")]
    public async Task<IActionResult> CreatePostAsync(ProductViewModel productViewModel, IFormFile? file)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View("Update", productViewModel);
            }

            string? fileName = null;
            var productRelPath = Path.Combine("images", "uploads", "products");
            if (file != null)
            {
                var productAbsPath = Path.Combine(_webHostEnvironment.WebRootPath, productRelPath);
                if (!Directory.Exists(productAbsPath))
                {
                    Directory.CreateDirectory(productAbsPath);
                }
                fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var fileAbsPath = Path.Combine(productAbsPath, fileName);
                await using var stream = new FileStream(fileAbsPath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            productViewModel.Product.ImageUrl = !string.IsNullOrEmpty(fileName) ? Path.Combine(productRelPath, fileName) : null;
            var createdProduct = await _productServices.CreateProductAsync(productViewModel.Product);
            TempData["success"] = $"Product '{createdProduct.Title}' created successfully";
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View("Update", productViewModel);
        }
    }
    
    [HttpGet]
    [ActionName("Update")]
    public async Task<IActionResult> UpdateGetAsync(int? id)
    {
        ProductViewModel productViewModel = new();
        try
        {
            productViewModel = new()
            {
                Action = "Update Product",
                Product = await _productServices.GetProductByIdAsync(id),
                CategoryList = (await _categoryServices.GetAllCategoriesAsync())
                    .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
                    .ToArray()
            };
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View(productViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Update")]
    public async Task<IActionResult> UpdatePostAsync(ProductViewModel productViewModel, IFormFile? file)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(productViewModel);
            }

            string? fileName = null;
            var productRelPath = Path.Combine("images", "uploads", "products");
            var productAbsPath = Path.Combine(_webHostEnvironment.WebRootPath, productRelPath);
            if (file != null)
            {
                if (!Directory.Exists(productAbsPath))
                {
                    Directory.CreateDirectory(productAbsPath);
                }
                fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var fileAbsPath = Path.Combine(productAbsPath, fileName);
                await using var stream = new FileStream(fileAbsPath, FileMode.Create);
                await file.CopyToAsync(stream);
            }

            if (!string.IsNullOrEmpty(productViewModel.Product.ImageUrl))
            {
                var existingImageAbsPath = Path.Combine(_webHostEnvironment.WebRootPath, productViewModel.Product.ImageUrl);
                if (System.IO.File.Exists(existingImageAbsPath))
                {
                    System.IO.File.Delete(existingImageAbsPath);
                }
            }
            productViewModel.Product.ImageUrl = !string.IsNullOrEmpty(fileName) ? Path.Combine(productRelPath, fileName) : null;
            var createdProduct = await _productServices.UpdateProductAsync(productViewModel.Product);
            TempData["success"] = $"Product '{createdProduct.Title}' updated successfully";
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(productViewModel);
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