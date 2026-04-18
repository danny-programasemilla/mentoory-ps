# HTTP Contract: Administrative Enrollment

**Feature**: 016-registration-access-hardening
**Endpoint**: `POST /Administration/Users/Enroll` (MVC action `Mentoory.Web.Areas.Administration.Controllers.UsersController.Enroll`)
**Audience**: Authenticated `IncubatorAdmin` or `GlobalAdmin`

This contract captures the admin path's response shape. Central invariants: **authorisation required**, and **field-attributed feedback** on uniqueness conflicts (the opposite of the public path).

---

## Request

- **Method**: `POST`
- **Path**: `/Administration/Users/Enroll`
- **Attributes**: `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]`, `[ValidateAntiForgeryToken]`
- **Content-Type**: `application/x-www-form-urlencoded`

### Form body

Same fields as the public endpoint (`Email`, `Country`, `NationalId`, `FirstName`, `LastName`, `Password`, `__RequestVerificationToken`). Same validation rules (FR-016-10) — including the new `PasswordIdentifyingDataRule`.

---

## Pre-authorisation

Unauthenticated or wrong-role requests MUST NOT reach the action. ASP.NET Core's auth middleware returns:

- **Unauthenticated**: `302 Found` → `/Account/Login?returnUrl=...` (existing behaviour)
- **Authenticated but wrong role**: `403 Forbidden` (existing behaviour)

An unauthenticated observer cannot distinguish these responses from any other `[Authorize]`-gated endpoint — so the specific-feedback behaviour (below) is NOT usable as an enumeration oracle (FR-016-09).

---

## Response classes

### Class A — Success

Triggered when all validator rules pass AND the submission is a fresh combination.

- **Status**: `302 Found`
- **Location header**: `/Administration/Users` (member list)
- `TempData["SuccessMessage"]` = `"Usuario inscrito exitosamente."` (displayed as toast on next GET)

Side effects: user persisted, verification email dispatched.

### Class B — Specific field-attributed failure (uniqueness conflict)

Triggered when all validator rules pass BUT the submission collides with an existing user on either email or national ID.

- **Status**: `200 OK`
- **Content-Type**: `text/html; charset=utf-8`
- **Body**: re-rendered `Enroll.cshtml` with ModelState containing exactly one field-attributed error:

| Conflict | ModelState key | Message |
|----------|----------------|---------|
| National ID already registered | `NationalId` | `"Ya existe una cuenta con este número de identificación."` |
| Email already registered | `Email` | `"Ya existe una cuenta con este correo electrónico."` |

If the command surfaces both (unlikely given the ordered check in `IUserProvisioningService`), the handler reports the first one encountered.

### Class C — Validator-rule failure

Triggered when any FluentValidation rule fails.

- **Status**: `200 OK`
- **Content-Type**: `text/html; charset=utf-8`
- **Body**: re-rendered `Enroll.cshtml` with per-field ModelState errors (standard admin-side behaviour).

Admins see the concrete per-field error copy from the validator — unlike the public path, no message-collapsing happens.

---

## Logging contract

Unchanged from existing admin logging pipeline — the `MediatRExecutor.SendAndLogIfFailureAsync(...)` call in the controller logs command failures at the existing granularity. No new structured outcome log is introduced for the admin path (FR-016-06 is public-scoped).

---

## Authorisation invariants

- `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]` stays in force; includes both higher privilege roles per Constitution § Principle X.
- `[ValidateAntiForgeryToken]` stays in force on POST.
- No new permission check is introduced; the existing role guard is deemed sufficient per spec Assumptions.

---

## Explicit non-goals

- Rate limiting on the admin endpoint. None exists today; the authenticated-role guard is the gating mechanism.
- Structured outcome logging at the same granularity as the public endpoint. Out of scope — FR-016-06 is public-only.
