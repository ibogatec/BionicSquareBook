using Microsoft.AspNetCore.Mvc;
using BionicSquare.Models.ViewModels;

namespace BionicSquareWeb.Areas.Identity.Controllers;

[Area("Identity")]
public class AccountController : Controller
{
    [HttpGet]
    [ActionName("Login")]
    public IActionResult LoginGet()
    {
        return View();
    }

    [HttpPost]
    [ActionName("Login")]
    public IActionResult LoginPost(LoginViewModel loginViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

        }
        catch (Exception e)
        {

        }
        
        return View();
    }
    
    [HttpGet]
    [ActionName("Register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet]
    [ActionName("AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();   
    }
    
}
