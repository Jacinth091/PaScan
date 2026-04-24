# PaScan — Module Breakdown
### What to Build, Who Builds It, and When
> 3-Week Development Plan | ASP.NET Core MVC | Azure SQL

---

## How to Read This Document

Each module has:
- **What it is** — plain description of the feature
- **Pages / Endpoints** — exact routes and views to build
- **Database operations** — which tables are touched
- **Who builds it** — member responsible
- **When** — which week it must be done
- **Dependencies** — what must exist before this can be built
- **Transactions** — if the module involves a transaction, it is documented here

> 🔴 **Required** — must be done to pass the project requirement  
> 🟡 **Bonus** — strengthens the project, build only if time allows  
> ✅ **Done** — mark when Member 6 has verified and signed off  

---

## Module List

| # | Module | Owner | Week | Priority |
|---|---|---|---|---|
| M1 | Database & Migrations | Member 2 | Week 1 | 🔴 Required |
| M2 | Authentication | Member 4 | Week 1 | 🔴 Required |
| M3 | Student — Device Registration | Member 4 + 5 | Week 1 | 🔴 Required |
| M4 | Admin — Device Approval | Member 4 + 5 | Week 2 | 🔴 Required |
| M5 | QR Token Display | Member 4 + 5 | Week 2 | 🔴 Required |
| M6 | Gate Scanning — QR | Member 4 + 5 | Week 2 | 🔴 Required |
| M7 | Gate Scanning — RFID | Member 4 | Week 2 | 🔴 Required |
| M8 | Admin — RFID Card Issuance | Member 4 + 5 | Week 2 | 🔴 Required |
| M9 | Student Dashboard | Member 5 | Week 1–2 | 🔴 Required |
| M10 | Admin Dashboard | Member 5 | Week 3 | 🔴 Required |
| M11 | QR Renewal | Member 4 + 5 | Week 3 | 🟡 Bonus |
| M12 | RFID Semester Renewal | Member 4 | Week 3 | 🟡 Bonus |
| M13 | Lost Card Replacement | Member 4 | Week 3 | 🟡 Bonus |
| M14 | Scanner Management | Member 4 + 5 | Week 3 | 🟡 Bonus |

---

## M1 — Database & Migrations
**Owner:** Member 2  
**Week:** 1  
**Priority:** 🔴 Required

### What it is
Set up the Azure SQL database using Entity Framework Core code-first migrations. This is the foundation everything else depends on. No other module can start until the database is live and seeded.

### What to build
- [ ] Create `AppDbContext` with all `DbSet<>` properties
- [ ] Create all EF Core model classes matching the DBML schema
- [ ] Add all enums as C# enums
- [ ] Configure relationships in `OnModelCreating` (foreign keys, unique constraints, soft delete filters)
- [ ] Write and run the initial migration
- [ ] Connect to Azure SQL — update `appsettings.json` with connection string
- [ ] Seed data: at least 3 courses, 1 admin account, 1 student account, 1 scanner account

### Tables to create
```
Course
User
Student
Admin
Scanner
DeviceRequest
DeviceRequestAccessory
Device
DeviceAccessory
QRToken
RFIDCard
GateScanLog
```

### Notes
- Use `newid()` as default for all UUID primary keys
- Use `getutcdate()` as default for all timestamp fields
- All tables with `deleted_at` must have a global query filter in EF Core: `.HasQueryFilter(e => e.DeletedAt == null)`
- Configure enums as `varchar` in Azure SQL using `.HasConversion<string>()`

### Dependencies
None — this is the first module.

---

## M2 — Authentication
**Owner:** Member 4  
**Week:** 1  
**Priority:** 🔴 Required

### What it is
Three separate login pages for three roles. Session-based authentication with role-based redirect on login and role-based authorization on all protected pages.

### Pages to build
| Page | Route | Who uses it |
|---|---|---|
| Student login | `/login/student` | Students |
| Admin login | `/login/admin` | Admins |
| Scanner login | `/login/scanner` | Scanner accounts |
| Logout | `/logout` | All roles |

