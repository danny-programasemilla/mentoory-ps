---
description: "Task list for 016-knowledge-module-core"
---

# Tasks: Knowledge Module Core

**Input**: Design documents from `/specs/016-knowledge-module-core/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (all present)

**Tests**: Included. NFR-K09 explicitly requires unit + handler + integration coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. Cross-schema SSDT changes are consolidated in Foundational per NFR-K08 (single-PR atomicity).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1..US5)
- Include exact file paths in descriptions

## Path Conventions

- **Domain/Application/Infrastructure projects**: `Mentoory.Knowledge.Domain/`, `Mentoory.Knowledge.Application/`, `Mentoory.Knowledge.Infrastructure/` (existing scaffolded projects, no code yet)
- **Diagnostic cross-module edits**: `Mentoory.Diagnostic.Domain/`, `Mentoory.Diagnostic.Application/`, `Mentoory.Diagnostic.Infrastructure/`
- **DB (SSDT)**: `Mentoory.Db/knowledge/` and `Mentoory.Db/diagnostic/`
- **Seed scripts**: `Mentoory.Db.PostDeployment/` (outside `Mentoory.Db/` per constitution VIII)
- **Web**: `Mentoory.Web/Areas/Coordination/`; JS in `Mentoory.Web/wwwroot/js/knowledge/`
- **Tests**: `tests/Mentoory.Knowledge.Tests/` (unit+handler), `tests/Mentoory.Tests.Integration/Knowledge/` (integration)

---

## Phase 1: Setup

**Purpose**: Project references, DI scaffolding, folder structure.

- [X] T001 Add `Mentoory.Knowledge.Domain`, `Mentoory.Knowledge.Application`, `Mentoory.Knowledge.Infrastructure` project references to `Mentoory.Web/Mentoory.Web.csproj` and `Mentoory.Aspire.AppHost/Mentoory.Aspire.AppHost.csproj`; add Application→Domain and Infrastructure→Application references on those three csproj files; verify `dotnet build` succeeds with zero warnings
- [X] T002 [P] Create folder skeletons inside each Knowledge project per plan.md Project Structure (Aggregates/KnowledgeStructureTemplate, Aggregates/KnowledgeStructure, Enums, ValueObjects, Repositories, Application/Commands, Application/Queries, Application/IntegrationEvents, Application/Mappings, Infrastructure/Persistence/Configurations, Infrastructure/Persistence/Repositories, Infrastructure/DependencyInjection) — no files yet, just `.gitkeep` placeholders to lock the tree
- [X] T003 [P] Create `Mentoory.Knowledge.Infrastructure/DependencyInjection/KnowledgeServiceCollectionExtensions.cs` with an empty `AddKnowledgeModule(this IServiceCollection, IConfiguration)` stub; register it from `Mentoory.Web/Program.cs` alongside the existing `AddDiagnosticModule` call
- [X] T004 [P] Ensure `tests/Mentoory.Knowledge.Tests/Mentoory.Knowledge.Tests.csproj` references `Mentoory.Knowledge.Domain`, `Mentoory.Knowledge.Application`, `Mentoory.Knowledge.Infrastructure`, `Mentoory.Shared.Application`; add `xunit`, `Moq`, `FluentAssertions`, `Microsoft.EntityFrameworkCore.InMemory` if missing

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-schema DB changes (one SSDT PR per NFR-K08), shared domain primitives, DbContext + repos used by every story. No user-story work can begin until this phase is complete.

**Critical sequencing**: T005–T015 and T025 must land in the same SSDT publish to preserve atomicity.

### DB schema — `knowledge.*` tables (all [P] relative to each other)

- [X] T005 [P] Create `Mentoory.Db/knowledge/Tables/KnowledgeStructureTemplates.sql` with columns per data-model.md
- [X] T006 [P] Create `Mentoory.Db/knowledge/Tables/ModuleTemplates.sql` with FK + index
- [X] T007 [P] Create `Mentoory.Db/knowledge/Tables/TopicTemplates.sql` with six DECIMAL priority-range columns
- [X] T008 [P] Create `Mentoory.Db/knowledge/Tables/SubjectTemplates.sql`
- [X] T009 [P] Create `Mentoory.Db/knowledge/Tables/ResourceTemplates.sql`
- [X] T010 [P] Create `Mentoory.Db/knowledge/Tables/KnowledgeStructures.sql` (SET NULL on SourceTemplateId)
- [X] T011 [P] Create `Mentoory.Db/knowledge/Tables/Modules.sql` with SourceTemplateModuleExternalId
- [X] T012 [P] Create `Mentoory.Db/knowledge/Tables/Topics.sql` with SourceTemplateTopicExternalId + 6 ranges
- [X] T013 [P] Create `Mentoory.Db/knowledge/Tables/Subjects.sql`
- [X] T014 [P] Create `Mentoory.Db/knowledge/Tables/Resources.sql`

### DB schema — cross-module FK additions (single SSDT PR)

- [X] T015 Modify `Mentoory.Db/diagnostic/Tables/FormTemplates.sql`: add `DefaultKnowledgeStructureTemplateId` + FK + index
- [X] T016 Modify `Mentoory.Db/diagnostic/Tables/Questions.sql`: add `FK_Questions_Topics` FK + `IX_Questions_TopicId` index
- [X] T017 Audit existing seed TopicIds (1-5 literal in `004.SeedTestData.sql`) — reconciled via SET IDENTITY_INSERT in 005 seed

### Seed (PostDeployment, single SSDT PR)

- [X] T018 Create `Mentoory.Db.PostDeployment/005.SeedKnowledgeData.sql` (idempotent, incl. Topic id 1-5 reconciliation + FormTemplate default binding UPDATE); registered in `.sqlproj` and `Script.PostDeployment.sql`.

### Shared domain primitives

- [X] T019 [P] Create `Mentoory.Knowledge.Domain/Enums/ResourceType.cs`
- [X] T020 [P] Create `Mentoory.Knowledge.Domain/Enums/SyncMode.cs`
- [X] T021 [P] Create `Mentoory.Knowledge.Domain/Enums/Priority.cs`
- [X] T022 Create `Mentoory.Knowledge.Domain/ValueObjects/PriorityRange.cs`
- [X] T023 [P] Create `tests/Mentoory.Knowledge.Tests/Domain/PriorityRangeTests.cs` — 14 tests, all pass

### DbContext + module DI

- [X] T024 Create `Mentoory.Knowledge.Infrastructure/Persistence/KnowledgeDbContext.cs` — default schema `"knowledge"` + ApplyConfigurationsFromAssembly (DbSets added in US1/US2 when aggregates land)
- [X] T025 Extend DI: DbContext registration wired; repos + MediatR/validators registered in `AddKnowledgeApplication()` + `AddKnowledgeInfrastructure()`. Repository DI deferred to US1/US2 when interfaces exist.
- [X] T026 [P] No Aspire AppHost change required — Knowledge uses the same shared `DefaultConnection` already wired via `Projects.Mentoory_Web`; `AddKnowledgeInfrastructure(builder, "DefaultConnection")` resolves automatically.

### Menu wiring

- [X] T027 Modify `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs`: added "Conocimiento" group with Plantillas/Estructuras sub-items

**Checkpoint**: Foundation is ready. `dotnet build` passes; `cd Mentoory.Db && publish-mentoorydb.sh` publishes without FK errors; Aspire dashboard shows the Knowledge module; tests from T023 pass. User story work can now begin.

---

## Phase 3: User Story 1 — Global admin curates the knowledge catalog (Priority: P1) 🎯 MVP ✅ COMPLETE

**Status**: All of T028–T056 complete. Knowledge module + Web project build with 0 warnings; 48 unit tests pass (PriorityRange, domain aggregate, handlers, priority-range handler). UI mounted at `/Coordination/Knowledge/Templates`.



**Goal**: Deliver global-admin template CRUD at all four levels with priority-range editor, archival, and deletion guard. A GlobalAdmin can build out the shared learning catalog.

**Independent Test**: quickstart.md US1 (steps 1–11) — GlobalAdmin creates a template, adds nested Modules/Topics/Subjects/Resources with priority bands, triggers range-overlap rejection, reorders, archives, toggles "Show archived", and deletes when no clones exist.

### Domain aggregate — KnowledgeStructureTemplate

- [ ] T028 [P] [US1] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructureTemplate/ResourceTemplate.cs` — entity with `Id`, `ExternalId`, `SubjectTemplateId`, `Title`, `Description`, `Url`, `ResourceType`, `SortOrder`; `internal static Create(...)` factory; `internal UpdateDetails(...)` and `internal UpdateSortOrder(int)` mutators
- [ ] T029 [P] [US1] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructureTemplate/SubjectTemplate.cs` — entity with private `List<ResourceTemplate> _resources`; `Resources` read-only view; `internal AddResource/UpdateResource/RemoveResource/ReorderResources` methods
- [ ] T030 [P] [US1] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructureTemplate/TopicTemplate.cs` — entity with private `List<SubjectTemplate> _subjects`; exposes `HighRange`/`MediumRange`/`LowRange` as `PriorityRange?` computed from the six nullable decimal columns; `internal UpdatePriorityRanges(PriorityRange? high, PriorityRange? medium, PriorityRange? low)` with overlap validation
- [ ] T031 [P] [US1] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructureTemplate/ModuleTemplate.cs` — entity with private `List<TopicTemplate> _topics`; add/update/remove/reorder topics
- [ ] T032 [US1] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructureTemplate/KnowledgeStructureTemplate.cs` — aggregate root implementing `Entity`, `IAggregateRoot`; private `List<ModuleTemplate> _modules`; factory `Create(name, description, utcNow)`; mutations for root details + module add/update/delete/reorder + topic add/update/delete/reorder + range update + subject add/update/delete/reorder + resource add/update/delete/reorder; EVERY mutation bumps `Version++`; `Archive()`/`Unarchive()` also bump version. Depends on T028–T031.

