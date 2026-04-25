# Implementation Plan: Knowledge Module Core

**Branch**: `016-knowledge-module-core` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/016-knowledge-module-core/spec.md`

## Summary

Stand up the `Mentoory.Knowledge` module (domain, application, infrastructure, tests) with two aggregate families — `KnowledgeStructureTemplate` (global catalog) and `KnowledgeStructure` (per-project clone) — each holding a four-level hierarchy of Module → Topic → Subject → Resource. Mirror the already-shipped `ProjectForm.CloneFromTemplate` pattern, extended with typed `SourceTemplateXExternalId` stamps at every node for rename-safe PartialSync matching. Add cross-module integration in one SSDT PR: `diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId` (nullable FK), `diagnostic.Questions.TopicId` FK to `knowledge.Topics.Id`, and auto-cascade in `CloneFormTemplateHandler` that clones-or-reuses the bound knowledge structure and rewrites `Question.TopicId` from template-topic ids to project-topic ids. Ship a `KnowledgeController` under `Mentoory.Web/Areas/Coordination/`, a PostDeployment seed for one sample template, and a `TopicPriorityRangesChanged` in-process `INotification` (no consumer in v1). Technical approach mirrors the existing Diagnostic module almost one-for-one, which keeps the new surface familiar to the codebase.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0 pre-release)
**Primary Dependencies**: ASP.NET Core MVC 10.x, MediatR 14.1, FluentValidation 12.1, Riok.Mapperly 4.x, Entity Framework Core 10.x, MailKit/MimeKit (not used by this spec), Tabler v1.4.0 + Bootstrap 5 (UI)
**Storage**: SQL Server (SSDT/DACPAC schema management; no EF migrations). New `knowledge` schema; cross-schema FK additions to `diagnostic.FormTemplates` and `diagnostic.Questions`
**Testing**: xUnit + Moq + FluentAssertions; EF Core InMemory for unit; Respawn for integration; Mentoory.Knowledge.Tests (unit), Mentoory.Tests.Integration (round-trip)
**Target Platform**: Linux server + .NET Aspire 13.2.0 orchestration
**Project Type**: Web application — modular monolith (existing shape; this spec adds one module family)
**Performance Goals**: Coordinator onboarding ≤ 10 minutes (SC-K01, end-to-end); deep-clone of a template with ~20 modules / ~100 topics / ~500 subjects / ~2000 resources completes in under 2 seconds on the Aspire dev profile
**Constraints**: Spanish UI (all user-facing strings); zero compiler warnings (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`); no `DateTime.UtcNow` in Domain; all external entities carry `ExternalId`; routes by ExternalId only
**Scale/Scope**: Initial catalog ~5 global templates, ~10–20 project clones across 3–5 incubators per tenant. Single-region deploy; no horizontal scale requirements in v1

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against `.specify/memory/constitution.md` v1.1.1. All 11 principles:

| Principle | Status | Evidence |
|---|---|---|
| I. Clean Architecture Layer Boundaries | ✅ PASS | NFR-K01. Domain has no infra deps; Application uses MediatR + Mapperly; Infrastructure owns DbContext; Web delegates via `MediatorExecutor`. Matches existing `Mentoory.Diagnostic.*` layout |
| II. CQRS Pattern | ✅ PASS | NFR-K03. Commands use `IBaseRequest`/`IBaseRequest<TResult>`; handlers derive `BaseCommandHandler<T>`; FluentValidation on user-input commands. Query handlers separate from command handlers |
| III. DDD Constraints | ✅ PASS | NFR-K02. External entities carry `ExternalId` (Guid); routes use ExternalId; aggregate roots own private collections with `.AsReadOnly()`. Cross-aggregate refs use ID only (`SourceTemplateId`, `ProjectId`, `IncubatorId`, `SourceTemplateXExternalId`) |
| IV. Integration Events (ADR-001) | ✅ PASS | FR-K30 places `TopicPriorityRangesChanged` in `Mentoory.Knowledge.Application/IntegrationEvents/`; contract is minimal `INotification` with DTO payload; no business logic shared cross-module |
| V. Zero Warnings | ✅ PASS | NFR-K06. All new code compiled with `TreatWarningsAsErrors`. Existing `Directory.Build.props` already enforces this |
| VI. DateTime Handling | ✅ PASS | NFR-K07. Domain factories accept `utcNow` as parameter; Application handlers inject `ITimeProvider`. No `DateTime.UtcNow` in Domain |
| VII. Naming Conventions | ✅ PASS | Commands follow `{Verb}{Entity}Command`; handlers follow `{CommandName}Handler`; repositories `I{Aggregate}Repository`. See contracts/ for full inventory |
| VIII. File Organization | ✅ PASS | One class per file; no JS in Views (FR-K41 UI uses `wwwroot/js/`); SSDT cross-schema FKs in `Mentoory.Db/diagnostic/Tables/*` referencing `Mentoory.Db/knowledge/Tables/*` (post-deployment seed outside `Mentoory.Db/`) |
| IX. Spanish-First UI | ✅ PASS | FR-K44. All controller action messages, validators, view text in Spanish (code/docs English). Toast usage via existing `showToast(message, type)` |
| X. Role Hierarchy & Session Context | ✅ PASS | NFR-K05 names explicit role sets: global-template CRUD `"GlobalAdmin"`; project-clone CRUD `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"`. NFR-K10 requires `MenuConfiguration.cs` entries to include GlobalAdmin. Controllers read `ActiveProjectId` via `ITenantContext` and redirect to `/Context/Select` on missing context (inherited from existing middleware) |
| XI. SSDT/DACPAC Strategy | ✅ PASS | NFR-K08. All DB changes ship in one SSDT PR. FR-K50 mandates idempotent PostDeployment seed. No EF migrations. Index syntax will follow INCLUDE-before-WHERE (enforced at SSDT build time) |