### What to build
- [ ] `AccountController` with `StudentLogin`, `AdminLogin`, `ScannerLogin`, `Logout` actions
- [ ] Student login: query `User` WHERE `student_number = ? AND password_hash = ? AND role = STUDENT`
- [ ] Admin login: query `User` WHERE `email = ? AND password_hash = ? AND role = ADMIN`
- [ ] Scanner login: query `User` WHERE `email = ? AND password_hash = ? AND role = SCANNER`
- [ ] On login: set session with `UserId`, `Role`, and profile `Id` (StudentId / AdminId / ScannerId)
- [ ] On login success: redirect to correct dashboard based on role
- [ ] Add `[Authorize]` attribute to all controllers with role checks
- [ ] Login views for all three pages

### Role redirects on login
```
STUDENT  → /student/dashboard
ADMIN    → /admin/dashboard
SCANNER  → /scan/qr
```

### Database operations
```
READ User WHERE student_number/email = ? AND role = ?
READ Student/Admin/Scanner WHERE user_id = ?
```

### Dependencies
- M1 must be complete (User, Student, Admin, Scanner tables exist)

---

## M3 — Student Device Registration
**Owner:** Member 4 (controller) + Member 5 (views)  
**Week:** 1  
**Priority:** 🔴 Required

### What it is
The student fills out a device registration form that mirrors the physical pink slip. They can add multiple accessories. The submission creates a `DeviceRequest` row with `status = PENDING` and one `DeviceRequestAccessory` row per accessory.

### Pages to build
| Page | Route | Description |
|---|---|---|
| Registration form | `/student/devices/register` | The form |
| Submission confirmation | `/student/devices/{id}` | Shows submitted request detail |

### What to build
- [ ] `DeviceController` — `Register` GET (show form) and POST (submit form)
- [ ] Form fields matching the pink slip:
  - Purpose, Device Name, Device Type (dropdown from enum), Brand, Model, Serial Number
  - Operating System, Color
  - Hardware specs (show/hide based on device type): Processor, Motherboard, Memory, Storage, Monitor Size, Casing, Has CD-ROM
- [ ] Dynamic accessory list — student can add/remove accessory rows (Name + Quantity)
- [ ] On submit: `INSERT DeviceRequest` + `INSERT DeviceRequestAccessory` (one per accessory)
- [ ] After submit: redirect to device detail page showing status = PENDING
- [ ] Input validation: serial number required and unique per student, device type required, brand/model required

### Database operations
```
INSERT DeviceRequest
INSERT DeviceRequestAccessory (one per accessory row)
```

### Form ViewModel
```csharp
public class DeviceRequestViewModel
{
    public string Purpose { get; set; }
    public string DeviceName { get; set; }
    public DeviceType DeviceType { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public string SerialNumber { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Color { get; set; }
    public string? Processor { get; set; }
    public string? Motherboard { get; set; }
    public string? Memory { get; set; }
    public string? Storage { get; set; }
    public string? MonitorSize { get; set; }
    public string? Casing { get; set; }
    public bool HasCdRom { get; set; }
    public List<AccessoryViewModel> Accessories { get; set; }
}

public class AccessoryViewModel
{
    public string AccessoryName { get; set; }
    public int Quantity { get; set; }
}
```

### Dependencies
- M1 (tables), M2 (student must be logged in)

---

## M4 — Admin Device Approval
**Owner:** Member 4 (controller) + Member 5 (views)  
**Week:** 2  
**Priority:** 🔴 Required — **contains Transaction T1**

### What it is
Admin sees all pending device requests and can approve or reject each one. Approval triggers Transaction T1 — the most important transaction in the project.

### Pages to build
| Page | Route | Description |
|---|---|---|
| Pending requests list | `/admin/requests` | All PENDING requests |
| Request detail | `/admin/requests/{id}` | Full device specs + accessories |
| Approve action | `POST /admin/requests/{id}/approve` | Triggers T1 |
| Reject action | `POST /admin/requests/{id}/reject` | Sets status = REJECTED |

### What to build
- [ ] `AdminController` — `Requests`, `RequestDetail`, `Approve`, `Reject` actions
- [ ] Pending list: show student name, device type, brand, model, submission date
- [ ] Detail page: show all pink slip fields + accessories list + student info
- [ ] Approve form: optional remarks field
- [ ] Reject form: required rejection_reason field

