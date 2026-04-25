# Implementation Plan: Project Lifecycle Application + UI Completion

**Branch**: `016-project-lifecycle-finish` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-project-lifecycle-finish/spec.md`

## Summary

Complete the project lifecycle feature by closing the three concrete gaps between the spec and the current codebase: (1) no application-layer command exists to advance a project's stage — the domain method `Project.AdvanceStage()` is wired to nothing; (2) the Coordination area has no project overview page showing the 7-stage lifecycle timeline and exposing the advance action; (3) coordination-area actions are not filtered by the project's current stage, so coordinators see actions that do not yet apply. Technical approach: add an `AdvanceProjectStageCommand` + handler + validator using established CQRS/Mapperly patterns; add a `GetProjectLifecycleQuery` returning the full stage history; build a new Coordination-area `ProjectsController` with `Index` (list) and `Lifecycle/{externalId}` (overview with advance action); introduce a single `StageActionRegistry` static mapping (stage → available actions) consumed by both the overview view model and controller-level guard filters on stage-gated coordination endpoints. Concurrency is handled by adding an EF Core optimistic-concurrency token (`RowVersion`) to the `Projects` table — currently absent — so two coordinators advancing the same project cannot both succeed.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Riok.Mapperly 4.x, EF Core 10.x, Tabler Admin Template (Bootstrap 5), DataTables 2.3.4, jQuery, Tabler Icons Webfont
**Storage**: SQL Server with SSDT/DACPAC schema (`Mentoory.Db`). Existing tables `tenant.Projects` and `tenant.ProjectStages` are reused. One additive schema change: add `RowVersion` optimistic-concurrency column to `tenant.Projects`.
**Testing**: xUnit, Moq, FluentAssertions, EF Core InMemory for handler tests; domain tests against `Project` aggregate.
**Target Platform**: Linux server (.NET 10.0 via Aspire 13.2.0), Spanish-language UI.
**Project Type**: Modular monolith web application (Clean Architecture, DDD). Web layer (Razor Views) + Application + Domain + Infrastructure inside the existing `Mentoory.Tenant.*` module; no new module introduced.
**Performance Goals**: Lifecycle overview page p95 < 2 s under normal load (SC aligned with spec SC-002). Advance action round-trip < 1 s at p95.
**Constraints**: Spanish UI only (constitution IX); `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — zero warnings (V); `DateTime.UtcNow` forbidden in Domain/Application (VI — use `ITimeProvider`); `ExternalId` on routes (III); `[Authorize]` must include all higher roles (X); SSDT-only schema changes (XI).
**Scale/Scope**: Per-incubator — typical project counts are tens per incubator, hundreds platform-wide. The overview page loads exactly one project's 7 stage records, so no pagination is needed.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluating against `constitution.md` v1.1.1 and `access-security-constitution.md` v1.0.0:

| # | Principle | Assessment |
|---|-----------|------------|
| I | Clean Architecture Layer Boundaries | **PASS**. Domain logic (`Project.AdvanceStage`) already exists; this feature adds an Application command, an Infrastructure-compliant query (EF Core via existing repository), and a Web controller that calls Application only. Web never touches repositories directly. |
| II | CQRS Pattern | **PASS**. `AdvanceProjectStageCommand : IBaseRequest<Guid>`, handler inherits `BaseCommandHandler<AdvanceProjectStageCommand, Guid>`, FluentValidation validator for inputs. `GetProjectLifecycleQuery : IBaseRequest<ProjectLifecycleDto>` — side-effect-free. |
| III | Domain-Driven Design | **PASS**. No new aggregate; `Project` aggregate mutates itself via the existing `AdvanceStage` method. Routes use `ExternalId` only. Collections remain `.AsReadOnly()`. The new query projects DTOs — no navigation property leak. |
| IV | Integration Events (ADR-001) | **N/A for MVP**. No cross-domain event is required to ship the feature. A future `ProjectStageAdvanced` event is possible (e.g., to notify `Mentoring` or `Notification` domains), but the spec does not require it and it is explicitly out of scope (spec Assumptions: notifications are handled by existing infrastructure). Leaving it out avoids premature integration. |
| V | Zero-Warnings | **PASS**. New code uses nullable annotations consistently; no warnings introduced. |
| VI | DateTime Handling | **PASS**. Handler injects `ITimeProvider`. Domain `AdvanceStage` already takes `DateTime utcNow` parameter. |
| VII | Naming Conventions | **PASS**. `AdvanceProjectStageCommand`, `AdvanceProjectStageHandler`, `AdvanceProjectStageValidator`, `GetProjectLifecycleQuery`, `GetProjectLifecycleHandler`, `ProjectLifecycleDto`, `LifecycleProjectViewModel`, `ProjectsController` (Coordination). |
| VIII | File Organization | **PASS**. One class per file; JavaScript (stage-gated action tooltip helpers) in `/wwwroot/js/`, not Views. |
| IX | Spanish-First UI | **PASS**. All labels, buttons, toasts, tooltips, confirmation text, error messages are Spanish. Stage display names are Spanish (`Registro`, `Formularios`, `Análisis`, `Asignación de Aprendizaje`, `Mentoría`, `Evaluación Final`, `Cierre`). |
| X | Role Hierarchy & Session Context | **PASS**. `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` on the Coordination `ProjectsController`. Missing incubator context → redirect to `/Context/Select` with Spanish warning (same pattern as existing `AdministrationProjectsController`). Menu includes GlobalAdmin. |
| XI | SSDT/DACPAC Database Strategy | **PASS**. The one schema change (add `RowVersion` column to `tenant.Projects`) is made in the SQL project (`Mentoory.Db/tenant/Tables/Projects.sql`) as an additive ALTER-compatible change. EF Core migrations are not used. No PostDeployment script is needed (new column has a default; existing rows are populated implicitly by SQL Server for `rowversion`/`timestamp`). |

