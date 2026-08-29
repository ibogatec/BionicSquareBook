using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BionicSquare.Business.Services;
using BionicSquare.Models;
using BionicSquare.Models.ViewModels;

namespace BionicSquare.Web.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IApplicationUserService _applicationUserService;
    
    public CartController(
        IShoppingCartService shoppingCartService,
        IApplicationUserService applicationUserService)
    {
        _shoppingCartService = shoppingCartService;
        _applicationUserService = applicationUserService;
    }
    
    #region UI CALLS

    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexGetAsync()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var cartItems = (await _shoppingCartService.GetUserCartItemsAsync(userId)).ToArray();
            var user = await _applicationUserService.GetUserByIdAsync(userId);
            var orderHeader = new OrderHeader()
            {
                OrderTotal = cartItems.Sum(s => s.Price * s.Quantity),
                PhoneNumber = user.PhoneNumber ?? "N/A",
                StreetAddress = user.StreetAddress ?? "N/A",
                City = user.City ?? "N/A",
                State = user.State ?? "N/A",
                PostalCode = user.PostalCode ?? "N/A",
                Name = user.Name,
                ApplicationUserId = userId,
                ApplicationUser = user,
            };
            var shoppingCartViewModel = new ShoppingCartViewModel()
            {
                CartItems = cartItems,
                OrderHeader = orderHeader
            };
            return View(shoppingCartViewModel);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(null);
        }
    }

    [HttpGet]
    [ActionName("Plus")]
    public async Task<IActionResult> PlusGetAsync(int cartId)
    {
        try
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartId);
            if (cart is null)
            {
                return NotFound();
            }
            cart.Quantity++;
            await _shoppingCartService.UpdateCartAsync(cart);
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(null);
        }
    }
    
    [HttpGet]
    [ActionName("Minus")]
    public async Task<IActionResult> MinusGetAsync(int cartId)
    {
        try
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartId);
            if (cart is null)
            {
                return NotFound();
            }
            cart.Quantity--;
            await _shoppingCartService.UpdateCartAsync(cart);
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(null);
        }
    }
    
    [HttpGet]
    [ActionName("Remove")]
    public async Task<IActionResult> RemoveGetAsync(int cartId)
    {
        try
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartId);
            if (cart is null)
            {
                return NotFound();
            }
            cart.Quantity = 0;
            await _shoppingCartService.UpdateCartAsync(cart);
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(null);
        }
    }

    #endregion
    
    # region API CALLS
    
    [HttpPost]
    [Route("api/cart/update")]
    public async Task<IActionResult> UpdateCartPostAsync(int cartId, int quantity)
    {
        try
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartId);
            if (cart is null)
            {
                return NotFound(new { message = $"Cart not found" });
            }
            cart.Quantity = quantity;
            await _shoppingCartService.UpdateCartAsync(cart);
            return Ok(new { message = "Cart updated successfully" });
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Error: {e.Message}" });
        }
    }
    
    #endregion

}