# Implementation Plan: Audit Pipeline Wiring

**Branch**: `016-audit-pipeline` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-audit-pipeline/spec.md`

## Summary

Uniform capture of security-sensitive commands via a hybrid mechanism: an `[Audited]` attribute on command classes plus an `AuditingBehavior<TRequest, TResponse>` MediatR pipeline behavior (automatic default path), with an explicit `AuditMode.Manual` escape hatch for handlers that need domain-specific detail (e.g., `CorrectAnswer` capturing before/after answer text). The `[audit].[AuditLog]` table is extended with five typed columns (`CorrelationId`, `Outcome`, `ExceptionType`, `UserEmail`, `RoleContext`) plus a filtered index on `CorrelationId`. Five shipped commands are retrofitted (`SetActiveContext`, `AssignRole`, `RegisterUser`, `LoginUser`, `CorrectAnswer`). An architecture test enforces `[Audited]` presence on any command whose name matches the sensitive-action regex, and a new "Audit Trail Obligations" section in `access-security-constitution.md` binds future commands to the same rule. A Platform-Admin-only read-only DataTable viewer at `/Administration/AuditLog` (Spanish UI, feature-013 pattern) surfaces the log. Audit writes run OUTSIDE the business transaction (preserving the existing ADO.NET-direct `AuditService`) so audit failure cannot cascade to business failure. HTTP correlation id + client IP are read through an `ICorrelationContext` abstraction to keep the Application layer free of web-framework dependencies (Principle I).

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, `System.Text.Json` (BCL), `Microsoft.Data.SqlClient` (already in use by `AuditService`), Tabler v1.4.0 + DataTables 2.3.4 + jQuery for the admin viewer (consistent with feature 013)
**Storage**: SQL Server via SSDT/DACPAC — `[audit].[AuditLog]` table extended with `CorrelationId UNIQUEIDENTIFIER`, `Outcome NVARCHAR(20) NOT NULL DEFAULT 'Success'`, `ExceptionType NVARCHAR(200)`, `UserEmail NVARCHAR(256)`, `RoleContext NVARCHAR(50)`; new filtered index `IX_AuditLog_CorrelationId`
**Testing**: xUnit + FluentAssertions + Moq (unit), Respawn + WebApplicationFactory (integration), reflection-based architecture tests in a new `Mentoory.Tests.Architecture` project
**Target Platform**: ASP.NET Core 10 web application; SQL Server 2022+ (existing deployment target)
**Project Type**: Modular monolith (Clean Architecture) — existing solution; feature adds two Application-layer primitives (`AuditedAttribute`, `AuditingBehavior`), one Infrastructure middleware (`CorrelationMiddleware`), one Web area controller + view (`/Administration/AuditLog`), schema changes in `Mentoory.Db`, and two new test projects (`Mentoory.Tests.Architecture`, `Mentoory.Shared.Application.Tests`)
**Performance Goals**: Admin viewer loads 1,000 most recent entries within 1 second on a modern browser over broadband (SC-007); architecture test completes in under 2 seconds (reflection only); audit write adds under 10 ms of latency per command (out-of-band to the business transaction)
**Constraints**: Audit write must be best-effort (never fails the wrapped business command); audit write must execute OUTSIDE the caller's DB transaction; no direct `IHttpContextAccessor` dependency in Application layer (Principle I); all user-facing text Spanish (Principle IX); zero-warnings build (Principle V); `ITimeProvider` only, never `DateTime.UtcNow` (Principle VI); DACPAC for schema (Principle XI)
**Scale/Scope**: ~500 audit rows per day at current traffic; ~180K rows/year; ~270 MB/year — well within SQL Server's comfortable scale for this workload. Retention strategy deferred (OQ-3).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Initial gate evaluation (pre-Phase 0)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Layer Boundaries | PASS | `AuditedAttribute`, `AuditingBehavior`, `ICorrelationContext`, `IAuditService` live in Application. `AuditService`, `WebCorrelationContext`, `AmbientCorrelationContext`, `CorrelationMiddleware` live in Infrastructure/Web. No framework types leak into Application; see FR-011a for the explicit rule about avoiding `IHttpContextAccessor` in the pipeline behavior. |
| II. CQRS Pattern Requirements | PASS | `GetAuditLogPagedQuery` uses `IBaseRequest<PagedResult<AuditLogDto>>`. No command changes impinge on the pattern; retrofitted commands already use `IBaseRequest` / `BaseCommandHandler`. |
| III. Domain-Driven Design Constraints | PASS | No aggregate changes. `AnswerCorrection` aggregate untouched — Manual-mode audit is additive, not a replacement. |
| IV. Integration Events (ADR-001) | N/A | No cross-domain events added. |
| V. Zero-Warnings Policy | PASS | Implementation is idiomatic C# 12/13; no pragmas anticipated. Nullable columns match nullable C# reference/value types on `AuditEntry`. |
| VI. DateTime Handling | PASS | `ITimeProvider.UtcNow` used by the behavior and by Manual-mode handlers; no `DateTime.UtcNow`. |
| VII. Naming Conventions | PASS | `AuditedAttribute` (matches attribute pattern), `AuditingBehavior` (matches existing `ValidatorBehavior` / `TransactionBehavior`), `GetAuditLogPagedQuery` (matches `Get*Query`), `AuditLogDto` (matches `*Dto`). Architecture test enforces command naming. |
| VIII. File Organization | PASS | One class per file; `audit-log.js` in `/wwwroot/js/` per rule. |
| IX. Spanish-First UI | PASS | Admin viewer page title `"Registro de auditoría"`, filter labels + empty state in Spanish (see contracts/admin-viewer.md). Code comments + identifiers English. |
| X. Role Hierarchy & Session Context | PASS-WITH-NOTE | Admin viewer restricted to `GlobalAdmin` alone — this is an intentional narrowing, NOT a violation: the constitution's rule (higher role inherits) applies to feature-level access control, whereas this view is platform-wide audit data that is genuinely restricted to the global scope. Documented in contracts/admin-viewer.md. |
| XI. SSDT/DACPAC Database Strategy | PASS | Schema changes in `Mentoory.Db/audit/Tables/AuditLog.sql`; new default value avoids backfill; no EF migration. |

**Overall:** no violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/016-audit-pipeline/
├── plan.md                         # This file
├── research.md                     # Phase 0 — 10 resolved unknowns
├── data-model.md                   # Phase 1 — schema + records + options
├── quickstart.md                   # Phase 1 — "how to add [Audited]"
├── contracts/
│   ├── attribute-and-mode.md       # AuditedAttribute + AuditMode
│   ├── audit-service.md            # IAuditService (unchanged shape, extended payload)
│   ├── auditing-behavior.md        # AuditingBehavior + redaction + anonymous resolvers
│   ├── correlation-context.md      # ICorrelationContext + middleware + Web/Ambient impls
│   ├── admin-viewer.md             # /Administration/AuditLog + DataTable contract
│   ├── architecture-test.md        # Mentoory.Tests.Architecture.AuditCoverageTests
│   └── governance.md               # access-security-constitution.md § Audit Trail Obligations
├── spec.md                         # (already produced by /speckit-specify)
├── REVIEW-SPEC.md                  # (already produced by spex:review-spec)
├── review_brief.md                 # (already produced during brainstorm)
├── checklists/
│   └── requirements.md             # spec quality checklist
└── tasks.md                        # Phase 2 output (generated by /speckit-tasks)
```

