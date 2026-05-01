using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaScan.Enums;
using PaScan.Models.ViewModels;
using PaScan.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PaScan.Controllers
{
    [Authorize(Roles = nameof(Role.STUDENT))]
    [Route("student/device")]
    public class DeviceController : Controller
    {
        private readonly IDeviceService _deviceService;

        public DeviceController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpGet("my-devices")]
        public async Task<IActionResult> MyDevices()
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("StudentLogin", "Auth");
            }
            
            var dashboard = await _deviceService.GetStudentDashboardAsync(studentId);
            return View(dashboard.Devices);
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            var model = new DeviceRequestViewModel();
            return View(model);
        }

        [HttpGet("renew-device/{deviceId:guid}")]
        public async Task<IActionResult> RenewDevice(Guid deviceId)
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("StudentLogin", "Auth");
            }

            try
            {
                var vm = await _deviceService.GetDeviceDetailAsync(deviceId, studentId);
                if (vm.Device.Status != DeviceStatus.EXPIRED)
                {
                    TempData["Error"] = "Only expired devices can be renewed for re-verification.";
                    return RedirectToAction("Dashboard", "Student");
                }

                var requestModel = new DeviceRequestViewModel
                {
                    Purpose = vm.Device.Purpose,
                    DeviceName = vm.Device.DeviceName,
                    DeviceType = vm.Device.DeviceType,
                    Brand = vm.Device.Brand,
                    Model = vm.Device.Model,
                    SerialNumber = vm.Device.SerialNumber,
                    OperatingSystem = vm.Device.OperatingSystem,
                    Color = vm.Device.Color,
                    Processor = vm.Device.Processor,
                    Motherboard = vm.Device.Motherboard,
                    Memory = vm.Device.Memory,
                    Storage = vm.Device.Storage,
                    MonitorSize = vm.Device.MonitorSize,
                    Casing = vm.Device.Casing,
                    HasCdRom = vm.Device.HasCdRom,
                    Accessories = vm.Device.Accessories.Select(a => new AccessoryViewModel
                    {
                        AccessoryName = a.AccessoryName,
                        Quantity = a.Quantity
                    }).ToList()
                };

                TempData["Info"] = "Please review your device details and submit a new request for admin verification.";
                return View("Register", requestModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading device: " + ex.Message;
                return RedirectToAction("Dashboard", "Student");
            }
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DeviceRequestViewModel request)
        {
            if (request.Accessories != null)
            {
                request.Accessories.RemoveAll(a => string.IsNullOrWhiteSpace(a.AccessoryName));
            }

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

                await _deviceService.SubmitDeviceRequestAsync(request, studentId);
                TempData["Success"] = "Device registered. Awaiting admin approval.";
                return RedirectToAction("Dashboard", "Student");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("SerialNumber", ex.Message);
                return View(request);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return View(request);
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("StudentLogin", "Auth");
            }

            try
            {
                var request = await _deviceService.GetDeviceRequestAsync(id, studentId);
                return View(request);
            }
            catch (Exception)
            {
                return RedirectToAction("Dashboard", "Student");
            }
        }

        [HttpGet("approved/{deviceId:guid}")]
        public async Task<IActionResult> DeviceDetail(Guid deviceId)
        {
            try
            {
                var studentIdStr = HttpContext.Session.GetString("StudentId");
                if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
                {
                    return RedirectToAction("StudentLogin", "Auth");
                }

                var vm = await _deviceService.GetDeviceDetailAsync(deviceId, studentId);
                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return NotFound();
            }
        }
        [HttpPost("approved/{deviceId:guid}/renew-qr")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenewQr(Guid deviceId)
        {
            try
            {
                var studentIdStr = HttpContext.Session.GetString("StudentId");
                if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
                {
                    return RedirectToAction("StudentLogin", "Auth");
                }

                await _deviceService.RenewQrAsync(deviceId, studentId);
                TempData["Success"] = "QR Code renewed successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while renewing QR code: " + ex.Message;
            }

            return RedirectToAction("DeviceDetail", new { deviceId });
        }
    }
}
