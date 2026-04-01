# Research: Phase 1-4 Hardening Audit Findings

**Branch**: `002-phase1-4-hardening` | **Date**: 2026-04-01

## Decision Summary

| # | Decision | Rationale | Alternatives Rejected |
|---|----------|-----------|----------------------|
| R-01 | Fix [Authorize] role hierarchy gaps | Constitution Principle X mandates all higher roles included | Leaving gaps creates unauthorized access paths |
| R-02 | Add project-context validation to all diagnostic queries/commands | Cross-project data leakage is a critical security flaw | Client-only validation (bypassable via URL manipulation) |
| R-03 | Register TenantContextMiddleware in pipeline | Currently defined but not wired — tenant isolation unenforced | Relying on controller-level checks only (inconsistent, error-prone) |
| R-04 | Implement role-based dashboard routing in HomeController | Currently all users see same generic view after login | Separate dashboard controllers per role (over-engineering) |
| R-05 | Build context-switching UI in top navigation bar | Currently API-only — users cannot switch context from UI | Requiring re-login for context change (poor UX) |
| R-06 | Fix "UserId" claim bug — controllers read wrong claim type | Latent bug: controllers read "UserId" but claim is set as ClaimTypes.NameIdentifier | Adding duplicate "UserId" claim (unnecessary, confusing) |
| R-07 | Add return URL support to context selection flow | Currently always redirects to Home/Index, losing user's intended destination | Session-based storage (overkill for a query parameter) |
| R-08 | Use Playwright for E2E testing | Constitution approves xUnit; Playwright is the industry standard for .NET browser testing | Selenium (heavier, more boilerplate), Cypress (not .NET native) |
| R-09 | Seed data via PostDeployment scripts (numbered, idempotent) | Consistent with existing pattern (000-003); deterministic and reproducible | In-memory seeding (not shared with manual testing), API-based seeding (fragile) |

---

## Directed Corrections (Known Issues)

### DC-01: Authorization Role Hierarchy Violations

**Finding**: Two controllers have incorrect `[Authorize(Roles)]` attributes.

| Controller | Current | Correct | Issue |
|-----------|---------|---------|-------|
| `Participant/DiagnosticController` (Line 12) | `Entrepreneur,IncubatorAdmin,GlobalAdmin` | `Entrepreneur,Mentor,ProjectCoordinator,IncubatorAdmin,GlobalAdmin` | Missing Mentor, ProjectCoordinator |
| AnswerCorrectionController (Line 11) | `ProjectCoordinator,IncubatorAdmin,GlobalAdmin,Mentor` | Already correct | Mentor is intentionally included as a lower-privilege role that needs answer correction access; all higher roles are present |

**Action**: Fix DiagnosticController Participant area. AnswerCorrectionController is correct (Mentor is the target role, and all higher roles are included).

### DC-02: MenuConfiguration — Participant Menu Missing Roles

**Finding**: The Participant menu group includes `["Entrepreneur", "IncubatorAdmin", "GlobalAdmin"]` but is missing `Mentor` and `ProjectCoordinator` per the hierarchy rule.

**Action**: Add `ProjectCoordinator` and `Mentor` to the Participant menu group roles. Constitution Principle X: "Menu items MUST include higher roles in the roles array."

### DC-03: HomeController — No Role-Based Dashboard Routing

**Finding**: `HomeController.Index()` renders a single generic view for all roles. After context selection, ContextController redirects to `Home/Index` regardless of role.

**Action**: Implement role-based routing:
- GlobalAdmin → `Platform/Incubators/Index` (Platform dashboard)
- IncubatorAdmin → `Administration/Dashboard/Index`
- ProjectCoordinator → `Coordination/Diagnostics/Index` (or a coordinator dashboard)
- Mentor → Coordination area (mentoring dashboard when available, fallback to diagnostics)
- Entrepreneur → `Participant/Diagnostic/Index` (or participant dashboard)
- Sponsor → Read-only dashboard (to be created)

### DC-04: Context Selection — No Return URL Support

**Finding**: `ContextController.SetContext()` (Line 71) always redirects to `Home/Index`. Deep-links and return-after-context-selection are not supported.

**Action**: Accept `returnUrl` parameter in Select GET/POST, validate it (prevent open redirect), and redirect to it after successful context selection.

### DC-05: Context Switching — No User-Facing UI

**Finding**: Context switch API endpoint exists (`POST /api/context/switch`) but there is:
- No context-switching button/dropdown in the top navigation bar
- No display of current active context (incubator, project, role) anywhere in the UI
- No unsaved-changes warning before switching

**Action**: Add context display and switch UI to `_TopBar.cshtml`. Show current role, incubator name, and project name. Include a dropdown to switch context.

### DC-06: Platform vs Administration Navigation Separation

**Finding**: MenuConfiguration correctly separates "Plataforma" (GlobalAdmin-only) from "Administración" (IncubatorAdmin+GlobalAdmin). The structure exists but needs visual reinforcement — both appear as equal-weight sidebar groups without clear grouping headers.

**Action**: Add visual section dividers or group headers in `_Navigation.cshtml` to distinguish platform-level vs context-level navigation. Ensure GlobalAdmin sees both sections simultaneously with clear separation when operating within a context.

---

## Undirected Findings (Discovered Issues)

