using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PaScan.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminController : Controller
{
    public IActionResult Dashboard()
    {
        // Placeholder for M10 Dashboard
        return Content("Welcome to the Admin Dashboard!");
    }
}