### ⚠️ Transaction T1 — Admin Approves Device Request
```
All steps must succeed or all roll back.

Step 1: UPDATE DeviceRequest
        SET status = APPROVED,
            reviewed_by = adminId,
            reviewed_at = now()
        WHERE id = ?

Step 2: INSERT Device
        (copy all pink slip fields from DeviceRequest)
        SET approved_by = adminId,
            approved_at = now(),
            status = ACTIVE,
            qr_renewal_count = 0

Step 3: INSERT DeviceAccessory
        (copy each row from DeviceRequestAccessory)
        SET device_id = new Device.id

Step 4: INSERT QRToken
        SET device_id = new Device.id,
            student_id = DeviceRequest.student_id,
            token_value = Guid.NewGuid().ToString(),
            issued_at = now(),
            expires_at = now() + 30 days,
            status = ACTIVE,
            renewal_number = 1,
            renewed_from = null
```

```csharp
// T1 implementation pattern
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Step 1 - Update request
    request.Status = RegisterStatus.APPROVED;
    request.ReviewedBy = adminId;
    request.ReviewedAt = DateTime.UtcNow;

    // Step 2 - Create device
    var device = new Device { /* copy fields */ };
    _context.Devices.Add(device);
    await _context.SaveChangesAsync();

    // Step 3 - Copy accessories
    foreach (var acc in request.Accessories)
    {
        _context.DeviceAccessories.Add(new DeviceAccessory
        {
            DeviceId = device.Id,
            AccessoryName = acc.AccessoryName,
            Quantity = acc.Quantity
        });
    }

    // Step 4 - Generate QR token
    _context.QRTokens.Add(new QRToken
    {
        DeviceId = device.Id,
        StudentId = request.StudentId,
        TokenValue = Guid.NewGuid().ToString(),
        IssuedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(30),
        Status = TokenStatus.ACTIVE,
        RenewalNumber = 1
    });

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Reject (no transaction needed — single update)
```
UPDATE DeviceRequest
SET status = REJECTED,
    reviewed_by = adminId,
    reviewed_at = now(),
    rejection_reason = ?
```

### Dependencies
- M1, M2 (admin must be logged in), M3 (device requests must exist)

---

## M5 — QR Token Display
**Owner:** Member 4 (controller) + Member 5 (views)  
**Week:** 2  
**Priority:** 🔴 Required

### What it is
After a device is approved, the student can view their device detail page which shows the QR code image. The QR code is generated from the `token_value` GUID using the QRCoder NuGet package.

### Pages to build
| Page | Route | Description |
|---|---|---|
| Device list | `/student/devices` | All student's devices + statuses |
| Device detail | `/student/devices/{id}` | Device info + QR code image |

### What to build
- [ ] `DeviceController` — `MyDevices` and `DeviceDetail` actions
- [ ] Device list: show device name, type, status, QR expiry date
- [ ] Device detail: show all device specs + accessories + QR code image
- [ ] Generate QR image using QRCoder from `QRToken.token_value`
- [ ] Show expiry date and days remaining on the QR
- [ ] If QR is EXPIRED: show "Renew QR" button (links to M11)
- [ ] If QR is REVOKED: show "Contact admin" message, no QR image

### QR Code Generation
```csharp
// Install: Install-Package QRCoder
using QRCoder;

