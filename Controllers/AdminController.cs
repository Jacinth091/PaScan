using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaScan.Models.ViewModels;
using PaScan.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PaScan.Controllers;

[Authorize(Roles = "ADMIN")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IRfidService _rfidService;

    public AdminController(IAdminService adminService, IRfidService rfidService)
    {
        _adminService = adminService;
        _rfidService = rfidService;
    }

    [Route("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var vm = await _adminService.GetDashboardStatsAsync();
        return View(vm);
    }

    [HttpGet("device/requests")]
    public async Task<IActionResult> Requests()
    {
        var vm = await _adminService.GetPendingRequestsAsync();
        return View(vm);
    }

    [HttpGet("device/requests/{id:guid}")]
    public async Task<IActionResult> RequestDetail(Guid id)
    {
        var vm = await _adminService.GetRequestDetailAsync(id);
        return View(vm);
    }

    [HttpPost("device/requests/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var adminIdStr = User.FindFirst("ProfileId")?.Value ?? HttpContext.Session.GetString("AdminId");
            var adminId = Guid.Parse(adminIdStr ?? Guid.Empty.ToString());
            await _adminService.ApproveDeviceRequestAsync(id, adminId);
            TempData["Success"] = "Device request approved.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex) 
        { 
            var msg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
            TempData["Error"] = $"Approval failed: {msg}"; 
        }
        return RedirectToAction("Requests");
    }

    [HttpPost("device/requests/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string rejectionReason)
    {
        try
        {
            var adminIdStr = User.FindFirst("ProfileId")?.Value ?? HttpContext.Session.GetString("AdminId");
            var adminId = Guid.Parse(adminIdStr ?? Guid.Empty.ToString());
            await _adminService.RejectDeviceRequestAsync(id, adminId, rejectionReason);
            TempData["Success"] = "Request rejected.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Requests");
    }

    [HttpGet("student/{id:guid}/rfid/issue")]
    public async Task<IActionResult> IssueRfid(Guid id)
    {
        var vm = await _adminService.GetStudentDetailAsync(id);
        if (vm.ActiveRfid != null)
        {
            TempData["Error"] = "Student already has an active RFID card.";
            return RedirectToAction("StudentDetail", new { id });
        }
        return View(vm);
    }

    [HttpPost("rfid/issue/{studentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueRfid(Guid studentId, IssueRfidViewModel vm)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("StudentDetail", new { id = studentId });

        try
        {
            var adminIdStr = User.FindFirst("ProfileId")?.Value ?? HttpContext.Session.GetString("AdminId");
            var adminId = Guid.Parse(adminIdStr ?? Guid.Empty.ToString());
            await _rfidService.IssueCardAsync(studentId, adminId, vm.CardUid, vm.SemesterExpiresAt);
            TempData["Success"] = "RFID card issued.";
        }
        catch (Exception ex)
        {
            var detailedMsg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
            Console.WriteLine($"[ERROR] RFID Issuance Failed: {detailedMsg}");
            TempData["Error"] = $"Registration failed: {detailedMsg}";
        }
        return RedirectToAction("StudentDetail", new { id = studentId });
    }

    [HttpGet("student/{id:guid}/rfid/manage")]
    public async Task<IActionResult> ManageRfid(Guid id)
    {
        var vm = await _adminService.GetStudentDetailAsync(id);
        if (vm.ActiveRfid == null)
        {
            TempData["Error"] = "Student does not have an active RFID card.";
            return RedirectToAction("StudentDetail", new { id });
        }
        return View(vm);
    }

    [HttpGet("student/{id:guid}/rfid/renew")]
    public async Task<IActionResult> RenewRfid(Guid id)
    {
        var vm = await _adminService.GetStudentDetailAsync(id);
        if (vm.ActiveRfid == null)
        {
            TempData["Error"] = "Student does not have an active RFID card.";
            return RedirectToAction("StudentDetail", new { id });
        }
        return View(vm);
    }

    [HttpPost("rfid/renew/{studentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenewRfidSemester(Guid studentId, DateTime semesterExpiresAt)
    {
        try
        {
            var adminIdStr = User.FindFirst("ProfileId")?.Value ?? HttpContext.Session.GetString("AdminId");
            var adminId = Guid.Parse(adminIdStr ?? Guid.Empty.ToString());
            await _rfidService.RenewSemesterAsync(studentId, adminId, semesterExpiresAt);
            TempData["Success"] = "RFID semester renewed successfully.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("ManageRfid", new { id = studentId });
    }

    [HttpGet("student/{id:guid}/rfid/replace")]
    public async Task<IActionResult> ReplaceRfid(Guid id)
    {
        var vm = await _adminService.GetStudentDetailAsync(id);
        if (vm.ActiveRfid == null)
        {
            TempData["Error"] = "Student does not have an active RFID card to replace.";
            return RedirectToAction("StudentDetail", new { id });
        }
        return View(vm);
    }

    [HttpPost("rfid/replace/{studentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplaceRfid(Guid studentId, string newCardUid, PaScan.Enums.InvalidationReason reason)
    {
        try
        {
            var adminIdStr = User.FindFirst("ProfileId")?.Value ?? HttpContext.Session.GetString("AdminId");
            var adminId = Guid.Parse(adminIdStr ?? Guid.Empty.ToString());
            await _rfidService.ReplaceCardAsync(studentId, adminId, newCardUid, reason);
            TempData["Success"] = "RFID card replaced successfully.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("ManageRfid", new { id = studentId });
    }

    [HttpGet("student/{id:guid}/rfid/revoke")]
    public async Task<IActionResult> RevokeRfid(Guid id)
    {
        var vm = await _adminService.GetStudentDetailAsync(id);
        if (vm.ActiveRfid == null)
        {
            TempData["Error"] = "Student does not have an active RFID card to revoke.";
            return RedirectToAction("StudentDetail", new { id });
        }
        return View(vm);
    }

    [HttpPost("rfid/revoke/{studentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeRfid(Guid studentId, PaScan.Enums.InvalidationReason reason)
    {
        try
        {
            var adminIdStr = User.FindFirst("ProfileId")?.Value ?? HttpContext.Session.GetString("AdminId");
            var adminId = Guid.Parse(adminIdStr ?? Guid.Empty.ToString());
            await _rfidService.RevokeCardAsync(studentId, adminId, reason);
            TempData["Success"] = "RFID card revoked successfully.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("StudentDetail", new { id = studentId });
    }

    [HttpGet("students")]
    public async Task<IActionResult> Students()
    {
        var vm = await _adminService.GetAllStudentsAsync();
        return View(vm);
    }

    [HttpGet("devices")]
    public async Task<IActionResult> Devices()
    {
        var vm = await _adminService.GetAllDevicesAsync();
        return View(vm);
    }

    [HttpGet("rfid")]
    public async Task<IActionResult> RfidCards()
    {
        var vm = await _adminService.GetAllRfidCardsAsync();
        return View(vm);
    }

    [HttpGet("devices/{id:guid}")]
    public async Task<IActionResult> DeviceDetail(Guid id)
    {
        try
        {
            var vm = await _adminService.GetDeviceDetailAsync(id);
            return View(vm);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost("devices/{id:guid}/revoke-qr")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeQr(Guid id, PaScan.Enums.RevocationReason reason)
    {
        try
        {
            await _adminService.RevokeQrTokenAsync(id, reason);
            TempData["Success"] = "QR token revoked.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("DeviceDetail", new { id });
    }

    [HttpGet("student/{id:guid}")]
    public async Task<IActionResult> StudentDetail(Guid id)
    {
        var vm = await _adminService.GetStudentDetailAsync(id);
        return View(vm);
    }

    [HttpGet("scanners")]
    public async Task<IActionResult> Scanners()
    {
        var vm = await _adminService.GetAllScannersAsync();
        return View(vm);
    }

    [HttpGet("scanners/create")]
    public IActionResult CreateScanner()
    {
        return View(new ScannerCreateViewModel());
    }

    [HttpPost("scanners/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateScanner(ScannerCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId") ?? Guid.Empty.ToString());
            await _adminService.CreateScannerAsync(vm, adminId);
            TempData["Success"] = "Scanner created successfully.";
            return RedirectToAction("Scanners");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
        catch
        {
            ModelState.AddModelError("", "An error occurred while creating the scanner.");
            return View(vm);
        }
    }

    [HttpGet("scanners/{id:guid}/edit")]
    public async Task<IActionResult> EditScanner(Guid id)
    {
        try
        {
            var vm = await _adminService.GetScannerForEditAsync(id);
            return View(vm);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost("scanners/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditScanner(Guid id, ScannerEditViewModel vm)
    {
        if (id != vm.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId") ?? Guid.Empty.ToString());
            await _adminService.EditScannerAsync(id, vm, adminId);
            TempData["Success"] = "Scanner updated successfully.";
            return RedirectToAction("Scanners");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
        catch
        {
            ModelState.AddModelError("", "An error occurred while updating the scanner.");
            return View(vm);
        }
    }

    [HttpGet("settings")]
    public IActionResult Settings()
    {
        return View();
    }

    [HttpPost("settings/end-academic-year")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndAcademicYear()
    {
        try
        {
            var adminIdStr = User.FindFirst("ProfileId")?.Value ?? HttpContext.Session.GetString("AdminId");
            var adminId = Guid.Parse(adminIdStr ?? Guid.Empty.ToString());
            
            await _adminService.EndAcademicYearAsync(adminId);
            TempData["Success"] = "Academic year ended successfully. All active devices and tokens have been expired.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred: {ex.Message}";
        }
        
        return RedirectToAction("Settings");
    }
}
