# Implementation Plan: Inline form-error feedback

**Branch**: `024-form-error-feedback` | **Date**: 2026-06-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/024-form-error-feedback/spec.md`

## Summary

Replace the default ASP.NET validation UX (top `asp-validation-summary` block + duplicated per-field messages, plain look) with an app-wide inline pattern: a **single centralized CSS rule** in `mentoory.css` styles every invalid field (danger border, persistent glow, inside-right alert-circle icon, restyled message); each in-scope form **drops its `asp-validation-summary`**; and genuine **page-level (non-field) errors are surfaced as danger toasts** via a shared layout-level partial reusing the existing `showToast`. Delivered in three waves (standard CRUD → auth → atypical), each independently shippable.

## Technical Context

**Language/Version**: C# / .NET 10.0; Razor views; CSS; vanilla JavaScript (ES5-compatible)

**Primary Dependencies**: ASP.NET Core MVC, Tabler v1.4.0 (Bootstrap 5), jQuery + jquery-validation-unobtrusive, Tabler Icons (existing `showToast` Bootstrap Toast helper)

**Storage**: N/A (no persistence changes)

**Testing**: Manual verification per wave (quickstart.md); optional Playwright E2E reusing the existing `Mentoory.Tests.E2E` fixture. Tests are not mandated by the spec.

**Target Platform**: Web (modern browsers)

**Project Type**: Web application (Razor MVC modular monolith) — Web layer only

**Performance Goals**: N/A — CSS + a small inline script; negligible cost

**Constraints**: Zero build warnings (`TreatWarningsAsErrors`); all user-facing text in Spanish; **no Domain/Application/Infrastructure changes**; no validation-rule changes

**Scale/Scope**: 13 in-scope form views across 3 waves; 1 global CSS block; 1 shared toast partial; 2 layouts touched

## Constitution Check

*GATE: Web-layer-only change. No architectural surface touched.*

- **No web deps in Domain/Application** — ✅ N/A, no Domain/Application changes.
- **Controllers never inject repositories / no business logic in Web** — ✅ unchanged; the only controller-adjacent work is reusing existing `ModelState` errors, no new logic.
- **All UI text in Spanish** — ✅ error copy is Spanish; no new English-facing strings.
- **No `DateTime.UtcNow`, ExternalId routing, command/handler rules** — ✅ N/A (no domain/app code).
- **Zero warnings** — ✅ enforced by build gate.

**Result: PASS** — no violations, Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/024-form-error-feedback/
├── spec.md           # feature spec (done)
├── plan.md           # this file
├── research.md       # decisions & findings
├── quickstart.md     # per-wave manual verification
├── checklists/
│   └── requirements.md
└── tasks.md          # /speckit-tasks output
```

### Source Code (repository root)

```text
Mentoory.Web/
├── wwwroot/
│   ├── css/mentoory.css                     # [MODIFY] global field-error rule (promote prototype)
│   └── js/site.js                            # [VERIFY] showToast; ensure available to both layouts
├── Views/Shared/
│   ├── _Layout.cshtml                        # [MODIFY] include _ModelStateToasts partial
│   ├── _AuthLayout.cshtml                    # [MODIFY] load site.js + include _ModelStateToasts partial
│   ├── _ModelStateToasts.cshtml              # [NEW] renders non-field ModelState errors -> showToast
│   └── Components/Toast/Default.cshtml       # [VERIFY] aria-live/role for announcement
└── Areas/**/Views/**                          # [MODIFY] remove asp-validation-summary from 13 forms
    └── Platform/Views/Incubators/Create.cshtml # [MODIFY] drop @section Styles + .form-inline-validation
```

**Structure Decision**: Web application, single project (`Mentoory.Web`). All changes are in views, CSS, one new shared partial, and the two layouts. No new projects, no backend/domain changes.

## Design

### D1 — Global field-error CSS (FR-001, FR-005, FR-006, FR-010, FR-013)

Promote the approved `Incubators/Create` prototype rules into `mentoory.css`, **unscoped** from `.form-inline-validation` so they apply to every `.form-control.input-validation-error` / `.form-select.input-validation-error` app-wide:

- Danger border + soft danger glow via `--tblr-danger` / `--tblr-danger-rgb`.
- Inside-right alert-circle icon (CSS `background-image` data-URI), top-right for `textarea`.
- `:focus` override so the danger glow persists on focus (not the primary ring).
- `<select>`: shift icon clear of the native arrow (extra right padding) — see research D-ICON.
- `.field-validation-error` message styling (extend the existing rule in `mentoory.css`).
- Valid-again clears automatically because the validator removes `input-validation-error` (FR-006).

This is the centralization: forms get the look with **zero per-form CSS**.

### D2 — Page-level errors → toast (FR-004, FR-011, US1/US2)

New partial `Views/Shared/_ModelStateToasts.cshtml`:

- Iterates `ViewData.ModelState` for entries whose **key is empty** (`string.Empty`) with errors — i.e. genuine non-field errors. Field-keyed errors are ignored (they render inline).
- Emits a small `<script>` calling `showToast(@Html.Raw(JSON-encoded message), 'danger')` for each, on `DOMContentLoaded`.
- Included once in `_Layout.cshtml` and `_AuthLayout.cshtml` → no per-form wiring (centralized).

### D3 — Auth layout enablement (FR-011)

`_AuthLayout.cshtml` already renders the Toast container component but does **not** load `site.js` (where `showToast` is defined). Add the `site.js` reference (and jQuery, if not already present) so `showToast` exists on auth pages, then include `_ModelStateToasts`.

### D4 — Summary removal (FR-002, FR-003)

Remove the `asp-validation-summary` element from each of the 13 in-scope views. Per-field `asp-validation-for` spans stay. Migrate `Incubators/Create.cshtml` off its page-scoped `@section Styles` and `.form-inline-validation` class.

### D5 — Accessibility (FR-008)

jquery-validation-unobtrusive already sets `aria-invalid`/`aria-describedby` on fields. Ensure the Toast container is an `aria-live="assertive"` / `role="alert"` region so toasts are announced. Verify in `Components/Toast/Default.cshtml`.

## Phases (map to user stories / waves)

- **Phase 1 (US1 / Wave 1)** — Foundation + standard CRUD: D1 (global CSS), D2 (toast partial), D5, include in `_Layout`; remove summaries from the 7 CRUD forms; migrate `Incubators/Create`. **MVP.**
- **Phase 2 (US2 / Wave 2)** — Auth: D3 (auth layout enablement); remove summaries from the 4 auth forms; verify credential failures toast.
- **Phase 3 (US3 / Wave 3)** — Atypical: BatchUpload (file-input icon adaptation) + Participant Diagnostic (per-question inline errors); remove summaries.

Each phase ends green (build + manual quickstart) and is independently shippable.

## Complexity Tracking

No constitution violations — section intentionally empty.
