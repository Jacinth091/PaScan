using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaScan.Services.Interfaces;

namespace PaScan.Controllers;

[Authorize(Roles = "STUDENT")]
[Route("student")]
public class StudentController : Controller
{
    private readonly IDeviceService _deviceService;

    public StudentController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var studentIdStr = HttpContext.Session.GetString("StudentId");
        if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
        {
            return RedirectToAction("StudentLogin", "Auth");
        }

        var viewModel = await _deviceService.GetStudentDashboardAsync(studentId);
        return View(viewModel);
    }
}
