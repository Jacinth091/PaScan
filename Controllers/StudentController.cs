using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaScan.Data;
using PaScan.Enums;
using PaScan.Models;
using PaScan.Models.ViewModels;


namespace PaScan.Controllers;

[Authorize(Roles = "STUDENT")]
[Route("student")]
public class StudentController : Controller
{
    private readonly AppDbContext _context;

    public StudentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        // Read StudentId from session
        var studentIdStr = HttpContext.Session.GetString("StudentId");
        if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
        {
            return RedirectToAction("StudentLogin", "Auth");
        }

        // Load the student with their course
        var student = await _context.Students
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return NotFound();

        // Load their device requests
        var requests = await _context.DeviceRequests
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new StudentRequestListItem
            {
                Id = r.Id,
                DeviceName = r.DeviceName,
                DeviceType = r.DeviceType,
                Status = r.Status,
                SubmittedAt = r.CreatedAt,
                RejectionReason = r.RejectionReason
            })
            .ToListAsync();

        // Load their approved devices
        var devices = await _context.Devices
            .Where(d => d.StudentId == studentId)
            .Select(d => new DeviceListItem
            {
                Id = d.Id,
                DeviceName = d.DeviceName,
                DeviceType = d.DeviceType,
                Brand = d.Brand,
                Model = d.Model,
                Status = d.Status,
            })
            .ToListAsync();

        // Load RFID info (most recent active card)
        var activeCard = await _context.RFIDCards
            .Where(c => c.StudentId == studentId && c.Status == TokenStatus.ACTIVE)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        var viewModel = new StudentDashboardViewModel
        {
            FirstName = student.FirstName,
            LastName = student.LastName,
            StudentNumber = student.StudentNumber,
            CourseName = student.Course.Name,
            IsRfidEnabled = student.IsRfidEnabled,
            RfidExpiresAt = activeCard?.SemesterExpiresAt,
            Requests = requests,
            Devices = devices
        };

        return View(viewModel);
    }
}
