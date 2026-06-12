# Research: Inline form-error feedback

**Feature**: 024-form-error-feedback | **Date**: 2026-06-12

## Findings (verified against the codebase)

### R1 — Validation class hooks
ASP.NET tag helpers (server) and jquery-validation-unobtrusive (client) emit `input-validation-error` on invalid inputs and `field-validation-error` on the message span (valid states: `input-validation-valid` / `field-validation-valid`). `_ValidationScriptsPartial` uses stock `jquery.validate` + `jquery.validate.unobtrusive` (no custom `setDefaults`), so these are the reliable, app-wide styling hooks. **Decision**: target `.input-validation-error` / `.field-validation-error` globally.

### R2 — Toast infrastructure
`wwwroot/js/site.js` defines `showToast(message, type)` (Bootstrap `Toast`, ~5s auto-dismiss). A shared Toast view component (`Views/Shared/Components/Toast/Default.cshtml`) renders the container. Several controllers + `form-helper.js` already use toasts. **Decision**: reuse `showToast(msg, 'danger')` for page-level errors; do not introduce a new mechanism.

### R3 — Auth layout gap (corrected from initial assumption)
`_AuthLayout.cshtml` **already** renders the Toast container (`@await Component.InvokeAsync("Toast")`). The real gap: `site.js` (and thus `showToast`) is loaded only by `_Layout.cshtml`, not `_AuthLayout.cshtml`. **Decision**: load `site.js` on `_AuthLayout` so `showToast` is defined there (Wave 2), then include the toast partial.

### R4 — Page-level vs field errors
Controllers add non-field errors via `ModelState.AddModelError(string.Empty, "...")` (e.g. `IncubatorsController.cs:71`, `:118`). These render only in the summary today. **Decision**: a shared partial emits toasts **only** for `ModelState` entries with an empty key; field-keyed errors stay inline. This prevents both duplication and lost errors.

### R5 — Existing CSS state
`mentoory.css` already styles `.field-validation-error` (color + size) and defines `--tblr-danger` / `--tblr-danger-rgb`. `.input-validation-error` is styled only in the `Incubators/Create` prototype's scoped block. **Decision**: extend the existing `.field-validation-error` rule and add a global `.input-validation-error` rule; remove the prototype's scoped block (FR-013).

### D-ICON — Icon placement on non-text inputs
The inside-right `background-image` icon works for text inputs and textareas (top-right). For `<select>`, the native arrow occupies the right edge → **Decision**: add extra right padding / shift the icon left of the arrow; if overlap is unavoidable, drop the inside icon for selects and rely on border + glow + message. For file inputs (BatchUpload), the icon would obscure the file-chooser → **Decision**: border + glow + message only, no inside icon. Border/glow/message always apply.

## Open items deferred to implementation
- Exact right-padding values for `<select>` to clear the Tabler arrow (tune visually).
- Whether the Participant Diagnostic questionnaire renders standard `field-validation-error` spans per question or needs a bespoke message hook (inspect during Wave 3).

## Alternatives considered (rejected)
- **Bootstrap-native `.is-invalid` via a jQuery `setDefaults` shim**: would change the class the validator emits app-wide; higher blast radius and risk than styling the existing `input-validation-error` class. Rejected (YAGNI).
- **Keep a `ModelOnly` summary instead of toasts**: rejected per the approved brainstorm decision (user wants the summary gone; toasts chosen).
- **Per-form opt-in class**: rejected in favor of the centralized global rule (lower churn, future-proof).