public IActionResult GetQrCode(string tokenValue)
{
    using var qrGenerator = new QRCodeGenerator();
    var qrData = qrGenerator.CreateQrCode(tokenValue, QRCodeGenerator.ECCLevel.Q);
    using var qrCode = new PngByteQRCode(qrData);
    var qrBytes = qrCode.GetGraphic(10);
    return File(qrBytes, "image/png");
}
```

### Database operations
```
READ Device WHERE id = ? AND student_id = ? (security check)
READ QRToken WHERE device_id = ? AND status = ACTIVE
READ DeviceAccessory WHERE device_id = ?
```

### Dependencies
- M1, M2, M4 (device must be approved and QRToken must exist)

---

## M6 — Gate Scanning — QR
**Owner:** Member 4 (controller + API) + Member 5 (scan page view)  
**Week:** 2  
**Priority:** 🔴 Required — **contains Transaction T2 (course requirement)**

### What it is
A SCANNER role account logs into the gate phone. The phone opens the QR scan page which accesses the camera. The student shows their QR code. The page reads the QR value and hits the scan API. The API validates the token and logs the result.

### Pages / Endpoints to build
| Page / Endpoint | Route | Description |
|---|---|---|
| QR scan page | `/scan/qr` | Camera scan UI — SCANNER only |
| QR scan API | `POST /api/scan/qr` | Validates token and logs result |

### What to build
- [ ] `ScanController` — `QrScanPage` GET and `ValidateQr` POST
- [ ] Scan page: open phone camera using browser JS (`jsQR` library via CDN)
- [ ] Camera reads QR → extracts `token_value` → POST to `/api/scan/qr`
- [ ] API validates token and logs result
- [ ] Return JSON: `{ allowed: true/false, denial_reason: "..." }`
- [ ] Scan page shows green (allowed) or red (denied) result

### ⚠️ Transaction T2 (course requirement) — QR Gate Scan
```
All steps must succeed or all roll back.

Step 1: Validate Scanner
        SELECT Scanner WHERE user_id = currentUser.id
        Check status = ACTIVE
        Check scanner_type = QR or BOTH
        → if invalid: return denied, no log

Step 2: Validate QRToken
        SELECT QRToken WHERE token_value = ?
        Check status = ACTIVE
        Check expires_at > now()
        → if invalid: proceed to log with is_allowed = false

Step 3: Validate Device
        SELECT Device WHERE id = QRToken.device_id
        Check status = ACTIVE
        → if invalid: proceed to log with is_allowed = false

Step 4: INSERT GateScanLog
        SET device_id = Device.id,
            qr_token_id = QRToken.id,
            rfid_card_id = null,
            scan_type = QR,
            scanner_id = Scanner.id,
            scanned_at = now(),
            is_allowed = true/false,
            denial_reason = null or reason
```

### jsQR CDN (add to scan page)
```html
<script src="https://cdn.jsdelivr.net/npm/jsqr@1.4.0/dist/jsQR.js"></script>
```

### Dependencies
- M1, M2 (SCANNER must be logged in), M4 (QRTokens must exist)

---

## M7 — Gate Scanning — RFID
**Owner:** Member 4  
**Week:** 2  
**Priority:** 🔴 Required — **contains Transaction T3 (course requirement)**

### What it is
The RFID hardware reader taps the student's school ID card. The hardware sends the `card_uid` and its `api_key` to the API. The API validates the key and card, fetches all approved devices for the student, and batch-inserts one `GateScanLog` row per device in a single transaction.

### Endpoint to build
| Endpoint | Route | Description |
|---|---|---|
| RFID scan API | `POST /api/scan/rfid` | Hardware hits this — no session, api_key in header |

### What to build
- [ ] `ScanController` — `ValidateRfid` POST action
- [ ] Extract `X-Api-Key` header from request
- [ ] Validate API key against `Scanner.api_key`
- [ ] Validate `RFIDCard` and fetch student's devices
- [ ] Batch insert `GateScanLog` rows in one transaction
- [ ] Return JSON: `{ allowed: true, device_count: 2 }` or `{ allowed: false, denial_reason: "..." }`

### ⚠️ Transaction T3 (course requirement) — RFID Gate Scan
```
All steps must succeed or all roll back.

Step 1: Validate API Key
        SELECT Scanner WHERE api_key = request.Headers["X-Api-Key"]
        Check status = ACTIVE
        Check scanner_type = RFID or BOTH
        → if invalid: return 401 Unauthorized, no log

Step 2: Validate RFIDCard
        SELECT RFIDCard WHERE card_uid = ?
        Check status = ACTIVE
        Check semester_expires_at > now()
        → if invalid: INSERT one GateScanLog (is_allowed = false), return denied

Step 3: Fetch approved devices
        SELECT Device WHERE student_id = RFIDCard.student_id
        AND status = ACTIVE

Step 4: Batch INSERT GateScanLog
        For each device:
            INSERT GateScanLog (
                device_id = device.Id,
                rfid_card_id = RFIDCard.id,
                qr_token_id = null,
                scan_type = RFID,
                scanner_id = Scanner.id,
                scanned_at = now(),
                is_allowed = true
            )
        All inserts in one transaction.
