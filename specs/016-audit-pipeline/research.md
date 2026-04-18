# Research: Audit Pipeline Wiring

**Feature:** 016-audit-pipeline
**Phase:** 0 (Outline & Research)
**Date:** 2026-04-18

This document resolves the technical unknowns that surfaced while drafting `plan.md`. All items are closed; no NEEDS CLARIFICATION remain.

---

## R-01: JSON serializer for command-payload capture

**Decision:** `System.Text.Json`.

**Rationale:** Already in the .NET 10.0 BCL (zero package additions), satisfies the constitution's zero-warnings and dependency-governance rules, and supports `JsonNode`/`JsonSerializerOptions` for top-level redaction via mutation of the DOM before writing to string.

**Alternatives considered:**
- **Newtonsoft.Json.** Widely available but not currently a project dependency; adding it violates "prefer BCL where possible." Its property-interceptor API is slightly more ergonomic for redaction but not enough to justify a new dependency.
- **Custom reflection-based serializer.** Rejected — reinventing the wheel; maintenance cost outweighs the payload-redaction simplification.

**Implications:** Use `JsonSerializer.SerializeToNode(command, options)` → walk top-level properties → replace matches with the redaction sentinel → `node.ToJsonString(options)`. Options: `WriteIndented = false`, `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` (matches our DTO convention), `DefaultIgnoreCondition = WhenWritingNull`.

---

## R-02: MediatR pipeline registration order

**Decision:** registration order `Validator → Auditing → Transaction`. `AuditingBehavior` wraps `TransactionBehavior` (registered BEFORE it in `Program.cs`); `TransactionBehavior` wraps the handler.

**Rationale:** MediatR pipeline behaviors execute in registration order, meaning the *last* registered behavior is the *innermost* wrapper. For the spec's intent ("business transaction committed or rolled back before audit writes"), audit must be OUTSIDE the transaction, i.e., registered EARLIER. Concretely:

1. `ValidatorBehavior` (outermost) — short-circuits on validation errors before any audit or DB work. Validation failures are not audited (they're input noise, not security events).
2. `AuditingBehavior` — calls `next()`, which delegates to `TransactionBehavior`, which commits or throws. On return, audit writes with the real outcome. On exception, audit writes `Outcome=Failure` + `ExceptionType`, then rethrows.
3. `TransactionBehavior` — wraps the handler, opens/commits/rolls-back the business transaction.
4. Handler — the terminal delegate.

**Alternatives considered:**
- **Auditing innermost (registered last).** Rejected — audit would write before Transaction commits, meaning the entry could say "Success" while the transaction later rolls back.
- **Auditing outermost (before Validator).** Rejected — validation failures would produce audit entries, flooding the log with noise and hiding real security events.

**Implications:** `Program.cs` MediatR registration adds `cfg.AddOpenBehavior(typeof(AuditingBehavior<,>))` between the existing `ValidatorBehavior` and `TransactionBehavior` lines.

---

## R-03: Architecture-test framework + host project

**Decision:** new project `tests/Mentoory.Tests.Architecture/` using **reflection-only** tests with xUnit + FluentAssertions (no external architecture-testing library).

**Rationale:**
- Zero new package dependencies (aligns with dependency governance).
- The assertions needed for FR-015 are narrow and reflection-doable: enumerate loaded assemblies → find types implementing `IBaseRequest` / `IBaseRequest<T>` → match names against the sensitive-action regex → assert `[Audited]` is present.
- Keeps architecture tests discoverable in one place rather than scattered across per-domain test projects.
- Constitution naming convention (`{Domain}.Tests`) is respected — `Mentoory.Tests.Architecture` follows the same dotted-name pattern as the existing `Mentoory.Tests.Integration` and `Mentoory.Tests.E2E`.

**Alternatives considered:**
- **NetArchTest.Rules.** Popular architecture-testing DSL. Rejected — adds a package dependency and the assertions we need are small enough that the DSL is overkill. Revisit if the architecture test suite grows beyond ~5 rules.
- **Host tests inside `Mentoory.Tests.Integration`.** Rejected — architecture tests are compile-time-driven (they load assemblies), not integration tests (which spin up the web host + Respawn); mixing concerns muddies the project purpose.
- **ArchUnitNET.** Same rejection rationale as NetArchTest.

**Implications:**
- New `.csproj` referencing `xunit`, `FluentAssertions`, and `Microsoft.NET.Test.Sdk` (already in use across existing test projects) plus project references to `Mentoory.Shared.Application` (for `IBaseRequest`) and one `ProjectReference` per module to ensure assemblies are loaded.
- A test helper (`SensitiveCommandRegistry`) loads all assemblies matching `Mentoory.*.Application` via `AppDomain.CurrentDomain.GetAssemblies()`, then enumerates types.

---

## R-04: Redaction implementation

**Decision:** post-serialization DOM walk using `JsonNode`. Convert the command to a `JsonObject` → iterate top-level properties → if property name (case-insensitive) matches `AuditOptions.RedactedFields` → replace with `"***REDACTED***"` → serialize to string.

**Rationale:**
- Simpler than custom `JsonConverter<T>` per-property (which would require knowing property types).
- Handles anonymous/record/class types uniformly.
- Configurable via options pattern (`AuditOptions.RedactedFields`) without code changes for extension.

**Alternatives considered:**
- **Custom `JsonConverter<T>` per sensitive type.** Rejected — requires attribute on properties or per-type converter registration. Violates "top-level-only" decision; unnecessarily complex.
- **Source generators.** Rejected — overkill for a list of ~7 property names.
- **String-level regex scrub.** Rejected — fragile (can match inside values, not just property names).

**Implications:** Redaction walks ONE level deep (top-level properties). This is the explicit v1 limitation documented in FR-007 and the edge case. OQ-1 tracks revisit conditions.

---

## R-05: Correlation middleware placement

**Decision:** register `CorrelationMiddleware` **first** in the pipeline — before `UseAuthentication()`. Implementation uses `HttpContext.Items[CorrelationContext.Key]` to store the identifier; `ICorrelationContext` reads from `IHttpContextAccessor`.

**Rationale:**
- Correlation must be available to every downstream middleware and handler, including authentication failures and rate-limiter rejections (which are themselves auditable events).
- Following the exception-middleware pattern: the earliest middleware that every request passes through.
- `HttpContext.Items` is the canonical per-request bag.

**Alternatives considered:**
- **`AsyncLocal<T>`-based context.** Rejected — adds a custom scoping mechanism when ASP.NET already provides `HttpContext.Items`.
- **Register as MediatR pipeline behavior instead of middleware.** Rejected — correlation must exist across HTTP/non-HTTP boundaries uniformly; MediatR-only would miss request-level logging.

**Implications:**
- `Program.cs` adds `app.UseMiddleware<CorrelationMiddleware>()` as the first `app.Use...` call.
- Web-layer `CorrelationContext` implementation reads/writes `HttpContext.Items`. Non-HTTP callers (background services) get a stub `CorrelationContext` registered as `IRequestContext` that derives from `Activity.Current?.Id` or generates a GUID.
- IP address: `HttpContext.Connection.RemoteIpAddress?.ToString()` exposed through the same abstraction (FR-011a).

---

## R-06: Admin viewer DataTable pattern

**Decision:** reuse feature-013's server-side DataTables pattern:
- Controller: `Mentoory.Web/Areas/Administration/Controllers/AuditLogController.cs` with `[Authorize(Roles = "GlobalAdmin")]`.
- View: `Mentoory.Web/Areas/Administration/Views/AuditLog/Index.cshtml`.
- JS: `Mentoory.Web/wwwroot/js/audit-log.js` (per Principle VIII — JavaScript in `/wwwroot/js/`).
- Query: `Mentoory.Shared.Application/Queries/Audit/GetAuditLogPagedQuery.cs` returning `PagedResult<AuditLogDto>`.
- Filters via URL params (matches feature-013 convention): `eventType`, `userEmail`, `outcome`, `from`, `to`.

**Rationale:** feature 013 is the established pattern; consistency reduces cognitive load and matches the constitution's UI framework section (Tabler + DataTables server-side).

**Alternatives considered:**
- **Client-side DataTable with virtualization.** Rejected — degrades past ~10k rows; FR-A7 (1,000-row perf target) is already met by server-side.
- **Blazor Server grid.** Rejected — introduces a new UI paradigm; the rest of the app is MVC + jQuery.

**Implications:** Shared application layer exposes the query; infrastructure reads `[audit].[AuditLog]` via EF Core (read-only; we don't need ADO.NET for reads — `AsNoTracking()` + `IOrderedQueryable` filters). The write path stays ADO.NET-direct for the same reason the current `AuditService` uses it (avoid circular DbContext dependencies).

---

## R-07: Event types and command retrofit mechanics

**Decision:** event-type string constants live in a new `Mentoory.Shared.Application/Audit/AuditEventTypes.cs` static class to prevent typos and allow compile-time reference from tests.

**Rationale:**
- Naming convention (Principle VII): no established pattern for constant classes, but precedent exists (enums, static readonly). A simple `static class` with `public const string` members fits.
- Using constants over raw strings lets the architecture test assert that the value from `[Audited]` on each retrofitted command matches the expected constant.

**Decided event types** (constants in `AuditEventTypes`):
```csharp
public const string ContextActivated = "Context.Activated";
public const string RoleAssigned     = "Role.Assigned";
public const string UserRegistered   = "User.Registered";
public const string UserLoggedIn     = "User.LoggedIn";
public const string AnswerCorrected  = "Answer.Corrected";
```

Reserved for future features (declared NOW so the governance rule surfaces in CI the moment their handlers ship):
```csharp
public const string ProjectStageAdvanced   = "Project.StageAdvanced";    // FR-053, Phase A
public const string MentoringPlanApproved  = "MentoringPlan.Approved";   // Phase B
```

**Implications:** Retrofitted commands carry `[Audited(AuditEventTypes.RoleAssigned, EntityType = "RoleAssignment")]` (attribute reads constants). Architecture test asserts both the presence of `[Audited]` AND (soft assertion) that the `EventType` value matches a known constant.

---

## R-08: Audit write transactional behavior preserved

**Decision:** keep current `AuditService` ADO.NET-direct implementation. Extend `AuditEntry` and the INSERT statement only to include the new columns.

**Rationale:**
- The existing `AuditService` already writes outside the DbContext transaction (fresh `SqlConnection`), aligning with FR-010 ("audit write MUST execute OUTSIDE the caller's database transaction").
- Switching to EF Core for writes would couple it to a specific DbContext, which was the original reason for the ADO.NET choice (per the comment in `AuditService.cs`).
- Best-effort semantics (`catch { _logger.LogError }`) are already in place — no changes needed.

**Alternatives considered:**
- **EF Core write via a dedicated `AuditDbContext`.** Rejected — creates a new DbContext for two tables (AuditLog + eventual configuration), needless complexity; the current direct-SQL approach is already minimal and works.
- **Write audit into `Shared` DbContext.** Rejected (ratified in brainstorm #10) — shared-DbContext breaks module boundaries.

**Implications:** `AuditService.LogAsync` SQL INSERT expands from 10 to 15 columns; parameterized — no SQL injection surface; identical try/catch/log error-handling remains.

---

## R-09: Integration with existing `ITenantContext`

**Decision:** `ITenantContext` already exposes `UserId`, `IncubatorId`, `ProjectId`, `Role` (active). The audit behavior reads from it directly (Application-to-Application — no layer violation). For `RoleContext` column we persist the active role name as a string (from `ITenantContext.Role`).

**Rationale:** The tenant context is already populated by `TenantContextMiddleware` before MediatR runs. No changes to `ITenantContext` are required for this feature.

**Implications:** Zero changes to `ITenantContext`. Retrofit handlers (anonymous phase — `RegisterUser`, `LoginUser`) fall back to command-payload values for `UserId` / `UserEmail` since the tenant context is empty pre-authentication.

**User email source:** the spec requires `UserEmail` on audit entries, but `ITenantContext` currently does not expose email. Two options considered:
1. Extend `ITenantContext` with `UserEmail` (populated by `TenantContextMiddleware` from the claims principal's email claim).
2. Look it up per audit write via a repository call.

**Option 1 chosen** — one-line addition to the interface + middleware, matches how `UserId` and `Role` are already populated from claims. Cheap and semantically right.

---

## R-10: Test coverage for the audit pipeline

**Decision:** test breakdown:
- **Architecture tests** (`Mentoory.Tests.Architecture`): enforce `[Audited]` presence on sensitive-named commands; assert registered pipeline ordering in `Program.cs` matches the intended order.
- **Unit tests** for `AuditingBehavior` (Shared.Application unit test host — create a nested test project `tests/Mentoory.Shared.Application.Tests/` OR colocate with architecture; decision: create `tests/Mentoory.Shared.Application.Tests/` since existing per-module test project pattern suggests each application assembly has its own test project).
- **Unit tests** for redaction utility.
- **Integration tests** (`Mentoory.Tests.Integration`): one happy path + one failure path per retrofitted command → assert AuditLog row is written.
- **Integration test** for correlation propagation: two commands in one request share the same CorrelationId.
- **Integration test** for best-effort semantics: simulated DB failure on audit does not fail the business command.

**Rationale:** Layered coverage maps directly onto FRs and SCs. Architecture tests prove FR-015 / SC-004; unit tests prove FR-003 / FR-006 / SC-003 (redaction); integration tests prove SC-002 / SC-005 / SC-006.

**Implications:** Two new test projects (`Mentoory.Tests.Architecture` and `Mentoory.Shared.Application.Tests`), one per R-03 and this R-10. The Integration project gains retrofit-specific test cases.

---

## Summary — resolved unknowns

| ID | Topic | Decision |
|----|-------|----------|
| R-01 | JSON serializer | `System.Text.Json` with `JsonNode` walk |
| R-02 | MediatR order | `Validator → Auditing → Transaction` (Auditing wraps Transaction) |
| R-03 | Arch-test host | New `Mentoory.Tests.Architecture` project, reflection-only |
| R-04 | Redaction | Top-level DOM walk against `AuditOptions.RedactedFields` |
| R-05 | Correlation middleware | First middleware in `Program.cs`; `HttpContext.Items`-backed |
| R-06 | Admin viewer | Reuse feature-013 server-side DataTable pattern |
| R-07 | Event-type constants | `AuditEventTypes` static class for compile-time reference |
| R-08 | Audit write path | Preserve ADO.NET-direct `AuditService`, extend columns only |
| R-09 | Tenant-context reuse | Extend `ITenantContext` with `UserEmail`; everything else already present |
| R-10 | Test coverage | Architecture + Shared.Application.Tests + Integration |
