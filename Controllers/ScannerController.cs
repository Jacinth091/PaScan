using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PaScan.Controllers;

[Authorize(Roles = "SCANNER")]
[Route("scanner")]
public class ScannerController : Controller
{
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }
}
