# PaScan Frontend Skill Design
### Mobile-Responsive Frontend Blueprint
> ASP.NET Core MVC Razor Views | Student/Admin/Scanner Role UX

---

## 1. Purpose

This document defines the frontend design direction for PaScan, focused on:

- Mobile-first layouts for real gate and campus usage
- Consistent UX across Student, Admin, and Scanner roles
- UI behavior aligned with project modules and transaction flows
- Fast implementation in Razor Views with maintainable CSS/JS structure

This is the practical design guide for frontend implementation, not just visual styling.

---

## 2. Source Context Used

This frontend design is based on project requirements and structure from:

- `.agents/DESIGN.md`
- `.agents/GUIDELINES.md`
- `.agents/MODULES.md`
- `.agents/SCHEMA.md`

All routes, statuses, module priorities, and transaction states in this file are aligned to those documents.
Visual language and UI tone should always follow the Apple-inspired standards in `.agents/DESIGN.md`.

---

## 3. Product UX Summary

PaScan replaces paper pink slips with a digital approval and gate scan flow.

### Core user goals by role

| Role | Primary goals | Must be optimized for |
|---|---|---|
| Student | Submit device request, monitor status, show/renew QR | Mobile phone first |
| Admin | Review requests, approve/reject, issue RFID, monitor activity | Desktop first, tablet supported |
| Scanner | Scan QR quickly and clearly at gate | Mobile phone camera workflow |

### UX outcomes to protect

- Lowest friction for scanning and showing pass credentials
- Clear status communication (`PENDING`, `APPROVED`, `REJECTED`, `ACTIVE`, `EXPIRED`, `REVOKED`)
- Error messages that explain exactly what to do next
- Fast loading on low-spec mobile devices and unstable campus connections

---

## 4. Mobile-Responsive Strategy

## 4.1 Breakpoints

Use mobile-first CSS with these breakpoints:

| Range | Width | Usage |
|---|---|---|
| `xs` | `< 576px` | Small phones |
| `sm` | `>= 576px` | Large phones |
| `md` | `>= 768px` | Tablets |
| `lg` | `>= 992px` | Laptops/desktop |
| `xl` | `>= 1200px` | Large desktop dashboards |

## 4.2 Grid and spacing behavior

- Default mobile layout is single-column.
- Card stacks on `xs/sm`; two columns allowed on `md` for summary blocks.
- Data tables collapse into stacked cards on `xs/sm`.
- Minimum touch target: 44px height for buttons and row actions.
- Form controls must be full-width on mobile.
- Keep critical actions (Approve, Reject, Scan, Renew) pinned in visible action bars.

## 4.3 Navigation behavior

- Student and Scanner: top app bar + simple action menu; avoid deep side nav.
- Admin: sidebar on `lg+`, collapsible drawer on `md` and below.
- Show role label in header for context (`STUDENT`, `ADMIN`, `SCANNER`).

---

## 5. Visual System

## 5.1 Color tokens (status-first)

Use semantic status colors consistently in badges, alerts, and result banners:

- `APPROVED`, `ACTIVE`, allowed scan: green family
- `PENDING`: amber family
- `REJECTED`, `REVOKED`, denied scan: red family
- `EXPIRED`: neutral/slate family
- Informational metadata (dates, IDs): blue-gray family

Suggested CSS variables in `wwwroot/css/site.css`:

```css
:root {
  --color-bg: #f5f7fb;
  --color-surface: #ffffff;
  --color-text: #1f2937;
  --color-muted: #64748b;

  --status-active: #166534;
  --status-active-bg: #dcfce7;

  --status-pending: #92400e;
  --status-pending-bg: #fef3c7;

  --status-rejected: #991b1b;
  --status-rejected-bg: #fee2e2;

  --status-expired: #334155;
  --status-expired-bg: #e2e8f0;

  --status-revoked: #7f1d1d;
  --status-revoked-bg: #fecaca;
}
```

## 5.2 Typography and hierarchy

- Strong page title + context subtitle (role or workflow state)
- Section headings per card group
- Dense metadata in smaller muted text
- Numeric dashboard counters in large weight with short labels

## 5.3 Reusable components

- `StatusBadge`
- `SummaryCard`
- `DataCardTable` (table on desktop, cards on mobile)
- `ActionBar` (sticky on mobile for primary actions)
- `InlineAlert` for transaction feedback
- `EmptyState` with one clear CTA

---

## 6. Information Architecture and Route UX Map

### 6.1 Required routes