### Source Code (repository root)

```text
Mentoory.Shared.Application/
├── Audit/
│   ├── AuditEntry.cs                               # MODIFIED — add 5 fields
│   ├── AuditEventTypes.cs                          # NEW
│   ├── AuditOptions.cs                             # NEW
│   ├── AuditedAttribute.cs                         # NEW
│   ├── AuditMode.cs                                # NEW (or inline in AuditedAttribute.cs)
│   ├── IAuditService.cs                            # unchanged shape
│   └── IAuditAnonymousResolver.cs                  # NEW (internal-ish; used by Access.Application)
├── Behaviors/
│   └── AuditingBehavior.cs                         # NEW
├── Interfaces/
│   ├── ICorrelationContext.cs                      # NEW
│   └── ITenantContext.cs                           # MODIFIED — add UserEmail
├── Queries/Audit/
│   ├── GetAuditLogPagedQuery.cs                    # NEW
│   └── AuditLogDto.cs                              # NEW
└── ServiceCollectionExtensions.cs                  # MODIFIED — Configure<AuditOptions>, register resolvers

Mentoory.Shared.Infrastructure/
├── Audit/
│   └── AuditService.cs                             # MODIFIED — extend SQL INSERT to 15 columns
├── Correlation/
│   └── AmbientCorrelationContext.cs                # NEW — non-HTTP fallback
├── Persistence/Audit/
│   └── AuditReadRepository.cs                      # NEW — EF Core read (AsNoTracking)
└── Services/
    └── TenantContextService.cs                     # MODIFIED — populate UserEmail claim

Mentoory.Web/
├── Areas/Administration/
│   ├── Controllers/AuditLogController.cs           # NEW
│   └── Views/AuditLog/Index.cshtml                 # NEW — Spanish, Tabler + DataTables
├── Infrastructure/
│   ├── Correlation/
│   │   ├── CorrelationMiddleware.cs                # NEW
│   │   ├── CorrelationMiddlewareExtensions.cs      # NEW — UseCorrelation()
│   │   └── WebCorrelationContext.cs                # NEW
│   └── Menu/MenuConfiguration.cs                   # MODIFIED — add Auditoría entry for GlobalAdmin
├── wwwroot/js/
│   └── audit-log.js                                # NEW — DataTable init + filter wiring
├── Program.cs                                      # MODIFIED — register AuditOptions, CorrelationContext, AuditingBehavior (between Validator and Transaction), UseCorrelation() first
└── (other files unchanged)

Mentoory.Diagnostic.Application/
└── Commands/CorrectAnswer/
    ├── CorrectAnswerCommand.cs                     # MODIFIED — add [Audited(Manual)]
    └── CorrectAnswerHandler.cs                     # MODIFIED — inject IAuditService, capture before/after, call LogAsync

Mentoory.Access.Application/
├── Commands/AssignRole/AssignRoleCommand.cs        # MODIFIED — add [Audited]
├── Commands/SetActiveContext/SetActiveContextCommand.cs  # MODIFIED — add [Audited]
├── Commands/RegisterUser/RegisterUserCommand.cs    # MODIFIED — add [Audited]
├── Commands/LoginUser/LoginUserCommand.cs          # MODIFIED — add [Audited]
├── Audit/
│   ├── RegisterUserAuditResolver.cs                # NEW
│   └── LoginUserAuditResolver.cs                   # NEW
└── ServiceCollectionExtensions.cs                  # MODIFIED — register resolvers

Mentoory.Db/
└── audit/Tables/AuditLog.sql                       # MODIFIED — add 5 columns + new index

.specify/memory/
├── access-security-constitution.md                 # MODIFIED — add § Audit Trail Obligations, minor-version bump
└── constitution.md                                 # MODIFIED — add checklist item 10 (patch-version bump)

tests/
├── Mentoory.Tests.Architecture/                    # NEW project
│   ├── Mentoory.Tests.Architecture.csproj
│   ├── AuditCoverageTests.cs
│   └── Internal/CommandTypeEnumerator.cs
├── Mentoory.Shared.Application.Tests/              # NEW project
│   ├── Mentoory.Shared.Application.Tests.csproj
│   ├── Behaviors/AuditingBehaviorTests.cs
│   └── Audit/AuditOptionsTests.cs                  # (redaction + truncation)
├── Mentoory.Tests.Integration/
│   ├── Audit/AuditPipelineTests.cs                 # NEW — retrofit verification per command
│   ├── Audit/CorrelationPropagationTests.cs       # NEW — two-command one-request case
│   └── Audit/BestEffortWriteTests.cs              # NEW — simulated DB failure test
└── Mentoory.Diagnostic.Tests/
    └── Handlers/CorrectAnswerHandlerTests.cs       # MODIFIED — add AuditLog assertion
```

