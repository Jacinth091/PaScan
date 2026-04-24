using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PaScan.Controllers;

[Authorize(Roles = "STUDENT")]
public class StudentController : Controller
{
    public IActionResult Dashboard()
    {
        // Placeholder for M9 Dashboard
        return Content("Welcome to the Student Dashboard!");
    }
}
