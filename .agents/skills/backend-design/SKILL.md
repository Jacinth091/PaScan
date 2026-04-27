# SKILL.md — PaScan Architecture Guide
### For AI Assistants Helping Implement Features
> ASP.NET Core MVC · Entity Framework Core · Azure SQL · Repository + Service Pattern

---

## What This File Is

This file teaches you the architecture of **PaScan** — a campus device entry management system. Read this entire file before writing any controller, service, or repository code. Every feature must follow the patterns defined here.

---

## Project Structure

```
/Controllers
    AccountController.cs        ← auth only (login, logout)
    DeviceController.cs         ← student-facing device actions
    AdminController.cs          ← all admin-facing actions
    ScanController.cs           ← gate scanning (QR + RFID)

/Services
    /Interfaces
        IDeviceService.cs
        IAdminService.cs
        IRfidService.cs
        IQrTokenService.cs
        IScanService.cs
    DeviceService.cs
    AdminService.cs
    RfidService.cs
    QrTokenService.cs
    ScanService.cs

/Repositories
    /Interfaces
        IDeviceRepository.cs
        IStudentRepository.cs
        IDeviceRequestRepository.cs
        IQrTokenRepository.cs
        IRfidCardRepository.cs
        IScanLogRepository.cs
        IScannerRepository.cs
    DeviceRepository.cs
    StudentRepository.cs
    DeviceRequestRepository.cs
    QrTokenRepository.cs
    RfidCardRepository.cs
    ScanLogRepository.cs
    ScannerRepository.cs

/Models
    /Entities          ← EF Core entity classes (match DB tables exactly)
        User.cs
        Student.cs
        Admin.cs
        Scanner.cs
        Device.cs
        DeviceRequest.cs
        DeviceAccessory.cs
        DeviceRequestAccessory.cs
        QRToken.cs
        RFIDCard.cs
        GateScanLog.cs
        Course.cs
    /Enums
        Role.cs
        DeviceType.cs
        RegisterStatus.cs
        DeviceStatus.cs
        TokenStatus.cs
        ScanType.cs
        ScannerType.cs
        ScannerStatus.cs
        InvalidationReason.cs
        RevocationReason.cs

/Models/ViewModels
    /Student
        DeviceRequestViewModel.cs
        DeviceDetailViewModel.cs
        StudentDashboardViewModel.cs
    /Admin
        AdminRequestListViewModel.cs
        AdminRequestDetailViewModel.cs
        AdminStudentDetailViewModel.cs
        AdminDeviceDetailViewModel.cs
        AdminDashboardViewModel.cs
        IssueRfidViewModel.cs
        RenewRfidViewModel.cs
        ReplaceCardViewModel.cs
        RegisterScannerViewModel.cs
    /Scan
        QrScanResultViewModel.cs
        RfidScanResultViewModel.cs

/Data
    AppDbContext.cs
    /Migrations

/Views
    /Device              ← student device views
    /Admin               ← admin views
    /Scan                ← scanner views
    /Account             ← login views
    /Shared
        _Layout.cshtml
        _ValidationScriptsPartial.cshtml

/wwwroot
    /css
    /js
    /images
```

---

## Layer Responsibilities

This project uses **4 layers**. Each layer has exactly one job. Never skip a layer or merge two layers into one.

### Layer 1 — Controller
- Handles HTTP: reads request, calls service, returns view or JSON
- Reads session to get `UserId`, `AdminId`, `StudentId`, `ScannerId`
- Passes a ViewModel to the view — never passes a raw entity
- Contains **no business logic and no direct DbContext calls**
- Validates input with `ModelState.IsValid` before calling service
- Catches service exceptions and returns error views

```csharp
// CORRECT — controller is thin
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Approve(Guid id)
{
    var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId")!);
    await _adminService.ApproveDeviceRequestAsync(id, adminId);
    return RedirectToAction("Requests");
}

// WRONG — business logic in controller
[HttpPost]
public async Task<IActionResult> Approve(Guid id)
{
    var request = await _context.DeviceRequests.FindAsync(id);
    request.Status = RegisterStatus.APPROVED;
    var device = new Device { ... };
    _context.Devices.Add(device);
    await _context.SaveChangesAsync();
    // ... this belongs in a service
}
```

### Layer 2 — Service
- Contains all business logic and validation rules
- Orchestrates calls across multiple repositories
- Owns all transactions — `BeginTransactionAsync()` lives here
- Takes plain parameters or ViewModels; returns ViewModels or primitives
- Never returns raw EF entities to the controller
- Throws descriptive exceptions on business rule violations

