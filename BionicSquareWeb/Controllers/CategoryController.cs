using Microsoft.AspNetCore.Mvc;

namespace BionicSquareWeb.Controllers;

public class CategoryController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    
}
