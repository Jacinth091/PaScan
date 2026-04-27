# Frontend Design Skill — PaScan

Use this skill when creating or updating frontend pages, UI specs, CSS, and UX behavior for PaScan.

## Goal

Build mobile-responsive, role-aware frontend experiences for Student, Admin, and Scanner flows in ASP.NET Core MVC Razor Views.

## Required Context (Read First)

Before writing frontend code or design docs, read:

1. `.agents/DESIGN.md`
2. `.agents/GUIDELINES.md`
3. `.agents/MODULES.md`
4. `.agents/SCHEMA.md`

Then align all UI decisions to routes, statuses, and transaction outcomes in those documents.

`DESIGN.md` is the visual authority. If there is a style conflict, follow `DESIGN.md` for color, typography, spacing, radius, and interaction tone.

## Project UX Priorities

1. Student and Scanner flows are mobile-first.
2. Admin flows are desktop-first but must still work on tablets/phones.
3. Visual language must remain Apple-inspired per `.agents/DESIGN.md`.
4. Transaction outcomes must be obvious and traceable in UI states.
5. Required modules/routes take priority over bonus pages.

## Core Routes to Support

### Required

- `/login/student`
- `/login/admin`
- `/login/scanner`
- `/student/dashboard`
- `/student/devices/register`
- `/student/devices`
- `/student/devices/{id}`
- `/admin/dashboard`
- `/admin/requests`
- `/admin/requests/{id}`
- `/admin/students/{id}/rfid`
- `/scan/qr`

### Bonus

- `POST /student/devices/{id}/renew-qr`
- `POST /admin/students/{id}/rfid/renew`
- `POST /admin/students/{id}/rfid/replace`
- `/admin/scanners`
- `/admin/scanners/{id}`

## Mobile-Responsive Rules

- Use mobile-first CSS.
- Breakpoints: `<576`, `>=576`, `>=768`, `>=992`, `>=1200`.
- Default to single-column on phones.
- Convert desktop tables to card lists on small screens.
- Keep primary actions visible and easy to tap (minimum 44px target height).
- Avoid horizontal scrolling on key forms and action pages.

## Status and State Mapping

Use consistent visual semantics for:

- Register/request states: `PENDING`, `APPROVED`, `REJECTED`
- Device/token states: `ACTIVE`, `EXPIRED`, `REVOKED`
- Scan outcomes: allowed vs denied with explicit denial reason

Never rely on color only. Always show text labels.

## Transaction-Aware UX

For T1 to T4 (required) and T5 to T7 (bonus), always include:

1. Loading state (disable duplicate submits)
2. Success state (confirm what changed)
3. Failure state (show reason and recovery action)

No silent failures for approval, issuance, renewal, or scan-related actions.

## Accessibility and Usability

- Maintain accessible contrast in badges and alerts.
- Ensure keyboard-friendly forms and actions.
- Provide clear inline validation messages.
- Add ARIA live updates for scanner result messages where applicable.

## Suggested Frontend Structure

Organize Razor views and assets with reusable UI blocks:

- Shared partials for status badges and alerts
- Role-specific page folders (`Account`, `Student`, `Admin`, `Scan`)
- Page-focused CSS/JS under `wwwroot/css/pages` and `wwwroot/js`

## Implementation Sequence

1. Authentication views
2. Student registration + dashboard
3. Admin request review + decision pages
4. Student QR display views
5. Scanner QR page
6. Admin RFID management page
7. Admin dashboard polish
8. Bonus renewal/scanner management pages

## Definition of Done

A frontend task is complete only when:

- Role route works end-to-end with backend action
- UI works on phone and desktop viewports
- Status badges and messages match backend states
- Validation and error paths are tested
- No layout breakage on core flows

## Extended Reference

For deeper page-by-page wireframe behavior and responsive detail, see:

- `.agents/FRONTEND_SKILL_DESIGN.md`