- [ ] T033 [P] [US1] Create `Mentoory.Knowledge.Domain/Repositories/IKnowledgeStructureTemplateRepository.cs` — interface per contracts/diagnostic-cascade.md (GetByExternalId, GetById, GetByExternalIdWithFullTreeAsync, GetByIdWithFullTreeAsync, ExistsByExternalIdAsync, Add/Update/Remove, `IUnitOfWork`)

### EF configurations + repository

- [ ] T034 [US1] Create `Mentoory.Knowledge.Infrastructure/Persistence/Configurations/KnowledgeStructureTemplateConfiguration.cs` — map PK, `ExternalId` unique index, columns, owned navigations to private `_modules`; string-based include via `Metadata.FindNavigation("_modules").SetPropertyAccessMode(PropertyAccessMode.Field)` mirror of Diagnostic
- [ ] T035 [P] [US1] Create `Mentoory.Knowledge.Infrastructure/Persistence/Configurations/ModuleTemplateConfiguration.cs` + `TopicTemplateConfiguration.cs` + `SubjectTemplateConfiguration.cs` + `ResourceTemplateConfiguration.cs` — same pattern (this is ONE task because all four configs are trivial sibling copies; split into 4 sub-tasks only if necessary)
- [ ] T036 [US1] Create `Mentoory.Knowledge.Infrastructure/Persistence/Repositories/KnowledgeStructureTemplateRepository.cs` — concrete implementation with `GetByExternalIdWithFullTreeAsync` that `.Include("_modules").ThenInclude("_topics").ThenInclude("_subjects").ThenInclude("_resources")` (four-level chain per research R4); use `AsSplitQuery()` on the full-tree path to avoid cartesian explosion; `AsNoTracking()` is NOT applied on write paths but IS applied on a parallel `GetByExternalIdReadOnlyAsync` used only by queries

### Unit tests — domain invariants

- [ ] T037 [P] [US1] Create `tests/Mentoory.Knowledge.Tests/Domain/KnowledgeStructureTemplateTests.cs` — cover: `Create` sets `Version = 1`; `AddModule` bumps `Version`; nested descendant changes bump root `Version`; `Archive`/`Unarchive` bump `Version`; overlapping ranges rejected by `UpdateTopicPriorityRanges`
- [ ] T038 [P] [US1] Create `tests/Mentoory.Knowledge.Tests/Domain/TopicTemplatePriorityResolutionTests.cs` — pre-emptive coverage of the pattern shared with Topic (the clone-side version lands in US2 tests); assert that a topic with High 8–10 / Medium 5–7.99 / Low 0–4.99 resolves 9 → High, 6 → Medium, 3 → Low, 11 → NotApplicable, -1 → NotApplicable

### Application commands & validators (all [P] — separate files)