| Route | Role | Module | Device priority |
|---|---|---|---|
| `/login/student` | Student | M2 | Mobile first |
| `/login/admin` | Admin | M2 | Desktop first |
| `/login/scanner` | Scanner | M2 | Mobile first |
| `/student/dashboard` | Student | M9 | Mobile first |
| `/student/devices/register` | Student | M3 | Mobile first |
| `/student/devices` | Student | M5 | Mobile first |
| `/student/devices/{id}` | Student | M5 | Mobile first |
| `/admin/dashboard` | Admin | M10 | Desktop first |
| `/admin/requests` | Admin | M4 | Desktop first, mobile fallback |
| `/admin/requests/{id}` | Admin | M4 | Desktop first, mobile fallback |
| `/admin/students/{id}/rfid` | Admin | M8 | Desktop first, mobile fallback |
| `/scan/qr` | Scanner | M6 | Mobile first |

### 6.2 Bonus routes (if time allows)

| Route | Role | Module |
|---|---|---|
| `POST /student/devices/{id}/renew-qr` | Student | M11 |
| `POST /admin/students/{id}/rfid/renew` | Admin | M12 |
| `POST /admin/students/{id}/rfid/replace` | Admin | M13 |
| `/admin/scanners` | Admin | M14 |
| `/admin/scanners/{id}` | Admin | M14 |

---

## 7. Page-Level Responsive Blueprints

## 7.1 Authentication pages (`/login/*`)

### Desktop
- Centered auth card with brand panel and short route-specific instructions.
- Keep role-specific helper text visible.

### Mobile
- Full-width form, minimal chrome, single CTA.
- Inputs 16px+ font to avoid iOS zoom.

### Required fields
- Student login: `student_number`, `password`
- Admin/Scanner login: `email`, `password`

---

## 7.2 Student dashboard (`/student/dashboard`)

### Desktop
- Top summary cards: pending requests, approved devices, active QR count, RFID status.
- Two lists below: requests timeline and approved devices.

### Mobile
- Summary cards become horizontal scroll chips or 2x2 compact grid.
- Requests and devices become stacked cards with status badge + quick action.
- Primary CTA pinned: `Register New Device`.

---

## 7.3 Device registration (`/student/devices/register`)

### Desktop
- Two-column form sections: identity and hardware specs.
- Accessory list in repeatable row table.

### Mobile
- Single-column wizard-like sections:
  1. Device basics
  2. Hardware specs
  3. Accessories
  4. Review and submit
- Add/remove accessory controls must stay visible without horizontal scroll.

### Dynamic form behavior
- Show/hide hardware fields based on `DeviceType`.
- Preserve entered values when validation fails.
- Inline validation with clear messages next to fields.

---

## 7.4 Student devices and detail (`/student/devices`, `/student/devices/{id}`)

### Desktop list
- Table with device name, type, status, QR expiry, actions.

### Mobile list
- Card list with status badge, expiry line, and action buttons (`View`, `Renew` if expired).

### Detail page
- Device profile card
- Accessories card
- QR card with token state
- Expiry countdown and renewal action

### State handling
- `ACTIVE`: show QR image and expiry
- `EXPIRED`: hide active QR, show renew prompt
- `REVOKED`: block renewal, show contact-admin notice

---

## 7.5 Admin requests list/detail (`/admin/requests`, `/admin/requests/{id}`)

### Desktop list
- Sortable table with student, device info, date, status, quick actions.

### Mobile list
- Request cards with compact metadata and sticky action row.

### Detail page layout
- Student info panel
- Device spec panel
- Accessory list panel
- Action panel (Approve/Reject)

### Action UX
- Require explicit confirmation modal before approve/reject.
- Disable submit while transaction is processing.
- On success, return to list with success alert and updated badge.

---

## 7.6 RFID management (`/admin/students/{id}/rfid`)

### Desktop
- Card status summary + issue/renew/replace forms in tabs.

### Mobile
- Accordion sections: Current card, Issue card, Renewal, Replacement.
- Keep `card_uid` field prominent and easy to copy/verify.

### Transaction-safe messaging
- Show exact result after issue/renew/replace.
- Show history notes when prior card was invalidated (`LOST_CARD`, `STOLEN_CARD`, `DAMAGED_CARD`, `SEMESTER_END`).

---

## 7.7 Scanner QR page (`/scan/qr`)

### Desktop/tablet
- Camera preview left, result panel right.

### Mobile
- Camera preview full width at top.
- Result banner directly under preview with high contrast:
  - Green: Allowed
  - Red: Denied with denial reason
- `Scan Again` button should always stay visible.

