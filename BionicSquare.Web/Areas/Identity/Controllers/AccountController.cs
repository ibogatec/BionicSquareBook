using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using BionicSquare.Models;
using BionicSquare.Models.ViewModels;
using BionicSquare.Utility;

namespace BionicSquareWeb.Areas.Identity.Controllers;

[Area("Identity")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    
    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    [ActionName("Login")]
    public IActionResult LoginGet(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Login")]
    public async Task<IActionResult> LoginPostAsync(LoginViewModel loginViewModel, string? returnUrl = null)
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
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
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
    public IActionResult RegisterGet(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var registerViewModel = new RegisterViewModel()
        {
            RoleList = 
            [
                new SelectListItem { Text = Role.Customer, Value = Role.Customer },
                new SelectListItem { Text = Role.Admin, Value = Role.Admin },
                new SelectListItem { Text = Role.Employee, Value = Role.Employee }
            ]
        };
        return View(registerViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Register")]
    public async Task<IActionResult> RegisterPostAsync(RegisterViewModel registerViewModel, string? returnUrl = null)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(registerViewModel);
            }

            if (!await _roleManager.RoleExistsAsync(registerViewModel.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(registerViewModel.Role));
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
                await _userManager.AddToRoleAsync(appUser, registerViewModel.Role);
                await _signInManager.SignInAsync(appUser, isPersistent: false);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
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
