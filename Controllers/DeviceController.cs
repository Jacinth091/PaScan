using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaScan.Data;
using PaScan.Enums;
using PaScan.Models;
using PaScan.Models.ViewModels;

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

        [HttpGet("student/register")]
        public IActionResult Register()
        {
            return View(new DeviceRequestViewModel());
        }

        [HttpPost("student/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DeviceRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (studentIdStr == null || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("Login", "Account");
            }
            bool exists = await _context.DeviceRequests
            .AnyAsync(d => d.StudentId == studentId && d.SerialNumber == model.SerialNumber && d.DeletedAt == null);
            if (exists)
            {
                ModelState.AddModelError("SerialNumber", "You have already registered this device.");
                return View(model);
            }
            var deviceRequest = new DeviceRequest
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Purpose = model.Purpose,
                DeviceName = model.DeviceName,
                DeviceType = model.DeviceType,
                Brand = model.Brand,
                Model = model.Model,
                SerialNumber = model.SerialNumber,
                OperatingSystem = model.OperatingSystem,
                Color = model.Color,
                Processor = model.Processor,
                Motherboard = model.Motherboard,
                Memory = model.Memory,
                Storage = model.Storage,
                MonitorSize = model.MonitorSize,
                Casing = model.Casing,
                HasCdRom = model.HasCdRom,
                Status = RegisterStatus.PENDING,
                CreatedAt = DateTime.Now,
            };
            _context.DeviceRequests.Add(deviceRequest);

            foreach (var a in model.Accessories.Where(x => !string.IsNullOrWhiteSpace(x.AccessoryName)))
            {
                _context.DeviceRequestAccessories.Add(new DeviceRequestAccessory
                {
                    Id = Guid.NewGuid(),
                    DeviceRequestId = deviceRequest.Id,
                    AccessoryName = a.AccessoryName,
                    Quantity = a.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Detail", new { id = deviceRequest.Id });
            // return RedirectToAction("Dashboard", "Student");
        }

        [HttpGet("student/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
            {
                return RedirectToAction("Login", "Account");
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

        // GET: DeviceController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DeviceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DeviceController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: DeviceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DeviceController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: DeviceController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