- [ ] T039 [P] [US1] `Mentoory.Knowledge.Application/Commands/CreateKnowledgeStructureTemplate/` — command record + FluentValidation validator (Name not empty ≤200, Description ≤2000) + `CreateKnowledgeStructureTemplateHandler : BaseCommandHandler<_, Guid>` returning new template's `ExternalId`; uses `ITimeProvider`
- [ ] T040 [P] [US1] `Mentoory.Knowledge.Application/Commands/UpdateKnowledgeStructureTemplate/` — record + validator + handler calling `template.UpdateDetails(...)`
- [ ] T041 [P] [US1] `Mentoory.Knowledge.Application/Commands/ArchiveKnowledgeStructureTemplate/` + `UnarchiveKnowledgeStructureTemplate/`  — two command slices in one task given the symmetry
- [ ] T042 [P] [US1] `Mentoory.Knowledge.Application/Commands/DeleteKnowledgeStructureTemplate/` — handler performs a count via `IKnowledgeStructureRepository.ListByProjectAsync` (iterates all and filters) OR a dedicated `CountClonesBySourceTemplateIdAsync` method (ADD this method to `IKnowledgeStructureRepository` while implementing); returns `Failure` with Spanish message `"No se puede eliminar: la plantilla tiene clones en uso por {n} proyecto(s). Archívela en su lugar."` if count > 0 (EC-01)
- [ ] T043 [P] [US1] `Mentoory.Knowledge.Application/Commands/AddModuleTemplate/`, `UpdateModuleTemplate/`, `DeleteModuleTemplate/`, `ReorderModuleTemplates/` — four command slices routed through root aggregate
- [ ] T044 [P] [US1] `Mentoory.Knowledge.Application/Commands/AddTopicTemplate/`, `UpdateTopicTemplate/`, `DeleteTopicTemplate/`, `ReorderTopicTemplates/` — four command slices; DeleteTopicTemplate does NOT require the diagnostic-question pre-check (templates aren't referenced by any diagnostic.Questions row — the FK from Questions points at `knowledge.Topics`, not `TopicTemplates`)
- [ ] T045 [P] [US1] `Mentoory.Knowledge.Application/Commands/UpdateTopicTemplatePriorityRanges/` — record + validator (enforces non-overlap across bands; in-band `min <= max`) + handler
- [ ] T046 [P] [US1] `Mentoory.Knowledge.Application/Commands/AddSubjectTemplate/` + Update/Delete/Reorder — one task containing the four command slices
- [ ] T047 [P] [US1] `Mentoory.Knowledge.Application/Commands/AddResourceTemplate/` + Update/Delete/Reorder — resource commands include validator enforcing `Url` absolute URI

### Application queries

- [ ] T048 [P] [US1] `Mentoory.Knowledge.Application/Queries/ListKnowledgeStructureTemplates/` — query record (`bool IncludeArchived`) + handler using `AsNoTracking()`; returns `IReadOnlyList<KnowledgeStructureTemplateListItemDto>` projected via Mapperly (sibling `KnowledgeStructureTemplateListItemMapper.cs`); module/topic counts computed via `SelectMany`/`Count()`
- [ ] T049 [P] [US1] `Mentoory.Knowledge.Application/Queries/GetKnowledgeStructureTemplate/` — full-tree DTO query; handler loads via `GetByExternalIdWithFullTreeAsync` and projects via Mapperly

### Handler tests

- [ ] T050 [P] [US1] `tests/Mentoory.Knowledge.Tests/Handlers/CreateKnowledgeStructureTemplateHandlerTests.cs` + similar tests for Update/Archive/Unarchive/Delete (five test files or one consolidated — keep consolidated for brevity; one file covering all five handlers)
- [ ] T051 [P] [US1] `tests/Mentoory.Knowledge.Tests/Handlers/ModuleTemplateCrudHandlerTests.cs` — add/update/delete/reorder for module + topic + subject + resource levels; one consolidated file covering the hierarchical CRUD surface (~16 tests)
- [ ] T052 [P] [US1] `tests/Mentoory.Knowledge.Tests/Handlers/UpdateTopicTemplatePriorityRangesHandlerTests.cs` — passing case, overlap rejection, min>max rejection, arbitrary decimal scale (no 0–100 bound per research R1), all bands unset legal state

### Web controller + views + JS

- [ ] T053 [US1] Create `Mentoory.Web/Areas/Coordination/Controllers/KnowledgeController.cs` — inherit `BaseController`; class-level `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`; action-level `[Authorize(Roles = "GlobalAdmin")]` overrides on every template-scoped action. Actions: `Templates(includeArchived)`, `TemplateDetail(Guid externalId)`, `CreateTemplate(CreateTemplateViewModel)`, `UpdateTemplate(...)`, `ArchiveTemplate(...)`, `UnarchiveTemplate(...)`, `DeleteTemplate(...)`, plus Module/Topic/Subject/Resource add/update/delete/reorder and `UpdateTopicPriorityRanges`
- [ ] T054 [P] [US1] Create `Mentoory.Web/Areas/Coordination/Models/Knowledge/` — `TemplateListItemViewModel.cs`, `TemplateDetailViewModel.cs`, `CreateTemplateViewModel.cs`, `UpdateTemplateViewModel.cs`, `PriorityRangeViewModel.cs` (with `Min`, `Max` fields) — all Spanish-labeled `[Display]` attributes
- [ ] T055 [P] [US1] Create Razor views under `Mentoory.Web/Areas/Coordination/Views/Knowledge/`: `Templates.cshtml` (list with "Mostrar archivados" toggle), `TemplateDetail.cshtml` (tree editor with Modules → Topics → Subjects → Resources), `_TemplateTree.cshtml` partial, `_PriorityRangeEditor.cshtml` partial; Spanish strings throughout; follow existing Diagnostic view patterns and Tabler styling
- [ ] T056 [P] [US1] Create `Mentoory.Web/wwwroot/js/knowledge/template-editor.js` — tree reorder via existing Tabler drag handles; inline add/edit/delete via fetch POSTs; priority-range editor with live validation (client-side overlap check mirroring server validation); toast wiring via existing `showToast(message, type)` helper

**Checkpoint**: US1 is fully functional. GlobalAdmin can populate the catalog end-to-end. Run quickstart.md US1 (steps 1–11). Templates table has non-trivial rows.

---

## Phase 4: User Story 2 — Coordinator clones and customizes a project structure (Priority: P1) ✅ COMPLETE

**Status**: T057–T086 complete. Clone aggregate with `CloneFromTemplate` deep-copy + source-stamping. 20 clone-side command slices. `IKnowledgeStructureRepository` with `CountClonesBySourceTemplateIdAsync` + cross-module `ITopicUsageQuery` + `DiagnosticTopicUsageQuery` implementation (EC-30 guard live). UI mounted at `/Coordination/Knowledge/Projects` with drift badge + sync-mode toggle.



**Goal**: Coordinator clones a template, edits the clone freely, without affecting the template. Onboards a project's learning structure in ≤10 minutes (SC-K01).

**Independent Test**: quickstart.md US2 (steps 1–8) — clone, rename, add clone-only topic, edit ranges, delete attempt-with-block-vs-success.

### Domain aggregate — KnowledgeStructure (project clone)

- [ ] T057 [P] [US2] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructure/Resource.cs` — same shape as `ResourceTemplate` plus nullable `SourceTemplateResourceExternalId`
- [ ] T058 [P] [US2] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructure/Subject.cs` — same shape as `SubjectTemplate` plus `SourceTemplateSubjectExternalId`; private `List<Resource> _resources`
- [ ] T059 [P] [US2] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructure/Topic.cs` — same shape as `TopicTemplate` plus `SourceTemplateTopicExternalId`; expose `Priority ResolvePriority(decimal score)` method (FR-K06); private `List<Subject> _subjects`
- [ ] T060 [P] [US2] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructure/Module.cs` — same shape as `ModuleTemplate` plus `SourceTemplateModuleExternalId`; private `List<Topic> _topics`
- [ ] T061 [US2] Create `Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructure/KnowledgeStructure.cs` — aggregate root implementing `Entity`, `IAggregateRoot`; factory `CloneFromTemplate(KnowledgeStructureTemplate template, long projectId, long incubatorId, DateTime utcNow)` that deep-copies all four levels and stamps every `SourceTemplateXExternalId`, sets `SourceTemplateId`, `SourceTemplateVersion`, `SyncMode = Disconnected`; factory `Create(...)` (no-template path, reserved — may leave as throwing stub for v1); mutations for all descendant levels (add/update/delete/reorder); `SetSyncMode(SyncMode)` throws when switching to `PartialSync` with null `SourceTemplateId`; `UpdateTopicPriorityRanges(Guid topicExternalId, ...)` — event emission deferred to US4 phase. Depends on T057–T060.

- [ ] T062 [P] [US2] Create `Mentoory.Knowledge.Domain/Repositories/IKnowledgeStructureRepository.cs` — interface per contracts/project-clones.md including `GetByProjectAndSourceTemplateIdAsync`, `ListByProjectAsync`, `CountQuestionsReferencingTopicAsync(long topicId)` (note: the Count implementation will cross-module; see T067)

### EF configurations + repository

- [ ] T063 [US2] Create `Mentoory.Knowledge.Infrastructure/Persistence/Configurations/KnowledgeStructureConfiguration.cs` — root + index on `(ProjectId, SourceTemplateId)` for reuse query
- [ ] T064 [P] [US2] Create `Mentoory.Knowledge.Infrastructure/Persistence/Configurations/ModuleConfiguration.cs` + `TopicConfiguration.cs` + `SubjectConfiguration.cs` + `ResourceConfiguration.cs` — one task, four sibling configs with `SourceTemplateXExternalId` mapped as nullable Guid
- [ ] T065 [US2] Create `Mentoory.Knowledge.Infrastructure/Persistence/Repositories/KnowledgeStructureRepository.cs` — full-tree include + split query; `GetByProjectAndSourceTemplateIdAsync` filters by both; `ListByProjectAsync` with `AsNoTracking()`
- [ ] T066 [US2] Add `CountClonesBySourceTemplateIdAsync(long sourceTemplateId, CancellationToken ct)` method on `KnowledgeStructureRepository` (referenced by DeleteKnowledgeStructureTemplate handler from T042)
- [ ] T067 [US2] Cross-module peek for delete-topic pre-check (EC-30): add `ITopicUsageQuery` interface under `Mentoory.Knowledge.Application/Abstractions/` exposing `Task<int> CountQuestionsReferencingTopicAsync(long topicId, CancellationToken)`. Implementation `DiagnosticTopicUsageQuery` lives in `Mentoory.Diagnostic.Infrastructure/CrossModule/DiagnosticTopicUsageQuery.cs` injecting `DiagnosticDbContext` and counting `Questions` by raw `TopicId`. Register the implementation in `Mentoory.Diagnostic.Infrastructure.DependencyInjection` so it's available through DI when Knowledge's delete handler is invoked. This avoids Knowledge.Infrastructure referencing Diagnostic.Infrastructure directly (constitution I)

### Unit tests — clone semantics

- [ ] T068 [P] [US2] `tests/Mentoory.Knowledge.Tests/Domain/KnowledgeStructureCloneTests.cs` — assert `CloneFromTemplate` produces identical tree shape with every node stamped; mutations on clone don't affect template rows (in-memory assertion on original template reference); `SyncMode` defaults Disconnected; locally-added topic has null `SourceTemplateTopicExternalId`
- [ ] T069 [P] [US2] `tests/Mentoory.Knowledge.Tests/Domain/TopicResolvePriorityTests.cs` — same assertions as T038 but on the clone-side `Topic.ResolvePriority`; also asserts arbitrary decimal scale works (e.g., High 7.50–8.00, score 7.75 → High)

### Application commands

- [ ] T070 [P] [US2] `Mentoory.Knowledge.Application/Commands/CloneKnowledgeStructureTemplate/` — command record + validator (project exists, template not archived) + handler that loads template full tree, calls `KnowledgeStructure.CloneFromTemplate(...)`, adds, saves, returns new `ExternalId`
- [ ] T071 [P] [US2] `Mentoory.Knowledge.Application/Commands/UpdateKnowledgeStructure/` — update root Name/Description
- [ ] T072 [P] [US2] `Mentoory.Knowledge.Application/Commands/AddModule/` + Update/Delete/Reorder — four command slices for clone's Module level; delete is unconstrained in v1 (EC-31)
- [ ] T073 [US2] `Mentoory.Knowledge.Application/Commands/AddTopic/` + Update/Reorder — THREE slices (Delete is its own task due to pre-check)
- [ ] T074 [US2] `Mentoory.Knowledge.Application/Commands/DeleteTopic/` — command + handler that calls `ITopicUsageQuery.CountQuestionsReferencingTopicAsync(topic.Id)`; if `> 0` return `Failure` with Spanish message per contracts/project-clones.md; else `structure.RemoveTopic(...)` + save. Depends on T067
- [ ] T075 [P] [US2] `Mentoory.Knowledge.Application/Commands/UpdateTopicPriorityRanges/` — record + validator (overlap + min<=max) + handler that applies ranges and saves. **Event emission deferred to US4 phase (T087).**
- [ ] T076 [P] [US2] `Mentoory.Knowledge.Application/Commands/AddSubject/` + Update/Delete/Reorder — four slices for clone's Subject level
- [ ] T077 [P] [US2] `Mentoory.Knowledge.Application/Commands/AddResource/` + Update/Delete/Reorder — four slices for clone's Resource level
- [ ] T078 [P] [US2] `Mentoory.Knowledge.Application/Commands/SetSyncMode/` — command + handler calling `structure.SetSyncMode(mode)` + save (SyncFromTemplate itself is US5 territory)

### Application queries

- [ ] T079 [P] [US2] `Mentoory.Knowledge.Application/Queries/ListProjectKnowledgeStructures/` — query + handler tenant-scoped via `ITenantContext.CurrentProjectId`; DTO includes `HasTemplateVersionDrift` computed via join to `KnowledgeStructureTemplates`; `AsNoTracking()`
- [ ] T080 [P] [US2] `Mentoory.Knowledge.Application/Queries/GetProjectKnowledgeStructure/` — full-tree DTO; Mapperly projection; source-stamp indicators on each node

### Handler tests

- [ ] T081 [P] [US2] `tests/Mentoory.Knowledge.Tests/Handlers/CloneKnowledgeStructureTemplateHandlerTests.cs` — clone from a built template using InMemory provider; verify deep-copy, stamps, default SyncMode
- [ ] T082 [P] [US2] `tests/Mentoory.Knowledge.Tests/Handlers/DeleteTopicHandlerTests.cs` — two scenarios: topic with zero referencing questions (succeeds); topic with N referencing questions (fails with correct Spanish message). Mock `ITopicUsageQuery`
- [ ] T083 [P] [US2] `tests/Mentoory.Knowledge.Tests/Handlers/ProjectStructureCrudHandlerTests.cs` — consolidated: add/update/delete/reorder for Module/Topic/Subject/Resource on clone

### Web controller actions + views + JS (extend US1 controller)

- [ ] T084 [US2] Extend `Mentoory.Web/Areas/Coordination/Controllers/KnowledgeController.cs` with coordinator-scoped actions: `Projects()`, `ProjectStructureDetail(Guid externalId)`, `CloneFromTemplate(Guid templateExternalId)`, plus hierarchical CRUD on clone, `UpdateTopicPriorityRanges(...)`, `SetSyncMode(...)`. Action-level `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`
- [ ] T085 [P] [US2] Create `Mentoory.Web/Areas/Coordination/Views/Knowledge/` additions: `Projects.cshtml`, `ProjectStructureDetail.cshtml`, `_ProjectStructureTree.cshtml`, reuse `_PriorityRangeEditor.cshtml` from T055; Spanish strings; Tabler styling
- [ ] T086 [P] [US2] Create `Mentoory.Web/wwwroot/js/knowledge/project-structure-editor.js` — analogous to T056 but tuned for the clone (shows source-stamp indicators in the tree; "Sincronizar" button is disabled pending US5 handler; SyncMode toggle calls T078 action)

**Checkpoint**: US2 functional. Coordinator can clone, edit, and safely delete (with EC-30 blocking). Run quickstart.md US2. SC-K01 + SC-K03 testable here.

---

## Phase 5: User Story 3 — Diagnostic form clone cascades (Priority: P2) ✅ COMPLETE

**Status**: T087–T094 complete (T092 stubbed — no form-template edit UI exists yet; `SetFormTemplateKnowledgeBindingCommand` ready for future UI wire-up). `ProjectForm.CloneFromTemplate` accepts `topicIdRewriteMap`; `CloneFormTemplateHandler` auto-cascades bound KS templates, reuses existing project structures, rewrites `Questions.TopicId`, throws + rolls back on unresolved topics (FR-K23). Integration round-trip test in `tests/Mentoory.Tests.Integration/Knowledge/`.



**Goal**: Close the dangling FK. Cloning a bound `FormTemplate` auto-creates/reuses the project's knowledge structure and rewrites `Question.TopicId` values. FR-K23 rollback on unresolvable topic.

**Independent Test**: quickstart.md US3 (steps 1–7) — clone form bound to template; verify Questions.TopicId resolves to project-owned knowledge.Topics rows.

- [ ] T087 [P] [US3] Modify `Mentoory.Diagnostic.Domain/Aggregates/FormTemplate/FormTemplate.cs`: add `Guid? DefaultKnowledgeStructureTemplateExternalId { get; private set; }`; add `SetDefaultKnowledgeStructureTemplate(Guid? externalId)` mutation that bumps `Version++`
- [ ] T088 [P] [US3] Modify `Mentoory.Diagnostic.Infrastructure/Persistence/Configurations/FormTemplateConfiguration.cs` (or equivalent file): map the new nullable Guid column plus a shadow internal `long? DefaultKnowledgeStructureTemplateId` FK to `knowledge.KnowledgeStructureTemplates`. Hydration of the internal id from the ExternalId happens via a repository-side lookup OR a DB-side computed column (recommend: keep only the ExternalId; the FK column is populated via a backing field + EF shadow property from the DB FK). Choose the simplest path that preserves FR-K20 semantics
- [ ] T089 [US3] Modify `Mentoory.Diagnostic.Domain/Aggregates/ProjectForm/ProjectForm.cs`: extend `CloneFromTemplate` signature to accept `IReadOnlyDictionary<long, long>? topicIdRewriteMap = null`; when non-null, each cloned `Question.TopicId` is rewritten via the map; throw `InvalidOperationException` (with offending question text) when a template question's `TopicId` is not a map key. Preserve backward compatibility (default null parameter = no rewrite = existing behavior). Depends on spec's decision recorded in plan post-design re-check.
- [ ] T090 [P] [US3] Create `Mentoory.Diagnostic.Application/Commands/SetFormTemplateKnowledgeBinding/` — command + validator (verifies target KS template exists via `IKnowledgeStructureTemplateRepository.ExistsByExternalIdAsync` when non-null) + handler routing through aggregate setter. Authorization GlobalAdmin only
- [ ] T091 [US3] Modify `Mentoory.Diagnostic.Application/Commands/CloneFormTemplate/CloneFormTemplateHandler.cs`: inject `IKnowledgeStructureTemplateRepository`, `IKnowledgeStructureRepository`, `IDbContextTransactionFactory` (or equivalent for obtaining a shared transaction spanning both DbContexts); implement the FR-K21 flow per contracts/diagnostic-cascade.md — load bound template (if any), reuse or clone project knowledge, build `Dictionary<long, long> topicIdRewriteMap`, pass to `ProjectForm.CloneFromTemplate`, save both contexts within one transaction, commit. Handle FR-K23 throw path: return `Failure` with offending question texts in Spanish message; transaction rolls back automatically. Depends on T089, T090
- [ ] T092 [P] [US3] Modify `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Edit.cshtml` (or whichever form-template-edit view exists — check existing Diagnostic UI): add a "Plantilla de conocimiento asociada" dropdown backed by `ListKnowledgeStructureTemplatesQuery(IncludeArchived: false)` (from T048); bind the selected ExternalId to `SetFormTemplateKnowledgeBindingCommand` on form submit
- [ ] T093 [P] [US3] `tests/Mentoory.Diagnostic.Tests/Handlers/CloneFormTemplateHandlerCascadeTests.cs` — handler tests covering: cascade creates new knowledge structure when none exists; reuses existing when present; rewrites TopicIds correctly; throws + rolls back when template question references non-bound topic (FR-K23)
- [ ] T094 [P] [US3] `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs` — end-to-end integration test (NFR-K09's ≥1 required integration test): seed a template, seed a FormTemplate bound to it, execute `CloneFormTemplateCommand`, assert in DB that every `Questions.TopicId` in the new form resolves to a `knowledge.Topics` row whose chain eventually reaches `KnowledgeStructure.ProjectId == expectedProjectId`. Use Respawn for cleanup between runs

**Checkpoint**: US3 functional. SC-K04 passes. Run quickstart.md US3. The "clone form → questions rewrite → aggregation query works" round-trip now resolves cleanly.

---

## Phase 6: User Story 4 — Priority range edits emit event (Priority: P2) ✅ COMPLETE

**Status**: T095–T098 complete. `TopicPriorityRangesChanged` INotification published by clone-side `UpdateTopicPriorityRangesHandler` after successful save. Template-side handler enforced not to depend on `IMediator` (reflection assertion test).



**Goal**: `TopicPriorityRangesChanged` published on every project-topic range edit. No consumer in v1. Template-side edits stay silent.

**Independent Test**: quickstart.md US4 — subscribe in test, edit clone topic ranges, verify one event; edit template topic ranges, verify zero events.

- [ ] T095 [P] [US4] Create `Mentoory.Knowledge.Application/IntegrationEvents/TopicPriorityRangesChanged.cs` — record per contracts/events.md, `INotification`, with `PriorityRangeDto(decimal Min, decimal Max)` sibling type
- [ ] T096 [US4] Extend the `UpdateTopicPriorityRanges` handler from T075 to publish `TopicPriorityRangesChanged` via `IMediator.Publish` AFTER `SaveChangesAsync` succeeds. If no domain-event dispatcher exists in Knowledge yet, the simplest wiring is: after save, construct the event from the mutated topic state and publish. (The data-model.md mentions a `DomainEventDispatcher` pattern; if the Diagnostic module already has one, mirror it. Otherwise the simple post-save publish is acceptable for a single-event module.)
- [ ] T097 [P] [US4] `tests/Mentoory.Knowledge.Tests/Handlers/UpdateTopicPriorityRangesEventTests.cs` — handler test with a captured `INotificationHandler<TopicPriorityRangesChanged>` subscriber; asserts ONE event per successful save with correct payload (TopicExternalId, ProjectId, all three ranges as they appear post-save). Also assert that editing TEMPLATE topic ranges (handler from T045) publishes ZERO events
- [ ] T098 [P] [US4] Add an assertion pass to the existing `UpdateTopicTemplatePriorityRangesHandlerTests.cs` (T052) to explicitly verify NO `TopicPriorityRangesChanged` is published from the template-side handler (mock `IMediator`, assert `Publish` never called)

**Checkpoint**: US4 functional. SC-K06 passes.

---

## Phase 7: User Story 5 — PartialSync pulls new template items (Priority: P3) ✅ COMPLETE

**Status**: T099–T104 complete (T103 in-memory stub variant; full DB integration test documented as follow-up). `ApplyPartialSync` domain method with recursive delta-append, `PartialSyncResult` counters, `SyncFromTemplateCommand` + handler, UI "Sincronizar desde plantilla" button wired with toast summary.



**Goal**: Append template-added items into a PartialSync-mode clone without touching local edits. Apply-and-summarize UX.

**Independent Test**: quickstart.md US5 — add template items after clone, trigger sync, verify append + local-edit preservation.

- [ ] T099 [P] [US5] Add `ApplyPartialSync(KnowledgeStructureTemplate template)` method on `KnowledgeStructure` (from T061): depth-first traversal by `SortOrder`; at each level, append template items whose `ExternalId` is not present in the clone as `SourceTemplateXExternalId`; never modify existing items; bump `SourceTemplateVersion = template.Version`; return `PartialSyncResult { int ModulesAdded, TopicsAdded, SubjectsAdded, ResourcesAdded }`. Throws if `SyncMode != PartialSync` or `template.Id != SourceTemplateId`
- [ ] T100 [P] [US5] `Mentoory.Knowledge.Application/Commands/SyncFromTemplate/` — command + handler per contracts/project-clones.md; loads clone + template full trees, calls `ApplyPartialSync`, maps result to `PartialSyncResultDto`, saves, returns DTO. Single transaction (default EF `SaveChangesAsync`)
- [ ] T101 [P] [US5] `tests/Mentoory.Knowledge.Tests/Domain/PartialSyncTests.cs` — cover: template adds a new topic → appended at max+1; template adds a new subject under an existing topic → appended; pre-existing cloned topic renamed locally → unchanged by sync; locally-added topic (no source stamp) → untouched; sync in Disconnected mode → throws; new item order is by template's SortOrder (ascending)
- [ ] T102 [P] [US5] `tests/Mentoory.Knowledge.Tests/Handlers/SyncFromTemplateHandlerTests.cs` — covers the command/handler level, mocks both repositories, asserts result DTO matches the mutated aggregate's summary counts
- [ ] T103 [P] [US5] `tests/Mentoory.Tests.Integration/Knowledge/KnowledgeStructureRoundTripTests.cs` — integration test: clone template, modify clone locally, add items to template, sync, assert end state matches expected union. Verifies SC-K05 end-to-end
- [ ] T104 [US5] Wire up the "Sincronizar desde plantilla" button in `ProjectStructureDetail.cshtml` (T085) to POST to the controller action; on success display the summary toast using `showToast` with count breakdown per FR-K43; disable the button when `SyncMode = Disconnected`

**Checkpoint**: US5 functional. SC-K05 passes. All user stories complete.

---

## Phase 8: Polish & Cross-Cutting Concerns ✅ COMPLETE

**Final status**: Full solution builds clean (`dotnet build` on Web: 0 warnings with `NuGetAudit=false` bypassing pre-existing MailKit CVE). SSDT DACPAC builds clean. 100 Knowledge unit/handler tests pass + 2 intentional skips (integration round-trip). 65 Diagnostic tests pass (old + new cascade tests). Spanish UI audit clean in all Knowledge views.



- [ ] T105 [P] Run `/simplify` across all new files per CLAUDE.md Code Review Standards — flag methods >30 lines, duplicated logic >2x, missing `AsNoTracking()` on read paths, EF Include chains loading more than needed. Apply agreed refactors
- [ ] T106 [P] Spanish-text audit: grep all new `.cshtml` and ViewModels for hardcoded English strings; ensure all user-facing text is in Spanish (constitution IX)
- [ ] T107 [P] Zero-warnings verification: `dotnet build` the solution; ensure zero warnings for all new assemblies (constitution V)
- [ ] T108 Execute `quickstart.md` end-to-end against a fresh Aspire dev environment; record any deviations; file follow-up tasks if any quickstart step fails
- [ ] T109 Update `brainstorm/07-knowledge-module.md` "Open Threads" section: remove entries now resolved by implementation (ResolvePriority semantics, partial sync semantics). Keep the three entries that remain deferred (outbox upgrade, score normalization if revisited, concurrent-edit semantics)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: no deps
- **Phase 2 Foundational**: depends on Phase 1. BLOCKS all user stories because DB tables, enums, PriorityRange, DbContext, and repos are prerequisites. T016 (FK on `diagnostic.Questions.TopicId`) also depends on T017+T018 (seed reconciliation) before SSDT publishes
- **Phase 3 US1 (P1)**: depends on Foundational. The MVP slice
- **Phase 4 US2 (P1)**: depends on Foundational. T067 adds a cross-module interface consumed by T074; T066 adds a method consumed by T042. These are light back-links into US1 via the shared interfaces — they don't block US1 from shipping first
- **Phase 5 US3 (P2)**: depends on US1 + US2 (needs Knowledge templates and clones to cascade against). T089's ProjectForm modification is independently testable
- **Phase 6 US4 (P2)**: depends on US2 (T075 handler is extended here). Can run in parallel with US3 since it touches a different handler
- **Phase 7 US5 (P3)**: depends on US2. Can run in parallel with US3 + US4
- **Phase 8 Polish**: after all desired user stories complete

### User Story Dependencies (summary)

```
Setup (Phase 1)
   │
   ▼
Foundational (Phase 2)
   │
   ├──► US1 (P1) — MVP
   │       │
   │       └──► US2 (P1) — depends on US1 domain types existing (but contracts are independent — US2 can start concurrently with US1 if team is staffed; integration testable only after both ship)
   │
   ├──► US3 (P2) — depends on US1 + US2 catalogs to cascade against
   ├──► US4 (P2) — depends on US2 (extends its handler) — can run parallel to US3
   └──► US5 (P3) — depends on US2 — can run parallel to US3 + US4
```

### Within Each User Story

- Tests and implementation can be developed in parallel, but tests MUST pass before the story is declared complete (per NFR-K09)
- Aggregate domain types before EF configurations
- EF configurations before repositories
- Repositories before handlers
- Handlers before controller actions
- Controller actions before views + JS
- Views before the end-to-end quickstart validation for that story

### Parallel Opportunities

- Setup: T002, T003, T004 all [P]
- Foundational DB: T005–T014 all [P] (independent files); T019–T023 [P]; T026 [P]
- US1: all descendant entity files [P] (T028–T031); all EF configs [P] (T035); most commands [P] (T039–T047 excl. T042 which depends on T066); most queries [P]; view + JS files [P]
- US2: entity files [P] (T057–T060); configs [P]; commands mostly [P]
- US3: T087, T088, T090 [P]; T093, T094 [P]
- US4: T095, T097, T098 [P]
- US5: T099, T101, T102, T103 [P]

---

## Parallel Example: User Story 1

```bash
# Launch all domain entity tasks for US1 together:
Task: T028 — Create ResourceTemplate.cs
Task: T029 — Create SubjectTemplate.cs
Task: T030 — Create TopicTemplate.cs
Task: T031 — Create ModuleTemplate.cs
# (T032 aggregate root waits on all four above)

# Once aggregate is in place, launch all commands concurrently:
Task: T039 — CreateKnowledgeStructureTemplate slice
Task: T040 — UpdateKnowledgeStructureTemplate slice
Task: T041 — Archive/Unarchive slices
Task: T043 — Module CRUD slices
Task: T044 — Topic CRUD slices
Task: T045 — UpdateTopicTemplatePriorityRanges slice
Task: T046 — Subject CRUD slices
Task: T047 — Resource CRUD slices

# And the query handlers:
Task: T048 — ListKnowledgeStructureTemplates
Task: T049 — GetKnowledgeStructureTemplate

# After domain + handlers land, launch test clusters in parallel:
Task: T050 — consolidated template handler tests
Task: T051 — consolidated module/topic/subject/resource CRUD handler tests
Task: T052 — priority-range handler tests

# UI work is independent of tests:
Task: T054 — ViewModels
Task: T055 — Views
Task: T056 — JS
```

---

## Implementation Strategy

### MVP First (US1 + US2 — both P1)

1. Complete Phase 1 Setup (T001–T004)
2. Complete Phase 2 Foundational (T005–T027) — **the entire SSDT PR ships in one atomic publish per NFR-K08**
3. Complete Phase 3 US1 (T028–T056)
4. Complete Phase 4 US2 (T057–T086) — MVP is US1+US2 together because coordinator workflow is unusable without both
5. **Stop and validate**: run quickstart.md US1 + US2 sections; verify SC-K01, SC-K02, SC-K03

### Incremental Delivery

1. MVP (above) → deploy/demo: GlobalAdmin has a catalog; Coordinator can clone and customize. Mentoring Plan team is unblocked to start its spec against the new Topic shape
2. Add US3 (T087–T094) → cascade unblocks form cloning with consistent `Question.TopicId`. Adds SC-K04. Deploy
3. Add US4 (T095–T098) → event emit. Adds SC-K06. Deploy
4. Add US5 (T099–T104) → PartialSync nice-to-have. Deploy

### Parallel Team Strategy

With multiple developers after Foundational is done:

- Dev A: US1 (templates)
- Dev B: US2 (clones) — coordinates with A on the shared aggregate patterns
- Dev C: US3 (Diagnostic cascade) — starts after A+B finish their domain aggregates; can prototype `ProjectForm.CloneFromTemplate` change (T089) earlier as it's isolated
- Dev D (optional): US4 + US5 in sequence after US2 domain is stable

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in the same phase
- [Story] label maps task to specific user story for traceability (US1..US5)
- NFR-K09 REQUIRES the test tasks; do not treat them as optional
- Commit after each task or logical group; prefer per-task commits during US1–US2 (large surface) and per-story commits during US3–US5 (smaller surface)
- SSDT changes land in ONE publish; do not split the Foundational-phase DB work across PRs
- Spanish UI text everywhere; code + docs in English
- Constitution X enforced throughout: every `[Authorize]` names the target role AND all higher roles; every menu group includes `GlobalAdmin`
- When in doubt about a cross-module boundary, follow the Diagnostic module's existing precedent — this spec is deliberately shaped to mirror it

---

## Phase 9: Amendment refactor — Project-owned KS binding (2026-04-19)

**Source**: [`AMENDMENT-PROJECT-KS-BINDING.md`](./AMENDMENT-PROJECT-KS-BINDING.md) · brainstorm [`11-knowledge-module-binding-redesign`](../../brainstorm/11-knowledge-module-binding-redesign.md)

**Goal**: move the KS binding from `FormTemplate` to `Project`, enforce 1 KS per project, materialize KS at project creation, simplify the form-clone handler to compatibility-check + rewrite only.

### Database

- [ ] T110 Alter `Mentoory.Db/tenant/Tables/Projects.sql`: add `[KnowledgeStructureTemplateExternalId] UNIQUEIDENTIFIER NOT NULL` (FK → `knowledge.KnowledgeStructureTemplates(ExternalId)`) and `[KnowledgeStructureExternalId] UNIQUEIDENTIFIER NOT NULL` (FK → `knowledge.KnowledgeStructures(ExternalId)`). Index both FKs.
- [ ] T111 Alter `Mentoory.Db/knowledge/Tables/KnowledgeStructures.sql`: add `CONSTRAINT [UQ_KnowledgeStructures_ProjectId] UNIQUE ([ProjectId])`; drop `IX_KnowledgeStructures_ProjectId_SourceTemplateId`. Change `SourceTemplateId` from `BIGINT NULL` to `BIGINT NOT NULL` (domain factory already rejects the null path).

### Domain

- [ ] T112 Modify `Mentoory.Tenant.Domain.Aggregates.Project.Project`: add `KnowledgeStructureTemplateExternalId` (Guid, required) and `KnowledgeStructureExternalId` (Guid, required) properties; amend `Create(...)` factory to accept both; add `internal void SetKnowledgeStructure(Guid templateExternalId, Guid structureExternalId)` that throws if either value is already set (immutability guard).

### Application / cross-module

- [ ] T113 Add `Mentoory.Tenant.Application.Abstractions.IKnowledgeStructureProvisioner` with `Task<Guid> CloneForProjectAsync(Guid templateExternalId, long projectId, long incubatorId, CancellationToken)`. Mirrors `ITopicUsageQuery` direction (Tenant.Application owns the interface; Knowledge.Infrastructure implements).
- [ ] T114 Implement `Mentoory.Knowledge.Infrastructure.CrossModule.KnowledgeStructureProvisioner : IKnowledgeStructureProvisioner` — loads KS template with full tree, calls `KnowledgeStructure.CloneFromTemplate(...)`, adds to repo, returns the new ExternalId. Register in `AddKnowledgeInfrastructure` DI.
- [ ] T115 Update `Mentoory.Knowledge.Domain.Repositories.IKnowledgeStructureRepository`: replace `GetByProjectAndSourceTemplateIdAsync` with `GetByProjectIdAsync(long projectId, CancellationToken)`. Keep `CountClonesBySourceTemplateIdAsync` (delete-template guard still needs it).
- [ ] T116 Update `Mentoory.Knowledge.Infrastructure.Persistence.Repositories.KnowledgeStructureRepository` to match the new interface.
- [ ] T117 Modify `Mentoory.Tenant.Application.Commands.CreateProject.CreateProjectCommand` + handler: add `KnowledgeStructureTemplateExternalId` required parameter; validate template exists + not archived via `IKnowledgeStructureTemplateRepository.GetByExternalIdAsync` (new cross-module dep); call `IKnowledgeStructureProvisioner.CloneForProjectAsync`; stamp `project.SetKnowledgeStructure(...)`; save Tenant + Knowledge contexts in one transaction (pattern per research R3).
- [ ] T118 Simplify `Mentoory.Diagnostic.Application.Commands.CloneFormTemplate.CloneFormTemplateHandler`: load project's KS via `IKnowledgeStructureRepository.GetByProjectIdAsync`; compatibility check (`template.DefaultKS` vs `project.KnowledgeStructureTemplateExternalId`); build rewrite map from the project's existing KS; rewrite on `ProjectForm.CloneFromTemplate`; save Diagnostic only. Remove the KS-creation/reuse branches + the dual-save pattern. Update the validator to carry the Spanish compatibility error.
- [ ] T119 Remove obsolete Knowledge handlers: `CloneKnowledgeStructureTemplateHandler` (still usable from Tenant.Application; keep the command but make its controller action internal-only / removed from the web surface).

### Web

- [ ] T120 Modify project-creation view (locate in `Mentoory.Web/Areas/Administration/Views/Projects/Create.cshtml` or equivalent): add a **required** KS template dropdown bound via `ListKnowledgeStructureTemplatesQuery(IncludeArchived: false)`. Corresponding `CreateProjectViewModel` gets the Guid property. Controller POST forwards the Guid into the updated command.
- [ ] T121 Update `Mentoory.Web.Infrastructure.Menu.MenuConfiguration.cs`: project-creation menu role list becomes `[ProjectCoordinator, IncubatorAdmin, GlobalAdmin]` if it isn't already.
- [ ] T122 Remove `Mentoory.Web/Areas/Coordination/Views/Knowledge/CloneFromTemplate.cshtml` and its controller actions (`CloneFromTemplateGet` + `CloneFromTemplate` POST) + the "Clonar desde plantilla" button in `Projects.cshtml`. Update the empty-state copy: `"Esta plantilla no tiene estructura de conocimiento — contacta a un administrador."` (defensive; schema should prevent it).

### Seeds (clean-state rebuild)

- [ ] T123 Rewrite `Mentoory.Db.PostDeployment/004.SeedTestData.sql`:
  - In § 3 Projects: each project's INSERT now sets `KnowledgeStructureTemplateExternalId = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER)` and `KnowledgeStructureExternalId = CAST('99999999-9999-9999-9999-999999999901' AS UNIQUEIDENTIFIER)`.
  - In § 3.5 KS seed: keep the IDENTITY_INSERT Topics 1-5 block, but seed `SourceTemplateId = (SELECT Id FROM knowledge.KnowledgeStructureTemplates WHERE ExternalId = '11111111-...')`. This requires § 3.5 to run **after** `005` seeds the KS template — reorder via `Script.PostDeployment.sql` so `005` runs before the § 3.5 portion, OR (cleaner) move the KS-template seed INTO `004` so the Topics seed has its template id in scope.
- [ ] T124 Rewrite `Mentoory.Tests.Integration.Fixtures.IntegrationTestBase.SeedKnowledgeTopicsAsync` to seed via the new flow: KS template (if not present) → call `CreateProjectCommand` for the seed project OR directly insert Project + KS rows with the new columns populated. Simpler: keep as raw SQL for speed; just add the new fields.

### Tests

- [ ] T125 Add `tests/Mentoory.Tenant.Tests/Handlers/CreateProjectHandlerTests.cs` (or extend existing): cover success with valid KS template, failure with missing template, failure with archived template, failure with duplicate project (FK cascade).
- [ ] T126 Update `tests/Mentoory.Diagnostic.Tests/Handlers/CloneFormTemplateHandlerCascadeTests.cs`: rename to `CloneFormTemplateHandlerCompatibilityTests.cs`. Scenarios: compatible form → success + rewrite; incompatible form (different `DefaultKS`) → failure with Spanish message; form without `DefaultKS` → legacy pass-through (no rewrite). Remove "cascade creates KS" assertions entirely.
- [ ] T127 Update `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs`: preseed project with KS via `CreateProjectCommand` invocation (not direct SQL). Assert the form clone rewrites to pre-existing project topics. Retire the "reuse existing KS" test or convert it to a unique-constraint regression test.
- [ ] T128 Update all Knowledge.Tests handlers that referenced `GetByProjectAndSourceTemplateIdAsync` to use `GetByProjectIdAsync` instead.
- [ ] T129 [P] Remove handler tests that assumed the coordinator-side Clone-from-Template command/flow (`CloneKnowledgeStructureTemplateHandler*` tests). Keep/move the domain-level clone tests on `KnowledgeStructureCloneTests` — the factory `CloneFromTemplate` is still exercised; just not from a coordinator command.

### Verification

- [ ] T130 Full solution build + `dotnet test` across Knowledge, Diagnostic, Tenant unit suites + Integration suite + E2E suite — all green (mirrors the end-of-US5 checklist).
- [ ] T131 Manual smoke: create a new project via Administration UI with a KS template; observe the KS auto-materializes; clone a compatible form; verify `Questions.TopicId` resolves; clone an incompatible form; verify the Spanish error appears.
- [ ] T132 Update `brainstorm/07-knowledge-module.md` and `07-knowledge-module-feedback.md` Open Threads: mark the "form-to-KS binding cardinality" issue as resolved (refs AMENDMENT-PROJECT-KS-BINDING.md).
