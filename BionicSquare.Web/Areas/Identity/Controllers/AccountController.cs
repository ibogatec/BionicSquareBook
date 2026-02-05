using Microsoft.AspNetCore.Mvc;

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
