# 🎓 Guided Learning: Student Module (M3 + M5 + M9 + M11)
### PaScan — Campus Device Entry Management System
> ASP.NET Core MVC | Entity Framework Core | Azure SQL

---

## What You're Building

The Student Module covers everything a student can see and do in PaScan:

| Module | Feature | Priority |
|---|---|---|
| **M3** | Device Registration — submit the digital pink slip | 🔴 Required |
| **M9** | Student Dashboard — view all requests, device statuses, and RFID status | 🔴 Required |
| **M5** | QR Token Display — view and display the QR code per approved device | 🔴 Required |
| **M11** | QR Renewal — renew an expired QR token from the device page | 🟡 Bonus |

> The student never manages RFID directly. The admin issues and renews RFID cards (M8). The student only **sees** their RFID status on the dashboard.

By the end, a student should be able to:
1. Log in → land on their dashboard (M9)
2. See their RFID card status on the dashboard (M9)
3. Click "Register a Device" → fill out the form → submit (M3)
4. See the request listed on the dashboard with status `PENDING` (M9)
5. After admin approves → navigate to the approved device page and see their QR code (M5)
6. When QR expires → click Renew to get a new one (M11, bonus)

---

## Before You Start — Checklist

Make sure these are done **before** you touch any code here:

- [x] **M1 is complete** — the database is live and seeded (tables exist in Azure SQL)
- [x] **M2 is complete** — student can log in at `/api/auth/login/student` and the session stores `UserId`, `Role`, and `ProfileId` via cookie claims
- [ ] You know which `StudentId` (UUID) belongs to your test student account from the seed data

> 💡 **Why does this matter?** The device registration form needs to know *which student* is submitting it. That comes from the session set during login (M2). If the session isn't set correctly, you'll get a null reference error in your controller.

---

## Current Project Architecture

Before writing code, understand how your project is structured:

```
PaScan/
├── Controllers/
│   ├── AuthController.cs       ← M2 (✅ Done)
│   ├── DeviceController.cs     ← M3 + M5 + M11 (✅ M3 implemented, M5+M11 to add)
│   ├── StudentController.cs    ← M9 (⏳ Placeholder only)
│   ├── AdminController.cs      ← M4/M8/M10 (⏳ Placeholder)
│   └── ScanController.cs       ← M6/M7 (⏳ Placeholder)
├── Models/
│   ├── BaseEntity.cs           ← All entities inherit from this
│   ├── Student.cs              ← ✅ Done
│   ├── DeviceRequest.cs        ← ✅ Done
│   ├── DeviceRequestAccessory.cs ← ✅ Done
│   ├── Device.cs               ← ✅ Done (created on admin approval)
│   ├── QRToken.cs              ← ✅ Done (generated on approval, renewed by student)
│   ├── RFIDCard.cs             ← ✅ Done (issued and managed by admin)
│   └── ViewModels/
│       ├── DeviceRequestViewModels.cs  ← ✅ Done
│       ├── StudentDashboardViewModel.cs ← ✅ Done (needs DeviceListItem extended)
│       └── AccountViewModels.cs         ← ✅ Done
├── Enums/
│   ├── DeviceType.cs           ← LAPTOP, DESKTOP, TABLET, PHONE
│   ├── RegisterStatus.cs       ← PENDING, APPROVED, REJECTED
│   ├── DeviceStatus.cs         ← ACTIVE, REVOKED, EXPIRED
│   └── TokenStatus.cs          ← ACTIVE, EXPIRED, REVOKED
├── Data/
│   ├── AppDbContext.cs          ← ✅ Done
│   └── DbSeeder.cs             ← ✅ Done
└── Views/
    ├── Auth/                    ← ✅ Login views done
    ├── Device/
    │   ├── Register.cshtml      ← ⏳ TODO (M3)
    │   ├── Details.cshtml       ← ⏳ TODO (M3 — request detail, PENDING/REJECTED)
    │   └── DeviceDetail.cshtml  ← ⏳ TODO (M5 — approved device page with QR)
    └── Student/
        └── Dashboard.cshtml     ← ⏳ TODO (M9)
```

---

## Key Foundation: BaseEntity

Every entity in PaScan inherits from `BaseEntity`, which gives you soft-delete and timestamps for free:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }  // soft-delete: null = active
}
```

> 💡 `AppDbContext` has **global query filters** that automatically exclude rows where `DeletedAt != null`. You never need to add `WHERE DeletedAt IS NULL` manually in LINQ queries.

---

## Key Foundation: Routing

Your `Program.cs` uses an `ApiPrefixConvention` that prepends `/api` to all controller routes (except `HomeController`). So:

- `DeviceController` has `[Route("device")]` → actual URL is `/api/device/...`
- `AuthController` has `[Route("auth")]` → actual URL is `/api/auth/...`
- `StudentController` has no `[Route]` → uses convention: `/api/student/...`

---

## Step 1 — Understand the Data Flow

Before touching code, trace what happens across all student-facing features:

### M3 — Device Registration
```
[Student fills form in browser]
        ↓
