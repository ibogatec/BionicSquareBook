using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BionicSquare.Models;
using BionicSquare.Business.Services;

namespace BionicSquare.Web.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly IProductServices _productServices;
    private readonly IShoppingCartService _shoppingCartService;
    
    public HomeController(
        IProductServices productServices,
        IShoppingCartService shoppingCartService)
    {
        _productServices = productServices;
        _shoppingCartService = shoppingCartService;
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
    public async Task<IActionResult> DetailsGetAsync(int productId, int quantity = 1)
    {
        try
        {
            var product = await _productServices.GetProductByIdAsync(productId, includeCategory: true);
            ShoppingCart cart = new()
            {
                Product = product,
                Quantity = quantity,
                ProductId = productId
            };
            return View(cart);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(null);
        }
    }
    
    [HttpPost]
    [ActionName("Details")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DetailsPostAsync(ShoppingCart cart)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            cart.ApplicationUserId = userId;
            var addedCart = await _shoppingCartService.AddToCartAsync(cart);
            return RedirectToAction("Details", new { productId = addedCart.ProductId, quantity = addedCart.Quantity });
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(null);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
}