### Performance and reliability
- Reuse media stream; do not reinitialize camera for each result.
- Debounce duplicate scan values for short interval to avoid duplicate logs.
- Provide fallback input mode for manual token testing (dev only).

---

## 7.8 Admin dashboard (`/admin/dashboard`)

### Desktop
- KPI row: Pending Requests, Active Devices, Total Students, Today Scans.
- Two tables: Recent pending requests and recent scan logs.
- Scanner status widget.

### Mobile
- KPI cards stack in 2-column compact grid.
- Tables become collapsible cards with top 3 records and `View More`.

---

## 8. Transaction-Aware UX States

Map frontend messaging to transaction outcomes from schema/modules.

| Transaction | Trigger UI | Loading state | Success state | Failure state |
|---|---|---|---|---|
| T1 Approve request | Approve button on request detail | Disable actions + spinner | Device + QR created message | Rollback error + retry option |
| T2 Issue RFID | Issue form submit | Lock form and show progress | RFID active confirmation | Explain which step failed |
| T3 QR scan | Scanner scan result panel | Scan in progress indicator | Allowed banner + short chime | Denied banner + denial reason |
| T4 RFID scan | API-driven result | Background processing | Allowed and device count | Denied with cause |
| T5/T6/T7 bonus | Renew/replace buttons | Button-level spinner | New token/card active | Show old and attempted state |

Rules:

- Never silently fail a transaction action.
- Always return user to a stable state with updated badges/counters.
- Preserve audit visibility in the UI (old token/card still represented as history).

---

## 9. Accessibility and Usability Requirements

- Meet WCAG contrast targets for status badges and alerts.
- Keyboard support for all forms and modal actions.
- Use ARIA live region for scan result announcements.
- Do not use color alone to communicate state; include text labels.
- Form errors must be announced and linked to invalid fields.

---

## 10. Frontend Performance Rules

- Keep first paint lightweight on mobile (defer non-critical JS).
- Compress and cache QR image responses.
- Limit dashboard query payload shown at once; paginate or lazy-load where possible.
- Minimize reflows in dynamic accessory form operations.
- Avoid heavy third-party UI frameworks that add unnecessary payload.

---

## 11. Recommended Razor/CSS/JS Structure

Use existing MVC folders with clearer frontend grouping.

```text
Views/
  Shared/
    _Layout.cshtml
    _StatusBadge.cshtml
    _InlineAlert.cshtml
  Account/
    StudentLogin.cshtml
    AdminLogin.cshtml
    ScannerLogin.cshtml
  Student/
    Dashboard.cshtml
    RegisterDevice.cshtml
    DeviceList.cshtml
    DeviceDetail.cshtml
  Admin/
    Dashboard.cshtml
    Requests.cshtml
    RequestDetail.cshtml
    StudentRfidDetail.cshtml
  Scan/
    QrScan.cshtml
wwwroot/
  css/
    site.css
    pages/
      student.css
      admin.css
      scan.css
  js/
    scan-qr.js
    device-form.js
    dashboard.js
```

---

## 12. Delivery Plan for Frontend Member

### Week 1
- Build authentication views and student registration form UX.
- Deliver base responsive layout and status badge styles.
- Implement student dashboard basic cards/lists.

### Week 2
- Build admin request list/detail and decision UI.
- Build student device list/detail with QR display states.
- Build scanner QR page with mobile-first camera result UX.
- Build admin RFID page.

### Week 3
- Build admin dashboard polish and responsive tuning.
- Finish bonus pages (renew/replace/scanner management) if required modules are stable.
- Conduct full mobile pass on all required pages.

---

## 13. Frontend Definition of Done

A frontend module is done only if all are true:

- Route view is complete for intended role.
- Works on mobile (`xs/sm`) and desktop (`lg+`) without layout break.
- Status badges and transaction feedback are accurate to backend state.
- Validation and error states are clear and recoverable.
- Page is tested in at least one real phone viewport and one desktop viewport.
- No horizontal scroll on core forms and key action screens.

---

## 14. Final Priorities if Time Is Limited

Build in this order for maximum requirement coverage:

1. Login pages (all roles)
2. Student device registration and dashboard
3. Admin approval list/detail and actions
4. Student QR display page
5. Scanner QR page
6. Admin RFID issue page
7. Admin dashboard polish
8. Bonus renewal and scanner management pages

This preserves the required module flow and keeps the project demo-safe.

---

*PaScan Frontend Skill Design*  
*Aligned to GUIDELINES.md, MODULES.md, and SCHEMA.md*
