using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PaScan.Controllers;

[Authorize(Roles = "SCANNER")]
public class ScanController : Controller
{
    public IActionResult QrScanPage()
    {
        // Placeholder for M6 QR Scanner view
        return Content("QR Scanner View - Ready to scan!");
    }
}