**Result: All gates pass; no violations to justify. Complexity Tracking section intentionally empty.**

### Post-Design Re-Check (after Phase 1)

Re-evaluated after `data-model.md`, `contracts/`, and `quickstart.md` were generated:

| Principle | Status | Post-Design Evidence |
|---|---|---|
| I. Clean Architecture | ✅ PASS | Domain types in `Mentoory.Knowledge.Domain/**`; no EF attributes on domain classes (configs live in Infrastructure); Web uses `MediatorExecutor` only |
| II. CQRS | ✅ PASS | Contracts enumerate every command as `IBaseRequest`/`IBaseRequest<T>`; handler base class and validator pattern called out uniformly |
| III. DDD | ✅ PASS | **Corrected during post-design pass**: initial cascade contract proposed `Question.RewriteTopicId` (a child-mutator bypassing root); corrected to route the rewrite through `ProjectForm.CloneFromTemplate` factory, keeping the root-controls-children invariant intact |
| IV. Integration Events | ✅ PASS | `TopicPriorityRangesChanged` placed in `Mentoory.Knowledge.Application/IntegrationEvents/`; minimal DTO contract; no business logic shared cross-module |
| V. Zero Warnings | ✅ PASS | No patterns in data-model/contracts that would introduce warnings |
| VI. DateTime Handling | ✅ PASS | Every factory takes `utcNow` (Domain); Application handlers specify `ITimeProvider` dependency |
| VII. Naming | ✅ PASS | Every command in contracts follows `{Verb}{Entity}Command`; handlers `{Command}Handler`; DTOs `{Entity}Dto`; repos `I{Aggregate}Repository` |
| VIII. File Organization | ✅ PASS | Structure tree in plan shows one class per file; JS under `wwwroot/js/knowledge/`; PostDeployment seed at `Mentoory.Db.PostDeployment/005.SeedKnowledgeData.sql` |
| IX. Spanish-First UI | ✅ PASS | All example error/toast messages in data-model and contracts are Spanish (`"No se puede eliminar..."`, `"Plantilla creada."`, etc.) |
| X. Role Hierarchy | ✅ PASS | Every contract file lists exact `[Authorize(Roles=...)]` strings with higher roles included. `KnowledgeController` menu configuration specifies role arrays including GlobalAdmin |
| XI. SSDT/DACPAC | ✅ PASS | Data-model enumerates tables with SQL types; cross-schema FK explicitly mapped in `Mentoory.Db/diagnostic/Tables/Questions.sql`; seed script numbered `005` per research R7 |

**Post-design deltas**:
- **One correction applied in `contracts/diagnostic-cascade.md`**: removed the proposed `Question.RewriteTopicId` child-mutator; routed the rewrite through an optional `topicIdRewriteMap` parameter on `ProjectForm.CloneFromTemplate` instead, preserving Principle III (aggregate root controls all mutations).
- **No other violations.** The plan is approved for Phase 2 task generation.

## Project Structure

### Documentation (this feature)

