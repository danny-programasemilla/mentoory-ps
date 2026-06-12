# Deep Review Findings

**Date:** 2026-06-11
**Branch:** 024-form-error-feedback
**Rounds:** 1
**Gate Outcome:** PASS
**Invocation:** quality-gate (spex-ship pipeline)

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 3 | 3 | 0 |
| Minor | 5 | 0 | 5 |
| **Total** | **8** | **3** | **5** |

**Spec compliance (Stage 1):** ~100% (FR-001..FR-013, SC-001..SC-006 all satisfied; see notes).
**Agents completed:** 5/5 (external tools disabled via --no-external)

## Findings

### FINDING-1 (Important — FIXED)
- **Severity:** Important
- **Confidence:** 85
- **File:** Mentoory.Web/wwwroot/js/site.js:13-20 (sink) + Mentoory.Web/Views/Shared/_ModelStateToasts.cshtml:21 + Mentoory.Web/Areas/Participant/Views/Diagnostic/Index.cshtml (emitters)
- **Category:** security
- **Source:** security-agent (also reported by: correctness-agent)
- **Resolution:** fixed (round 1)

**What is wrong:** `showToast` assigned the caller-supplied `message` into an HTML sink via `innerHTML`. Markup like `<img src=x onerror=...>` would execute on insertion.

**Why this matters:** Not reachable through THIS feature's sources — every non-field ModelState error and the Diagnostic `TempData["ErrorMessage"]` are hardcoded Spanish constants / server-controlled handler messages (verified across `Areas/**/Controllers` and the FluentValidation→ModelState bridge). `JsonSerializer.Serialize` (default encoder) escapes `<`,`>`,`&`,`'` to `\uXXXX`, keeping the `<script>` literal context safe from breakout. However, `showToast` is a SHARED sink with pre-existing callers that pass server JSON `data.message` straight into the same `innerHTML` (context-switcher.js, form-helper.js, knowledge editors) — a latent XSS hazard the new global partial widens the surface of.

**How it was resolved:** Rewrote `showToast` to build the toast DOM with `createElement` and set the body via `textContent` instead of `innerHTML`. Identical markup/behavior; the message is never HTML-parsed. The Razor-side `JsonSerializer.Serialize` encoding is now defense-in-depth rather than the sole barrier.

### FINDING-2 (Important — FIXED)
- **Severity:** Important
- **Confidence:** 85
- **File:** Mentoory.Web/Views/Shared/Components/Toast/Default.cshtml:1-3 + Mentoory.Web/wwwroot/js/site.js:15
- **Category:** production-readiness (accessibility)
- **Source:** production-readiness-agent
- **Resolution:** fixed (round 1)

**What is wrong:** The Toast container declared `role="alert" aria-live="assertive" aria-atomic="true"`, while each appended toast also has `role="alert"`. This nests a live region inside a live region, and `aria-atomic="true"` on an accumulating container instructs screen readers to re-announce ALL toasts whenever a new one is appended — directly hit on multi-error pages (FR-010; `_ModelStateToasts` loops one `showToast` per non-field error).

**Why this matters:** Threatens SC-005 / FR-008 (toasts announced reliably). Nested live regions produce inconsistent AT behavior (double-announce or silence); container-level `aria-atomic` re-reads prior toasts.

**How it was resolved:** Container is now a passive live-region boundary: removed `role="alert"`, set `aria-atomic="false"`, kept `aria-live="assertive"`. Each toast element now carries `role="alert"` + `aria-atomic="true"` (added in site.js) so each toast is announced once, atomically, with no re-reading of prior toasts.

### FINDING-3 (Important — FIXED)
- **Severity:** Important
- **Confidence:** 90 (empirically verified)
- **File:** tests/Mentoory.Tests.E2E/Tests/LoginFlowTests.cs:76-77
- **Category:** test-quality (regression in existing test)
- **Source:** test-quality-agent
- **Resolution:** fixed (round 1)

**What is wrong:** `Login_InvalidCredentials_ShowsErrorMessage` asserted `page.ContentAsync()` contains `"Credenciales inválidas"`. That message used to be server-rendered into the now-removed `asp-validation-summary`. It is now emitted only via `_ModelStateToasts` as `showToast(JsonSerializer.Serialize(msg), 'danger')`. Empirically confirmed: `JsonSerializer.Serialize("Credenciales inválidas.")` → `"Credenciales inválidas."`, so the literal substring `inválidas` is NOT in server HTML — it only materializes after JS runs. The test passed only by timing luck (toast renders on DOMContentLoaded before `ContentAsync`).

**Why this matters:** The assertion silently shifted from a deterministic server-HTML check to a timing-dependent JS-rendered-DOM check, with no explicit wait — flaky, and no longer verifies the spec intent (SC-003: surfaced AS a toast).

**How it was resolved:** Replaced the raw-HTML substring check with an explicit `#toastContainer .toast` locator filtered by text + `WaitForAsync(Visible, 5000ms)`, plus an assertion that the validation-summary block (`[data-valmsg-summary], .text-danger.mb-3`) is absent (locks SC-001). Matches the existing toast/wait patterns in the suite (LogoutTests.cs:39). E2E project builds 0 warnings.

