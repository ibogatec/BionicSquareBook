using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BionicSquare.Models;
using BionicSquare.Models.ViewModels;

namespace BionicSquareWeb.Areas.Identity.Controllers;

[Area("Identity")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    
    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    [ActionName("Login")]
    public IActionResult LoginGet()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Login")]
    public async Task<IActionResult> LoginPostAsync(LoginViewModel loginViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(loginViewModel);
            }
            var result = await _signInManager.PasswordSignInAsync(
                userName: loginViewModel.Email,
                password: loginViewModel.Password,
                isPersistent: loginViewModel.RememberMe,
                lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(loginViewModel);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(loginViewModel);
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Logout")]
    public async Task<IActionResult> LogoutPost()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home",  new { area = "Customer" });
    }
    
    [HttpGet]
    [ActionName("Register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Register")]
    public async Task<IActionResult> RegisterPostAsync(RegisterViewModel registerViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(registerViewModel);
            }
            
            ApplicationUser appUser = new()
            {
                UserName = registerViewModel.Email,
                Email = registerViewModel.Email,
                PhoneNumber = registerViewModel.PhoneNumber,
                Name = registerViewModel.Name,
                PostalCode = registerViewModel.PostalCode,
                State = registerViewModel.State,
                StreetAddress = registerViewModel.StreetAddress,
                City = registerViewModel.City
            };
            
            var result = await _userManager.CreateAsync(appUser, registerViewModel.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(appUser, isPersistent: false);
                return RedirectToAction("Index", "Home",  new { area = "Customer" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            
            return View(registerViewModel);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", $"Error: {e.Message}");
            return View(registerViewModel);
        }
    }

    [HttpGet]
    [ActionName("AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();   
    }
    
}
