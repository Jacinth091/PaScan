# Architecture Migration Plan
### PaScan — Moving from Fat Controllers to Repository + Service Pattern
> ASP.NET Core MVC · SRP · Contracts · Repositories · Services

---

## The Problem

Currently, all business logic, validation rules, database queries, and transaction handling are written directly inside controller action methods. This makes the code hard to track, hard to extend, and easy to break when touching shared logic.

**What fat controller code looks like:**
```csharp
// AdminController — everything crammed in one place
public async Task<IActionResult> Approve(Guid id)
{
    var request = await _context.DeviceRequests
        .Include(r => r.Accessories)
        .FirstOrDefaultAsync(r => r.Id == id); // ← direct DB query in controller

    if (request.Status != RegisterStatus.PENDING) // ← business rule in controller
        return BadRequest();

    using var transaction = await _context.Database.BeginTransactionAsync(); // ← transaction in controller
    try { ... }
}
```

**What it should look like after migration:**
```csharp
// AdminController — thin, just coordinates
public async Task<IActionResult> Approve(Guid id)
{
    var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId")!);
    await _adminService.ApproveDeviceRequestAsync(id, adminId);
    return RedirectToAction("Requests");
}
```

---

## Target Architecture

```
HTTP Request
    ↓
Controller          ← reads session, validates ModelState, calls service, returns View
    ↓
Service             ← business logic, validation rules, transaction orchestration
    ↓
Repository          ← EF Core queries, returns entities
    ↓
AppDbContext        ← EF Core, Azure SQL
```

Each layer only talks to the layer directly below it. Controllers never touch repositories. Services never touch HttpContext.

---

## Phase 1 — Set Up the Skeleton (Do This First, Before Touching Any Logic)

This phase creates empty files and wires up dependency injection. No logic is moved yet.

### Step 1.1 — Create folder structure

Create these folders if they don't exist:
```
/Services/Interfaces/
/Repositories/Interfaces/
/Models/ViewModels/Student/
/Models/ViewModels/Admin/
/Models/ViewModels/Scan/
```

### Step 1.2 — Create repository interfaces

Create one interface file per entity. Start with the entities your current student module already uses.

**`/Repositories/Interfaces/IDeviceRequestRepository.cs`**
```csharp
public interface IDeviceRequestRepository
{
    Task<DeviceRequest?> GetByIdWithAccessoriesAsync(Guid id);
    Task<List<DeviceRequest>> GetByStudentIdAsync(Guid studentId);
    Task<List<DeviceRequest>> GetAllPendingAsync();
    Task AddAsync(DeviceRequest request);
    Task AddAccessoryAsync(DeviceRequestAccessory accessory);
}
```

**`/Repositories/Interfaces/IDeviceRepository.cs`**
```csharp
public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id);
    Task<Device?> GetByIdWithAccessoriesAndTokenAsync(Guid id);
    Task<List<Device>> GetByStudentIdAsync(Guid studentId);
    Task<List<Device>> GetAllActiveAsync();
    Task AddAsync(Device device);
    Task AddAccessoryAsync(DeviceAccessory accessory);
}
```

**`/Repositories/Interfaces/IStudentRepository.cs`**
```csharp
public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id);
    Task<Student?> GetByIdWithUserAsync(Guid id);
    Task<List<Student>> GetAllAsync();
    Task UpdateAsync(Student student);
}
```

**`/Repositories/Interfaces/IQrTokenRepository.cs`**
```csharp
public interface IQrTokenRepository
{
    Task<QRToken?> GetActiveByDeviceIdAsync(Guid deviceId);
    Task<QRToken?> GetByTokenValueAsync(string tokenValue);
    Task AddAsync(QRToken token);
    Task UpdateAsync(QRToken token);
}
```

**`/Repositories/Interfaces/IRfidCardRepository.cs`**
```csharp
public interface IRfidCardRepository
{
    Task<RFIDCard?> GetActiveByStudentIdAsync(Guid studentId);
    Task<RFIDCard?> GetActiveByCardUidAsync(string cardUid);
    Task AddAsync(RFIDCard card);
    Task UpdateAsync(RFIDCard card);
}
```

**`/Repositories/Interfaces/IScanLogRepository.cs`**
```csharp
public interface IScanLogRepository
{
    Task AddAsync(GateScanLog log);
    Task AddRangeAsync(List<GateScanLog> logs);
    Task<List<GateScanLog>> GetRecentAsync(int count);
}
```

**`/Repositories/Interfaces/IScannerRepository.cs`**
```csharp
public interface IScannerRepository
{
    Task<Scanner?> GetByUserIdAsync(Guid userId);
    Task<Scanner?> GetByApiKeyAsync(string apiKey);
    Task<Scanner?> GetByIdAsync(Guid id);
    Task<List<Scanner>> GetAllAsync();
    Task AddAsync(Scanner scanner);
    Task UpdateAsync(Scanner scanner);
}
```

### Step 1.3 — Create service interfaces

**`/Services/Interfaces/IDeviceService.cs`**
```csharp
public interface IDeviceService
{
    Task<StudentDashboardViewModel> GetStudentDashboardAsync(Guid studentId);
    Task<DeviceDetailViewModel> GetDeviceDetailAsync(Guid deviceId, Guid studentId);
    Task SubmitDeviceRequestAsync(DeviceRequestViewModel vm, Guid studentId);
    Task RenewQrAsync(Guid deviceId, Guid studentId);  // T5 — bonus
}
```

**`/Services/Interfaces/IAdminService.cs`**
```csharp
public interface IAdminService
{
    Task<List<AdminRequestListViewModel>> GetPendingRequestsAsync();
    Task<AdminRequestDetailViewModel> GetRequestDetailAsync(Guid requestId);
    Task ApproveDeviceRequestAsync(Guid requestId, Guid adminId);   // T1
    Task RejectDeviceRequestAsync(Guid requestId, Guid adminId, string reason);
    Task<AdminDeviceDetailViewModel> GetDeviceDetailAsync(Guid deviceId);
    Task RevokeQrTokenAsync(Guid deviceId, RevocationReason reason);
    Task RevokeDeviceAsync(Guid deviceId);
    Task<AdminDashboardViewModel> GetDashboardStatsAsync();
    Task<AdminStudentDetailViewModel> GetStudentDetailAsync(Guid studentId);
}
```

**`/Services/Interfaces/IRfidService.cs`**
```csharp
public interface IRfidService
{
    Task IssueCardAsync(Guid studentId, Guid adminId, string cardUid, DateTime semesterExpiresAt);  // T2
    Task RenewSemesterAsync(Guid studentId, Guid adminId, DateTime newExpiresAt);                   // T6 — bonus
    Task ReplaceCardAsync(Guid studentId, Guid adminId, string newCardUid, InvalidationReason reason); // T7 — bonus
}
```

**`/Services/Interfaces/IScanService.cs`**
```csharp
public interface IScanService
{
    Task<QrScanResultViewModel> ValidateQrScanAsync(string tokenValue, Guid scannerId);   // T3
    Task<RfidScanResultViewModel> ValidateRfidScanAsync(string cardUid, string apiKey);  // T4
}
```

**`/Services/Interfaces/IQrTokenService.cs`**
```csharp
public interface IQrTokenService
{
    byte[] GenerateQrCodeImage(string tokenValue);
}
```

### Step 1.4 — Create empty concrete implementations

Create empty classes that implement the interfaces. Leave method bodies throwing `NotImplementedException` for now — you will fill them in during Phase 2.

```csharp
// /Repositories/DeviceRequestRepository.cs
public class DeviceRequestRepository : IDeviceRequestRepository
{
    private readonly AppDbContext _context;
    public DeviceRequestRepository(AppDbContext context) => _context = context;

    public Task<DeviceRequest?> GetByIdWithAccessoriesAsync(Guid id)
        => throw new NotImplementedException();
    // ... rest of methods
}
```

Repeat this pattern for all 7 repositories and all 5 services.

### Step 1.5 — Register everything in Program.cs

```csharp
// Repositories
builder.Services.AddScoped<IDeviceRequestRepository, DeviceRequestRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IQrTokenRepository, QrTokenRepository>();
builder.Services.AddScoped<IRfidCardRepository, RfidCardRepository>();
builder.Services.AddScoped<IScanLogRepository, ScanLogRepository>();
builder.Services.AddScoped<IScannerRepository, ScannerRepository>();

// Services
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRfidService, RfidService>();
builder.Services.AddScoped<IScanService, ScanService>();
builder.Services.AddScoped<IQrTokenService, QrTokenService>();
```

