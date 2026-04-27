# Admin Module — Implementation Plan
### PaScan · ASP.NET Core MVC · Azure SQL

> Follow phases in order. Each phase must be functional before the next begins.
> Transactions marked with 🔷 require `BeginTransactionAsync()` + `CommitAsync()` / `RollbackAsync()`.

---

## Legend

| Symbol | Meaning |
|--------|---------|
| 🔴 Required | Must be done to satisfy course requirement |
| 🟡 Bonus | Build only if time allows |
| 🔷 Transaction | Multi-step DB operation — all or nothing |
| ⚠ Warning | Rollback test required by QA (Member 6) |

---

## Phase 1 — Foundation (Prerequisite)

### Step 1 — AdminController scaffold + admin login 🔴

**Routes:** `GET /login/admin` · `GET /admin/dashboard`

**What to build:**
- Create `AdminController` in `/Controllers/` — decorate with `[Authorize(Roles = "ADMIN")]`
- Admin login queries `User` WHERE `email = ? AND password_hash = ? AND role = ADMIN`
- On login success: set session keys `UserId`, `Role = ADMIN`, `AdminId`
- Redirect to `/admin/dashboard` on login success
- Create `AdminLoginViewModel` with `Email` and `Password` fields
- Verify the seeded admin account (from M1) exists and can log in

> ⚠ Do not proceed to Phase 2 until session auth works and role redirect is confirmed.

---

## Phase 2 — Device Approval (Transaction T1 — Required)

### Step 2 — Pending requests list page 🔴

**Route:** `GET /admin/requests`

**What to build:**
- Query `DeviceRequest` WHERE `status = PENDING`, include `Student` navigation property
- Display table columns: student name, student number, device type, brand/model, submitted date
- Each row links to `/admin/requests/{id}`
- Show count of pending requests in the page header
- Create a `AdminRequestListViewModel` as a list model

---

### Step 3 — Request detail page 🔴

**Route:** `GET /admin/requests/{id}`

**What to build:**
- Load `DeviceRequest` + `DeviceRequestAccessory` list by `id`
- Display all pink slip fields: purpose, device name, type, brand, model, serial number, OS, color, and all hardware specs
- Display accessories table: accessory name + quantity per row
- Show student info: full name, student number, course
- Show two action buttons: **Approve** (POST → T1) and **Reject** (POST with reason input)
- If `status` is not `PENDING`, show a read-only view with the current status badge

---

### Step 4 — Approve device request 🔴 🔷 T1

**Route:** `POST /admin/requests/{id}/approve`

**Pre-check:** Verify `DeviceRequest.status == PENDING` before starting — return 400 if already processed.

**Transaction steps (all or nothing):**

```
Step 1 — UPDATE DeviceRequest
         SET status = APPROVED,
             reviewed_by = adminId,
             reviewed_at = now()

Step 2 — INSERT Device
         Copy from DeviceRequest:
           student_id, device_name, device_type, brand, model,
           serial_number, operating_system, color, processor,
           motherboard, memory, storage, monitor_size, casing,
           has_cd_rom, purpose
         SET original_request_id = DeviceRequest.id,
             status = ACTIVE

Step 3 — INSERT DeviceAccessory (one row per DeviceRequestAccessory)
         Copy: device_id (new Device), accessory_name, quantity

Step 4 — INSERT QRToken
         SET device_id, student_id,
             token_value = Guid.NewGuid().ToString(),
             issued_at = now(),
             expires_at = now() + 30 days,
             status = ACTIVE,
             renewal_number = 1,
             renewed_from = null
```

**Rollback scenario:** If the QRToken insert fails, the Device insert and DeviceRequest update must also roll back. Member 6 must verify this.

---

### Step 5 — Reject device request 🔴

**Route:** `POST /admin/requests/{id}/reject`

**What to build:**
- Require a `rejection_reason` string from the form — block empty submissions
- `UPDATE DeviceRequest` SET `status = REJECTED`, `rejection_reason`, `reviewed_by`, `reviewed_at`
- Single UPDATE — no full transaction needed, but wrap in try-catch
- Redirect to `/admin/requests` on success

---

## Phase 3 — RFID Card Issuance (Transaction T2 — Required)

### Step 6 — Student list page (admin view) 🔴

**Route:** `GET /admin/students`

**What to build:**
- List all `Student` records — include `User` and `Course` navigation properties
- Display: student name, student number, course, RFID status badge (`is_rfid_enabled`)
- Each row links to `/admin/students/{id}`
- Optional filters: by RFID status, by course

