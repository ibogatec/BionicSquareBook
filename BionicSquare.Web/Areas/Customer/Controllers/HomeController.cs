using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BionicSquare.Models;
using BionicSquare.Business.Services;

namespace BionicSquare.Web.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly IProductServices _productServices;
    
    public HomeController(IProductServices productServices)
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
            products = await _productServices.GetAllProductsAsync(includeCategory: true);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View(products);
    }
    
    [HttpGet]
    [ActionName("Details")]
    public async Task<IActionResult> DetailsGetAsync(int productId)
    {
        Product? product = null;
        try
        {
            product = await _productServices.GetProductByIdAsync(productId, includeCategory: true);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
        }
        return View(product);
    }
    
    [HttpPost]
    [ActionName("Details")]
    public async Task<IActionResult> DetailsPostAsync(int id)
    {
        Product? product = null;
        return View(product);
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
}