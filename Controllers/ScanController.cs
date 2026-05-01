using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaScan.Services.Interfaces;
using PaScan.Models.ViewModels;
using System;
using System.Threading.Tasks;

namespace PaScan.Controllers;

[Authorize]
[Route("scan")]
public class ScanController : Controller
{
    private readonly IScanService _scanService;
    private readonly IRfidService _rfidService;

    public ScanController(IScanService scanService, IRfidService rfidService)
    {
        _scanService = scanService;
        _rfidService = rfidService;
    }

    [HttpGet("start-poll/{deviceId}")]
    [Authorize(Roles = "ADMIN")]
    public IActionResult StartPoll(string deviceId)
    {
        Console.WriteLine($"[DEBUG] Admin started polling for device: {deviceId}");
        _rfidService.FlagAdminWaiting(deviceId);
        return Ok(new { message = "Polling started." });
    }

    [HttpGet("check-scan/{deviceId}")]
    [Authorize(Roles = "ADMIN")]
    public IActionResult CheckScan(string deviceId)
    {
        var tagId = _rfidService.GetLastScan(deviceId);
        if (tagId != null) Console.WriteLine($"[DEBUG] Scan retrieved by browser for device: {deviceId}");
        return Ok(new { tagId });
    }

    [HttpPost("qr")]
    [Authorize(Roles = "SCANNER")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ValidateQr([FromBody] QrScanRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.TokenValue))
            return BadRequest(new { error = "Token is required." });

        var scannerIdStr = HttpContext.Session.GetString("ScannerId");
        if (string.IsNullOrEmpty(scannerIdStr) || !Guid.TryParse(scannerIdStr, out var scannerId))
            return Unauthorized(new { error = "Scanner not authenticated." });

        try
        {
            var result = await _scanService.ValidateQrScanAsync(request.TokenValue, scannerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error.", message = ex.Message });
        }
    }

    public class QrScanRequest
    {
        public string TokenValue { get; set; } = null!;
    }

    [HttpPost("rfid")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ValidateRfid([FromBody] RfidScanRequest request, [FromHeader(Name = "X-Api-Key")] string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return Unauthorized(new { error = "X-Api-Key header is required." });

        if (request == null || string.IsNullOrEmpty(request.CardUid))
            return BadRequest(new { error = "CardUid is required." });

        // 1. Authenticate Scanner
        var scanner = await _scanService.GetScannerByApiKeyAsync(apiKey);
        if (scanner == null || scanner.Status != PaScan.Enums.ScannerStatus.ACTIVE)
            return Unauthorized(new { error = "Invalid or inactive scanner API key." });

        Console.WriteLine($"[DEBUG] Received RFID Scan from scanner '{scanner.Name}' (DeviceID in payload: {request.DeviceId})");

        // 2. SMART INTERCEPT (Check both the reported DeviceId and the Scanner's DB name)
        string? interceptId = null;
        
        // We check three possible keys that the Admin might be listening on:
        // A. The custom DeviceId reported by the scanner app (e.g. "Front Gate")
        // B. The official Name of the scanner in our Database (e.g. "Main Entrance")
        // C. The Scanner GUID itself (for absolute certainty)
        if (!string.IsNullOrEmpty(request.DeviceId) && _rfidService.IsAdminWaiting(request.DeviceId))
            interceptId = request.DeviceId;
        else if (_rfidService.IsAdminWaiting(scanner.Name))
            interceptId = scanner.Name;
        else if (_rfidService.IsAdminWaiting(scanner.Id.ToString()))
            interceptId = scanner.Id.ToString();

        // FALLBACK: If the admin page is listening to MachineName (default), 
        // and our scanner app is reporting a friendly name, they might not match.
        // But since the Scanner is authenticated via API key, we can trust it.
        // If we still haven't found an intercept, we can check if there's ANY 
        // admin waiting on a "Machine Name" style ID if the scanner's reported DeviceId 
        // doesn't match. But to be safe, we'll stick to the explicit checks above first.

        if (interceptId != null)
        {
            Console.WriteLine($"[DEBUG] Intercepting scan for Admin on device key: {interceptId}");
            _rfidService.RegisterScan(request.CardUid, interceptId);
            return Ok(new 
            { 
                allowed = true, 
                message = "Captured for Registration",
                studentName = "SYSTEM",
                studentNumber = "PENDING"
            });
        }

        // 3. Normal Validation
        try
        {
            var result = await _scanService.ValidateRfidScanAsync(request.CardUid, apiKey);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error.", message = ex.Message });
        }
    }

    public class RfidScanRequest
    {
        public string CardUid { get; set; } = null!;
        public string DeviceId { get; set; } = null!;
    }
}