---

### Step 7 — Student detail page (admin view) 🔴

**Route:** `GET /admin/students/{id}`

**What to build:**
- Show student profile: name, student number, course, contact info
- Show current RFID card info (if any): card UID, issued date, expiry, status badge
- Show all student devices: device name, type, QR token status
- If `is_rfid_enabled = false`: show **Issue RFID Card** button
- If active card exists: show **Renew** and **Replace Card** buttons instead

---

### Step 8 — Issue RFID card to student 🔴 🔷 T2

**Route:** `POST /admin/students/{id}/rfid/issue`

**Form inputs:** `card_uid` (chip UID from physical card), `semester_expires_at` (date picker)

**Pre-check:** Verify student does NOT already have an `ACTIVE` RFIDCard — guard against double-issue.

**Transaction steps (all or nothing):**

```
Step 1 — INSERT RFIDCard
         SET student_id,
             card_uid (from form),
             issued_at = now(),
             semester_expires_at (from form),
             status = ACTIVE,
             renewal_number = 1,
             renewed_from = null,
             is_replacement = false,
             issued_by = adminId

Step 2 — UPDATE Student
         SET is_rfid_enabled = true,
             rfid_renewal_count = 0
```

> ⚠ Rollback scenario: if the Student UPDATE fails, the RFIDCard insert must also roll back. Member 6 must verify this.

---

## Phase 4 — QR Token Management (Admin Actions)

### Step 9 — Device list & device detail (admin view) 🔴

**Routes:** `GET /admin/devices` · `GET /admin/devices/{id}`

**What to build:**
- List all `Device` records across all students — include student name, device type, status badge
- Device detail shows: all specs, accessories list, current QRToken (status + expiry), recent scan log entries
- Show **Revoke QR** button if current QRToken `status = ACTIVE`
- Show **Revoke Device** button to set `Device.status = REVOKED`

---

### Step 10 — Revoke QR token 🔴

**Route:** `POST /admin/devices/{id}/revoke-qr`

**What to build:**
- Require a `RevocationReason` dropdown: `ADMIN_REVOKED`, `DEVICE_LOST`, `DEVICE_STOLEN`, `ACCOUNT_DISABLED`
- Find the ACTIVE `QRToken` for this device — UPDATE `status = REVOKED`, set `revocation_reason`
- Single UPDATE — no full transaction needed, but wrap in try-catch
- Once REVOKED, the student cannot self-renew — only admin action can reissue

---

## Phase 5 — Scanner Management (Bonus)

### Step 11 — Scanner list & register scanner 🟡

**Routes:** `GET /admin/scanners` · `POST /admin/scanners/register`

**What to build:**
- List all scanners: name, location, scanner type (QR/RFID/BOTH), status badge
- Register form inputs: name, location, scanner type, login email, password
- On register: `INSERT User` (role = SCANNER, email, password_hash) + `INSERT Scanner` (created_by = adminId)
- API key is NOT generated here — admin generates it from the detail page

---

### Step 12 — Scanner detail, API key & status toggle 🟡

**Routes:** `GET /admin/scanners/{id}` · `POST …/generate-key` · `POST …/toggle`

**What to build:**
- Detail page: scanner info, type, location, current status badge
- API key section: hidden by default — **Show** and **Copy** buttons to reveal
- Generate key action:
  ```csharp
  var apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
  scanner.ApiKey = apiKey;
  scanner.ApiKeyIssuedAt = DateTime.UtcNow;
  scanner.ApiKeyGeneratedBy = adminId;
  ```
- Toggle action: flip `Scanner.status` between `ACTIVE` ↔ `INACTIVE`
- Optionally allow setting `MAINTENANCE` status via a separate action

---

## Phase 6 — RFID Renewal & Replacement (Bonus)

### Step 13 — Renew RFID semester access 🟡 🔷 T6

**Route:** `POST /admin/students/{id}/rfid/renew`

**Form input:** New `semester_expires_at` date

**Pre-check:** Requires an ACTIVE RFIDCard to exist — block if status is REVOKED.

**Transaction steps (all or nothing):**

