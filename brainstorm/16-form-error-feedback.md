# Brainstorm: Inline form-error feedback

**Date:** 2026-06-11
**Status:** active

## Problem Framing

Server- and client-side validation errors are currently rendered with the default
ASP.NET Core pattern: an `asp-validation-summary="All"` block at the top of the form
**plus** a per-field `asp-validation-for` span. The result is that every field error
appears **twice** — once in the top summary and again under its field — and the field
itself gets no visual emphasis (only the default border). The errors read as plain,
duplicated text and are easy to miss.

Goal: make form errors clear and eye-catching, and stop duplicating them. A validated
prototype was built on `Platform/Incubators/Create` and approved by the user:

- Top validation summary **removed**.
- Invalid fields get a danger-red border, a soft red glow (persistent, including on
  focus), and an alert-circle **icon inside the field on the right** (top-right for
  textareas).
- The per-field message is restyled (clearer, danger-colored).

This brainstorm decides how to turn that prototype into an app-wide pattern.

## Approaches Considered

### Page-level (non-field) errors — where do they go once the summary is gone?

Several controllers add errors with no field to attach to, e.g.
`ModelState.AddModelError(string.Empty, "Error al crear la incubadora.")`
(`IncubatorsController.cs:71`, `:118`). These render **only** in the summary today, so
removing it would silently swallow real failures (DB error, concurrency conflict,
"name already exists", etc.).

- **A — Toast notification** *(chosen)*: surface non-field errors as a red toast via the
  existing `showToast` (Bootstrap Toast in `site.js`). Nothing renders at the top of the
  form. Fully satisfies the "no summary" goal.
- **B — Slim inline alert (non-field only)**: keep a compact top alert that shows *only*
  page-level errors (never field errors → no duplication).
- **C — Both**: toast + inline alert as a persistent record.

### Rollout scope

- **Everywhere, in waves** *(chosen)*: all form types, sequenced to de-risk the odd ones.
- Conventional forms only (CRUD + auth), defer atypical.
- Standard CRUD only.

### Application model

- **Centralized convention** *(chosen)*: field-error CSS lives globally in
  `mentoory.css` (every invalid field app-wide gets it automatically, current and
  future forms, zero per-form CSS); a shared layout-level helper turns page-level
  ModelState errors into toasts automatically. Per-form work = delete the summary line.
- Opt-in per form (class + partial each time) — isolated but repetitive.
- Reusable field component / tag helper — best uniformity, over-engineering for now (YAGNI).

## Decision

Build an app-wide **inline form-error pattern**, delivered as a **centralized
convention** and rolled out **in waves**:

1. **Field-level errors** — global CSS in `mentoory.css` targeting the
   `input-validation-error` class that ASP.NET tag helpers + jQuery-unobtrusive already
   emit: danger border, persistent soft glow, alert-circle icon inside-right (top-right
   for textareas), restyled inline message. Applies to every form automatically.
2. **Page-level errors** — a centralized mechanism renders non-field ModelState errors
   and fires `showToast(..., 'danger')` on load, so each form needs no per-form wiring.
3. **Summary removal** — delete `asp-validation-summary` from each form, wave by wave.

**Rollout waves:**
- **Wave 1 — Standard CRUD (8):** Incubators Create/Edit, Administration Projects/Create,
  Users Enroll, Users RegisterInternal, Coordination Diagnostics Clone, Knowledge
  CreateTemplate.
- **Wave 2 — Auth pages (4):** Login, ForgotPassword, ResetPassword, ChangePassword.
  Uses `_AuthLayout` — requires the toast container/component to be present there.
- **Wave 3 — Atypical (2):** BatchUpload (file input) and Participant Diagnostic
  (dynamically-generated questionnaire) — non-standard structure, handled with care.

The Incubators/Create prototype is the reference implementation (its scoped `@section
Styles` block is promoted into global `mentoory.css`).

## Key Requirements

- **No duplication:** field errors appear once, under/inside the field — never repeated
  in a top summary.
- **Eye-catching invalid fields:** danger border + persistent soft danger glow (also when
  focused, overriding the default primary focus ring) + alert-circle icon inside on the
  right (top-right for textareas). Implemented globally via `mentoory.css`.
- **No lost errors:** genuine page-level (non-field) errors must still reach the user —
  as a red toast via the existing `showToast` infrastructure.
- **Centralized:** field CSS is global; page-level-error→toast is wired once at the
  layout level. Per-form change is limited to removing the summary markup.
- **All UI text in Spanish** (unchanged).
- **Phased delivery** across the three waves; each wave independently shippable.

## Open Questions

- **Same-request toast mechanism:** define exactly how non-field ModelState errors on a
  re-rendered (non-redirect) view are turned into toasts — an on-load script reading
  rendered error data, a data attribute, or a `_Layout` partial. (TempData is for the
  redirect case; many of these are same-request re-renders.)
- **Auth layout:** confirm `_AuthLayout` includes the toast container + `Toast` view
  component (Wave 2), or add it. "Invalid credentials" on Login is itself a page-level
  error and must toast correctly.
- **`<select>` fields:** native dropdown arrow vs the inside-right error icon — decide
  icon handling for `.form-select` to avoid overlap.
- **Atypical forms (Wave 3):** icon placement on file inputs (BatchUpload); per-question
  error display for the dynamic Participant Diagnostic questionnaire — may need bespoke
  treatment rather than the generic field rule.
- **Accessibility:** `aria-invalid` on fields, `role="alert"`/`aria-live` for messages and
  toasts so errors are announced by screen readers.
- **Icon rendering:** keep the CSS background-image alert-circle (approved) vs a real
  Tabler icon element — confirm the background approach holds for selects/file inputs.
