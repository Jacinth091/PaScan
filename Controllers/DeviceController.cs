using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaScan.Data;
using PaScan.Enums;
using PaScan.Models;
using PaScan.Models.ViewModels;
using QRCoder;

namespace PaScan.Controllers
{
    [Authorize(Roles = nameof(Role.STUDENT))]
    [Route("device")]
    public class DeviceController : Controller
    {
        private readonly AppDbContext _context;

        public DeviceController(AppDbContext context)
        {
            _context = context;
        }
        // GET: device/student/
        [HttpGet("student")]
        public async Task<IActionResult> MyDevices()
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("StudentLogin", "Auth");
            }

            var devices = await _context.Devices
               .Where(d => d.StudentId == studentId)
                .Select(d => new DeviceListItem
                {
                    Id = d.Id,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType,
                    Brand = d.Brand,
                    Model = d.Model,
                    Status = d.Status
                })
            .ToListAsync();

            return View(devices);
        }

        [HttpGet("student/register")]
        public IActionResult Register()
        {
            var model = new DeviceRequestViewModel();
            return View(model);
        }

        [HttpPost("student/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DeviceRequestViewModel request)
        {
            // Remove empty accessories from the list so they don't trigger validation errors
            if (request.Accessories != null)
            {
                request.Accessories.RemoveAll(a => string.IsNullOrWhiteSpace(a.AccessoryName));
            }

            // Re-validate the model after cleanup
            ModelState.Clear();
            TryValidateModel(request);

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var studentIdStr = HttpContext.Session.GetString("StudentId");
                if (studentIdStr == null || !Guid.TryParse(studentIdStr, out var studentId))
                {
                    return RedirectToAction("StudentLogin", "Auth");
                }

                bool exists = await _context.DeviceRequests
                    .AnyAsync(d => d.StudentId == studentId && d.SerialNumber == request.SerialNumber && d.DeletedAt == null);

                if (exists)
                {
                    ModelState.AddModelError("SerialNumber", "You have already registered a request for this serial number.");
                    return View(request);
                }

                var deviceRequest = new DeviceRequest
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    Purpose = request.Purpose,
                    DeviceName = request.DeviceName,
                    DeviceType = request.DeviceType,
                    Brand = request.Brand,
                    Model = request.Model,
                    SerialNumber = request.SerialNumber,
                    OperatingSystem = request.OperatingSystem,
                    Color = request.Color,
                    Processor = request.Processor,
                    Motherboard = request.Motherboard,
                    Memory = request.Memory,
                    Storage = request.Storage,
                    MonitorSize = request.MonitorSize,
                    Casing = request.Casing,
                    HasCdRom = request.HasCdRom,
                    Status = RegisterStatus.PENDING,
                    CreatedAt = DateTime.Now,
                };

                _context.DeviceRequests.Add(deviceRequest);

                if (request.Accessories != null)
                {
                    foreach (var a in request.Accessories.Where(x => !string.IsNullOrWhiteSpace(x.AccessoryName)))
                    {
                        _context.DeviceRequestAccessories.Add(new DeviceRequestAccessory
                        {
                            Id = Guid.NewGuid(),
                            DeviceRequestId = deviceRequest.Id,
                            AccessoryName = a.AccessoryName,
                            Quantity = a.Quantity
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = deviceRequest.Id });
            }
            catch (Exception ex)
            {
                // This will display the actual database error at the top of the form
                ModelState.AddModelError("", "Database Error: " + ex.Message);
                return View(request);
            }
        }

        [HttpGet("student/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("StudentLogin", "Auth");
            }

            var request = await _context.DeviceRequests
                .Include(r => r.Accessories)
                .FirstOrDefaultAsync(r => r.Id == id && r.StudentId == studentId);

            if (request == null)
            {
                return NotFound();
            }
            return View(request);

            // var model = new DeviceRequestViewModel
            // {
            //     Id = request.Id,
            //     Status = request.Status,
            //     CreatedAt = request.CreatedAt,
            //     Purpose = request.Purpose,
            //     DeviceName = request.DeviceName,
            //     DeviceType = request.DeviceType,
            //     Brand = request.Brand,
            //     Model = request.Model,
            //     SerialNumber = request.SerialNumber,
            //     OperatingSystem = request.OperatingSystem,
            //     Color = request.Color,
            //     Processor = request.Processor,
            //     Motherboard = request.Motherboard,
            //     Memory = request.Memory,
            //     Storage = request.Storage,
            //     MonitorSize = request.MonitorSize,
            //     Casing = request.Casing,
            //     HasCdRom = request.HasCdRom,
            //     Accessories = request.Accessories.Select(a => new AccessoryViewModel
            //     {
            //         AccessoryName = a.AccessoryName,
            //         Quantity = a.Quantity
            //     }).ToList()
            // };
            // return View(model);
        }

        [HttpGet("/student/approved/{deviceId:guid}")]
        public async Task<IActionResult> DeviceDetail(Guid deviceId)
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("StudentLogin", "Auth");
            }
            var device = await _context.Devices
            .Include(d => d.Accessories)
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.StudentId == studentId);

            if (device == null) return NotFound();

            var qrToken = await _context.QRTokens
            .Where(t => t.DeviceId == deviceId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

            string? qrImageBase64 = null;
            if (qrToken != null && qrToken.Status != TokenStatus.REVOKED)
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(qrToken.TokenValue, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrData);
                var qrBytes = qrCode.GetGraphic(10);
                qrImageBase64 = Convert.ToBase64String(qrBytes);
            }

            var viewModel = new DeviceDetailViewModel
            {
                Device = device,
                QrToken = qrToken,
                QrImageBase64 = qrImageBase64
            };

            return View(viewModel);
        }
    }
}