```text
specs/016-knowledge-module-core/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (one file per grouping: templates, clones, cascade, sync, events)
├── review_brief.md      # From /spex:brainstorm
├── REVIEW-SPEC.md       # From spex:review-spec
├── spec.md              # Approved specification
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (from /speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
Mentoory.Knowledge.Domain/
├── Aggregates/
│   ├── KnowledgeStructureTemplate/
│   │   ├── KnowledgeStructureTemplate.cs   (root)
│   │   ├── ModuleTemplate.cs
│   │   ├── TopicTemplate.cs
│   │   ├── SubjectTemplate.cs
│   │   └── ResourceTemplate.cs
│   └── KnowledgeStructure/
│       ├── KnowledgeStructure.cs           (root; clone)
│       ├── Module.cs
│       ├── Topic.cs
│       ├── Subject.cs
│       └── Resource.cs
├── Enums/
│   ├── ResourceType.cs
│   ├── SyncMode.cs                         (mirror Diagnostic; consider share via Shared)
│   └── Priority.cs                         (High/Medium/Low/NotApplicable)
├── Events/                                 (domain-event markers; integration event in Application)
├── Repositories/
│   ├── IKnowledgeStructureTemplateRepository.cs
│   └── IKnowledgeStructureRepository.cs
└── ValueObjects/
    └── PriorityRange.cs                    (min+max decimal pair; value object with factory)

Mentoory.Knowledge.Application/
├── Commands/
│   ├── CreateKnowledgeStructureTemplate/
│   ├── UpdateKnowledgeStructureTemplate/
│   ├── ArchiveKnowledgeStructureTemplate/
│   ├── UnarchiveKnowledgeStructureTemplate/
│   ├── AddModuleTemplate/                  (+ UpdateModuleTemplate, DeleteModuleTemplate, ReorderModuleTemplates)
│   ├── AddTopicTemplate/                   (+ Update..., Delete..., Reorder..., UpdateTopicTemplatePriorityRanges)
│   ├── AddSubjectTemplate/                 (+ Update..., Delete..., Reorder...)
│   ├── AddResourceTemplate/                (+ Update..., Delete..., Reorder...)
│   ├── CloneKnowledgeStructureTemplate/
│   ├── SetSyncMode/                        (clone only)
│   ├── SyncFromTemplate/                   (clone only; FR-K15)
│   ├── AddModule/ (...Topic/Subject/Resource/Update/Delete/Reorder on clone)
│   └── UpdateTopicPriorityRanges/          (clone; emits TopicPriorityRangesChanged)
├── Queries/
│   ├── ListKnowledgeStructureTemplates/
│   ├── GetKnowledgeStructureTemplate/
│   ├── ListProjectKnowledgeStructures/
│   └── GetProjectKnowledgeStructure/
├── IntegrationEvents/
│   └── TopicPriorityRangesChanged.cs       (INotification; FR-K30)
└── Mappings/                               (Mapperly source-gen mappers)

Mentoory.Knowledge.Infrastructure/
├── Persistence/
│   ├── KnowledgeDbContext.cs
│   ├── Configurations/                     (per-aggregate EF type configs)
│   └── Repositories/
│       ├── KnowledgeStructureTemplateRepository.cs
│       └── KnowledgeStructureRepository.cs
└── DependencyInjection/
    └── KnowledgeServiceCollectionExtensions.cs

Mentoory.Diagnostic.Domain/Aggregates/FormTemplate/FormTemplate.cs   (MODIFIED — add DefaultKnowledgeStructureTemplateExternalId)
Mentoory.Diagnostic.Application/Commands/CloneFormTemplate/CloneFormTemplateHandler.cs   (MODIFIED — auto-cascade per FR-K21)

Mentoory.Web/Areas/Coordination/
├── Controllers/KnowledgeController.cs      (NEW; FR-K40/K41/K42/K43)
├── Views/Knowledge/                        (Index, Templates, Clones, TemplateDetail, CloneDetail, _Tree partial)
├── Models/Knowledge/                       (ViewModels)
└── MenuConfiguration.cs                    (MODIFIED — add Knowledge entries; NFR-K10)

Mentoory.Web/wwwroot/js/knowledge/           (tree CRUD JS, reorder controls, priority-range editor)

Mentoory.Db/knowledge/
├── Schema.sql                              (MODIFIED — already exists with CREATE SCHEMA)
└── Tables/
    ├── KnowledgeStructureTemplates.sql
    ├── ModuleTemplates.sql
    ├── TopicTemplates.sql
    ├── SubjectTemplates.sql
    ├── ResourceTemplates.sql
    ├── KnowledgeStructures.sql
    ├── Modules.sql
    ├── Topics.sql
    ├── Subjects.sql
    └── Resources.sql

Mentoory.Db/diagnostic/Tables/FormTemplates.sql   (MODIFIED — add DefaultKnowledgeStructureTemplateId + FK)
Mentoory.Db/diagnostic/Tables/Questions.sql       (MODIFIED — add FK on TopicId to knowledge.Topics.Id)

Mentoory.Db.PostDeployment/
└── 015.SeedKnowledgeSampleTemplate.sql     (NEW; idempotent; FR-K50)

tests/Mentoory.Knowledge.Tests/              (EXISTING scaffold; populate)
├── Domain/                                  (aggregate invariants, sync semantics, priority resolution)
├── Handlers/                                (command + query handler tests)
└── Mentoory.Knowledge.Tests.csproj

tests/Mentoory.Tests.Integration/Knowledge/   (NEW subfolder)
├── KnowledgeStructureRoundTripTests.cs      (clone + CRUD + sync)
└── DiagnosticCascadeRoundTripTests.cs       (clone form → knowledge cascaded → TopicId rewritten)
```