**Access & Security Constitution gates**:

- **Tenant isolation**: `GetProjectLifecycleQuery` filters by the acting user's active incubator context (`User.GetActiveIncubatorId()`); `AdvanceProjectStageCommand` handler verifies the loaded project's `IncubatorId` matches the acting user's incubator context (or user is `GlobalAdmin`). **PASS**.
- **Scope enforcement**: Stage-gated action endpoints enforce the stage check at the controller level via a reusable filter, not only in the view. **PASS**.
- **Backend authority**: UI hides unavailable actions, but every controller that owns a stage-gated action rejects out-of-stage requests server-side. **PASS**.
- **Audit trail**: `AdvancedByUserId`, `StartedAtUtc`, `CompletedAtUtc` are already on `ProjectStages`. Handler sets them via existing domain method. **PASS**.

**Result**: All gates pass. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/016-project-lifecycle-finish/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Phase 1 entities & DTOs
├── quickstart.md        # Phase 1 walkthrough
├── contracts/
│   ├── advance-project-stage-command.md
│   ├── get-project-lifecycle-query.md
│   └── coordination-ui-routes.md
├── checklists/
│   └── requirements.md  # Existing (from /speckit.specify)
└── tasks.md             # Created by /speckit.tasks (NOT by this command)
```

### Source Code (repository root)

```text
Mentoory.Tenant.Domain/
└── Aggregates/Project/
    └── Project.cs                                   # EXISTING — AdvanceStage() reused as-is

Mentoory.Tenant.Application/
├── Commands/AdvanceProjectStage/                    # NEW
│   ├── AdvanceProjectStageCommand.cs
│   ├── AdvanceProjectStageHandler.cs
│   └── AdvanceProjectStageValidator.cs
└── Projects/Queries/
    ├── GetProjectLifecycle/                         # NEW
    │   ├── GetProjectLifecycleQuery.cs
    │   ├── GetProjectLifecycleHandler.cs
    │   ├── ProjectLifecycleDto.cs
    │   └── ProjectLifecycleStageDto.cs
    └── ListPublicProjects/                          # EXISTING — unchanged

Mentoory.Tenant.Infrastructure/
└── Persistence/
    ├── Configurations/ProjectConfiguration.cs       # MODIFIED — add RowVersion mapping
    └── TenantDbContext.cs                           # MODIFIED if RowVersion needs explicit config

Mentoory.Access.Application/
└── StageActions/                                    # NEW (cross-feature registry)
    ├── StageGatedAction.cs                          # enum of stage-gated actions
    └── StageActionRegistry.cs                       # static mapping StageType → available actions
    (Placed in Access.Application because it's policy/registry, not tenant state.
     Alternative: Mentoory.Web.Infrastructure — see research.md)

Mentoory.Web/
├── Areas/Coordination/
│   ├── Controllers/
│   │   └── ProjectsController.cs                    # NEW — Index + Lifecycle + AdvanceStage
│   ├── Models/
│   │   ├── LifecycleProjectViewModel.cs             # NEW
│   │   ├── LifecycleStageViewModel.cs               # NEW
│   │   └── StageActionViewModel.cs                  # NEW
│   ├── Views/
│   │   └── Projects/
│   │       ├── Index.cshtml                         # NEW — project list for coordination
│   │       └── Lifecycle.cshtml                     # NEW — 7-stage timeline + advance button
│   └── _ViewImports.cshtml                          # EXISTING — unchanged
├── Infrastructure/
│   ├── Filters/
│   │   └── RequiresStageAttribute.cs                # NEW — action filter for stage gating
│   └── Menu/MenuConfiguration.cs                    # MODIFIED — add Coordination → Proyectos entry
└── wwwroot/js/
    └── coordination-lifecycle.js                    # NEW — advance confirmation dialog,
                                                     # stage-gate tooltip enhancement

Mentoory.Db/
└── tenant/Tables/Projects.sql                       # MODIFIED — add [RowVersion] ROWVERSION column

tests/
├── Mentoory.Tenant.Tests/
│   ├── Domain/
│   │   └── ProjectTests.cs                          # EXISTING — add AdvanceStage edge cases
│   ├── Handlers/
│   │   ├── AdvanceProjectStageHandlerTests.cs       # NEW
│   │   └── GetProjectLifecycleHandlerTests.cs       # NEW
│   └── Validators/
│       └── AdvanceProjectStageValidatorTests.cs     # NEW
└── Mentoory.Web.Tests/ (if exists, else add there)
    └── Filters/
        └── RequiresStageAttributeTests.cs           # NEW
```

**Structure Decision**: Modular monolith — single ASP.NET Core solution with module boundaries (`Mentoory.Tenant.*`, `Mentoory.Access.*`, `Mentoory.Diagnostic.*`, `Mentoory.Web`). This feature lives in the `Tenant` module (domain/application/infrastructure) and the `Web` module (controllers/views). A small cross-cutting registry (stage → actions) is placed in `Mentoory.Access.Application` because it is policy data consumed by both UI and authorization filters (see research.md for the rejected alternative of placing it in `Mentoory.Web`).

## Complexity Tracking

> No constitution violations. This table is intentionally left empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | — | — |
