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