### UF-01: CRITICAL — Cross-Project Data Leakage in Diagnostic Module

**Severity**: CRITICAL (Security)

**Finding**: Multiple diagnostic query handlers and controllers do NOT filter by project context:

| Component | Issue | Impact |
|-----------|-------|--------|
| `GetProjectFormHandler` | Loads form by ExternalId only — no project filter | Any user can view any form by GUID |
| `GetDiagnosticResponseHandler` | Loads response by ExternalId only — no project filter | Any authorized user can view any response |
| `SubmitDiagnosticResponseHandler` | Validates form exists but not that it belongs to user's project | User can submit diagnostic for wrong project's form |
| `CorrectAnswerHandler` | Loads response by ExternalId only — no project validation | Mentor can correct answers in another project |
| `DiagnosticsController.Details()` | No project context check before loading form | Cross-project form access |
| `AnswerCorrectionController.Index()` | No project context check | Cross-project response access |
| `AnswerCorrectionController.Correct()` | No project context check | Cross-project answer correction |

**Action**: Add project context filtering to all diagnostic repositories, query handlers, and controllers. Repository methods must accept projectId parameter. Handlers must validate project ownership.

### UF-02: CRITICAL — "UserId" Claim Never Set

**Severity**: CRITICAL (Latent Bug)

**Finding**: Two controllers read a `"UserId"` claim that is never populated:
- `Participant/DiagnosticController.cs` (Line 81): `User.FindFirst("UserId")?.Value`
- `Coordination/AnswerCorrectionController.cs` (Line 41): `User.FindFirst("UserId")?.Value`

The login flow sets `ClaimTypes.NameIdentifier` (not `"UserId"`). These are different claim types. The read always returns `null`, causing TryParse to fail and the controller to return an error.

**Action**: Replace `User.FindFirst("UserId")` with `User.FindFirst(ClaimTypes.NameIdentifier)` in both controllers.

### UF-03: HIGH — TenantContextMiddleware Not Registered

**Severity**: HIGH (Infrastructure Gap)

**Finding**: `TenantContextMiddleware` is defined in `/Infrastructure/Authorization/TenantContextMiddleware.cs` with extension method `UseTenantContext()`, but it is NOT called in `Program.cs`. The middleware populates `ITenantContext.CurrentIncubatorId` from claims — without it, `ITenantContext` is always empty.

**Action**: Register `app.UseTenantContext()` in `Program.cs` after `UseAuthorization()`. Verify all services depending on `ITenantContext` work correctly after activation.

### UF-04: HIGH — SessionAuthenticationMiddleware Completely Stubbed

**Severity**: HIGH (Security Infrastructure)

**Finding**: `SessionAuthenticationMiddleware` is an empty passthrough (`await _next(context)`). Comment indicates it's deferred to T032/T034. Session tokens are created during login but never validated on subsequent requests — a stolen or expired token remains valid until the cookie expires.

**Action**: This is out of scope for Phase 1-4 hardening (deferred to future task T032/T034). Document as a known limitation. Add a comment in the codebase noting the security risk.

### UF-05: MEDIUM — ContextController.GetUserId() Force Unwrap

**Severity**: MEDIUM (Crash Risk)

**Finding**: `ContextController.GetUserId()` (Line 109) uses `User.FindFirst(ClaimTypes.NameIdentifier)!.Value` with the null-forgiving operator. If the claim is missing (e.g., corrupted cookie), this throws NullReferenceException.

**Action**: Replace with safe parsing: `long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)` and redirect to login if parsing fails.

### UF-06: MEDIUM — No Diagnostic Seed Data

**Severity**: MEDIUM (Testing Gap)

**Finding**: PostDeployment scripts (000-003) seed roles, GlobalAdmin, and subscription plan, but NO diagnostic data (form templates, project forms, questions, answer options). Testing the diagnostic module requires manual data creation.

**Action**: Create seed script `004.SeedTestData.sql` with:
- Multiple test users (multi-role, multi-incubator, multi-project)
- Test incubators and projects
- Diagnostic form templates with questions and answer options
- Cloned project forms
- Sample diagnostic responses

### UF-07: LOW — No E2E Test Infrastructure

**Severity**: LOW (currently — HIGH for this effort)

**Finding**: `tests/Mentoory.Tests.E2E/` exists but is empty. No Playwright configuration, no test files.

**Action**: Configure Playwright in the E2E project. Write tests for all user stories defined in the spec.

### UF-08: LOW — Administration DashboardController Has No Context Validation

**Severity**: LOW

**Finding**: `DashboardController.Index()` in the Administration area does not read or validate ActiveIncubatorId. It may render correctly due to data filtering elsewhere, but the controller itself does not enforce context.

**Action**: Add ActiveIncubatorId validation in DashboardController, consistent with other Administration controllers.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Cross-project data leakage already exploitable | HIGH | CRITICAL | Priority fix — add project filtering to all diagnostic handlers/repos |
| UserId claim bug causes diagnostic submission failures | HIGH | HIGH | Quick fix — change claim type reference |
| Missing middleware causes silent tenant isolation failure | MEDIUM | HIGH | Register middleware; verify downstream services |
| Dashboard routing confuses users after login | HIGH | MEDIUM | Implement role-based routing in HomeController |
| No E2E tests means corrections can't be proven | HIGH | HIGH | Set up Playwright and write comprehensive tests |