**Structure Decision**: The feature reuses the existing Clean-Architecture layout and adds two new test projects. Changes are spread across Shared.Application (new primitives), Shared.Infrastructure (extended service + new correlation impl), Web (middleware + admin viewer), per-module Application assemblies (attribute application + resolvers for anonymous commands), Mentoory.Db (schema), and `.specify/memory/` (governance). All paths follow existing conventions (one class per file, JS under `/wwwroot/js/`, PostDeployment scripts at project-root `Mentoory.Db.PostDeployment/`).

## Complexity Tracking

> No constitution violations — this section intentionally left without entries.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | — | — |

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 artifacts (contracts/, data-model.md, quickstart.md) were written.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | PASS | FR-011a enforced throughout contracts; `AuditingBehavior` takes only Application-layer dependencies. |
| II. CQRS | PASS | No handlers introduced outside the existing `BaseCommandHandler` / query pattern. |
| III. DDD | PASS | Zero aggregate changes confirmed. |
| IV. Integration Events | N/A | Confirmed no events added. |
| V. Zero-Warnings | PASS | No analyzer suppressions anticipated in any contract. |
| VI. DateTime | PASS | All contracts use `ITimeProvider.UtcNow`. |
| VII. Naming | PASS | Architecture test itself is named `AuditCoverageTests` (matches `*Tests`). |
| VIII. File Organization | PASS | `/wwwroot/js/audit-log.js` path specified; Views under Areas/Administration/Views/AuditLog/. |
| IX. Spanish UI | PASS | Admin-viewer contract enumerates Spanish strings. |
| X. Role Hierarchy | PASS-WITH-NOTE | Narrowing to `GlobalAdmin` only is documented as intentional. |
| XI. SSDT/DACPAC | PASS | AuditLog.sql edit is the schema-change mechanism; no EF migration. |