```

### Test without hardware
Since the RFID reader may not always be available, build a test form at `/scan/rfid-test` (admin only) that manually submits a `card_uid` and `api_key` to simulate a hardware request. Remove or hide this for the final demo.

### Dependencies
- M1, M8 (RFIDCard must exist), Scanner must have an `api_key` configured

---

## M8 — Admin RFID Card Issuance
**Owner:** Member 4 (controller) + Member 5 (views)  
**Week:** 2  
**Priority:** 🔴 Required — **contains Transaction T4 (course requirement)**

### What it is
Admin issues an RFID card to a student who has requested or is eligible for RFID access. The card is linked to the student's physical school ID chip UID. This is also where admin manages card status.

### Pages to build
| Page | Route | Description |
|---|---|---|
| Student RFID detail | `/admin/students/{id}/rfid` | Current card status + issue/manage |
| Issue card form | `POST /admin/students/{id}/rfid/issue` | Triggers T4 |

### What to build
- [ ] `AdminController` — `RfidDetail`, `IssueRfid` actions
- [ ] RFID detail page: show current card status, card_uid, expiry, renewal count
- [ ] Issue card form: `card_uid` input (physical chip ID), `semester_expires_at` date picker
- [ ] On issue: Transaction T4

### ⚠️ Transaction T4 (course requirement) — Admin Issues RFID Card
```
All steps must succeed or all roll back.

Step 1: INSERT RFIDCard
        SET student_id = ?,
            card_uid = ?,
            issued_at = now(),
            semester_expires_at = ?,
            status = ACTIVE,
            renewal_number = 1,
            renewed_from = null,
            issued_by = adminId

Step 2: UPDATE Student
        SET is_rfid_enabled = true,
            rfid_renewal_count = 0
        WHERE id = ?
```

### Dependencies
- M1, M2, student must have at least one approved device

---

## M9 — Student Dashboard
**Owner:** Member 5  
**Week:** 1 (basic) → Week 2 (complete)  
**Priority:** 🔴 Required

### What it is
The landing page for students after login. Shows a summary of their registered devices, request statuses, and QR/RFID status at a glance.

### Page to build
| Page | Route | Description |
|---|---|---|
| Student dashboard | `/student/dashboard` | Overview of all devices and statuses |

### What to build
- [ ] List of all `DeviceRequest` rows for the student with status badges (PENDING / APPROVED / REJECTED)
- [ ] List of all approved `Device` rows with QR expiry date and status
- [ ] RFID status banner: "RFID Enabled — Expires [date]" or "RFID Not Enabled"
- [ ] Quick action buttons: Register New Device, View QR, Renew QR (if expired)
- [ ] Empty state for students with no devices yet

### Database operations
```
READ DeviceRequest WHERE student_id = ? ORDER BY created_at DESC
READ Device WHERE student_id = ?
READ QRToken WHERE device_id IN (...) AND status = ACTIVE
READ RFIDCard WHERE student_id = ? AND status = ACTIVE
```

### Dependencies
- M1, M2, M3 (device requests), M5 (QR tokens)

---

## M10 — Admin Dashboard
**Owner:** Member 5  
**Week:** 3  
**Priority:** 🔴 Required

### What it is
The landing page for admins after login. Shows summary stats and recent activity.

### Page to build
| Page | Route | Description |
|---|---|---|
| Admin dashboard | `/admin/dashboard` | System overview |

### What to build
- [ ] Count cards: Pending Requests, Active Devices, Total Students, Today's Scans
- [ ] Recent pending requests table (top 5) with quick Approve/Reject buttons
- [ ] Recent scan log table (top 10) — device, scan type, result, time
- [ ] Active scanners status list

### Database operations
```
COUNT DeviceRequest WHERE status = PENDING
COUNT Device WHERE status = ACTIVE
COUNT Student WHERE is_active = true
COUNT GateScanLog WHERE scanned_at >= today
SELECT TOP 5 DeviceRequest WHERE status = PENDING ORDER BY created_at DESC
SELECT TOP 10 GateScanLog ORDER BY scanned_at DESC
SELECT Scanner WHERE status = ACTIVE
```

### Dependencies
- All required modules must be complete first

---

## M11 — QR Renewal (Student Self-Service)
**Owner:** Member 4 (controller) + Member 5 (views)  
**Week:** 3  
**Priority:** 🟡 Bonus

### What it is
When a student's QR code expires, they can renew it themselves from the device detail page. A new token row is inserted, the old one is marked EXPIRED, and the renewal counter on the device is incremented.

### Endpoint to build
| Endpoint | Route | Description |
|---|---|---|
| Renew QR | `POST /student/devices/{id}/renew-qr` | Student initiates renewal |

### What to build
- [ ] `DeviceController` — `RenewQr` POST action
- [ ] Check current QRToken status = EXPIRED (not REVOKED — if REVOKED, block and show error)
- [ ] Run renewal transaction

### Transaction T5 — QR Renewal
```
Step 1: UPDATE old QRToken
        SET status = EXPIRED
        WHERE device_id = ? AND status = ACTIVE OR EXPIRED

Step 2: INSERT new QRToken
        SET device_id = ?,
            student_id = ?,
            token_value = Guid.NewGuid().ToString(),
            issued_at = now(),
            expires_at = now() + 30 days,
            status = ACTIVE,
            renewal_number = old.renewal_number + 1,
            renewed_from = old.id

Step 3: UPDATE Device
        SET qr_renewal_count = qr_renewal_count + 1
```

### Dependencies
- M1, M4, M5 (QRToken must exist and be EXPIRED)

---

## M12 — RFID Semester Renewal
**Owner:** Member 4  
**Week:** 3  
**Priority:** 🟡 Bonus

### What it is
At the start of each semester, admin renews a student's RFID access. The old card row is marked EXPIRED and a new row is inserted with the new semester expiry date. The same physical card is used.

### Endpoint to build
| Endpoint | Route | Description |
|---|---|---|
| Renew RFID | `POST /admin/students/{id}/rfid/renew` | Admin renews semester access |

### Transaction T6 — RFID Semester Renewal
```
Step 1: UPDATE old RFIDCard
        SET status = EXPIRED,
            invalidated_at = now(),
            invalidated_by = adminId,
            invalidation_reason = SEMESTER_END
        WHERE student_id = ? AND status = ACTIVE

Step 2: INSERT new RFIDCard
        SET student_id = ?,
            card_uid = old.card_uid,        ← same physical card
            issued_at = now(),
            semester_expires_at = ?,        ← new date set by admin
            status = ACTIVE,
            renewal_number = old.renewal_number + 1,
            renewed_from = old.id,
            is_replacement = false,
            issued_by = adminId

Step 3: UPDATE Student
        SET rfid_renewal_count = rfid_renewal_count + 1
```

### Dependencies
- M1, M8 (RFIDCard must exist)

---

## M13 — Lost Card Replacement
**Owner:** Member 4  
**Week:** 3  
**Priority:** 🟡 Bonus

### What it is
Student reports their physical school ID as lost, stolen, or damaged. Admin revokes the old card and issues a new one with a different chip UID (new physical card). Student's devices are completely unaffected since they are linked to `Student.id` not `RFIDCard.id`.

### Endpoint to build
| Endpoint | Route | Description |
|---|---|---|
| Replace card | `POST /admin/students/{id}/rfid/replace` | Admin handles replacement |

### Transaction T7 — Lost Card Replacement
```
Step 1: UPDATE old RFIDCard
        SET status = REVOKED,
            invalidated_at = now(),
            invalidated_by = adminId,
            invalidation_reason = LOST_CARD / STOLEN_CARD / DAMAGED_CARD
        WHERE student_id = ? AND status = ACTIVE

Step 2: INSERT new RFIDCard
        SET student_id = ?,
            card_uid = ?,               ← NEW chip UID from replacement card
            issued_at = now(),
            semester_expires_at = old.semester_expires_at,  ← same expiry
            status = ACTIVE,
            renewal_number = old.renewal_number + 1,
            renewed_from = old.id,
            is_replacement = true,
            previous_card_uid = old.card_uid,   ← preserve old chip ID
            issued_by = adminId

Step 3: UPDATE Student
        SET rfid_renewal_count = rfid_renewal_count + 1