**Structure Decision**: Mirror the existing `Mentoory.Diagnostic.*` module shape one-for-one. The existing empty `Mentoory.Knowledge.Domain/.Application/.Infrastructure` projects and `tests/Mentoory.Knowledge.Tests/` scaffold are reused. The only new top-level projects are none — all new code lives inside existing assemblies or under existing area directories. Cross-module modifications are surgical: one file in `FormTemplate`, one handler in `CloneFormTemplate`, two SSDT table files in `diagnostic/`. UI additions under `Areas/Coordination/` follow the existing Diagnostic/Coordination pattern (controllers → views → view models; JS under `wwwroot/js/{area}`).

## Complexity Tracking

> Empty — all Constitution gates passed without justification-required deviations.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| *none* | — | — |

---

# Phase 9 Amendment Plan — Project-owned KS binding

**Date**: 2026-04-19
**Source**: [`AMENDMENT-PROJECT-KS-BINDING.md`](./AMENDMENT-PROJECT-KS-BINDING.md) · brainstorm [`11`](../../brainstorm/11-knowledge-module-binding-redesign.md)
**Supersedes**: the cascade-based KS provisioning described in Phases 1–8 and `contracts/diagnostic-cascade.md`.
**Branch**: `016-knowledge-module-core` (continuation of PR #14).

## Summary (Phase 9)

Move the KS binding from `FormTemplate` to `Project`. At project creation, materialize a per-project `KnowledgeStructure` cloned from a caller-selected KS template; this becomes the single KS that every diagnostic form in that project references. `CloneFormTemplateHandler` simplifies from auto-cascade to compatibility-check + topic-id rewrite. Enforce 1 KS per project via UNIQUE on `KnowledgeStructures.ProjectId`. UI adds a required KS-template dropdown to the project-create form; the coordinator-facing "Clone from Template" UI is retired. Seeds rebuild for the new shape (clean-state, pre-release).

## Technical Context (Phase 9 deltas)

**Language/Version**: unchanged — C# / .NET 10.
**Primary Dependencies**: unchanged.
**Storage**: SQL Server via SSDT/DACPAC. Cross-schema ALTERs on `tenant.Projects` (+1 col, maybe more) and `knowledge.KnowledgeStructures` (UNIQUE on ProjectId, drop composite index, tighten `SourceTemplateId` to NOT NULL). Cross-module transaction semantics resolved in Phase 0 research below.
**Testing**: xUnit + Testcontainers; existing integration + E2E suites must stay green after seed rebuild.
**Constraints**:
- Every `Project` row must have exactly one `KnowledgeStructure` row (UNIQUE on `KnowledgeStructures.ProjectId`).
- `Project.KnowledgeStructureTemplateExternalId` — NOT NULL, immutable at application layer.
- `CloneFormTemplateHandler` may not write to `knowledge.*` schema.
- Spec 016 is pre-release — no production data, no migration, clean-state seed rebuild allowed.

## Constitution Re-check (Phase 9)

| Principle | Status | Evidence |
|---|---|---|
| I. Clean Architecture | ✅ | New abstraction `IKnowledgeStructureProvisioner` in `Mentoory.Tenant.Application.Abstractions`, implementation in `Mentoory.Knowledge.Infrastructure.CrossModule` — mirrors `ITopicUsageQuery` precedent |
| II. CQRS | ✅ | `CreateProjectCommand` remains an `IBaseRequest<Guid>`, handler unchanged shape (just extra dep + extra step) |
| III. DDD | ✅ | Project aggregate gains a single field; immutability enforced in the `SetKnowledgeStructure(...)` internal mutation |
| IV. Integration Events (ADR-001) | ✅ | No new events; cross-module provisioning is synchronous + in-transaction, not event-driven |
| V. Zero Warnings | ✅ | No new warning sources |
| VI. DateTime Handling | ✅ | KS clone timestamp passed from the command's `_timeProvider.UtcNow`; existing pattern |
| VII. Naming | ✅ | Standard Command/Handler/Validator naming retained |
| VIII. File Organization | ✅ | New abstraction under `Mentoory.Tenant.Application.Abstractions/`, impl under `Mentoory.Knowledge.Infrastructure.CrossModule/` |
| IX. Spanish UI | ✅ | New KS dropdown label + validator messages in Spanish |
| X. Role Hierarchy | ✅ | `ProjectsController` `[Authorize]` widens to `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"` |
| XI. SSDT/DACPAC | ✅ | One SSDT publish covers the ALTERs + constraint changes + seed rebuild |

**Result**: no violations; no complexity-tracking entries needed.

## Phase 0: Research

Three unknowns surfaced during brainstorm 11. Resolutions below.

### R1 — Cross-module transaction atomicity for CreateProject + KS materialization

**Question**: how do we atomically write a `Project` (to `TenantDbContext`) and a `KnowledgeStructure` (to `KnowledgeDbContext`) so that either both commit or neither does?

**Findings**:
- Existing `TransactionBehavior` pipeline resolves a single DbContext per request type via `IDbContextFactory.TryGetDbContextForRequest<TRequest>()` and wraps only that context. No precedent for multi-context transactions.
- `TransactionScope` + SQL Server distributed transactions would require MSDTC; operationally heavy and not in the project's dependency set.
- `Database.UseTransaction(IDbContextTransaction)` can share a transaction across two `DbContext` instances IF they use the same `SqlConnection`. Not wired in the existing `DbContextFactory`.

**Decision**: **Option G — eliminate the circular FK, drop `Project.KnowledgeStructureExternalId`, use `KS.ProjectId` (UNIQUE, NOT NULL) as the sole binding, accept a compensation-based reliability model.**

Rationale:
- The amendment doc proposed storing `Project.KnowledgeStructureExternalId` for symmetry. That creates a **circular FK**: `Project.KSExternalId → KS.ExternalId` and `KS.ProjectId → Project.Id`. Neither row can be inserted first without relaxing one of the FKs.
- If we drop `Project.KSExternalId` entirely, the "which KS does this project use?" query becomes `KS WHERE ProjectId = @projectId` — O(1) on the UNIQUE index, semantically identical, and removes the atomicity problem.
- The handler flow becomes linear-deterministic:
  1. Validate template (read `knowledge.KnowledgeStructureTemplates` via repo).
  2. Create `Project` aggregate → `projectRepository.Add(project)`.
  3. `TransactionBehavior` commits `TenantDbContext`; `project.Id` is now populated.
  4. Call `IKnowledgeStructureProvisioner.CloneForProjectAsync(templateExternalId, project.Id, incubator.Id)` — writes KS row via `KnowledgeDbContext` in a **separate** transaction.
  5. Return `Success(project.ExternalId)`.
- If step 4 fails after step 3 commits, the Project exists with no KS. Exposure: any subsequent form-clone fails with `"El proyecto no tiene estructura de conocimiento."` and the admin sees a "Repair knowledge structure" action in the project detail view (new, minimal).
- The 1:1 invariant is still enforced by `UNIQUE (KS.ProjectId)`; the domain invariant is enforced at the application layer (handler + tests).

**Rejected alternatives**:
- **Distributed transaction (MSDTC / `TransactionScope`)**: out of the project's operational envelope.
- **Shared-connection transaction (`DbContext.Database.UseTransaction`)**: possible but requires plumbing in `DbContextFactory` + `TransactionBehavior` that touches framework code beyond this spec's scope. Deferrable to a cross-cutting hardening spec if compensation fails in practice.
- **Synchronous in-process event with outbox**: adds eventual-consistency complexity; spec 010's outbox pattern not yet implemented on Tenant/Knowledge side.

**Amendment doc update needed**: the "Invariant 3: `Project.KnowledgeStructureExternalId` NOT NULL" is softened to "every Project has exactly one KS row (enforced by UNIQUE on KS.ProjectId); no redundant ExternalId stored on Project." Schema delta in data-model.md updated below.

### R2 — Fate of `CloneKnowledgeStructureTemplateCommand`

**Question**: does the existing Knowledge-side `CloneKnowledgeStructureTemplateCommand` stay, or fold into the domain factory?

**Decision**: **keep the domain factory (`KnowledgeStructure.CloneFromTemplate`), retire the MediatR command.**

- The command was introduced in Phase 4 US2 (T070) to let coordinators manually clone a KS template from the UI. With Phase 9, coordinators no longer invoke this — the KS materializes at project creation.
- The provisioner (`KnowledgeStructureProvisioner` implementing `IKnowledgeStructureProvisioner`) calls the factory directly and saves via `IKnowledgeStructureRepository` + `UnitOfWork.SaveEntitiesAsync`. No MediatR hop needed.
- Removing `CloneKnowledgeStructureTemplateCommand` also removes its validator, handler, unit tests, and controller action. Net deletion.

### R3 — Where does the KS-template dropdown's data come from, and does it need tenancy filtering?

**Question**: a project-creation form run by a `ProjectCoordinator` vs a `GlobalAdmin` — do they see the same catalog?

**Decision**: **all three roles see the full (non-archived) catalog; KS templates are global, not per-incubator.**

- `knowledge.KnowledgeStructureTemplates` has no `IncubatorId` column (existing design — global catalog).
- `ListKnowledgeStructureTemplatesQuery(IncludeArchived: false)` is already `[Authorize(Roles = "...GlobalAdmin, IncubatorAdmin, ProjectCoordinator")]`-callable (existing surface, verified by reading the controller action US1 set up).
- No new data access control required.

## Phase 1: Design & Contracts

### Data-model delta (overrides `AMENDMENT-PROJECT-KS-BINDING.md` based on R1)

#### `tenant.Projects` — ALTER

Add one column (not two):

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `KnowledgeStructureTemplateExternalId` | `UNIQUEIDENTIFIER` | **NOT NULL** | FK → `knowledge.KnowledgeStructureTemplates(ExternalId)`. Application-immutable after INSERT. |

**No `KnowledgeStructureExternalId` column** — the KS row is looked up via `KS.ProjectId`.

#### `knowledge.KnowledgeStructures` — ALTER

- Add `CONSTRAINT [UQ_KnowledgeStructures_ProjectId] UNIQUE ([ProjectId])`.
- Drop `IX_KnowledgeStructures_ProjectId_SourceTemplateId` (superseded).
- `SourceTemplateId` becomes `BIGINT NOT NULL` (no create-from-scratch path; domain factory already throws on that path).

### Contract — `IKnowledgeStructureProvisioner`

**Location**: `Mentoory.Tenant.Application/Abstractions/IKnowledgeStructureProvisioner.cs` (new folder; mirrors `Mentoory.Knowledge.Application/Abstractions/` layout).

```csharp
namespace Mentoory.Tenant.Application.Abstractions;

public interface IKnowledgeStructureProvisioner
{
    /// <summary>Clones the given KS template into a new project-scoped KnowledgeStructure.</summary>
    /// <returns>The ExternalId of the created structure, or Failure with a localized Spanish message.</returns>
    Task<Result<Guid>> CloneForProjectAsync(
        Guid templateExternalId,
        long projectId,
        long incubatorId,
        CancellationToken cancellationToken);
}
```

**Implementation**: `Mentoory.Knowledge.Infrastructure/CrossModule/KnowledgeStructureProvisioner.cs` (new), DI-registered in `AddKnowledgeInfrastructure`.

```csharp
public class KnowledgeStructureProvisioner(
    IKnowledgeStructureTemplateRepository templateRepo,
    IKnowledgeStructureRepository structureRepo,
    ITimeProvider timeProvider) : IKnowledgeStructureProvisioner
{
    public async Task<Result<Guid>> CloneForProjectAsync(...)
    {
        var template = await templateRepo.GetByExternalIdWithFullTreeAsync(templateExternalId, ct);
        if (template is null) return Failure(..., ("Plantilla", "La plantilla de conocimiento no fue encontrada."));
        if (template.IsArchived) return Failure(..., ("Plantilla", "No se puede crear un proyecto con una plantilla archivada."));

        var structure = KnowledgeStructure.CloneFromTemplate(template, projectId, incubatorId, timeProvider.UtcNow);
        structureRepo.Add(structure);
        await structureRepo.UnitOfWork.SaveEntitiesAsync(ct);

        return Success(structure.ExternalId);
    }
}
```

### Contract — updated `CreateProjectCommand`

```csharp
public sealed record CreateProjectCommand(
    Guid IncubatorExternalId,
    string Name,
    string? Description,
    Guid KnowledgeStructureTemplateExternalId,   // NEW, required
    bool IsPublic = false,
    EnrollmentVariant EnrollmentVariant = EnrollmentVariant.FullFlow) : IBaseRequest<Guid>;
```

Validator adds `RuleFor(x => x.KnowledgeStructureTemplateExternalId).NotEmpty().WithMessage("La plantilla de conocimiento es requerida.");`.

### Contract — updated `Project` aggregate

```csharp
public Guid KnowledgeStructureTemplateExternalId { get; private set; }   // NEW

public static Project Create(
    long incubatorId,
    string name,
    string? description,
    Guid knowledgeStructureTemplateExternalId,   // NEW, required
    DateTime utcNow,
    bool isPublic = false,
    EnrollmentVariant enrollmentVariant = EnrollmentVariant.FullFlow)
{
    // ... existing guards
    ArgumentException.ThrowIfNull(knowledgeStructureTemplateExternalId);
    if (knowledgeStructureTemplateExternalId == Guid.Empty)
        throw new ArgumentException("KS template is required.", nameof(knowledgeStructureTemplateExternalId));
    // ... existing body, set the new property
}
```

No separate `SetKnowledgeStructure` mutator — immutability by virtue of being private-set + only populated in the `Create` factory.

### Contract — updated `CreateProjectHandler`

```csharp
public override async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken ct)
{
    var incubator = await _incubatorRepo.GetByExternalIdAsync(request.IncubatorExternalId, ct);
    if (incubator is null) return Failure(..., (nameof(request.IncubatorExternalId), "Incubadora no encontrada."));

    // Validate KS template exists + is not archived BEFORE creating Project, so we can fail fast.
    var templateExists = await _ksTemplateRepo.ExistsByExternalIdAsync(request.KnowledgeStructureTemplateExternalId, ct);
    if (!templateExists) return Failure(..., ("KnowledgeStructureTemplate", "La plantilla de conocimiento no fue encontrada."));

    var project = Project.Create(
        incubator.Id, request.Name, request.Description,
        request.KnowledgeStructureTemplateExternalId,
        _timeProvider.UtcNow, request.IsPublic, request.EnrollmentVariant);

    _projectRepository.Add(project);
    await _projectRepository.UnitOfWork.SaveEntitiesAsync(ct);   // tenant tx commits here; project.Id populated

    // Now materialize the KS in the Knowledge schema, separate transaction.
    var ksResult = await _provisioner.CloneForProjectAsync(
        request.KnowledgeStructureTemplateExternalId, project.Id, incubator.Id, ct);

    if (ksResult.IsFailure)
    {
        // Compensation NOT attempted in v1 — Project exists without KS, admin must run "Repair KS" action.
        // Logged at Error level for visibility.
        LogKsProvisioningFailed(project.ExternalId, request.KnowledgeStructureTemplateExternalId, ksResult);
        return Failure(ResultErrorCodes.GenericError,
            ("KnowledgeStructure", "Proyecto creado pero falló la creación de la estructura de conocimiento. Contacte a un administrador."));
    }

    LogProjectCreated(project.ExternalId, request.IncubatorExternalId);
    return Success(project.ExternalId);
}
```

**Handler dependencies gain**:
- `IKnowledgeStructureTemplateRepository` (for the fail-fast existence check)
- `IKnowledgeStructureProvisioner` (for the actual clone)
- This pulls `Mentoory.Tenant.Application` → `Mentoory.Knowledge.Domain` (project reference ok; Knowledge.Domain is already a shared dependency).

**Note**: `IKnowledgeStructureTemplateRepository.ExistsByExternalIdAsync` already exists (T033). No new interface method needed.

### Contract — simplified `CloneFormTemplateHandler`

```csharp
public override async Task<Result> Handle(CloneFormTemplateCommand request, CancellationToken ct)
{
    var template = await _formTemplateRepository.GetByExternalIdWithQuestionsAsync(request.SourceTemplateExternalId, ct);
    if (template is null) return Failure(..., ("Template", "La plantilla de formulario no fue encontrada."));

    // Load the project's single KnowledgeStructure (guaranteed to exist by project-creation invariant).
    var projectStructure = await _ksStructureRepo.GetByProjectIdAsync(request.ProjectId, ct);
    if (projectStructure is null)
    {
        // Defensive — should never happen, but surface a specific error if it does.
        return Failure(..., ("KnowledgeStructure", "El proyecto no tiene estructura de conocimiento."));
    }

    // Compatibility check (new).
    if (template.DefaultKnowledgeStructureTemplateExternalId is Guid formKs &&
        formKs != projectStructure.SourceTemplateExternalId)   // see note below
    {
        return Failure(..., ("Cascada",
            "Este formulario está diseñado para una estructura de conocimiento diferente a la del proyecto."));
    }

    // Build rewrite map from the project's existing topics (all have SourceTemplateTopicExternalId stamps).
    var rewriteMap = BuildMapFromProjectStructure(projectStructure);

    var projectForm = ProjectForm.CloneFromTemplate(template, request.ProjectId, request.IncubatorId, _timeProvider.UtcNow, rewriteMap);
    _projectFormRepository.Add(projectForm);
    await _projectFormRepository.UnitOfWork.SaveEntitiesAsync(ct);

    LogFormCloned(projectForm.ExternalId, template.ExternalId);
    return Success();
}
```

**Note on compatibility check**: `projectStructure.SourceTemplateExternalId` isn't currently materialized on the clone — the clone stores `SourceTemplateId` (BIGINT). To check matching template ExternalIds we either:
- (a) Resolve `templateExternalId` at check-time via `_ksTemplateRepo.GetByIdAsync(projectStructure.SourceTemplateId)`. Extra DB call; acceptable.
- (b) Add a computed `SourceTemplateExternalId` property on `KnowledgeStructure` hydrated at query time.

**Decision**: (a). One indexed lookup at form-clone time is cheap and keeps the aggregate lean.

### Repository interface delta

`IKnowledgeStructureRepository`:
- **Replace** `GetByProjectAndSourceTemplateIdAsync` with `GetByProjectIdAsync(long projectId, CancellationToken)`.
- **Keep** `CountClonesBySourceTemplateIdAsync` (still used by `DeleteKnowledgeStructureTemplate` EC-01 guard).
- **Keep** `ListByProjectAsync` — though with UNIQUE on ProjectId it'll return 0 or 1. Consider renaming to `GetSingleByProjectIdAsync` for clarity; mark the old name as obsolete (one release).

### UI delta

**`Administration/Views/Projects/Create.cshtml`**:
- Add a `<select asp-for="KnowledgeStructureTemplateExternalId">` between Description and "Configuración" divider.
- Populate options from `ViewBag.KnowledgeTemplates` via `ListKnowledgeStructureTemplatesQuery(includeArchived: false)`.
- Required-select validation handled by FluentValidation on the command + client-side `data-val-required`.

**`ProjectsController.Create` [GET]**: populate `ViewBag.KnowledgeTemplates`; then same pattern as the existing Diagnostic Clone view.

**`ProjectsController.Create` [POST]**: pass `model.KnowledgeStructureTemplateExternalId` into the command. Authorization widens to `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"` (update class-level attribute).

**`CreateProjectViewModel`**: add `[Required]` `Guid KnowledgeStructureTemplateExternalId` with Spanish display label `"Plantilla de conocimiento"`.

**Knowledge area**:
- **Remove** `Areas/Coordination/Views/Knowledge/CloneFromTemplate.cshtml`.
- **Remove** controller actions `CloneFromTemplateGet` / `CloneFromTemplate` (POST) in `KnowledgeController`.
- **Remove** `CloneStructureViewModel` from ViewModels file.
- **Update** `Projects.cshtml` empty-state: remove the "Clonar desde plantilla" CTA; replace with a plain "No hay estructuras de conocimiento" message (should never render post-amendment because invariant guarantees one exists).

### Seed delta

`004.SeedTestData.sql` § 3 (Projects): each `tenant.Projects` INSERT now sets `KnowledgeStructureTemplateExternalId = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER)` ("Emprendimiento Básico", seeded in 005).

`004.SeedTestData.sql` § 3.5 (the project-side KS for Topic FK parent): **must now run AFTER `005` has seeded the template**, OR move the Emprendimiento Básico template seed into 004 (earlier in the file). Path B is simpler — inline the template seed at the start of `004` § 3.5 before the KS row. Keeps dependency ordering within one script.

`005.SeedKnowledgeData.sql` § 1 becomes a no-op if 004 already seeded the template (idempotent `IF NOT EXISTS` keeps it safe). § 2 (FormTemplate wiring) unchanged.

`IntegrationTestBase.SeedKnowledgeTopicsAsync`: change the hardcoded `SourceTemplateId = NULL` to a real template id (same sentinel approach as the seed). Alternatively, seed via `CreateProjectCommand` for test projects — slower but more representative.

### Tests delta

- `tests/Mentoory.Tenant.Tests/Handlers/CreateProjectHandlerTests.cs` (new or extended):
  - success with valid template
  - failure: missing incubator
  - failure: missing KS template
  - failure: archived KS template
  - failure: provisioner returns failure → Project saved but Result is Failure (verify logging)
- `tests/Mentoory.Diagnostic.Tests/Handlers/CloneFormTemplateHandler*Tests.cs`:
  - rename cascade tests to compatibility tests
  - scenarios: compatible form → success + rewrite; incompatible → rejection; unbound form → legacy pass-through
- `tests/Mentoory.Knowledge.Tests/Handlers`:
  - remove or repurpose `CloneKnowledgeStructureTemplateHandlerTests` (command retired)
  - keep domain-level clone tests on `KnowledgeStructureCloneTests` — factory exercised via provisioner
- `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs`:
  - preseed via `CreateProjectCommand` (not raw SQL)
  - first test: clone compatible form → topic-id rewrite verified
  - second test was "reuse existing KS" — now trivially satisfied; convert to a regression check that a second compatible form clone does NOT create another KS row (UNIQUE constraint gives us this for free, but the test is documentation)
- E2E: `PlaywrightFixture` may need to handle the new KS-template dropdown on the project-creation page (if any E2E covers that flow).

## Phase 2 Planning (not executed here)

The refactor tasks are already enumerated in `tasks.md` Phase 9 (T110–T132). They remain valid with one amendment from Phase 0:

- **T110** — Projects.sql ALTER: **only one** new column (`KnowledgeStructureTemplateExternalId`), **not two**. Drop the `KnowledgeStructureExternalId` row from the table's schema delta.
- **T112** — Project aggregate: drop the `KnowledgeStructureExternalId` property + the `SetKnowledgeStructure(...)` mutator. `Create(...)` factory takes just the template Guid.
- **T117** — Handler flow updated to the two-transaction model with compensation-via-visible-failure described in R1.
- **T130/T131** — add a "Repair knowledge structure" action in the project detail view as a catch-net for the rare KS-provisioning failure. Admin-only. Deferred to a follow-up if out of scope for this amendment — capture as open thread if so.

## Complexity Tracking (Phase 9)

No violations. The two-transaction approach is a deliberate simplification over shared-connection plumbing; the failure mode (orphan Project without KS) is rare, detectable, and recoverable.

## Progress

- [X] Tech Context filled
- [X] Constitution Check (re-evaluated)
- [X] Phase 0: Research complete (R1, R2, R3)
- [X] Phase 1: Design & Contracts defined
- [X] Phase 1: Post-design Constitution re-check (still passes)
- [ ] Phase 2: Tasks generated (already present in `tasks.md` Phase 9; adjustments listed above)
- [ ] Phase 3: Implementation (not started)