> ✅ Checkpoint: Project should still build and run with no errors before moving to Phase 2.

---

## Phase 2 — Migrate the Student Module (Already Started)

Migrate the existing student device functionality first since it's already partially built. This gives you a working reference pattern before touching admin.

### Step 2.1 — Implement DeviceRequestRepository

Move all `_context.DeviceRequests...` queries out of `DeviceController` into the repository:

```csharp
public async Task<DeviceRequest?> GetByIdWithAccessoriesAsync(Guid id)
{
    return await _context.DeviceRequests
        .Include(r => r.Accessories)
        .Include(r => r.Student).ThenInclude(s => s.User)
        .FirstOrDefaultAsync(r => r.Id == id);
}

public async Task<List<DeviceRequest>> GetByStudentIdAsync(Guid studentId)
{
    return await _context.DeviceRequests
        .Where(r => r.StudentId == studentId)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();
}

public async Task AddAsync(DeviceRequest request)
{
    await _context.DeviceRequests.AddAsync(request);
    // NOTE: Do NOT call SaveChangesAsync here — service controls when to save
}
```

### Step 2.2 — Implement DeviceService

Move the business logic from `DeviceController` into `DeviceService`. The service maps entities to ViewModels before returning.

```csharp
public async Task SubmitDeviceRequestAsync(DeviceRequestViewModel vm, Guid studentId)
{
    // Business rule: student cannot submit duplicate serial number
    var existing = await _deviceRepo.GetBySerialNumberAndStudentAsync(vm.SerialNumber, studentId);
    if (existing != null)
        throw new InvalidOperationException("A device with this serial number is already registered.");

    var request = new DeviceRequest
    {
        StudentId    = studentId,
        DeviceName   = vm.DeviceName,
        DeviceType   = vm.DeviceType,
        Brand        = vm.Brand,
        Model        = vm.Model,
        SerialNumber = vm.SerialNumber,
        Status       = RegisterStatus.PENDING,
        // ... rest of fields
    };

    await _deviceRequestRepo.AddAsync(request);

    foreach (var acc in vm.Accessories)
    {
        await _deviceRequestRepo.AddAccessoryAsync(new DeviceRequestAccessory
        {
            DeviceRequestId = request.Id,
            AccessoryName   = acc.AccessoryName,
            Quantity        = acc.Quantity
        });
    }

    await _context.SaveChangesAsync();
}
```

### Step 2.3 — Slim down DeviceController

Replace all the logic in the controller with service calls:

```csharp
[Authorize(Roles = "STUDENT")]
public class DeviceController : Controller
{
    private readonly IDeviceService _deviceService;

    public DeviceController(IDeviceService deviceService)
        => _deviceService = deviceService;

    public async Task<IActionResult> Dashboard()
    {
        var studentId = Guid.Parse(HttpContext.Session.GetString("StudentId")!);
        var vm = await _deviceService.GetStudentDashboardAsync(studentId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(DeviceRequestViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            var studentId = Guid.Parse(HttpContext.Session.GetString("StudentId")!);
            await _deviceService.SubmitDeviceRequestAsync(vm, studentId);
            TempData["Success"] = "Device registered. Awaiting admin approval.";
            return RedirectToAction("Dashboard");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }
}
```

> ✅ Checkpoint: Student registration and device list must work end-to-end before moving to Phase 3.

---

## Phase 3 — Implement Admin Module (Using the Pattern)

Now build the admin module correctly from the start — no migration needed, just implement.

### Step 3.1 — Implement AdminService.ApproveDeviceRequestAsync (T1)