[POST /api/device/student/register]
        ↓
[DeviceController.Register (POST)]
        ↓
[Read StudentId from session]
        ↓
[INSERT DeviceRequest row]  ← one row
[INSERT DeviceRequestAccessory rows]  ← one per accessory
        ↓
[Redirect to /api/device/student/{newRequestId}]
        ↓
[Student sees their request with status = PENDING]
```

### M5 — QR Code Display (depends on M4 — Admin Approval)
```
[Admin approves request → T1 runs → Device + QRToken rows created]
        ↓
[Student navigates to /api/device/student/approved/{deviceId}]
        ↓
[DeviceController loads Device + active QRToken]
        ↓
[QRCoder generates PNG image from token_value (GUID string)]
        ↓
[View shows QR image, expiry date, token status]
        ↓
[If TokenStatus = EXPIRED → show Renew button]
```

### M11 — QR Renewal (Bonus — T5)
```
[Student clicks Renew on expired QR page]
        ↓
[POST /api/device/student/approved/{deviceId}/renew-qr]
        ↓
[DeviceController.RenewQr (POST)]
        ↓
[BEGIN TRANSACTION]
  Step 1: UPDATE old QRToken → status = EXPIRED
  Step 2: INSERT new QRToken → new GUID, expires_at = now + 30 days,
                               renewal_number = old + 1, renewed_from = old.id
  Step 3: UPDATE Device → qr_renewal_count += 1
[COMMIT]
        ↓
[Redirect back to /api/device/student/approved/{deviceId}]
        ↓
[Student sees fresh QR code]
```

> ⚠️ M11 uses `BeginTransaction()` — it's the first explicit transaction on the student side. M3 does not need it (simple inserts only).

---

## Step 2 — Entity Models (✅ Already Done)

### DeviceRequest Entity

File: `/Models/DeviceRequest.cs`

```csharp
public class DeviceRequest : BaseEntity
{
    [Required] public Guid StudentId { get; set; }

    [Required] [StringLength(500)] public string Purpose { get; set; } = null!;
    [Required] [StringLength(100)] public string DeviceName { get; set; } = null!;
    [Required] public DeviceType DeviceType { get; set; }
    [Required] [StringLength(50)]  public string Brand { get; set; } = null!;
    [Required] [StringLength(50)]  public string Model { get; set; } = null!;
    [Required] [StringLength(100)] public string SerialNumber { get; set; } = null!;

    // Hardware specs (nullable — not all device types have these)
    public string? OperatingSystem { get; set; }
    public string? Color { get; set; }
    public string? Processor { get; set; }
    public string? Motherboard { get; set; }
    public string? Memory { get; set; }
    public string? Storage { get; set; }
    public string? MonitorSize { get; set; }
    public string? Casing { get; set; }
    public bool HasCdRom { get; set; }