**Phase 2 readiness:** `/speckit-tasks` may proceed.

## Phase 2 Preview

`/speckit-tasks` will generate `tasks.md` with the following story-grouped structure (P1 first, per user-story priorities):

- **Story 1 (P1) — Platform Admin reviews security-sensitive actions.** Schema migration → `AuditEntry` record extension → `AuditService` SQL update → register `ICorrelationContext` + `CorrelationMiddleware` → add `AuditingBehavior` (Automatic path) → retrofit `SetActiveContext` + `AssignRole` + `RegisterUser` + `LoginUser` with `[Audited]` → build admin viewer (`GetAuditLogPagedQuery`, controller, view, JS, menu entry).
- **Story 2 (P2) — Developer coverage.** Create `Mentoory.Tests.Architecture` → `AuditCoverageTests` → `AuditEventTypes` constants → update `access-security-constitution.md` (Audit Trail Obligations section + version bump) → update `constitution.md` checklist (patch bump).
- **Story 3 (P3) — Correlation for incident reconstruction.** `ICorrelationContext` interface (if not already built in Story 1) → `WebCorrelationContext` + `AmbientCorrelationContext` → wire the behavior to read from it → integration test `CorrelationPropagationTests`.
- **Story 4 (P2) — Correction handler (Manual mode).** `[Audited(..., Mode = Manual)]` on `CorrectAnswerCommand` → `CorrectAnswerHandler` reads before-value from aggregate, injects `IAuditService`, calls `LogAsync` with before/after in `Details` → test assertion.
- **Cross-cutting** (tie to each story as appropriate): best-effort failure test (`BestEffortWriteTests`), redaction unit tests (`AuditingBehaviorTests`), truncation test, post-design simplification pass, Spanish UI QA.

## Artifacts reference

- `spec.md` — functional requirements, user stories, success criteria.
- `research.md` — 10 Phase-0 decisions (serializer, pipeline ordering, test host, redaction strategy, middleware placement, viewer pattern, event-type constants, write path, tenant-context reuse, test coverage split).
- `data-model.md` — AuditLog schema, `AuditEntry` shape, attributes, enums, options, entity relationships, volume estimates.
- `contracts/` — 7 public-surface contracts (attribute, service, behavior, correlation context, admin viewer, architecture test, governance).
- `quickstart.md` — one-page recipe for applying `[Audited]` to future commands (Automatic + Manual modes).
