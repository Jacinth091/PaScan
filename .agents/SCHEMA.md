# SCHEMA.md — PaScan Database Schema
### Campus Device Entry Management System
> Azure SQL Database | Entity Framework Core | ASP.NET Core MVC

---

## Table of Contents
1. [Overview](#1-overview)
2. [Enums](#2-enums)
3. [Tables](#3-tables)
   - [Course](#course)
   - [User](#user)
   - [Student](#student)
   - [Admin](#admin)
   - [Scanner](#scanner)
   - [DeviceRequest](#devicerequest)
   - [DeviceRequestAccessory](#devicerequestaccessory)
   - [Device](#device)
   - [DeviceAccessory](#deviceaccessory)
   - [QRToken](#qrtoken)
   - [RFIDCard](#rfidcard)
   - [GateScanLog](#gatescanlog)
4. [Relationships](#4-relationships)
5. [Design Decisions](#5-design-decisions)
6. [Transactions](#6-transactions)
7. [Denial Reasons Reference](#7-denial-reasons-reference)

---

## 1. Overview

PaScan uses a single Azure SQL database with 12 tables across four logical groups:

| Group | Tables | Purpose |
|---|---|---|
| **Academic** | `Course` | Reference data for student enrollment |
| **Users & Profiles** | `User`, `Student`, `Admin`, `Scanner` | Authentication and role-based profiles |
| **Device Workflow** | `DeviceRequest`, `DeviceRequestAccessory`, `Device`, `DeviceAccessory` | Registration, approval, and device tracking |
| **Access & Logging** | `QRToken`, `RFIDCard`, `GateScanLog` | Gate access tokens and scan history |

### Key principles this schema follows

- **Auth and profile are separated.** `User` handles login credentials only. `Student`, `Admin`, and `Scanner` store profile data linked via `user_id`.
- **Requests and approved records are separated.** `DeviceRequest` is the student's submitted form — immutable after submission. `Device` is created only when an admin approves the request and is the source of truth.
- **Tokens are never mutated on renewal.** On every QR or RFID renewal, a new row is inserted and the old row is set to `EXPIRED`. This preserves a full history chain.
- **Soft deletes everywhere.** Records are never physically deleted. `deleted_at` is set instead. EF Core global query filters exclude soft-deleted rows automatically.
- **RFID is per-student, QR is per-device.** One RFID card covers all of a student's approved devices. One QR token is tied to one specific device.

---

## 2. Enums

All enums are stored as `varchar` in Azure SQL and converted using EF Core's `.HasConversion<string>()`.

---

### `Role`
Controls which dashboard and pages a user can access.

| Value | Used by | Login identifier |
|---|---|---|
| `ADMIN` | Admin profile | Email |
| `STUDENT` | Student profile | Student number |
| `SCANNER` | Scanner profile | Email (QR) or api_key (RFID hardware) |

---

### `ScanType`
The method used to scan at the gate.

| Value | Description |
|---|---|
| `QR` | Phone camera reads a QR code |
| `RFID` | Hardware reader taps the student's school ID card |

---

### `ScannerType`
What a registered scanner device is capable of handling.

| Value | Description |
|---|---|
| `QR` | Only handles QR code scans |
| `RFID` | Only handles RFID card taps |
| `BOTH` | Handles both methods |

---

### `ScannerStatus`
Operational state of a scanner device.

| Value | Description |
|---|---|
| `ACTIVE` | Scanner is live and accepting scans |
| `INACTIVE` | Scanner is disabled by admin |
| `MAINTENANCE` | Scanner is temporarily offline |

---

### `RegisterStatus`
Lifecycle state of a device registration request.

| Value | Description |
|---|---|
| `PENDING` | Submitted by student, awaiting admin review |
| `APPROVED` | Admin approved — `Device` row has been created |
| `REJECTED` | Admin rejected — `rejection_reason` is filled |

---

### `DeviceStatus`
State of an approved device record.

| Value | Description |
|---|---|
| `ACTIVE` | Device is approved and can pass through the gate |
| `REVOKED` | Manually revoked by admin |
| `EXPIRED` | Device registration has lapsed |

---

### `TokenStatus`
State of a `QRToken` or `RFIDCard` row.

| Value | Description |
|---|---|
| `ACTIVE` | Token is valid and scannable |
| `EXPIRED` | Token lapsed naturally — eligible for renewal |
| `REVOKED` | Manually killed by admin — requires admin action to reactivate |

> Only one row per device (QR) or per student (RFID) should have `status = ACTIVE` at any time. Enforced at the application layer.

---

### `DeviceType`
The category of device being registered.

| Value | Description |
|---|---|
| `LAPTOP` | Laptop computer |
| `DESKTOP` | Desktop computer |
| `TABLET` | Tablet device |
| `PHONE` | Mobile phone |

---

### `InvalidationReason`
Why an `RFIDCard` was set to `EXPIRED` or `REVOKED`.

| Value | When used |
|---|---|
| `LOST_CARD` | Physical school ID card was lost |
| `STOLEN_CARD` | Physical school ID card was stolen |
| `DAMAGED_CARD` | Physical card chip is unreadable |
| `ADMIN_REVOKED` | Admin manually revoked the card |
| `SEMESTER_END` | Semester expired — card was renewed with a new row |

---

### `RevocationReason`
Why a `QRToken` was set to `REVOKED`.

| Value | When used |
|---|---|
| `ADMIN_REVOKED` | Admin manually revoked the token |
| `DEVICE_LOST` | Student reported the device as lost |
| `DEVICE_STOLEN` | Student reported the device as stolen |
| `ACCOUNT_DISABLED` | Student account was disabled |

---

## 3. Tables

---

### `Course`
Reference table for academic courses. Used to associate students with their enrolled program.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `int` | PK, auto-increment | Surrogate key |
| `course_code` | `varchar(15)` | unique, not null | e.g. `BSIT`, `BSCS` |
| `course_name` | `varchar(255)` | not null | e.g. `Bachelor of Science in Information Technology` |
| `is_active` | `boolean` | default `true` | Soft-disable a course without deleting |
| `created_at` | `timestamp` | default `getutcdate()` | |
| `updated_at` | `timestamp` | default `getutcdate()` | |
| `deleted_at` | `timestamp` | null | Soft delete |

---

### `User`
Authentication table only. Stores login credentials for all roles. Profile data lives in the role-specific tables.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `student_number` | `varchar(50)` | unique, **null** | Filled for `STUDENT` role only |
| `email` | `varchar(255)` | unique, **null** | Filled for `ADMIN` and `SCANNER` roles only |
| `password_hash` | `varchar(255)` | not null | Bcrypt hashed password |
| `role` | `Role` | not null | `ADMIN`, `STUDENT`, or `SCANNER` |
| `is_active` | `boolean` | default `true` | Disabled accounts cannot log in |
| `created_at` | `timestamp` | default `getutcdate()` | |
| `updated_at` | `timestamp` | default `getutcdate()` | |
| `deleted_at` | `timestamp` | null | Soft delete |

> **Rule:** `student_number` must not be null when `role = STUDENT`. `email` must not be null when `role = ADMIN` or `SCANNER`. Enforced at the application layer on account creation.

---

### `Student`
Profile data for students. Linked one-to-one with `User`.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `user_id` | `uuid` | unique, not null, FK → `User.id` | One-to-one with User |
| `student_number` | `varchar(50)` | unique, not null | Matches `User.student_number` |
| `first_name` | `varchar(200)` | not null | |
| `last_name` | `varchar(200)` | not null | |
| `middle_name` | `varchar(100)` | null | |
| `email` | `varchar(255)` | null | Contact email only — not used for login |
| `contact_number` | `varchar(15)` | null | For admin/guard reach-out |
| `course_id` | `int` | not null, FK → `Course.id` | |
| `year_level` | `int` | not null | Valid values: 1 to 5 |
| `is_rfid_enabled` | `boolean` | default `false` | Flipped to `true` when `RFIDCard` is issued |
| `rfid_renewal_count` | `int` | not null, default `0` | Audit counter — incremented on every RFID renewal or card replacement |
| `is_active` | `boolean` | default `true` | |
| `created_at` | `timestamp` | default `getutcdate()` | |
| `updated_at` | `timestamp` | default `getutcdate()` | |
| `deleted_at` | `timestamp` | null | Soft delete |

---

### `Admin`
Profile data for admin users. Linked one-to-one with `User`.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `user_id` | `uuid` | unique, not null, FK → `User.id` | One-to-one with User |
| `first_name` | `varchar(200)` | not null | |
| `last_name` | `varchar(200)` | not null | |
| `middle_name` | `varchar(100)` | null | |
| `department` | `varchar(100)` | null | e.g. `IT Department`, `Security Office` |
| `is_active` | `boolean` | default `true` | |
| `created_by` | `uuid` | null, FK → `Admin.id` | Which admin created this account |
| `created_at` | `timestamp` | default `getutcdate()` | |
| `updated_at` | `timestamp` | default `getutcdate()` | |
| `deleted_at` | `timestamp` | null | Soft delete |

---

### `Scanner`
Represents a physical scanning device registered at a gate location. Linked one-to-one with a `SCANNER` role `User` account.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `user_id` | `uuid` | unique, not null, FK → `User.id` | The SCANNER role account for this device |
| `scanner_name` | `varchar(100)` | not null | e.g. `Main Gate Scanner` |
| `description` | `varchar(255)` | null | Optional notes about this scanner |
| `location` | `varchar(255)` | not null | e.g. `Main Entrance Building A` |
| `scanner_type` | `ScannerType` | not null | `QR`, `RFID`, or `BOTH` |
| `status` | `ScannerStatus` | not null, default `ACTIVE` | `ACTIVE`, `INACTIVE`, or `MAINTENANCE` |
| `api_key` | `varchar(255)` | unique, null | RFID hardware authentication key — stored plain text, admin can view anytime |
| `api_key_issued_at` | `timestamp` | null | When the current key was generated |
| `api_key_generated_by` | `uuid` | null, FK → `Admin.id` | Which admin generated the key |
| `installed_at` | `timestamp` | null | When the physical device was installed |
| `created_by` | `uuid` | not null, FK → `Admin.id` | Which admin registered this scanner |
| `created_at` | `timestamp` | default `getutcdate()` | |
| `updated_at` | `timestamp` | default `getutcdate()` | |
| `deleted_at` | `timestamp` | null | Soft delete |

> **Authentication:**
> - `scanner_type = QR` → gate phone logs into `/login/scanner` using `User.email` + password. Session-based.
> - `scanner_type = RFID` → hardware sends `X-Api-Key: {api_key}` header with every request. No session.
> - Admin logs into the RFID scanner's `User` account to manage and view the `api_key` from the admin panel.

---

### `DeviceRequest`
The student's submitted device registration form. This is a **snapshot** of what the student declared. It is **never modified after submission** — preserved as an immutable audit trail.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `student_id` | `uuid` | not null, FK → `Student.id` | |
| `purpose` | `varchar(500)` | not null | Why the student is bringing the device |
| `device_name` | `varchar(255)` | not null | Friendly name e.g. `My Laptop` |
| `device_type` | `DeviceType` | not null | `LAPTOP`, `DESKTOP`, `TABLET`, `PHONE` |
| `brand` | `varchar(100)` | not null | e.g. `Dell`, `Apple` |
| `model` | `varchar(100)` | not null | e.g. `XPS 15`, `MacBook Pro` |
| `serial_number` | `varchar(255)` | not null | Device serial number |
| `operating_system` | `varchar(150)` | null | e.g. `Windows 11`, `macOS Ventura` |
| `color` | `varchar(50)` | null | Helps physical identification at gate |
| `processor` | `varchar(150)` | null | Hardware spec — null if not applicable |
| `motherboard` | `varchar(150)` | null | Hardware spec — null if not applicable |
| `memory` | `varchar(150)` | null | e.g. `16GB DDR4` |
| `storage` | `varchar(150)` | null | Covers HDD and SSD e.g. `512GB SSD` |
| `monitor_size` | `varchar(100)` | null | e.g. `15.6 inches` |
| `casing` | `varchar(100)` | null | Desktop casing description |
| `has_cd_rom` | `boolean` | default `false` | Whether the device has a CD/DVD drive |
| `status` | `RegisterStatus` | not null, default `PENDING` | `PENDING`, `APPROVED`, or `REJECTED` |
| `reviewed_by` | `uuid` | null, FK → `Admin.id` | Which admin reviewed this request |
| `reviewed_at` | `timestamp` | null | When the review happened |
| `rejection_reason` | `varchar(500)` | null | Required when `status = REJECTED` |
| `remarks` | `varchar(500)` | null | Optional admin notes |
| `created_at` | `timestamp` | default `getutcdate()` | |
| `updated_at` | `timestamp` | default `getutcdate()` | |
| `deleted_at` | `timestamp` | null | Soft delete |

> **Hardware specs** (`processor`, `motherboard`, `memory`, `storage`, `monitor_size`, `casing`) are all nullable because they do not apply to every device type. A phone has no motherboard. A laptop has no separate casing. The UI should show or hide these fields based on `device_type`.

---

### `DeviceRequestAccessory`
The list of accessories the student declared alongside their device request. One row per accessory.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `device_request_id` | `uuid` | not null, FK → `DeviceRequest.id` | |
| `accessory_name` | `varchar(255)` | not null | e.g. `Mouse`, `Charger`, `Laptop Bag` |
| `quantity` | `int` | not null, default `1` | |

---

### `Device`
Created by the system when an admin approves a `DeviceRequest`. This is the **source of truth** for all approved devices. Data is copied from the `DeviceRequest` at the time of approval to preserve the original submission independently.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `student_id` | `uuid` | not null, FK → `Student.id` | |
| `original_request_id` | `uuid` | unique, not null, FK → `DeviceRequest.id` | One-to-one — one request produces one device |
| `device_name` | `varchar(255)` | not null | Copied from `DeviceRequest` |
| `device_type` | `DeviceType` | not null | Copied from `DeviceRequest` |
| `brand` | `varchar(100)` | not null | Copied from `DeviceRequest` |
| `model` | `varchar(100)` | not null | Copied from `DeviceRequest` |
| `serial_number` | `varchar(255)` | unique, not null | Copied from `DeviceRequest` |
| `operating_system` | `varchar(150)` | null | Copied from `DeviceRequest` |
| `color` | `varchar(50)` | null | Copied from `DeviceRequest` |
| `processor` | `varchar(150)` | null | Copied from `DeviceRequest` |
| `motherboard` | `varchar(150)` | null | Copied from `DeviceRequest` |
| `memory` | `varchar(150)` | null | Copied from `DeviceRequest` |
| `storage` | `varchar(150)` | null | Copied from `DeviceRequest` |
| `monitor_size` | `varchar(100)` | null | Copied from `DeviceRequest` |
| `casing` | `varchar(100)` | null | Copied from `DeviceRequest` |
| `has_cd_rom` | `boolean` | default `false` | Copied from `DeviceRequest` |
| `status` | `DeviceStatus` | not null, default `ACTIVE` | `ACTIVE`, `REVOKED`, or `EXPIRED` |
| `approved_by` | `uuid` | not null, FK → `Admin.id` | Which admin approved |
| `approved_at` | `timestamp` | not null | When approval happened |
| `remarks` | `varchar(500)` | null | Admin notes on approval |
| `qr_renewal_count` | `int` | not null, default `0` | Audit counter — incremented every time QR is renewed |
| `registered_at` | `timestamp` | default `getutcdate()` | |
| `updated_at` | `timestamp` | default `getutcdate()` | |
| `deleted_at` | `timestamp` | null | Soft delete |

---

### `DeviceAccessory`
Accessories copied from `DeviceRequestAccessory` when the device request is approved. Linked to `Device`, not `DeviceRequest`.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `device_id` | `uuid` | not null, FK → `Device.id` | |
| `accessory_name` | `varchar(255)` | not null | e.g. `Mouse`, `Charger`, `Laptop Bag` |
| `quantity` | `int` | not null, default `1` | |

---

### `QRToken`
One active QR token per device at any time. On renewal, a new row is inserted and the previous row is set to `EXPIRED` — enabling a full renewal history chain via `renewed_from`.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `device_id` | `uuid` | not null, FK → `Device.id` | **Not unique** — allows multiple history rows per device |
| `student_id` | `uuid` | not null, FK → `Student.id` | Denormalized for faster scan lookup — avoids a join through `Device` during gate scans |
| `token_value` | `varchar(500)` | unique, not null | The scannable GUID value encoded in the QR image. Fresh GUID on every renewal |
| `issued_at` | `timestamp` | default `getutcdate()` | |
| `expires_at` | `timestamp` | not null | `issued_at + 30 days` |
| `status` | `TokenStatus` | not null, default `ACTIVE` | `ACTIVE`, `EXPIRED`, or `REVOKED` |
| `renewal_number` | `int` | not null, default `1` | `1` = original issuance. Incremented on each renewal |
| `renewed_from` | `uuid` | null, FK → `QRToken.id` | Points to the previous token row. `null` if this is the original |
| `revoked_at` | `timestamp` | null | Filled only when `status = REVOKED` |
| `revoked_by` | `uuid` | null, FK → `Admin.id` | Which admin revoked — filled only when `status = REVOKED` |
| `revocation_reason` | `RevocationReason` | null | Why the token was revoked |
| `created_at` | `timestamp` | default `getutcdate()` | |

> **Querying the active token for a device:**
> ```sql
> SELECT * FROM QRToken
> WHERE device_id = ? AND status = 'ACTIVE'
> ```
> Only one row will match at any time — enforced at the application layer.

---

### `RFIDCard`
One active RFID card per student at any time. Represents the student's physical school ID card. On semester renewal, a new row is inserted with the same `card_uid`. On lost/stolen/damaged card replacement, a new row is inserted with a new `card_uid`.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `student_id` | `uuid` | not null, FK → `Student.id` | **Not unique** — allows multiple history rows per student |
| `card_uid` | `varchar(100)` | unique, not null | Physical chip ID read by the RFID hardware. Always globally unique |
| `issued_at` | `timestamp` | default `getutcdate()` | |
| `semester_expires_at` | `timestamp` | not null | Set by admin at issuance — end of semester date |
| `status` | `TokenStatus` | not null, default `ACTIVE` | `ACTIVE`, `EXPIRED`, or `REVOKED` |
| `renewal_number` | `int` | not null, default `1` | `1` = original issuance. Incremented on each renewal or replacement |
| `renewed_from` | `uuid` | null, FK → `RFIDCard.id` | Points to the previous card row. `null` if original |
| `is_replacement` | `boolean` | default `false` | `true` means the physical card changed (lost/stolen/damaged). `false` means same card, new semester |
| `previous_card_uid` | `varchar(100)` | null | Old chip ID preserved for audit — filled only when `is_replacement = true` |
| `issued_by` | `uuid` | not null, FK → `Admin.id` | Which admin issued this card |
| `invalidated_at` | `timestamp` | null | When the card was expired or revoked |
| `invalidated_by` | `uuid` | null, FK → `Admin.id` | Which admin invalidated the card |
| `invalidation_reason` | `InvalidationReason` | null | Why the card was invalidated |
| `created_at` | `timestamp` | default `getutcdate()` | |

> **Querying the active card for a student:**
> ```sql
> SELECT * FROM RFIDCard
> WHERE student_id = ? AND status = 'ACTIVE'
> ```
> Only one row will match at any time — enforced at the application layer.

> **Lost card note:** When a card is replaced, the student's devices are **not affected**. Devices are linked to `Student.id`, not `RFIDCard.id`. The new card immediately covers all the student's approved devices.

---

### `GateScanLog`
An immutable log of every scan attempt at the gate. One row per device per scan event.

- **QR scan** → one row per scan (one device)
- **RFID tap** → multiple rows per tap (one row per approved device, batch inserted in one transaction)

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `uuid` | PK, default `newid()` | |
| `device_id` | `uuid` | not null, FK → `Device.id` | Which device was scanned |
| `qr_token_id` | `uuid` | null, FK → `QRToken.id` | Filled when `scan_type = QR`. Null otherwise |
| `rfid_card_id` | `uuid` | null, FK → `RFIDCard.id` | Filled when `scan_type = RFID`. Null otherwise |
| `scan_type` | `ScanType` | not null | `QR` or `RFID` |
| `scanner_id` | `uuid` | not null, FK → `Scanner.id` | Which scanner triggered this event |
| `scanned_at` | `timestamp` | default `getutcdate()` | When the scan happened |
| `is_allowed` | `boolean` | not null | `true` = device passed through. `false` = denied |
| `denial_reason` | `varchar(255)` | null | See denial reasons reference below. Null when `is_allowed = true` |

> **Mutual exclusivity:** `qr_token_id` and `rfid_card_id` are mutually exclusive — exactly one is filled per row depending on `scan_type`. Enforced at the application layer in `ScanController`.

> **Records are never deleted.** `GateScanLog` has no `deleted_at` column — scan history is permanent.

---

## 4. Relationships

```
Course          1 ──< *   Student                 (course_id)
User            1 ─── 1   Student                 (user_id) — one-to-one
User            1 ─── 1   Admin                   (user_id) — one-to-one
User            1 ─── 1   Scanner                 (user_id) — one-to-one
Admin           1 ──< *   Scanner                 (created_by)
Admin           1 ──< *   Admin                   (created_by — self-referencing)
Student         1 ──< *   DeviceRequest           (student_id)
DeviceRequest   1 ──< *   DeviceRequestAccessory  (device_request_id)
DeviceRequest   1 ─── 1   Device                  (original_request_id) — created on approval
Student         1 ──< *   Device                  (student_id)
Device          1 ──< *   DeviceAccessory         (device_id)
Device          1 ──< *   QRToken                 (device_id) — one ACTIVE at a time
Student         1 ──< *   RFIDCard                (student_id) — one ACTIVE at a time
QRToken         1 ──< *   QRToken                 (renewed_from — self-referencing chain)
RFIDCard        1 ──< *   RFIDCard                (renewed_from — self-referencing chain)
Scanner         1 ──< *   GateScanLog             (scanner_id)
Device          1 ──< *   GateScanLog             (device_id)
QRToken         1 ──< *   GateScanLog             (qr_token_id)
RFIDCard        1 ──< *   GateScanLog             (rfid_card_id)
```

---

## 5. Design Decisions

### Why `DeviceRequest` and `Device` are separate tables
The request is what the student declared. The device is what was approved. Keeping them separate means:
- The original student submission is never altered — it is preserved exactly as submitted
- Admin can edit device details during approval without touching the student's original record
- `GateScanLog` references `Device`, not `DeviceRequest` — if the request was rejected or pending, it cannot appear in scan logs

### Why `QRToken` and `RFIDCard` use new rows on renewal instead of updates
Updating the same row on renewal would corrupt historical data. A `GateScanLog` row references a `qr_token_id` — if that token row is updated, the log now points to a token with different values than when the scan happened. New rows preserve the integrity of every historical log reference.

### Why `student_id` is denormalized on `QRToken`
During a gate scan, the system needs to know which student a token belongs to. Without denormalization, it would require two joins: `QRToken → Device → Student`. With `student_id` on `QRToken`, it is one direct lookup. Gate scans are high-frequency operations so this is an intentional performance tradeoff.

### Why `User.email` and `User.student_number` are both nullable
Each role uses a different login identifier. Students log in with `student_number`. Admins and scanners log in with `email`. Storing both as nullable in one `User` table keeps authentication logic in one place — one table to query, one session to manage — without duplicating `password_hash` across separate login tables.

### Why `RFIDCard.student_id` is not unique
A student can have multiple `RFIDCard` rows over time — one per semester renewal and one per card replacement. The `unique` constraint is removed to allow this history. Only one row will ever have `status = ACTIVE` at any time, enforced at the application layer.

### Why `Device` is linked to `Student.id` and not `RFIDCard.id`
This is intentional. If a student loses their physical ID card, their registered devices should not be affected. By linking devices to `Student.id`, a card replacement only requires issuing a new `RFIDCard` row — all existing `Device` rows remain valid and immediately accessible through the new card.

---

## 6. Transactions

All transactions must be implemented using `DbContext.Database.BeginTransaction()` with `Commit()` and `Rollback()`. No partial saves allowed — if any step fails, all changes roll back.

---

### T1 — Admin Approves Device Request ✅ Required
**Trigger:** Admin clicks Approve on a pending `DeviceRequest`

```
1. UPDATE DeviceRequest  SET status = APPROVED, reviewed_by, reviewed_at
2. INSERT Device         (all pink slip fields copied from DeviceRequest)
3. INSERT DeviceAccessory (all rows copied from DeviceRequestAccessory)
4. INSERT QRToken        (token_value = new GUID, expires_at = now + 30 days,
                          status = ACTIVE, renewal_number = 1, renewed_from = null)
```

---

### T2 — Admin Issues RFID Card ✅ Required
**Trigger:** Admin issues an RFID card to a student

```
1. INSERT RFIDCard  (card_uid, semester_expires_at, renewal_number = 1,
                     renewed_from = null, issued_by = adminId)
2. UPDATE Student   SET is_rfid_enabled = true, rfid_renewal_count = 0
```

---

### T3 — QR Gate Scan ✅ Required
**Trigger:** Student's QR code is scanned at the gate by a SCANNER account

```
1. Validate Scanner  (status = ACTIVE, scanner_type = QR or BOTH)
2. Validate QRToken  (status = ACTIVE, expires_at > now())
3. Validate Device   (status = ACTIVE)
4. INSERT GateScanLog (is_allowed = true/false, denial_reason if denied)
```

---

### T4 — RFID Gate Scan ✅ Required
**Trigger:** Student taps school ID on RFID hardware reader

```
1. Validate Scanner via api_key  (status = ACTIVE, scanner_type = RFID or BOTH)
2. Validate RFIDCard             (status = ACTIVE, semester_expires_at > now())
3. Fetch all Devices             WHERE student_id = ? AND status = ACTIVE
4. Batch INSERT GateScanLog      (one row per device, all in one transaction)
```

---

### T5 — Student Renews QR 🟡 Bonus
**Trigger:** Student clicks Renew on device detail page when QR is `EXPIRED`

```
1. UPDATE old QRToken  SET status = EXPIRED
                       WHERE device_id = ? AND status = ACTIVE
2. INSERT new QRToken  (new token_value GUID, expires_at = now + 30 days,
                        renewal_number = old + 1, renewed_from = old.id)
3. UPDATE Device       SET qr_renewal_count = qr_renewal_count + 1
```

> Only allowed when current token `status = EXPIRED`. Blocked if `status = REVOKED`.

---

### T6 — Admin Renews RFID Semester 🟡 Bonus
**Trigger:** Admin renews a student's RFID access for a new semester

```
1. UPDATE old RFIDCard  SET status = EXPIRED, invalidated_at = now(),
                            invalidation_reason = SEMESTER_END
                        WHERE student_id = ? AND status = ACTIVE
2. INSERT new RFIDCard  (same card_uid, new semester_expires_at,
                         renewal_number = old + 1, renewed_from = old.id,
                         is_replacement = false)
3. UPDATE Student       SET rfid_renewal_count = rfid_renewal_count + 1
```

---

### T7 — Admin Replaces Lost / Stolen / Damaged Card 🟡 Bonus
**Trigger:** Admin handles a physical card replacement request

```
1. UPDATE old RFIDCard  SET status = REVOKED, invalidated_at = now(),
                            invalidated_by = adminId,
                            invalidation_reason = LOST_CARD / STOLEN_CARD / DAMAGED_CARD
                        WHERE student_id = ? AND status = ACTIVE
2. INSERT new RFIDCard  (NEW card_uid from replacement card,
                         same semester_expires_at as old card,
                         renewal_number = old + 1, renewed_from = old.id,
                         is_replacement = true,
                         previous_card_uid = old.card_uid)
3. UPDATE Student       SET rfid_renewal_count = rfid_renewal_count + 1
```

> Student's `Device` rows are **not touched**. They remain linked to `Student.id` and are immediately accessible through the new card.

---

## 7. Denial Reasons Reference

Used in `GateScanLog.denial_reason` when `is_allowed = false`.

| Value | Meaning | Scan type |
|---|---|---|
| `EXPIRED` | QR token or RFID card has passed its expiry date | Both |
| `REVOKED` | Token or card was manually revoked by admin | Both |
| `NOT_APPROVED` | Device exists but `status != ACTIVE` | Both |
| `DEVICE_NOT_FOUND` | No device matches the scanned token | QR |
| `SCANNER_INACTIVE` | Scanner is `INACTIVE` or `MAINTENANCE` | Both |

---

*PaScan SCHEMA.md — Last updated based on full project design discussion*  
*ASP.NET Core MVC | Azure SQL Database | Entity Framework Core*