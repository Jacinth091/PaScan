# PaScan — Project Guidelines
### Campus Device Entry Management System
> ELNET Final Project | ASP.NET Core MVC | Azure SQL Database | 3-Week Timeline

---

## Table of Contents
1. [Project Summary](#1-project-summary)
2. [Tech Stack](#2-tech-stack)
3. [Team Roles](#3-team-roles)
4. [Database Schema Overview](#4-database-schema-overview)
5. [System Modules](#5-system-modules)
6. [The 4 Required Transactions](#6-the-4-required-transactions)
7. [All 7 Transactions (Full List)](#7-all-7-transactions-full-list)
8. [User Flows](#8-user-flows)
9. [Access Control Rules](#9-access-control-rules)
10. [Weekly Roadmap](#10-weekly-roadmap)
11. [Code Conventions](#11-code-conventions)
12. [Definition of Done](#12-definition-of-done)
13. [Priority Order if Time Runs Short](#13-priority-order-if-time-runs-short)

---

## 1. Project Summary

**PaScan** replaces the manual pink slip process used at the campus gate. Currently, students fill out a physical form every time they bring a device onto campus. PaScan digitizes the entire workflow:

- Students **register their devices online** through a structured form
- Admins **review and approve** requests from a web dashboard
- Approved devices receive a **unique QR code** (valid 30 days) or use the student's **RFID school ID** (valid per semester)
- At the gate, a **phone camera scans QR codes** or the **RFID hardware reader** taps the ID — both are autonomous, no guard needed
- Every scan attempt is **logged** with full context

### What the system replaces
| Before (Manual) | After (PaScan) |
|---|---|
| Paper pink slip filled at gate | Online device registration form |
| Guard manually checks slip | Autonomous gate scan (QR or RFID) |
| No expiry or revocation | 30-day QR / semester RFID with renewal |
| No audit trail | Full scan log per device per event |
| Slips can be lost or faked | Unique token per device, admin-controlled |

---

## 2. Tech Stack

| Technology | Role in Project | Why |
|---|---|---|
| **ASP.NET Core MVC** | Full web framework | Required by course spec |
| **Azure SQL Database** | Cloud database | Required by course spec |
| **Entity Framework Core** | ORM & migrations | Preferred by course, simplifies transactions |
| **Razor Views (.cshtml)** | Frontend templating | Native to ASP.NET Core MVC |
| **QRCoder (NuGet)** | QR code image generation | Lightweight, no external API needed |
| **Visual Studio 2022** | IDE | Required by course spec |

> **Data Access:** Use `DbContext.Database.BeginTransaction()` with `Commit()` and `Rollback()` for all transactions. No partial saves allowed.

---

## 3. Team Roles

| # | Role | Owns | Key Deliverable |
|---|---|---|---|
| 1 | **System Analyst** | Requirements, Use Case Diagram, scope doc | System specification document by end of Week 1 |
| 2 | **Database Designer** | ERD, Azure SQL schema, EF Core migrations, seed data | DB fully migrated and seeded by end of Week 1 |
| 3 | **UI/UX Designer** | Wireframes, `_Layout.cshtml`, design system | Wireframes approved Day 3; layout ready by end of Week 1 |
| 4 | **Backend Developer** | All Controllers, all transaction logic, scan API, auth | All controllers + transactions functional by end of Week 2 |
| 5 | **Frontend Developer** | All Razor Views, forms, QR display page, dashboard | All views connected to controllers by end of Week 2 |
| 6 | **QA / Docs Lead** | Test cases, bug log, transaction evidence, final docs | Test cases by Week 2; full docs by end of Week 3 |

---

## 4. Database Schema Overview

### Enums
```
Role              → ADMIN | STUDENT | SCANNER
ScanType          → QR | RFID
ScannerType       → QR | RFID | BOTH
ScannerStatus     → ACTIVE | INACTIVE | MAINTENANCE
RegisterStatus    → PENDING | APPROVED | REJECTED
DeviceStatus      → ACTIVE | REVOKED | EXPIRED
TokenStatus       → ACTIVE | EXPIRED | REVOKED
DeviceType        → LAPTOP | DESKTOP | TABLET | PHONE
InvalidationReason→ LOST_CARD | STOLEN_CARD | DAMAGED_CARD | ADMIN_REVOKED | SEMESTER_END
RevocationReason  → ADMIN_REVOKED | DEVICE_LOST | DEVICE_STOLEN | ACCOUNT_DISABLED
```

### Tables & Relationships
```
Course          ──< Student               (one course, many students)
User            ─── Student              (one-to-one)
User            ─── Admin                (one-to-one)
User            ─── Scanner              (one-to-one)
Admin           ──< Scanner              (created_by)
Student         ──< DeviceRequest        (one student, many requests)
DeviceRequest   ──< DeviceRequestAccessory
DeviceRequest   ─── Device              (created on approval, one-to-one)
Student         ──< Device              (one student, many devices)
Device          ──< DeviceAccessory
Device          ──< QRToken             (one ACTIVE at a time, rest are history)
Student         ──< RFIDCard            (one ACTIVE at a time, rest are history)
Scanner         ──< GateScanLog
Device          ──< GateScanLog
QRToken         ──< GateScanLog
RFIDCard        ──< GateScanLog
```

### Key Design Decisions
- **`User` table** is auth-only. `student_number` filled for students, `email` filled for admins/scanners.
- **`DeviceRequest`** is immutable after submission — it is the audit trail of what the student declared.
- **`Device`** is created on approval — it is the source of truth for an approved device. Data is copied from `DeviceRequest`.
- **`QRToken` and `RFIDCard`** are never updated on renewal — a new row is inserted, old row is set to `EXPIRED`. This preserves full renewal history.
- **`GateScanLog`** has one row per device per scan event. RFID taps produce multiple rows in one batch transaction (one per approved device).
- **`Scanner`** has a `User` account for QR (logs in via browser) and an `api_key` for RFID (hardware sends key in header).

---

## 5. System Modules

### Module 1 — Authentication
- Separate login pages: `/login/student`, `/login/admin`, `/login/scanner`
- Student logs in with `student_number` + password
- Admin/Scanner logs in with `email` + password
- Session-based auth with role-based redirect on login

### Module 2 — Device Registration
- Student fills device form (mirrors pink slip fields)
- Declares accessories in a dynamic list
- Submission creates a `DeviceRequest` row with `status = PENDING`
- Student dashboard shows all their requests and statuses

### Module 3 — Admin Approval
- Admin sees all `PENDING` device requests
- Can view full device specs and accessory list
- Approve → triggers **Transaction T1**
- Reject → sets `status = REJECTED` with a reason

### Module 4 — QR Token Management
- QR code displayed on device detail page using QRCoder
- Expires 30 days after issuance
- Student clicks **Renew** when expired → triggers **Transaction T3**
- Admin can revoke a QR → sets `status = REVOKED`

### Module 5 — RFID Card Management
- Admin issues RFID card to student → triggers **Transaction T2**
- Admin renews RFID each semester → triggers **Transaction T4**
- Admin replaces lost/stolen/damaged card → triggers **Transaction T5**
- One card per student at any time; one active row, rest are history

### Module 6 — Gate Scanning
- **QR scan:** Scanner phone logs in, opens `/scan/qr`, student shows QR to camera, camera reads value and hits API → **Transaction T6**
- **RFID scan:** Hardware reader taps student ID, sends `card_uid` + `X-Api-Key` header to API → **Transaction T7**
- Both write to `GateScanLog` with `is_allowed = true/false` and `denial_reason` if denied

### Module 7 — Scanner Management
- Admin registers a new scanner (creates `User` + `Scanner` rows)
- Admin generates API key for RFID scanners from the scanner detail page
- API key stored plain text — visible anytime from admin panel
- Admin can enable, disable, or mark a scanner as under maintenance

### Module 8 — Dashboard
- Admin dashboard: pending requests count, recent scan logs, active devices, system stats
- Student dashboard: device list, statuses, QR availability, RFID status

---

## 6. The 4 Required Transactions

> These 4 are the minimum required by the course. They must use `BeginTransaction()` with `Commit()` and `Rollback()`. If any step fails, **all changes roll back**.

### ✅ Transaction 1 — Admin Approves Device Request (Multi-step Create + Update)
**Trigger:** Admin clicks Approve on a pending DeviceRequest

**Steps (all or nothing):**
1. `UPDATE DeviceRequest` → set `status = APPROVED`, `reviewed_by`, `reviewed_at`
2. `INSERT Device` → copy all pink slip fields from `DeviceRequest`
3. `INSERT DeviceAccessory` → copy all rows from `DeviceRequestAccessory`
4. `INSERT QRToken` → new GUID `token_value`, `expires_at = now + 30 days`, `renewal_number = 1`

**Rollback scenario:** If QRToken insert fails, Device and DeviceRequest update must also roll back.

---

### ✅ Transaction 2 — Admin Issues RFID Card (Multi-table Insert + Update)
**Trigger:** Admin issues an RFID card to a student

**Steps (all or nothing):**
1. `INSERT RFIDCard` → `card_uid`, `semester_expires_at`, `renewal_number = 1`, `issued_by`
2. `UPDATE Student` → set `is_rfid_enabled = true`, `rfid_renewal_count = 0`

**Rollback scenario:** If Student update fails, RFIDCard insert must roll back.

---

### ✅ Transaction 3 — QR Gate Scan (Validation + Logging)
**Trigger:** Student's QR code is scanned at the gate

**Steps (all or nothing):**
1. Validate `Scanner` → `status = ACTIVE`, `scanner_type = QR or BOTH`
2. Validate `QRToken` → `status = ACTIVE`, `expires_at > now()`
3. Validate `Device` → `status = ACTIVE`
4. `INSERT GateScanLog` → `is_allowed = true/false`, `denial_reason` if denied

**Rollback scenario:** If log insert fails, no partial scan record is saved.

---

### ✅ Transaction 4 — RFID Gate Scan (Validation + Batch Logging)
**Trigger:** Student taps RFID card on hardware reader

**Steps (all or nothing):**
1. Validate `Scanner` via `api_key` → `status = ACTIVE`, `scanner_type = RFID or BOTH`
2. Validate `RFIDCard` → `status = ACTIVE`, `semester_expires_at > now()`
3. Fetch all `Device` WHERE `student_id = ? AND status = ACTIVE`
4. Batch `INSERT GateScanLog` → one row per device, all in one transaction

**Rollback scenario:** If any log insert in the batch fails, all inserts roll back.

---

## 7. All 7 Transactions (Full List)

> Transactions 1–4 above are the required minimum. Transactions 5–7 are bonus business processes that strengthen the project.

| # | Name | Trigger | Required? |
|---|---|---|---|
| T1 | Admin approves device request | Admin clicks Approve | ✅ Required |
| T2 | Admin issues RFID card | Admin issues card to student | ✅ Required |
| T3 | QR gate scan | QR code scanned at gate | ✅ Required |
| T4 | RFID gate scan | RFID card tapped at gate | ✅ Required |
| T5 | Student renews QR | Student clicks Renew on device page | ⭐ Bonus |
| T6 | Admin renews RFID (semester) | Admin renews student's semester access | ⭐ Bonus |
| T7 | Admin replaces lost card | Admin handles lost/stolen/damaged ID | ⭐ Bonus |

### T5 — Student Renews QR
1. Verify current `QRToken` status = `EXPIRED` (not `REVOKED`)
2. `UPDATE` old `QRToken` → set `status = EXPIRED`
3. `INSERT` new `QRToken` → new `token_value`, `expires_at = now + 30 days`, `renewal_number = old + 1`, `renewed_from = old.id`
4. `UPDATE Device` → `qr_renewal_count + 1`

### T6 — Admin Renews RFID (Semester)
1. Verify current `RFIDCard` status = `EXPIRED`
2. `UPDATE` old `RFIDCard` → `status = EXPIRED`, `invalidation_reason = SEMESTER_END`
3. `INSERT` new `RFIDCard` → same `card_uid`, new `semester_expires_at`, `renewal_number = old + 1`, `is_replacement = false`
4. `UPDATE Student` → `rfid_renewal_count + 1`

### T7 — Admin Replaces Lost Card
1. `UPDATE` old `RFIDCard` → `status = REVOKED`, `invalidation_reason = LOST_CARD/STOLEN_CARD/DAMAGED_CARD`
2. `INSERT` new `RFIDCard` → new `card_uid`, same `semester_expires_at`, `renewal_number = old + 1`, `is_replacement = true`, `previous_card_uid = old.card_uid`
3. `UPDATE Student` → `rfid_renewal_count + 1`

> **Note:** Student's devices are NOT affected — they are linked to `Student.id`, not to the `RFIDCard`. Only the card changes.

---

## 8. User Flows

### Student Flow
```
Register account (/login/student)
  → Log in with student_number + password
  → Student dashboard (list of devices + statuses)
  → Submit device registration (device form + accessories)
  → Wait for admin approval (status = PENDING)
  → Approved: device shows ACTIVE, QR code available
  → Show QR at gate → scan → GateScanLog created
  → After 30 days: QR expires → click Renew → new QR generated
  → Optional: request RFID upgrade → admin issues card
  → Tap school ID at RFID reader → all devices logged
```

### Admin Flow
```
Log in (/login/admin) with email + password
  → Admin dashboard (pending count, recent logs, stats)
  → Review pending DeviceRequests
  → Approve → T1 runs → Device + QRToken created
  → Reject → status = REJECTED with reason
  → Issue RFID to student → T2 runs
  → Renew RFID each semester → T6 runs
  → Replace lost card → T7 runs
  → Manage scanners → register, generate API key, enable/disable
```

### QR Scanner Flow
```
Admin provisions SCANNER account (email + password)
  → Gate phone logs in at /login/scanner
  → Lands on /scan/qr page only
  → Student shows QR to phone camera
  → Camera reads token_value → POST /api/scan/qr
  → T3 runs → GateScanLog inserted
  → Page shows green (allowed) or red (denied)
```

### RFID Hardware Flow
```
Admin registers RFID scanner → generates api_key
  → Admin copies api_key → pastes into console app config
  → Student taps school ID on reader
  → Hardware sends POST /api/scan/rfid { card_uid } + X-Api-Key header
  → T4 runs → validates api_key, card, devices → batch log inserted
  → Hardware receives allowed/denied response
```

---

## 9. Access Control Rules

| Page / Endpoint | STUDENT | ADMIN | SCANNER |
|---|---|---|---|
| `/login/student` | ✅ | ❌ | ❌ |
| `/login/admin` | ❌ | ✅ | ❌ |
| `/login/scanner` | ❌ | ❌ | ✅ |
| `/student/dashboard` | ✅ | ❌ | ❌ |
| `/student/devices` | ✅ | ❌ | ❌ |
| `/student/devices/register` | ✅ | ❌ | ❌ |
| `/admin/dashboard` | ❌ | ✅ | ❌ |
| `/admin/requests` | ❌ | ✅ | ❌ |
| `/admin/scanners` | ❌ | ✅ | ❌ |
| `/scan/qr` | ❌ | ❌ | ✅ |
| `POST /api/scan/qr` | ❌ | ❌ | ✅ (session) |
| `POST /api/scan/rfid` | ❌ | ❌ | ✅ (api_key) |

> Enforce using `[Authorize(Roles = "STUDENT")]` etc. on all controllers. Unauthorized access returns 403 or redirects to the correct login page.

---

## 10. Weekly Roadmap

### Week 1 — Foundation
**Goal:** Database live, auth working, device registration form functional.

| Task | Owner |
|---|---|
| Finalize schema → EF Core models → Azure SQL migration → seed data | Member 2 |
| AccountController: student login, admin login, scanner login, session auth | Member 4 |
| Wireframes for all pages + `_Layout.cshtml` | Member 3 |
| Use Case Diagram + system specification document | Member 1 |
| Student dashboard view + device registration form view | Member 5 |
| Write test cases for auth and device registration | Member 6 |

**End of Week 1 checkpoint:** Student can register, log in, and submit a device request that saves to Azure SQL.

---

### Week 2 — Core Workflows
**Goal:** All 4 required transactions working. QR generation functional. Scan API live.

| Task | Owner |
|---|---|
| AdminController: approval workflow (T1), RFID issue (T2) | Member 4 |
| ScanController: QR scan API (T3), RFID scan API (T4) | Member 4 |
| ScannerController: register scanner, generate API key | Member 4 |
| Admin approval panel, QR display page, scanner login + scan page | Member 5 |
| Test all 4 transactions — success and rollback scenarios | Member 6 |
| Review all views for design consistency | Member 3 |

**End of Week 2 checkpoint:** Full end-to-end flow — registration → approval → QR display → gate scan — all working.

---

### Week 3 — Polish, Testing & Documentation
**Goal:** System stable, all docs complete, demo rehearsed.

| Task | Owner |
|---|---|
| Bug fixes from Week 2 testing, input validation on all forms | Member 4 |
| Admin dashboard summary page, UI polish, responsive fixes | Member 5 |
| Transaction testing evidence (screenshots), final validation report | Member 6 |
| Project documentation: title, objectives, scope, Use Case Diagram, MVC explanation | Member 1 |
| ERD diagram for submission, normalization documentation | Member 2 |
| Demo rehearsal + individual role defense prep | All |

**End of Week 3 checkpoint:** Fully functional system, complete documentation, demo-ready.

---

## 11. Code Conventions

### Naming
```
Classes & Methods   → PascalCase     (DeviceController, ApproveRequest)
Variables & Params  → camelCase      (studentId, tokenValue)
DB Column Names     → snake_case     (student_number, approved_at)
Razor Views         → PascalCase     (DeviceDetail.cshtml)
```

### Folder Structure
```
/Controllers        → All MVC controllers
/Models             → EF Core entity models
/Models/ViewModels  → ViewModels for form binding
/Views/{Controller} → Razor Views per controller
/Views/Shared       → _Layout.cshtml, partials
/Services           → Business logic extracted from controllers
/Data               → AppDbContext, migrations
/wwwroot            → CSS, JS, images
```

### Branching Strategy
```
main          → always deployable, never commit directly
feature/*     → one branch per feature (e.g., feature/device-registration)
              → merge via pull request, reviewed by Member 4 or Member 1
```

### Transaction Template
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Step 1
    // Step 2
    // Step 3
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (Exception)
{
    await transaction.RollbackAsync();
    throw; // or return error view
}
```

> **Rule:** Every multi-step DB operation uses this pattern. No `SaveChangesAsync()` outside of a try-catch with rollback.

---

## 12. Definition of Done

A feature is **done** when ALL of the following are true:

- [ ] Controller action handles the request and returns the correct view or API response
- [ ] View renders correctly and client-side validation passes
- [ ] Database operation executes — correct rows are created, updated, or soft-deleted
- [ ] If the feature involves a transaction: **rollback scenario tested and verified** by Member 6
- [ ] Role restriction enforced — unauthorized roles cannot access the page or endpoint
- [ ] No hardcoded test data left in controller or view
- [ ] Member 6 has written and run a test case for this feature

---

## 13. Priority Order if Time Runs Short

If the team falls behind, implement in this exact order. Stop when time runs out — everything below the cut line is a bonus.

```
Priority 1 ── Auth + Device Registration form              (Week 1 core — non-negotiable)
Priority 2 ── Admin Approval + QR Generation              (Transaction T1 — 1 of 4 required)
Priority 3 ── QR Gate Scan                                (Transaction T3 — 2 of 4 required)
Priority 4 ── RFID Card Issue                             (Transaction T2 — 3 of 4 required)
Priority 5 ── RFID Gate Scan                              (Transaction T4 — 4 of 4 required)
─────────────────────────────────────────────────────────────────────────────────────────────
Priority 6 ── QR Renewal                                  (Transaction T5 — bonus)
Priority 7 ── RFID Semester Renewal + Lost Card           (Transactions T6, T7 — bonus)
Priority 8 ── Scanner Management UI                       (bonus)
Priority 9 ── Admin dashboard stats, UI polish            (bonus)
```

> **Rule:** Priorities 1–5 satisfy the full course requirement (4 transactions + CRUD + 3 modules). Do not sacrifice these for bonus features.

---

## Quick Reference

### Denial Reasons (GateScanLog.denial_reason)
| Value | Meaning |
|---|---|
| `EXPIRED` | Token or card has passed its expiry date |
| `REVOKED` | Token or card was manually revoked by admin |
| `NOT_APPROVED` | Device exists but is not in ACTIVE status |
| `DEVICE_NOT_FOUND` | No device matches the scanned token |
| `SCANNER_INACTIVE` | Scanner is disabled or under maintenance |

### NuGet Packages Needed
```
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
QRCoder
Microsoft.AspNetCore.Authentication.Cookies
```

### Key Azure SQL Connection String (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:{server}.database.windows.net,1433;
      Initial Catalog={db};Persist Security Info=False;
      User ID={user};Password={password};
      MultipleActiveResultSets=False;Encrypt=True;
      TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

---

*PaScan Project Guidelines — ELNET Final Project*
*ASP.NET Core MVC | Azure SQL | Entity Framework Core*