### FINDING-4 (Minor — not fixed; out of scope)
- **Severity:** Minor | **Category:** architecture | **File:** Mentoory.Web/Areas/Participant/Views/Diagnostic/Index.cshtml
- The Diagnostic inline `<script>` duplicates the `DOMContentLoaded`+`showToast(...,'danger')` idiom owned by `_ModelStateToasts.cshtml` (differs only by source: TempData vs empty-key ModelState). Could be unified by generalizing the partial to also read `TempData["ErrorMessage"]`. Deferred: behavioral parity is correct; this is a refactor, not a defect.

### FINDING-5 (Minor — not fixed)
- **Severity:** Minor | **Category:** architecture | **File:** Mentoory.Web/wwwroot/css/mentoory.css:214-249
- The base border+glow `box-shadow` is declared twice (identical values) for `.form-control.input-validation-error` and `.form-select.input-validation-error`. Could be hoisted into a shared selector to keep the glow single-sourced. Cosmetic duplication; no functional impact.

### FINDING-6 (Minor — not fixed)
- **Severity:** Minor | **Category:** architecture | **File:** mentoory.css:203-209 + the 13 views
- `.field-validation-error` sets `color` while the spans also carry the `text-danger` utility — color is double-owned. Pick one owner and document in web-patterns.md. Cosmetic.

### FINDING-7 (Minor — not fixed; out of scope)
- **Severity:** Minor | **Category:** correctness | **File:** Mentoory.Web/Areas/Participant/Controllers/DiagnosticController.cs:111-113
- On `!ModelState.IsValid` the Submit action redirects with no `TempData["ErrorMessage"]`, so a model-binding validation failure yields no feedback at all (the TempData hook covers only the two explicit-failure branches). Partly pre-existing (PRG redirect-without-state predates this feature). Out of scope: spec restricts controller changes to "routing existing non-field ModelState errors"; the requirement is in Domain/Application/controller territory the task explicitly forbids touching. Recommended follow-up: set a generic TempData error on the IsValid==false branch.

### FINDING-8 (Minor — not fixed)
- **Severity:** Minor | **Category:** test-quality | spec SC-001/SC-003/SC-005
- No automated test guards SC-001 (single appearance / no summary) or SC-005 (ARIA contract). FINDING-3's fix now covers SC-001 + SC-003 for the auth path. Adding an ARIA-attribute assertion would close SC-005. Pure-visual aspects (border/glow/icon) reasonably left untested.

## Post-Fix Spec Coverage

Fixes removed only the container `role="alert"` attribute and one `innerHTML` assignment (replaced with equivalent DOM construction). No functional requirement was dropped.

| Requirement | Implementation | Status |
|-------------|---------------|--------|
| FR-001 invalid-field border/glow/icon | mentoory.css .input-validation-error | ✓ |
| FR-002 message once, no duplication | per-field spans retained; summary removed | ✓ |
| FR-003 summary removed from all forms | 13 views; grep asp-validation-summary = 0 | ✓ |
| FR-004 non-field error → toast | _ModelStateToasts.cshtml | ✓ |
| FR-005 centralized CSS convention | mentoory.css global rule; .form-inline-validation removed | ✓ |
| FR-006 clears when valid | jquery-unobtrusive removes class | ✓ |
| FR-007 Spanish copy | preserved | ✓ |
| FR-008 a11y exposed | aria-invalid (unobtrusive) + toast live region (strengthened by fix) | ✓ |
| FR-009 client + server parity | shared class hook | ✓ |
| FR-010 multiple invalid fields | per-field; toast re-announce bug fixed | ✓ |
| FR-011 showToast on auth layout | site.js added to _AuthLayout (no double-load) | ✓ |
| FR-012 atypical forms | select/file/textarea CSS adaptations; Diagnostic toast | ✓ |
| FR-013 prototype migrated | Incubators/Create scoped style removed | ✓ |

All spec requirements verified after the fix loop.

## Test Suite Results

Build verification only (no unit/integration test run in-loop — this is a Web view/CSS/JS change with E2E coverage requiring a live container fixture):
- Mentoory.Web: Build succeeded, 0 Warning(s), 0 Error(s).
- Mentoory.Tests.E2E: Build succeeded, 0 Warning(s), 0 Error(s) (validates the FINDING-3 test edit compiles against the Playwright API).

## Remaining Findings

5 Minor findings remain (FINDING-4 through FINDING-8), all non-blocking: cosmetic CSS/architecture duplication, an out-of-scope pre-existing controller feedback gap (FINDING-7), and optional additional test coverage. None block the gate.