```csharp
public async Task ApproveDeviceRequestAsync(Guid requestId, Guid adminId)
{
    var request = await _deviceRequestRepo.GetByIdWithAccessoriesAsync(requestId)
        ?? throw new InvalidOperationException("Request not found.");

    if (request.Status != RegisterStatus.PENDING)
        throw new InvalidOperationException("This request has already been reviewed.");

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Step 1 — Update request
        request.Status     = RegisterStatus.APPROVED;
        request.ReviewedBy = adminId;
        request.ReviewedAt = DateTime.UtcNow;

        // Step 2 — Create device
        var device = new Device
        {
            StudentId          = request.StudentId,
            OriginalRequestId  = request.Id,
            DeviceName         = request.DeviceName,
            DeviceType         = request.DeviceType,
            Brand              = request.Brand,
            Model              = request.Model,
            SerialNumber       = request.SerialNumber,
            OperatingSystem    = request.OperatingSystem,
            Color              = request.Color,
            Processor          = request.Processor,
            Motherboard        = request.Motherboard,
            Memory             = request.Memory,
            Storage            = request.Storage,
            MonitorSize        = request.MonitorSize,
            Casing             = request.Casing,
            HasCdRom           = request.HasCdRom,
            Purpose            = request.Purpose,
            Status             = DeviceStatus.ACTIVE,
            QrRenewalCount     = 0
        };
        await _deviceRepo.AddAsync(device);
        await _context.SaveChangesAsync(); // flush to get device.Id

        // Step 3 — Copy accessories
        foreach (var acc in request.Accessories)
        {
            await _deviceRepo.AddAccessoryAsync(new DeviceAccessory
            {
                DeviceId      = device.Id,
                AccessoryName = acc.AccessoryName,
                Quantity      = acc.Quantity
            });
        }

        // Step 4 — Issue QR token
        await _qrTokenRepo.AddAsync(new QRToken
        {
            DeviceId      = device.Id,
            StudentId     = request.StudentId,
            TokenValue    = Guid.NewGuid().ToString(),
            IssuedAt      = DateTime.UtcNow,
            ExpiresAt     = DateTime.UtcNow.AddDays(30),
            Status        = TokenStatus.ACTIVE,
            RenewalNumber = 1,
            RenewedFrom   = null
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Step 3.2 — Implement remaining AdminService methods

Follow the same structure. Each method: validate inputs → call repositories → map to ViewModel → return.

Methods to implement in order:
1. `GetPendingRequestsAsync()` — list query, map to `AdminRequestListViewModel`
2. `GetRequestDetailAsync()` — single query with accessories, map to `AdminRequestDetailViewModel`
3. `RejectDeviceRequestAsync()` — single UPDATE, no transaction needed
4. `GetDeviceDetailAsync()` — device + token + accessories, map to `AdminDeviceDetailViewModel`
5. `RevokeQrTokenAsync()` — single UPDATE on active QRToken
6. `GetDashboardStatsAsync()` — aggregate counts, map to `AdminDashboardViewModel`

### Step 3.3 — Implement RfidService (T2)

```csharp
public async Task IssueCardAsync(Guid studentId, Guid adminId, string cardUid, DateTime semesterExpiresAt)
{
    var student = await _studentRepo.GetByIdAsync(studentId)
        ?? throw new InvalidOperationException("Student not found.");

    var existing = await _rfidCardRepo.GetActiveByStudentIdAsync(studentId);
    if (existing != null)
        throw new InvalidOperationException("Student already has an active RFID card.");

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        await _rfidCardRepo.AddAsync(new RFIDCard
        {
            StudentId         = studentId,
            CardUid           = cardUid,
            IssuedAt          = DateTime.UtcNow,
            SemesterExpiresAt = semesterExpiresAt,
            Status            = TokenStatus.ACTIVE,
            RenewalNumber     = 1,
            IsReplacement     = false,
            IssuedBy          = adminId
        });

        student.IsRfidEnabled   = true;
        student.RfidRenewalCount = 0;
        await _studentRepo.UpdateAsync(student);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Step 3.4 — Wire up AdminController

```csharp
[Authorize(Roles = "ADMIN")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IRfidService _rfidService;

    public AdminController(IAdminService adminService, IRfidService rfidService)
    {
        _adminService = adminService;
        _rfidService  = rfidService;
    }

    // --- Requests ---

    public async Task<IActionResult> Requests()
    {
        var vm = await _adminService.GetPendingRequestsAsync();
        return View(vm);
    }

    public async Task<IActionResult> RequestDetail(Guid id)
    {
        var vm = await _adminService.GetRequestDetailAsync(id);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId")!);
            await _adminService.ApproveDeviceRequestAsync(id, adminId);
            TempData["Success"] = "Device request approved.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception) { TempData["Error"] = "Approval failed. Please try again."; }
        return RedirectToAction("Requests");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string rejectionReason)
    {
        try
        {
            var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId")!);
            await _adminService.RejectDeviceRequestAsync(id, adminId, rejectionReason);
            TempData["Success"] = "Request rejected.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Requests");
    }

    // --- RFID ---

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueRfid(Guid studentId, IssueRfidViewModel vm)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("StudentDetail", new { id = studentId });

        try
        {
            var adminId = Guid.Parse(HttpContext.Session.GetString("AdminId")!);
            await _rfidService.IssueCardAsync(studentId, adminId, vm.CardUid, vm.SemesterExpiresAt);
            TempData["Success"] = "RFID card issued.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("StudentDetail", new { id = studentId });
    }
}
```

> ✅ Checkpoint: Full admin approval flow (T1) and RFID issuance (T2) must work end-to-end.

---

## Phase 4 — Implement Scan Module

### Step 4.1 — Implement ScanService (T3 + T4)

Both scan transactions live in `ScanService`. The service handles all validation and logging — the controller only calls it and returns JSON.

```csharp
public async Task<QrScanResultViewModel> ValidateQrScanAsync(string tokenValue, Guid scannerId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var scanner = await _scannerRepo.GetByIdAsync(scannerId);
        if (scanner == null || scanner.Status != ScannerStatus.ACTIVE
            || (scanner.ScannerType != ScannerType.QR && scanner.ScannerType != ScannerType.BOTH))
        {
            await transaction.RollbackAsync();
            return new QrScanResultViewModel { Allowed = false, DenialReason = "SCANNER_INACTIVE" };
        }

        var token  = await _qrTokenRepo.GetByTokenValueAsync(tokenValue);
        var device = token != null ? await _deviceRepo.GetByIdAsync(token.DeviceId) : null;

        string? denialReason = null;
        if (token == null || device == null) denialReason = "DEVICE_NOT_FOUND";
        else if (token.Status == TokenStatus.REVOKED) denialReason = "REVOKED";
        else if (token.Status == TokenStatus.EXPIRED || token.ExpiresAt < DateTime.UtcNow) denialReason = "EXPIRED";
        else if (device.Status != DeviceStatus.ACTIVE) denialReason = "NOT_APPROVED";

        await _scanLogRepo.AddAsync(new GateScanLog
        {
            DeviceId     = device?.Id ?? Guid.Empty,
            QrTokenId    = token?.Id,
            RfidCardId   = null,
            ScanType     = ScanType.QR,
            ScannerId    = scannerId,
            ScannedAt    = DateTime.UtcNow,
            IsAllowed    = denialReason == null,
            DenialReason = denialReason
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new QrScanResultViewModel { Allowed = denialReason == null, DenialReason = denialReason };
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## Phase 5 — Bonus Features (Time Permitting)

Implement only after Phases 1–4 are fully working.

| Feature | Service method | Transaction |
|---------|---------------|-------------|
| QR Renewal (student) | `IDeviceService.RenewQrAsync()` | T5 |
| RFID Semester Renewal | `IRfidService.RenewSemesterAsync()` | T6 |
| Lost Card Replacement | `IRfidService.ReplaceCardAsync()` | T7 |
| Scanner Management | Extend `IAdminService` | None |
| Admin Dashboard Stats | `IAdminService.GetDashboardStatsAsync()` | None |

---

## Summary — Build Order

```
Phase 1  Skeleton: folders → interfaces → empty impls → Program.cs registration
Phase 2  Student module: DeviceRequestRepo → DeviceRepo → DeviceService → slim DeviceController
Phase 3  Admin module:   AdminService (T1) → RfidService (T2) → AdminController
Phase 4  Scan module:    ScanService (T3 + T4) → ScanController
Phase 5  Bonus:          QR renewal (T5) → RFID renewal (T6) → card replace (T7)
```

Each phase must compile and run before starting the next.

---

*PaScan Architecture Migration Plan*
*ELNET Final Project · ASP.NET Core MVC · Repository + Service Pattern*