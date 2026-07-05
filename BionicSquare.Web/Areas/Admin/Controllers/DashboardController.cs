using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BionicSquare.Utility;

namespace BionicSquare.Web.Controllers;

[Area("Admin")]
[Authorize(Roles = $"{Role.Admin},{Role.Employee}")]
public class DashboardController : Controller
{
    
    [HttpGet]
    [ActionName("Index")]
    public IActionResult IndexGet()
    {
        return View();
    }

    
}