```csharp
// CORRECT — service owns the transaction and business rules
public async Task ApproveDeviceRequestAsync(Guid requestId, Guid adminId)
{
    var request = await _deviceRequestRepository.GetByIdAsync(requestId)
        ?? throw new InvalidOperationException("Request not found.");

    if (request.Status != RegisterStatus.PENDING)
        throw new InvalidOperationException("Request is no longer pending.");

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Steps 1–4 of T1 here
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Layer 3 — Repository
- Handles all direct database queries through EF Core
- Takes and returns **entities** only — no business logic
- One repository per entity (not per controller)
- Wraps common queries: GetById, GetAll, Add, Update, Delete
- Never calls `SaveChangesAsync()` inside a repository method — let the service control when to save (important for transactions)

```csharp
// CORRECT — repository is a thin query wrapper
public async Task<DeviceRequest?> GetByIdWithAccessoriesAsync(Guid id)
{
    return await _context.DeviceRequests
        .Include(r => r.Accessories)
        .Include(r => r.Student).ThenInclude(s => s.User)
        .FirstOrDefaultAsync(r => r.Id == id);
}

// WRONG — SaveChangesAsync inside repository breaks transaction control
public async Task UpdateStatusAsync(Guid id, RegisterStatus status)
{
    var req = await _context.DeviceRequests.FindAsync(id);
    req.Status = status;
    await _context.SaveChangesAsync(); // ← never do this in a repo
}
```

### Layer 4 — ViewModel
- One ViewModel per view — shaped exactly for what the view needs
- Never expose raw entity properties directly to views
- Carry validation attributes (`[Required]`, `[StringLength]`, etc.)
- Read-only display ViewModels are plain classes with get-only props
- Form ViewModels use data annotations for client-side + server-side validation

---

## Contracts (Interfaces)

Every service and every repository must have an interface. This is mandatory — not optional.

### Why
- Controllers depend on the interface, not the concrete class
- Enables unit testing (mock the interface)
- Makes `Program.cs` the single place where implementations are swapped

### Interface naming convention
- `IDeviceService` → implemented by `DeviceService`
- `IDeviceRepository` → implemented by `DeviceRepository`

### Registration in Program.cs

```csharp
// Repositories
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IDeviceRequestRepository, DeviceRequestRepository>();
builder.Services.AddScoped<IQrTokenRepository, QrTokenRepository>();
builder.Services.AddScoped<IRfidCardRepository, RfidCardRepository>();
builder.Services.AddScoped<IScanLogRepository, ScanLogRepository>();
builder.Services.AddScoped<IScannerRepository, ScannerRepository>();

// Services
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRfidService, RfidService>();
builder.Services.AddScoped<IQrTokenService, QrTokenService>();
builder.Services.AddScoped<IScanService, ScanService>();
```

### Constructor injection pattern

```csharp
// Repository — inject DbContext
public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _context;
    public DeviceRepository(AppDbContext context) => _context = context;
}

// Service — inject repositories + DbContext (for transaction control only)
public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly IDeviceRequestRepository _deviceRequestRepo;
    private readonly IDeviceRepository _deviceRepo;
    private readonly IQrTokenRepository _qrTokenRepo;

    public AdminService(
        AppDbContext context,
        IDeviceRequestRepository deviceRequestRepo,
        IDeviceRepository deviceRepo,
        IQrTokenRepository qrTokenRepo)
    {
        _context = context;
        _deviceRequestRepo = deviceRequestRepo;
        _deviceRepo = deviceRepo;
        _qrTokenRepo = qrTokenRepo;
    }
}

// Controller — inject services only (never repositories directly)
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IRfidService _rfidService;

    public AdminController(IAdminService adminService, IRfidService rfidService)
    {
        _adminService = adminService;
        _rfidService = rfidService;
    }
}
```

---

## Transaction Rules

All multi-step DB operations use this exact pattern. No exceptions.

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Step 1 — repository call or direct EF operation
    // Step 2 — repository call or direct EF operation
    // Step 3 — repository call or direct EF operation

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (Exception)
{
    await transaction.RollbackAsync();
    throw;
}
```

**Rules:**
- `BeginTransactionAsync()` is always called inside the **service**, never the controller or repository
- `SaveChangesAsync()` is called **once** at the end of the try block — not after each step
- If any step throws, `RollbackAsync()` runs and the exception propagates up to the controller
- Single-step operations (one UPDATE, one INSERT) do not need a transaction — just `SaveChangesAsync()` in a try-catch

---

## Session Keys

Read these from `HttpContext.Session` inside the controller. Pass the extracted value as a parameter to the service — never pass `HttpContext` into a service.

| Key | Type | Set when |
|-----|------|----------|
| `UserId` | `Guid` | Any login |
| `Role` | `string` | Any login |
| `StudentId` | `Guid` | Student login |
| `AdminId` | `Guid` | Admin login |
| `ScannerId` | `Guid` | Scanner login |

```csharp
// Reading session in a controller action
var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId")!);

// Passing to service — correct
await _adminService.ApproveDeviceRequestAsync(requestId, adminId);

// WRONG — never inject IHttpContextAccessor into a service
```

---

## Authorization

Every controller class (except `AccountController`) must be decorated with `[Authorize]`. Role restriction goes on the class, not individual actions.

```csharp
[Authorize(Roles = "ADMIN")]
public class AdminController : Controller { }

[Authorize(Roles = "STUDENT")]
public class DeviceController : Controller { }

[Authorize(Roles = "SCANNER")]
public class ScanController : Controller { }
```

---

## ViewModel Mapping

Never pass an EF entity directly to a view. Map entity → ViewModel in the service layer before returning to the controller.

```csharp
// WRONG — raw entity in view
return View(device); // exposes navigation properties, lazy load issues

// CORRECT — map to ViewModel in service
public async Task<DeviceDetailViewModel> GetDeviceDetailAsync(Guid deviceId, Guid studentId)
{
    var device = await _deviceRepo.GetByIdWithTokenAsync(deviceId);

    // security check
    if (device.StudentId != studentId)
        throw new UnauthorizedAccessException();

    return new DeviceDetailViewModel
    {
        DeviceId    = device.Id,
        DeviceName  = device.DeviceName,
        Brand       = device.Brand,
        Model       = device.Model,
        Status      = device.Status,
        QrStatus    = device.ActiveToken?.Status,
        QrExpiresAt = device.ActiveToken?.ExpiresAt,
        Accessories = device.Accessories.Select(a => new AccessoryViewModel
        {
            Name     = a.AccessoryName,
            Quantity = a.Quantity
        }).ToList()
    };
}
```

---

## Error Handling Pattern

```csharp
// In controller — catch service exceptions, show user-friendly error
public async Task<IActionResult> Approve(Guid id)
{
    try
    {
        var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId")!);
        await _adminService.ApproveDeviceRequestAsync(id, adminId);
        TempData["Success"] = "Device request approved.";
        return RedirectToAction("Requests");
    }
    catch (InvalidOperationException ex)
    {
        TempData["Error"] = ex.Message;
        return RedirectToAction("RequestDetail", new { id });
    }
    catch (Exception)
    {
        TempData["Error"] = "Something went wrong. Please try again.";
        return RedirectToAction("Requests");
    }
}
```

---

## Naming Conventions

| Thing | Convention | Example |
|-------|-----------|---------|
| Classes & methods | PascalCase | `DeviceService`, `ApproveRequestAsync` |
| Variables & params | camelCase | `studentId`, `tokenValue` |
| DB column names | snake_case | `student_number`, `approved_at` |
| Razor views | PascalCase | `DeviceDetail.cshtml` |
| Interfaces | `I` prefix | `IDeviceService` |
| Async methods | `Async` suffix | `GetByIdAsync`, `ApproveDeviceRequestAsync` |
| ViewModels | `ViewModel` suffix | `DeviceDetailViewModel` |

---

## Service Map — What Each Service Owns

| Service | Owns |
|---------|------|
| `IDeviceService` | Student device registration, QR display, QR renewal (T5) |
| `IAdminService` | Device approval (T1), rejection, device + QR revocation, dashboard stats |
| `IRfidService` | RFID issuance (T2), semester renewal (T6), card replacement (T7) |
| `IScanService` | QR gate scan (T3), RFID gate scan (T4) |
| `IQrTokenService` | QR code image generation (QRCoder), token validation helpers |

---

## Repository Map — What Each Repository Owns

| Repository | Owns |
|------------|------|
| `IDeviceRequestRepository` | DeviceRequest + DeviceRequestAccessory queries |
| `IDeviceRepository` | Device + DeviceAccessory queries |
| `IStudentRepository` | Student + User + Course queries |
| `IQrTokenRepository` | QRToken queries |
| `IRfidCardRepository` | RFIDCard queries |
| `IScanLogRepository` | GateScanLog insert + read queries |
| `IScannerRepository` | Scanner + api_key queries |

---

## How to Implement a New Feature — Checklist

When adding any new feature, follow these steps in order:

1. **Repository first** — add the query method to the interface and implement it
2. **Service next** — add the business method to the service interface and implement it, calling the repository
3. **ViewModel** — create or update the ViewModel the controller will pass to the view
4. **Controller** — add the action, inject the service, call it, return the ViewModel to the view
5. **View** — build the Razor view using the ViewModel
6. **Register** — if new service/repo was created, register it in `Program.cs`
7. **Test** — verify the happy path, then test the error/rollback path

---

## What Not to Do

- ❌ Never call `_context` directly inside a controller
- ❌ Never call `SaveChangesAsync()` inside a repository
- ❌ Never pass a raw EF entity to a Razor view
- ❌ Never put `BeginTransactionAsync()` in a controller or repository
- ❌ Never inject `IHttpContextAccessor` into a service
- ❌ Never create a repository method that does more than one thing
- ❌ Never skip the interface — every service and repository needs one
- ❌ Never hardcode IDs, role strings, or magic values — use enums and session keys

---

*PaScan SKILL.md — Architecture reference for AI-assisted development*
*ASP.NET Core MVC · Repository + Service Pattern · EF Core · Azure SQL*