```
Step 1 — UPDATE old RFIDCard
         SET status = EXPIRED,
             invalidated_at = now(),
             invalidated_by = adminId,
             invalidation_reason = SEMESTER_END
         WHERE student_id = ? AND status = ACTIVE

Step 2 — INSERT new RFIDCard
         SET card_uid = old.card_uid        ← same physical card
             semester_expires_at = new date from form,
             renewal_number = old.renewal_number + 1,
             renewed_from = old.id,
             is_replacement = false,
             issued_by = adminId

Step 3 — UPDATE Student
         SET rfid_renewal_count = rfid_renewal_count + 1
```

---

### Step 14 — Replace lost / stolen / damaged card 🟡 🔷 T7

**Route:** `POST /admin/students/{id}/rfid/replace`

**Form inputs:** New `card_uid` (new physical card's chip UID), `invalidation_reason` (LOST / STOLEN / DAMAGED)

**Transaction steps (all or nothing):**

```
Step 1 — UPDATE old RFIDCard
         SET status = REVOKED,
             invalidated_at = now(),
             invalidated_by = adminId,
             invalidation_reason = LOST_CARD | STOLEN_CARD | DAMAGED_CARD
         WHERE student_id = ? AND status = ACTIVE

Step 2 — INSERT new RFIDCard
         SET card_uid = NEW chip UID from form   ← different physical card
             semester_expires_at = old.semester_expires_at  ← same expiry
             renewal_number = old.renewal_number + 1,
             renewed_from = old.id,
             is_replacement = true,
             previous_card_uid = old.card_uid,
             issued_by = adminId

Step 3 — UPDATE Student
         SET rfid_renewal_count = rfid_renewal_count + 1
```

> NOTE: Device rows are NOT touched. They link to `Student.id`, not `RFIDCard.id`. The new card immediately covers all existing approved devices.

---

## Phase 7 — Admin Dashboard (Required — Build Last)

### Step 15 — Admin dashboard summary 🔴

**Route:** `GET /admin/dashboard`

**What to build:**
- Stat cards:
  - Count of PENDING device requests
  - Count of ACTIVE devices
  - Count of students with `is_rfid_enabled = true`
  - Count of ACTIVE scanners
- Recent scan log table: last 10 `GateScanLog` rows — device name, student, scan type, result (`is_allowed`), timestamp
- Quick navigation links: "View all pending", "Manage students", "Manage scanners"

> Build this last — it reads from all other modules and depends on them being functional.

---

## Transaction Reference

| Transaction | Step | Course Req? | Route |
|-------------|------|-------------|-------|
| T1 — Approve device request | Step 4 | 🔴 Required | `POST /admin/requests/{id}/approve` |
| T2 — Issue RFID card | Step 8 | 🔴 Required | `POST /admin/students/{id}/rfid/issue` |
| T6 — Renew RFID semester | Step 13 | 🟡 Bonus | `POST /admin/students/{id}/rfid/renew` |
| T7 — Replace lost card | Step 14 | 🟡 Bonus | `POST /admin/students/{id}/rfid/replace` |

---

## Transaction Code Template

All multi-step operations must follow this pattern exactly:

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Step 1 — ...
    // Step 2 — ...
    // Step 3 — ...

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (Exception)
{
    await transaction.RollbackAsync();
    throw; // or return error view
}
```

---

## Build Order Summary

```
Phase 1  →  Step 1   Admin login + controller scaffold
Phase 2  →  Step 2   Pending requests list
             Step 3   Request detail page
             Step 4   Approve (T1) ← required transaction
             Step 5   Reject
Phase 3  →  Step 6   Student list
             Step 7   Student detail
             Step 8   Issue RFID card (T2) ← required transaction
Phase 4  →  Step 9   Device list + detail
             Step 10  Revoke QR token
Phase 5  →  Step 11  Scanner list + register      ← bonus
             Step 12  Scanner detail + API key     ← bonus
Phase 6  →  Step 13  RFID semester renewal (T6)   ← bonus
             Step 14  Lost card replacement (T7)   ← bonus
Phase 7  →  Step 15  Admin dashboard (build last)
```

---

## Definition of Done (per step)

A step is complete when ALL of the following are true:

- [ ] Controller action handles the request and returns the correct view or response
- [ ] View renders correctly and client-side validation passes
- [ ] Database rows are created/updated correctly — verified in Azure SQL
- [ ] If the step involves a transaction: rollback scenario tested and confirmed by Member 6
- [ ] Role restriction enforced — non-admin roles cannot access the route
- [ ] No hardcoded test data left in controller or view
- [ ] Member 6 has written and run a test case for this step

---

*PaScan Admin Module Implementation Plan*
*ELNET Final Project · ASP.NET Core MVC · Azure SQL*