    // Review fields (populated by admin in M4)
    [Required] public RegisterStatus Status { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Remarks { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
    public Admin? AdminReviewer { get; set; }
    public ICollection<DeviceRequestAccessory> Accessories { get; set; } = new List<DeviceRequestAccessory>();
    public Device? ApprovedDevice { get; set; }
}
```

### DeviceRequestAccessory Entity

File: `/Models/DeviceRequestAccessory.cs`

```csharp
public class DeviceRequestAccessory : BaseEntity
{
    [Required] public Guid DeviceRequestId { get; set; }
    [Required] [StringLength(100)] public string AccessoryName { get; set; } = null!;
    [Range(1, 100)] public int Quantity { get; set; }

    // Navigation
    public DeviceRequest DeviceRequest { get; set; } = null!;
}
```

> 💡 **Why a separate table for accessories?** One device request can have many accessories (1-to-many). Storing them as a comma-separated string would make querying and validation impossible. The separate table lets you enforce `[Range(1, 100)]` per row and cleanly include them with `.Include(r => r.Accessories)`.

---

## Step 3 — ViewModel (✅ Already Done)

File: `/Models/ViewModels/DeviceRequestViewModels.cs`

```csharp
public class DeviceRequestViewModel
{
    // Tracking properties (populated by system/details view, not by student)
    public Guid? Id { get; set; }
    public RegisterStatus? Status { get; set; }
    public DateTime? CreatedAt { get; set; }

    [Required] [StringLength(500)] public string Purpose { get; set; } = null!;
    [Required] [StringLength(100)] public string DeviceName { get; set; } = null!;
    [Required] public DeviceType DeviceType { get; set; }
    [Required] [StringLength(50)]  public string Brand { get; set; } = null!;
    [Required] [StringLength(50)]  public string Model { get; set; } = null!;
    [Required] [StringLength(100)] public string SerialNumber { get; set; } = null!;

    public string? OperatingSystem { get; set; }
    public string? Color { get; set; }
    public string? Processor { get; set; }
    public string? Motherboard { get; set; }
    public string? Memory { get; set; }
    public string? Storage { get; set; }
    public string? MonitorSize { get; set; }
    public string? Casing { get; set; }
    public bool HasCdRom { get; set; }

    public List<AccessoryViewModel> Accessories { get; set; } = new();
}

public class AccessoryViewModel
{
    [Required] [StringLength(100)] public string AccessoryName { get; set; } = null!;
    [Range(1, 100)] public int Quantity { get; set; } = 1;
}
```

> 💡 **Why a separate ViewModel and not the entity directly?** Your `DeviceRequest` entity has fields like `StudentId`, `Status`, `ReviewedBy`, and `RejectionReason` that the student should never fill in themselves. Using the entity directly on a form is a security risk — a user could POST those hidden fields. The ViewModel only exposes what you want the student to control.

---

## Step 4 — DeviceController (✅ Implemented)

File: `/Controllers/DeviceController.cs`

This controller is protected by `[Authorize(Roles = "STUDENT")]` and uses explicit `[Route("device")]` with the api prefix convention → all routes are under `/api/device/...`.

### GET — Show the registration form

```csharp
[HttpGet("student/register")]
public IActionResult Register()
{
    return View(new DeviceRequestViewModel());
}
```

### POST — Process the submitted form

```csharp
[HttpPost("student/register")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(DeviceRequestViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    // 1) Read StudentId from session
    var studentIdStr = HttpContext.Session.GetString("StudentId");
    if (studentIdStr == null || !Guid.TryParse(studentIdStr, out var studentId))
    {
        return RedirectToAction("Login", "Account");
    }

    // 2) Check serial number uniqueness (including soft-delete filter via DeletedAt)
    bool exists = await _context.DeviceRequests
        .AnyAsync(d => d.StudentId == studentId
                    && d.SerialNumber == model.SerialNumber
                    && d.DeletedAt == null);
    if (exists)
    {
        ModelState.AddModelError("SerialNumber", "You have already registered this device.");
        return View(model);
    }

    // 3) Build & insert DeviceRequest
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

    // 4) Insert accessories (skip empty rows from JS dynamic list)
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
}
```

> 💡 **Why no `BeginTransaction()` here?** This is a simple batch insert — all rows are new. EF Core's `SaveChangesAsync()` wraps all pending changes in an implicit transaction. The explicit `BeginTransaction()` pattern is required for **multi-step operations that mix inserts with updates** (like T1 Approval in M4).

### GET — View a specific request's details

```csharp
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

    return View(request);  // passes the entity directly (not ViewModel) for detail view
}
```

> 💡 **Why filter by `StudentId` in the query?** If you only filter by `id`, any student who guesses another student's request UUID can view it. Always scope queries to the current user's ID on student-facing pages.

---

## Step 5 — Create the Registration Form View (⏳ TODO)

You need to create: `Views/Device/Register.cshtml`

```html
@model PaScan.Models.ViewModels.DeviceRequestViewModel

<h2>Register a Device</h2>

<form asp-action="Register" method="post">
    @Html.AntiForgeryToken()

    <div>
        <label asp-for="Purpose"></label>
        <textarea asp-for="Purpose" class="form-control"></textarea>
        <span asp-validation-for="Purpose" class="text-danger"></span>
    </div>

    <div>
        <label asp-for="DeviceName"></label>
        <input asp-for="DeviceName" class="form-control" />
        <span asp-validation-for="DeviceName" class="text-danger"></span>
    </div>

    <div>
        <label asp-for="DeviceType"></label>
        <select asp-for="DeviceType"
                asp-items="Html.GetEnumSelectList<PaScan.Enums.DeviceType>()"
                id="deviceTypeSelect"
                class="form-control">
            <option value="">-- Select Type --</option>
        </select>
        <span asp-validation-for="DeviceType" class="text-danger"></span>
    </div>

    <div>
        <label asp-for="Brand"></label>
        <input asp-for="Brand" class="form-control" />
        <span asp-validation-for="Brand" class="text-danger"></span>
    </div>

    <div>
        <label asp-for="Model"></label>
        <input asp-for="Model" class="form-control" />
        <span asp-validation-for="Model" class="text-danger"></span>
    </div>

    <div>
        <label asp-for="SerialNumber"></label>
        <input asp-for="SerialNumber" class="form-control" />
        <span asp-validation-for="SerialNumber" class="text-danger"></span>
    </div>

    <div>
        <label asp-for="OperatingSystem"></label>
        <input asp-for="OperatingSystem" class="form-control" />
    </div>

    <div>
        <label asp-for="Color"></label>
        <input asp-for="Color" class="form-control" />
    </div>

    <!-- Hardware specs (show only for LAPTOP / DESKTOP) -->
    <div id="hardwareSpecs" style="display:none;">
        <h4>Hardware Specifications</h4>

        <label asp-for="Processor"></label>
        <input asp-for="Processor" class="form-control" />

        <label asp-for="Motherboard"></label>
        <input asp-for="Motherboard" class="form-control" />

        <label asp-for="Memory"></label>
        <input asp-for="Memory" class="form-control" />

        <label asp-for="Storage"></label>
        <input asp-for="Storage" class="form-control" />

        <label asp-for="MonitorSize"></label>
        <input asp-for="MonitorSize" class="form-control" />

        <label asp-for="Casing"></label>
        <input asp-for="Casing" class="form-control" />

        <label asp-for="HasCdRom"></label>
        <input asp-for="HasCdRom" type="checkbox" />
    </div>

    <!-- Accessories section -->
    <h4>Accessories</h4>
    <div id="accessoryList"></div>
    <button type="button" onclick="addAccessory()">+ Add Accessory</button>

    <br />
    <button type="submit" class="btn btn-primary">Submit Registration</button>
</form>

@section Scripts {
<script>
    // Show/hide hardware specs based on device type
    document.getElementById('deviceTypeSelect').addEventListener('change', function () {
        var val = this.value;
        var show = val === 'LAPTOP' || val === 'DESKTOP';
        document.getElementById('hardwareSpecs').style.display = show ? 'block' : 'none';
    });

    // Dynamic accessory rows
    var accessoryIndex = 0;
    function addAccessory() {
        var div = document.createElement('div');
        div.innerHTML = `
            <input name="Accessories[${accessoryIndex}].AccessoryName" placeholder="Accessory name" />
            <input name="Accessories[${accessoryIndex}].Quantity" type="number" value="1" min="1" />
            <button type="button" onclick="this.parentElement.remove()">Remove</button>
        `;
        document.getElementById('accessoryList').appendChild(div);
        accessoryIndex++;
    }
</script>
}
```

> 💡 **How does ASP.NET Core bind the accessory list?** The `name` attributes use the pattern `Accessories[0].AccessoryName`, `Accessories[0].Quantity`, etc. ASP.NET Core's model binder reads these indexed names and populates the `List<AccessoryViewModel>` automatically. The JavaScript counter `accessoryIndex` must never repeat — duplicate indexes are silently dropped.

---

## Step 6 — Create the Request Detail View (⏳ TODO)

You need to create: `Views/Device/Details.cshtml`

Since the controller passes the entity directly (`return View(request)`), your model type is `DeviceRequest`:

```html
@model PaScan.Models.DeviceRequest

<h2>Device Request — @Model.DeviceName</h2>

<p>Status: <strong>@Model.Status</strong></p>
<p>Submitted: @Model.CreatedAt.ToString("MMM dd, yyyy")</p>

<table class="table">
    <tr><th>Brand</th><td>@Model.Brand</td></tr>
    <tr><th>Model</th><td>@Model.Model</td></tr>
    <tr><th>Type</th><td>@Model.DeviceType</td></tr>
    <tr><th>Serial Number</th><td>@Model.SerialNumber</td></tr>
    <tr><th>Purpose</th><td>@Model.Purpose</td></tr>
</table>

@if (Model.Accessories.Any())
{
    <h4>Accessories</h4>
    <ul>
        @foreach (var acc in Model.Accessories)
        {
            <li>@acc.AccessoryName — Qty: @acc.Quantity</li>
        }
    </ul>
}

@if (Model.Status == PaScan.Enums.RegisterStatus.REJECTED)
{
    <div class="alert alert-danger">
        <strong>Rejection Reason:</strong> @Model.RejectionReason
    </div>
}
```

---

## Step 7 — QR Token Display per Approved Device (M5) (⏳ TODO)

This is the page the student visits to actually *use* their QR code at the gate. It only exists for **approved** devices — the `Device` row and its `QRToken` are created by the admin during approval (T1).

### 🧠 Think about it first

This page is different from the request detail page (`Details.cshtml`). That page shows the `DeviceRequest` — the form submission. This page shows the `Device` — the approved record — plus the live QR image generated from the active `QRToken`.

You need to:
1. Load the `Device` by `Id` + `StudentId` (security scope)
2. Load its active `QRToken` (status = ACTIVE or EXPIRED — show either, but gate behavior differs)
3. Generate a QR image from `token_value` using QRCoder
4. Pass everything to the view

### Step 7a — Add the controller action

Add this to `DeviceController.cs`:

```csharp
// GET /api/device/student/approved/{deviceId}
[HttpGet("student/approved/{deviceId:guid}")]
public async Task<IActionResult> DeviceDetail(Guid deviceId)
{
    var studentIdStr = HttpContext.Session.GetString("StudentId");
    if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
        return RedirectToAction("StudentLogin", "Auth");

    // Load the approved Device — scoped to this student
    var device = await _context.Devices
        .Include(d => d.Accessories)
        .FirstOrDefaultAsync(d => d.Id == deviceId && d.StudentId == studentId);

    if (device == null) return NotFound();

    // Load the most recent QRToken (could be ACTIVE or EXPIRED)
    var qrToken = await _context.QRTokens
        .Where(t => t.DeviceId == deviceId)
        .OrderByDescending(t => t.CreatedAt)
        .FirstOrDefaultAsync();

    // Generate QR code image as base64 string (if token exists and is not REVOKED)
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
```

> 💡 **What is QRCoder doing here?** `token_value` is a GUID string stored in the database. QRCoder encodes that string into a QR image (PNG bytes). The gate scanner's camera reads the QR → gets back the GUID string → looks it up in the database. The image itself contains no secrets — the GUID is the key, and validity is checked server-side on scan.

### Step 7b — Create the DeviceDetailViewModel

Add this to `/Models/ViewModels/DeviceRequestViewModels.cs` (or a new file):

```csharp
public class DeviceDetailViewModel
{
    public Device Device { get; set; } = null!;
    public QRToken? QrToken { get; set; }
    public string? QrImageBase64 { get; set; }  // null if token is REVOKED
}
```

### Step 7c — Install QRCoder (if not already added)

In the NuGet Package Manager or terminal:

```
dotnet add package QRCoder
```

Add the using statement at the top of `DeviceController.cs`:

```csharp
using QRCoder;
```

### Step 7d — Create the view

Create `Views/Device/DeviceDetail.cshtml`:

```html
@model PaScan.Models.ViewModels.DeviceDetailViewModel
@using PaScan.Enums

<h2>@Model.Device.DeviceName</h2>
<p>@Model.Device.Brand @Model.Device.Model — @Model.Device.DeviceType</p>
<p>Serial: @Model.Device.SerialNumber</p>
<p>Device Status: <strong>@Model.Device.Status</strong></p>

<hr />

<h3>QR Code</h3>

@if (Model.QrToken == null)
{
    <p>No QR token has been issued for this device yet.</p>
}
else if (Model.QrToken.Status == TokenStatus.REVOKED)
{
    <div class="alert alert-danger">
        <strong>QR Revoked.</strong> This QR code has been revoked by an admin.
        Contact the admin to resolve this.
    </div>
}
else
{
    <p>
        Status:
        @if (Model.QrToken.Status == TokenStatus.ACTIVE)
        {
            <span style="color: green;"><strong>ACTIVE</strong></span>
        }
        else
        {
            <span style="color: orange;"><strong>EXPIRED</strong></span>
        }
    </p>
    <p>Issued: @Model.QrToken.CreatedAt.ToString("MMM dd, yyyy")</p>
    <p>Expires: @Model.QrToken.ExpiresAt.ToString("MMM dd, yyyy")</p>
    <p>Renewal #: @Model.QrToken.RenewalNumber</p>

    @if (Model.QrImageBase64 != null && Model.QrToken.Status == TokenStatus.ACTIVE)
    {
        <div>
            <img src="data:image/png;base64,@Model.QrImageBase64"
                 alt="QR Code"
                 style="width: 250px; height: 250px;" />
            <p><small>Show this QR code to the gate scanner.</small></p>
        </div>
    }

    @if (Model.QrToken.Status == TokenStatus.EXPIRED)
    {
        <div class="alert alert-warning">
            <strong>Your QR code has expired.</strong> Renew it to regain gate access.
        </div>

        <form asp-action="RenewQr" asp-route-deviceId="@Model.Device.Id" method="post">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-warning">🔄 Renew QR Code</button>
        </form>
    }
}

<hr />

@if (Model.Device.Accessories.Any())
{
    <h4>Accessories</h4>
    <ul>
        @foreach (var acc in Model.Device.Accessories)
        {
            <li>@acc.AccessoryName — Qty: @acc.Quantity</li>
        }
    </ul>
}

<a asp-controller="Student" asp-action="Dashboard">← Back to Dashboard</a>
```

> 💡 **Why `data:image/png;base64,...`?** Instead of saving the QR image to a file on disk, you embed it directly in the HTML as a base64 string. This means no file storage needed — the image is generated fresh on every page load from the current `token_value`. If the token is renewed, the next page load shows the new QR automatically.

---

## Step 8 — QR Renewal (M11 — Bonus Transaction T5) (⏳ TODO)

This is the first time on the student side that you'll use `BeginTransaction()`. It's required because renewal is a **3-step operation** that mixes an UPDATE with two INSERTs — if any step fails, all must roll back.

### 🧠 Think about it first

The renewal rule from SCHEMA.md:
- Only allowed when `QRToken.Status = EXPIRED`. If `REVOKED`, block it — the student can't self-renew a revoked token.
- Old token is marked `EXPIRED` (it already is, but you still set the status explicitly)
- New token is inserted with a new GUID, 30-day expiry, and `renewal_number = old + 1`
- `Device.QrRenewalCount` is incremented

### Step 8a — Add the controller action

Add this to `DeviceController.cs`:

```csharp
// POST /api/device/student/approved/{deviceId}/renew-qr
[HttpPost("student/approved/{deviceId:guid}/renew-qr")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RenewQr(Guid deviceId)
{
    var studentIdStr = HttpContext.Session.GetString("StudentId");
    if (string.IsNullOrEmpty(studentIdStr) || !Guid.TryParse(studentIdStr, out var studentId))
        return RedirectToAction("StudentLogin", "Auth");

    // Load device — scoped to this student
    var device = await _context.Devices
        .FirstOrDefaultAsync(d => d.Id == deviceId && d.StudentId == studentId);

    if (device == null) return NotFound();

    // Load the current active/expired token
    var oldToken = await _context.QRTokens
        .Where(t => t.DeviceId == deviceId)
        .OrderByDescending(t => t.CreatedAt)
        .FirstOrDefaultAsync();

    // Block renewal if no token exists or if it was REVOKED
    if (oldToken == null || oldToken.Status == TokenStatus.REVOKED)
    {
        TempData["Error"] = "QR renewal is not available for this device.";
        return RedirectToAction("DeviceDetail", new { deviceId });
    }

    // Block renewal if token is still ACTIVE (not yet expired)
    if (oldToken.Status == TokenStatus.ACTIVE && oldToken.ExpiresAt > DateTime.UtcNow)
    {
        TempData["Error"] = "Your QR code is still active and does not need renewal.";
        return RedirectToAction("DeviceDetail", new { deviceId });
    }

    // T5 — QR Renewal Transaction
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Step 1: Mark old token as EXPIRED (in case it wasn't already set)
        oldToken.Status = TokenStatus.EXPIRED;
        oldToken.UpdatedAt = DateTime.UtcNow;

        // Step 2: Insert new token
        var newToken = new QRToken
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            StudentId = studentId,
            TokenValue = Guid.NewGuid().ToString(),  // new unique scannable value
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Status = TokenStatus.ACTIVE,
            RenewalNumber = oldToken.RenewalNumber + 1,
            RenewedFrom = oldToken.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.QRTokens.Add(newToken);

        // Step 3: Increment renewal count on device
        device.QrRenewalCount += 1;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        TempData["Error"] = "Renewal failed. Please try again.";
        return RedirectToAction("DeviceDetail", new { deviceId });
    }

    TempData["Success"] = "QR code renewed successfully. Valid for 30 days.";
    return RedirectToAction("DeviceDetail", new { deviceId });
}
```

> 💡 **Why does `TokenValue` change on renewal?** The `token_value` is what the QR code encodes — it's the scannable secret. If you renewed using the same GUID, the old expired token's value would still scan. A new GUID ensures only the new token is valid. The old row in the database remains as history but its GUID is no longer encoded in any active QR image.

### Step 8b — Show TempData messages in the view

Add this near the top of `DeviceDetail.cshtml` so error/success messages appear:

```html
@if (TempData["Error"] != null)
{
    <div class="alert alert-danger">@TempData["Error"]</div>
}
@if (TempData["Success"] != null)
{
    <div class="alert alert-success">@TempData["Success"]</div>
}
```

---

## Step 9 — Build the Student Dashboard (⏳ TODO)

The `StudentController.cs` currently has only a placeholder:

```csharp
// Current state — needs to be replaced
[Authorize(Roles = "STUDENT")]
public class StudentController : Controller
{
    public IActionResult Dashboard()
    {
        return Content("Welcome to the Student Dashboard!");  // ← placeholder
    }
}
```

### What you need to implement

The `StudentDashboardViewModel` is already defined in `/Models/ViewModels/StudentDashboardViewModel.cs`:

```csharp
public class StudentDashboardViewModel
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string CourseName { get; set; } = null!;

    // RFID summary
    public bool IsRfidEnabled { get; set; }
    public DateTime? RfidExpiresAt { get; set; }

    // Device requests
    public List<StudentRequestListItem> Requests { get; set; } = new();

    // Approved devices
    public List<DeviceListItem> Devices { get; set; } = new();
}

public class StudentRequestListItem
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = null!;
    public DeviceType DeviceType { get; set; }
    public RegisterStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? RejectionReason { get; set; }
}
```

### Updated controller implementation

Replace `StudentController.cs` with:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaScan.Data;
using PaScan.Enums;
using PaScan.Models.ViewModels;

namespace PaScan.Controllers;

[Authorize(Roles = "STUDENT")]
public class StudentController : Controller
{
    private readonly AppDbContext _context;

    public StudentController(AppDbContext context)
    {
        _context = context;
    }

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
            CourseName = student.Course.CourseName,
            IsRfidEnabled = student.IsRfidEnabled,
            RfidExpiresAt = activeCard?.SemesterExpiresAt,
            Requests = requests,
            Devices = devices
        };

        return View(viewModel);
    }
}
```

### Dashboard View

Create `Views/Student/Dashboard.cshtml`:

```html
@model PaScan.Models.ViewModels.StudentDashboardViewModel

<h2>Welcome, @Model.FirstName @Model.LastName</h2>
<p>@Model.StudentNumber — @Model.CourseName</p>

<!-- RFID Status -->
<div>
    <h4>RFID Status</h4>
    @if (Model.IsRfidEnabled && Model.RfidExpiresAt.HasValue)
    {
        <p>✅ Active — Expires: @Model.RfidExpiresAt.Value.ToString("MMM dd, yyyy")</p>
    }
    else
    {
        <p>❌ No active RFID card</p>
    }
</div>

<!-- Register new device -->
<a asp-controller="Device" asp-action="Register" class="btn btn-primary">
    + Register a New Device
</a>

<!-- Device Requests -->
<h4>My Device Requests</h4>
@if (!Model.Requests.Any())
{
    <p>You have no device requests yet.</p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Device</th>
                <th>Type</th>
                <th>Status</th>
                <th>Submitted</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var req in Model.Requests)
            {
                <tr>
                    <td>@req.DeviceName</td>
                    <td>@req.DeviceType</td>
                    <td>
                        @if (req.Status == PaScan.Enums.RegisterStatus.PENDING)
                        {
                            <span style="color: orange;">PENDING</span>
                        }
                        else if (req.Status == PaScan.Enums.RegisterStatus.APPROVED)
                        {
                            <span style="color: green;">APPROVED</span>
                        }
                        else
                        {
                            <span style="color: red;">REJECTED</span>
                        }
                    </td>
                    <td>@req.SubmittedAt.ToString("MMM dd, yyyy")</td>
                    <td>
                        <a asp-controller="Device" asp-action="Details"
                           asp-route-id="@req.Id">View</a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

<!-- Approved Devices -->
<h4>My Approved Devices</h4>
@if (!Model.Devices.Any())
{
    <p>No approved devices yet.</p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Device</th>
                <th>Type</th>
                <th>Brand / Model</th>
                <th>Status</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var dev in Model.Devices)
            {
                <tr>
                    <td>@dev.DeviceName</td>
                    <td>@dev.DeviceType</td>
                    <td>@dev.Brand @dev.Model</td>
                    <td>@dev.Status</td>
                    <td>
                        <a asp-controller="Device"
                           asp-action="DeviceDetail"
                           asp-route-deviceId="@dev.Id">View QR</a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}
```

---

## Step 10 — Session Setup (⚠️ Important)

Your `DeviceController` reads `StudentId` from **session** (`HttpContext.Session.GetString("StudentId")`), but your `AuthController` currently stores the student ID via **cookie claims** (`ProfileId` claim), **not** session.

You need to make sure these are consistent. Options:

### Option A: Add session storage to AuthController (recommended)

In `AuthController.StudentLoginPost`, after `SignInUser(...)`, add:

```csharp
HttpContext.Session.SetString("StudentId", user.Student.Id.ToString());
```

This also requires adding session services in `Program.cs`:

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ... after app.UseRouting() and before app.UseAuthentication():
app.UseSession();
```

### Option B: Change DeviceController to read from claims instead

Replace session reads with:

```csharp
var studentIdStr = User.FindFirst("ProfileId")?.Value;
```

This uses the claim that's already set by `AuthController.SignInUser()`.

> ⚠️ **Pick one approach and use it consistently.** Mixing session and claims will cause bugs that are hard to track down.

---

## Step 11 — Test Your Work

### M3 — Registration Happy Path
- [ ] Log in as the test student at `/api/auth/login/student`
- [ ] Navigate to `/api/device/student/register`
- [ ] Fill in all required fields and submit
- [ ] You are redirected to `/api/device/student/{id}`
- [ ] The page shows status = `PENDING`
- [ ] Check Azure SQL — one `DeviceRequest` row exists with your data
- [ ] If you added accessories, check `DeviceRequestAccessory` — one row per accessory

### M3 — Validation Tests
- [ ] Submit the form empty — required field errors appear
- [ ] Submit the same serial number twice — error appears on the second attempt
- [ ] Try to access `/api/device/student/{someOtherId}` — should return 404

### M9 — Dashboard
- [ ] Navigate to student dashboard
- [ ] Your submitted request appears in the list with `PENDING` badge
- [ ] "View" link → goes to the request detail page
- [ ] If RFID is not issued yet → shows "No active RFID card"
- [ ] If RFID has been issued (by admin) → shows expiry date
- [ ] Approved devices section shows a "View QR" link

### M5 — QR Display (requires M4 admin approval first)
- [ ] Have an admin approve your test device request (T1 runs → `Device` + `QRToken` created)
- [ ] Navigate to `/api/device/student/approved/{deviceId}`
- [ ] QR code image renders on the page
- [ ] Expiry date is 30 days from approval
- [ ] Status shows `ACTIVE` in green
- [ ] Try to access another student's device ID → should return 404

### M5 — Revoked QR
- [ ] Have an admin revoke the QR token
- [ ] Reload the device page → no QR image shown, revocation message appears
- [ ] Renew button does NOT appear (only shown for EXPIRED, not REVOKED)

### M11 — QR Renewal (Bonus)
- [ ] Manually set the QRToken's `expires_at` to the past in Azure SQL to simulate expiry
- [ ] Reload the device page → status shows `EXPIRED` in orange, Renew button appears
- [ ] Click Renew → success message appears, new QR code is shown
- [ ] Check Azure SQL — old `QRToken` row has `status = EXPIRED`, new row exists with `status = ACTIVE`
- [ ] New token's `renewal_number = 2`, `renewed_from` = old token's ID
- [ ] Device row's `qr_renewal_count` incremented by 1
- [ ] Try clicking Renew again on a still-ACTIVE token → blocked with error message

---

## Common Mistakes to Watch For

| Mistake | What happens | Fix |
|---|---|---|
| Not reading `StudentId` from session/claims | NullReferenceException in controller | Always null-check before parsing |
| Accessory `name` attribute not indexed | Accessories list is empty after POST | Use `Accessories[0].AccessoryName` pattern |
| Missing `[ValidateAntiForgeryToken]` on POST | 400 Bad Request on form submit | Add the attribute and `@Html.AntiForgeryToken()` in view |
| Not filtering detail query by `StudentId` | Any student can view any request | Add `&& r.StudentId == studentId` |
| Session not configured in `Program.cs` | `GetString` returns null always | Add `AddSession()` and `UseSession()` |
| Forgetting the `/api` prefix in URLs | 404 errors | All non-Home routes are prefixed with `/api` |

---

## Implementation Status Summary

| Item | File | Module | Status |
|------|------|--------|--------|
| DeviceRequest entity | `Models/DeviceRequest.cs` | M3 | ✅ Done |
| DeviceRequestAccessory entity | `Models/DeviceRequestAccessory.cs` | M3 | ✅ Done |
| Device entity | `Models/Device.cs` | M5 | ✅ Done |
| QRToken entity | `Models/QRToken.cs` | M5/M11 | ✅ Done |
| RFIDCard entity | `Models/RFIDCard.cs` | M9 (display only) | ✅ Done |
| DeviceRequestViewModel | `Models/ViewModels/DeviceRequestViewModels.cs` | M3 | ✅ Done |
| DeviceDetailViewModel | `Models/ViewModels/DeviceRequestViewModels.cs` | M5 | ❌ Add this |
| StudentDashboardViewModel | `Models/ViewModels/StudentDashboardViewModel.cs` | M9 | ✅ Done |
| DeviceController — Register GET/POST | `Controllers/DeviceController.cs` | M3 | ✅ Done |
| DeviceController — Details GET | `Controllers/DeviceController.cs` | M3 | ✅ Done |
| DeviceController — DeviceDetail GET | `Controllers/DeviceController.cs` | M5 | ❌ Add this |
| DeviceController — RenewQr POST | `Controllers/DeviceController.cs` | M11 | ❌ Add this |
| StudentController — Dashboard | `Controllers/StudentController.cs` | M9 | ⏳ Placeholder only |
| Register view | `Views/Device/Register.cshtml` | M3 | ❌ Not created |
| Request detail view | `Views/Device/Details.cshtml` | M3 | ❌ Not created |
| Approved device + QR view | `Views/Device/DeviceDetail.cshtml` | M5/M11 | ❌ Not created |
| Dashboard view | `Views/Student/Dashboard.cshtml` | M9 | ❌ Not created |
| Session setup in Program.cs | `Program.cs` | All | ⚠️ Verify |
| QRCoder NuGet package | `.csproj` | M5/M11 | ⚠️ Install if missing |

---

## What's Next After This Module

Once M3, M5, M9, and (optionally M11) pass your checklist, the complete student experience is done. The next modules are on the **admin and scanner side**:

> **M4 — Admin Device Approval (Transaction T1)** — admin reviews pending requests, approves → creates the `Device` and `QRToken` that M5 depends on.

> **M8 — Admin RFID Card Issuance (Transaction T2)** — admin issues an RFID card to the student, which then appears in the M9 dashboard RFID status section.

> **M6 — Gate Scanning QR (Transaction T3)** — scanner account reads the QR code generated in M5 and logs the gate entry.

The student side is now fully self-contained — everything from registration to gate access is covered.

---

*PaScan Student Module Guide — M3 + M5 + M9 + M11 — Guided Learning Document*
*ASP.NET Core MVC | Entity Framework Core | Azure SQL*