NOTE: Student devices are NOT touched.
      They are linked to Student.id, not RFIDCard.id.
      The new card immediately covers all existing approved devices.
```

### Dependencies
- M1, M8 (active RFIDCard must exist)

---

## M14 — Scanner Management
**Owner:** Member 4 (controller) + Member 5 (views)  
**Week:** 3  
**Priority:** 🟡 Bonus

### What it is
Admin can register new scanner accounts, generate API keys for RFID scanners, and manage scanner status. Each scanner has a `User` account (SCANNER role) and a `Scanner` profile row.

### Pages to build
| Page | Route | Description |
|---|---|---|
| Scanner list | `/admin/scanners` | All registered scanners |
| Scanner detail | `/admin/scanners/{id}` | Details + API key management |
| Register scanner | `POST /admin/scanners/register` | Create new scanner |
| Generate API key | `POST /admin/scanners/{id}/generate-key` | Generate/regenerate api_key |
| Toggle status | `POST /admin/scanners/{id}/toggle` | ACTIVE ↔ INACTIVE |

### What to build
- [ ] `ScannerController` — `List`, `Detail`, `Register`, `GenerateKey`, `Toggle` actions
- [ ] Register: create `User` (role = SCANNER, email, password) + create `Scanner` row
- [ ] Generate API key: create random key, store plain text in `Scanner.api_key`, show in UI
- [ ] Detail page: show scanner info, type, location, status, api_key (hidden by default with Show/Copy buttons)
- [ ] Toggle: flip `Scanner.status` between ACTIVE and INACTIVE

### API Key Generation
```csharp
// Generate a secure random key
var apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
// Store plain text — admin can view anytime
scanner.ApiKey = apiKey;
scanner.ApiKeyIssuedAt = DateTime.UtcNow;
scanner.ApiKeyGeneratedBy = adminId;
```

### Dependencies
- M1, M2 (admin must be logged in)

---

## Build Order Summary

```
WEEK 1
  M1 → Database & Migrations          (Member 2)    — everything depends on this
  M2 → Authentication                 (Member 4)    — everything depends on this
  M3 → Student Device Registration    (Member 4+5)  — depends on M1, M2
  M9 → Student Dashboard (basic)      (Member 5)    — depends on M1, M2

WEEK 2
  M4 → Admin Device Approval          (Member 4+5)  — depends on M3    ← T1 here
  M5 → QR Token Display               (Member 4+5)  — depends on M4
  M8 → Admin RFID Issuance            (Member 4+5)  — depends on M1    ← T4 here
  M6 → Gate Scanning QR               (Member 4+5)  — depends on M5    ← T2 here
  M7 → Gate Scanning RFID             (Member 4)    — depends on M8    ← T3 here
  M9 → Student Dashboard (complete)   (Member 5)    — depends on M5, M8

WEEK 3
  M10 → Admin Dashboard               (Member 5)    — depends on all modules
  M11 → QR Renewal                    (Member 4+5)  — depends on M5    ← T5 bonus
  M12 → RFID Semester Renewal         (Member 4)    — depends on M8    ← T6 bonus
  M13 → Lost Card Replacement         (Member 4)    — depends on M8    ← T7 bonus
  M14 → Scanner Management            (Member 4+5)  — depends on M2    ← bonus
```

---

## Transaction Map

| Transaction | Module | Course Req? | Who implements |
|---|---|---|---|
| T1 — Admin approves device | M4 | ✅ Required | Member 4 |
| T2 — QR gate scan | M6 | ✅ Required | Member 4 |
| T3 — RFID gate scan | M7 | ✅ Required | Member 4 |
| T4 — Admin issues RFID card | M8 | ✅ Required | Member 4 |
| T5 — Student renews QR | M11 | 🟡 Bonus | Member 4 |
| T6 — Admin renews RFID semester | M12 | 🟡 Bonus | Member 4 |
| T7 — Admin replaces lost card | M13 | 🟡 Bonus | Member 4 |

> All 4 required transactions are in Member 4's hands. This is the most critical role for Week 2.

---

*PaScan Module Breakdown — ELNET Final Project*  
*Last updated based on full project discussion and proposal*