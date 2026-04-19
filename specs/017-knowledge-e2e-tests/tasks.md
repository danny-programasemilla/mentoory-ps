---

description: "Task list for implementing spec 017 — Knowledge Module E2E Test Coverage"
---

# Tasks: Knowledge Module E2E Test Coverage

**Input**: Design documents from `/specs/017-knowledge-e2e-tests/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: This feature IS the test suite. There is no separate "tests for the tests" layer; test-file authorship tasks are themselves the implementation.

**Organization**: Tasks are grouped by the user story they cover in `spec.md` so each file can be landed and reviewed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1–US6 from spec.md)
- File paths in every task are absolute from repo root.

## Path Conventions

- **Production code** (touched minimally): `Mentoory.Db.PostDeployment/`, optional `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` patch.
- **Test code**: `tests/Mentoory.Tests.E2E/Tests/`, `tests/Mentoory.Tests.E2E/Infrastructure/`, `tests/Mentoory.Tests.Integration/Knowledge/`.
- Paths mirror the Structure Decision in `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: One-time changes that unblock every user-story phase.

- [X] T001 Verify the 016 branch builds and the current E2E suite passes locally by running `dotnet build` and then `dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj --filter "FullyQualifiedName~KnowledgeTemplatesTests.Templates_PageLoads"`. If either fails, stop and fix the baseline before proceeding.
- [X] T002 [P] Read `specs/016-knowledge-module-core/contracts/diagnostic-cascade.md` and `Mentoory.Db/diagnostic/Tables/Questions.sql` to resolve the open item flagged in `specs/017-knowledge-e2e-tests/contracts/seed-additions.md` § Addition 2 — specifically, whether `diagnostic.Questions.TopicId` FK is conditional on template-vs-project, uses `TopicTemplates.Id`, or uses `Topics.Id`. Document the answer as a one-paragraph note at the top of `contracts/seed-additions.md`; this drives the exact TopicId values used in T004.
- [X] T003 [P] Patch `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` to honor `E2E_HEADED=1` env var per research R7: `Headless = Environment.GetEnvironmentVariable("E2E_HEADED") != "1"` and `SlowMo = Environment.GetEnvironmentVariable("E2E_HEADED") == "1" ? 250 : 0`. Keep CI default (unset = headless).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Seed data + shared helpers that every user-story phase consumes.

**⚠️ CRITICAL**: No user-story task may begin until Phase 2 is complete.

- [X] T004 Extend `Mentoory.Db.PostDeployment/005.SeedKnowledgeData.sql` with one new `diagnostic.FormTemplates` row named `"Diagnóstico Básico de Emprendimiento"` (ExternalId `33333333-3333-3333-3333-333333333333`), bound to the seeded `Emprendimiento Básico` KS template, plus 2–3 `diagnostic.QuestionTemplates` rows (NOT `Questions` — template-side; see T002 resolution) whose `TopicId`s resolve to `knowledge.TopicTemplates.Id` (the seeded `Propuesta de valor` TopicTemplate). Wrap every insert in `IF NOT EXISTS` guards following the existing idempotent pattern. Verify by running `dotnet build Mentoory.Db/MentooryDb.sqlproj`; it must publish without errors.
- [X] T005 Extend `Mentoory.Db.PostDeployment/004.SeedTestData.sql` with: (a) a third `tenant.Incubators` row named `"Incubadora Norte"` (ExternalId `22222222-2222-2222-2222-222222222222`), (b) an `access.Users` row for `coordnorte@test.mentoory.com` hashed with the same password helper as the existing seed (`Test123!@#`) — NOT `coord2` as originally drafted; see `contracts/seed-additions.md` § Addition 1 implementation note for rationale, (c) an `access.RoleAssignments` row granting ProjectCoordinator on `Incubadora Norte`, (d) a `tenant.Projects` row `"Proyecto Norte Uno"` bound to the seeded KS template + a hand-seeded `knowledge.KnowledgeStructures` row (since `IKnowledgeStructureProvisioner` only fires at runtime, not at seed time), and (e) a project-scoped role assignment for `coordnorte` to `Proyecto Norte Uno`. All inserts idempotent. Verify via `dotnet build Mentoory.Db/MentooryDb.sqlproj`.
- [X] T006 [P] Create `tests/Mentoory.Tests.E2E/Infrastructure/KnowledgeTestHelpers.cs` implementing the full API in `contracts/e2e-helpers.md` (public static class, `ContextSelection` record with static presets, `LoginAsync`/`SelectContextAsync`/`LoginAndSelectAsync`/`ScreenshotOnFailureAsync`/`WaitForSpanishMessageAsync`). Use the existing `LoginAndSelectContextAsync` in `KnowledgeTemplatesTests.cs` as the reference implementation. Ensure the dropdown-XHR wait pattern (`WaitForFunctionAsync("sel => sel.options.length > 1", ...)`) is internal and not callable-around.
- [X] T007 Create `tests/Mentoory.Tests.E2E/Infrastructure/KnowledgeIntegrationHelpers.cs` exposing the API in `contracts/test-files.md` § Shared Integration Helpers: `CreateFormTemplateAsync`, `AddTopicToTemplateAsync`, `CountProjectFormsAsync`, `CountProjectKnowledgeStructuresAsync`, `CloneFormIntoProjectAsync`, plus `GetSeededKsTemplateExternalIdAsync` and `GetSeededBoundFormTemplateExternalIdAsync`. Every method takes a `WebApplicationFactory<Program>` (which `PlaywrightFixture` is) and uses `Services.CreateScope()` + `IMediator.Send` / `DbContext` queries. No raw SQL.
- [X] T008 [P] Migrate existing test files to consume `KnowledgeTestHelpers.LoginAndSelectAsync` and delete their private `LoginAndSelectContextAsync`/`LoginAsCoordinatorAsync` methods: `tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs`, `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectStructureTests.cs`, `tests/Mentoory.Tests.E2E/Tests/ProjectCreationTests.cs`, `tests/Mentoory.Tests.E2E/Tests/AvailableProjectsTests.cs`. Ensure each file still passes `dotnet test` in isolation after migration; commit one file per commit for reviewability.

**Checkpoint**: Seed data extended, helpers in place. User-story work can now begin in parallel.

---

## Phase 3: User Story 1 - Global-admin template curation is regression-proof (Priority: P1) 🎯 MVP

**Goal**: US1's 9 acceptance scenarios fail loudly on any regression to template CRUD, priority-range editing, reorder, archive, or hard-delete block.

**Independent Test**: Running `dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj --filter "FullyQualifiedName~KnowledgeTemplatesTests"` exits green on a fresh DACPAC deploy, with all 11 test methods (existing 5 + new 6) passing in under 90s wall-time.

### Implementation for User Story 1

All tasks edit the same file (`tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs`); no [P] markers.

> **Phase 3 status (CP3 follow-up, 2026-04-19)**: 11/11 US1 tests green. The 6 previously-skipped tests (T012–T017) were un-skipped after the CP3 follow-up pass isolated the shared root cause: `WaitForLoadStateAsync(NetworkIdle)` returns immediately when the page is already idle, so the post-fetch `window.location.reload()` chain was racing ahead of the following Playwright action (cancelling in-flight POSTs). Fix: a `ClickAndWaitForReloadAsync` helper that stamps the `<html>` element before the click and waits for the stamp to vanish post-reload. T015 additionally had a JSON property-name mismatch (`orderedIds` vs `externalIds`); T017 had a Spanish substring mismatch (assertion now matches the verbatim `Archívela en su lugar`).

- [X] T009 [US1] Extend existing `Templates_PageLoads_ShowsSeededTemplate` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs` with the archived/active-state assertion and "Nueva plantilla" action-reachable assertion to cover the full US1-1 scenario (replaces current partial coverage). Method stays named the same.
- [X] T010 [US1] Add `Templates_ListSurface_ShowsArchivedStateAndNewButton` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs` covering the remainder of US1-1 (visible archived indicator + "Mostrar archivadas" toggle presence).
- [X] T011 [US1] Extend existing `TemplateDetail_PageLoads_ShowsTree` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs` with assertions that all four hierarchy levels (Module, Topic, Subject, Resource) render in the tree with their Spanish names. Covers US1-2.
- [X] T012 [US1] Add `TemplateDetail_AddNodes_PersistsAfterReload` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs`. Covers US1-3. Root-cause fix: `ClickAndWaitForReloadAsync` helper stamp-and-wait pattern.
- [X] T013 [US1] `TopicPriorityRanges_SaveAndReload_PersistsThreeBands`. Covers US1-4.
- [X] T014 [US1] `TopicPriorityRanges_OverlappingBands_ShowsValidationError`. Covers US1-5. First save uses `waitForReload: true`; overlap rejection is client-side (no reload).
- [X] T015 [US1] `TemplateModules_Reorder_PersistsAfterReload`. Covers US1-6. Fixed JSON property name (`externalIds`) and exercised via `page.EvaluateAsync` since no drag-drop UI exists today.
- [X] T016 [US1] `Template_ArchiveAndUnarchive_ReflectsInListToggles`. Covers US1-7. Uses the shared `ClickAndWaitForReloadAsync` helper; `?includeArchived=true` navigation side-steps the checkbox/form-submit race.
- [X] T017 [US1] `Template_HardDelete_BlockedWhenProjectCloneExists`. Covers US1-8. Assertion uses the verbatim Spanish `Archívela en su lugar`.
- [X] T018 [US1] Replace existing `Templates_CoordinatorCannotAccess` assertion shape in `tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs` with a comment pointing to `KnowledgeAuthorizationTests.ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes` (T045) that covers this plus the `/Create`, `/{id}/Edit`, `/{id}/Modules/**` routes. Keep the simpler existing test as a breadcrumb smoke.

**Checkpoint**: US1 fully green. Commit + push. Merge-ready increment.

---

## Phase 4: User Story 2 - Project creation with KS binding is regression-proof (Priority: P1)

**Goal**: Phase 9 invariants (exactly one KS per project, required KS-template dropdown, legacy clone-UI retired) fail loudly on regression.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~KnowledgeProjectStructureTests"` plus the relevant assertions still green in `ProjectCreationTests`. Wall-time under 60s.

### Implementation for User Story 2

- [X] T019 [US2] Keep existing `Projects_PageLoads_ShowsMaterializedKs` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectStructureTests.cs` (already migrated to shared helper in T008). Covers US2-1. Scenario-to-method mapping added to the class doc comment.
- [X] T020 [US2] Keep existing `Projects_NoCloneFromTemplateButton_Phase9Regression` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectStructureTests.cs`. Covers US2-5. Green after T008 migration.
- [X] T021 [US2] Keep existing `CloneFromTemplate_LegacyRoute_NoLongerAccessible` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectStructureTests.cs`. Covers US2-6. Green after T008 migration.
- [X] T022 [US2] Extended `ProjectStructureDetail_PageLoads_ShowsTree` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectStructureTests.cs` with (a) `"Módulos / Temas / Asignaturas / Recursos"` header assertion (covers all four tree levels), (b) `"Desconectada"` SyncMode-badge assertion, (c) seeded `Diagnóstico` module and `Modelo de Negocio` topic name assertions. Also replaced the silent early-return (when the "Ver detalles" link is absent) with a hard assertion.
- [X] T023 [US2] Added `ProjectStructureDetail_TreeContainsClonedNames` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectStructureTests.cs` asserting seeded `Finanzas` and `Mercado` topics are present. Regression guard for the US4-2 rename-doesn't-leak baseline.
- [X] T024 [US2] Existing `ProjectCreationTests.CreateProject_PageLoads_WithNewFields`, `CreateProject_ValidSubmission_RedirectsToProjectsList`, `CreateProject_MissingKsTemplate_ShowsValidationError`, `CreateProject_IsPublicCheckbox_DefaultsToUnchecked` (plus `CreateProject_EmptyName_ShowsValidationError`) all green after T008 migration. Scenario-to-method mapping added to the class doc comment.

**Checkpoint**: US2 fully green. Phase 9 invariants protected.

---

## Phase 5: User Story 3 - Cross-module form-clone cascade is regression-proof (Priority: P1)

**Goal**: `Question.TopicId` rewriting, mismatch rejection, null-binding no-op, and double-clone idempotency all covered.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~KnowledgeFormCloneCascadeTests"` plus the two new integration-backstop tests green. Combined wall-time under 75s (E2E portion under 45s; integration portion under 30s since they don't drive Playwright).

### Implementation for User Story 3

> **Phase 5 status (2026-04-19)**: 4/4 new tests green (2 E2E + 2 integration). Two glue changes were required to make the tasks executable:
> 1. **Production fix** in `DiagnosticsController.Clone` POST — it was swallowing the handler's specific error messages and surfacing only `"Error al clonar el formulario diagnóstico."`. Research R6 had claimed the controller already mapped errors via the constitution's `MapErrorsToModelStateAndSetErrorToast<T>` pattern, but the actual code hardcoded the generic string. Mirrored the `KnowledgeController.FirstErrorMessage` pattern (2-line diff) so T027 can assert the verbatim Spanish substring.
> 2. **Test fixture helper** `CreateProjectWithCoordinatorAsync` in `KnowledgeIntegrationHelpers.cs` — T026's happy path needs a project whose KS was materialized by the Phase-9 provisioner (with `SourceTemplateTopicExternalId` populated). The seeded `Proyecto Innovación` KS was hand-coded with those values `NULL`, so cloning the seeded FormTemplate against it throws mid-rewrite. The helper goes through `CreateProjectCommand` → Provisioner → `AssignRoleCommand`, granting the project to `coord2` (not `coord1`) to avoid turning coord1 into a multi-project user — several legacy tests still rely on coord1 auto-routing past `/Context/Select`.

- [X] T025 [P] [US3] Create `tests/Mentoory.Tests.E2E/Tests/KnowledgeFormCloneCascadeTests.cs` with the xUnit + `[Collection(E2ETestCollection.Name)]` scaffold and a constructor that takes `PlaywrightFixture`.
- [X] T026 [US3] Add `CloneCompatibleForm_HappyPath_CreatesProjectForm`. Covers US3-1 at the UI layer. Uses `coord2` (not `coord1`) against a freshly-provisioned project; see Phase 5 status note above.
- [X] T027 [US3] Add `CloneMismatchedForm_ShowsPhase9MismatchError`. Covers US3-2. Submit-button locator filters on `"Clonar Plantilla"` text to avoid matching the navbar dropdown submit. Production fix in `DiagnosticsController` surfaces the verbatim Spanish error through `ModelState`.
- [X] T028 [P] [US3] Add `CloneFormTemplate_NullBinding_NoKnowledgeCascade` to `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs`. Covers US3-3. Asserts the clone succeeds, the project's KS rowcount stays at 1, exactly one new `ProjectForm` row is created, and its Question.TopicId matches the source (no rewrite).
- [X] T029 [P] [US3] Add `KnowledgeIntegrationHelpers_UtilityContract_RegressionGuard` to `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs`. Covers helper drift via command-layer equivalents (`CreateKnowledgeStructureTemplateCommand` + `AddModuleTemplateCommand` + `AddTopicTemplateCommand` round-tripped through a fresh `KnowledgeDbContext` scope). Completes in under 1 second.

**Checkpoint**: US3 fully green. The `Question.TopicId → knowledge.Topics.Id` FK is protected at the UI and DB layers.

---

## Phase 6: User Story 4 - Coordinator project-tree editing is regression-proof (Priority: P2)

**Goal**: Project-side CRUD, priority ranges, rename-doesn't-leak, delete-guard, tenant isolation all covered.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~KnowledgeProjectTreeEditingTests"`. Wall-time under 2.5 minutes (7 tests × ≤ 30s each per FR-T30).

### Implementation for User Story 4

- [ ] T030 [P] [US4] Create `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs` with the xUnit + `[Collection(E2ETestCollection.Name)]` scaffold and a constructor that takes `PlaywrightFixture`.
- [ ] T031 [US4] Add `AddNodesAtEachLevel_PersistsAfterReload` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs`: log in as `coord1`, navigate to project KS detail, add unique-named Module → Topic → Subject → Resource, reload, assert all present AND flagged as clone-only (no template-source badge). Covers US4-1.
- [ ] T032 [US4] Add `RenameClonedTopic_PersistsOnCloneOnly` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs`: rename a seeded cloned topic (e.g., `Finanzas` → `Finanzas Básicas`) in one browser context (coordinator), then open `/Coordination/Knowledge/Templates/{seededKsExtId}` in a second browser context as GlobalAdmin, assert template's original name unchanged. Covers US4-2.
- [ ] T033 [US4] Add `ProjectTopicPriorityRanges_SaveAndReload_PersistsBands` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs`: set High/Medium/Low on a project topic, reload, assert bands in order High → Medium → Low. Covers US4-3.
- [ ] T034 [US4] Add `ProjectTopicPriorityRanges_OverlappingBands_ShowsValidationError` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs`: submit overlapping bands, assert Spanish error, assert previous bands preserved. Covers US4-4.
- [ ] T035 [US4] Add `DeleteProjectTopic_Referenced_Blocked` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs`: **setup**: use `KnowledgeIntegrationHelpers.CloneFormIntoProjectAsync` to create a ProjectForm whose Questions reference a project topic. **act**: attempt delete on that topic. **assert**: Spanish error naming the referencing-question count, topic still in tree. Covers US4-5.
- [ ] T036 [US4] Add `DeleteProjectTopic_Unreferenced_Succeeds` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs`: add a new clone-only topic to the project, delete it, assert gone after reload. Covers US4-6.
- [ ] T037 [US4] Add `ProjectTopic_TenantIsolation_CrossProjectReturnsNotFound` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs`: log in as `coord1`, obtain the KS ExternalId of `Proyecto Norte Uno` (via `KnowledgeIntegrationHelpers`), attempt GET `/Coordination/Knowledge/Projects/{foreignKsExtId}`, assert ≥400 OR body does NOT contain any of `Proyecto Norte Uno`'s tree names. Covers US4-7 (also overlaps US6-3 — cross-story coverage is OK).

**Checkpoint**: US4 fully green. Coordinator's day-one workflow protected.

---

## Phase 7: User Story 5 - PartialSync is regression-proof (Priority: P2)

**Goal**: SyncMode toggle, append-new-items, preserve-local-items, summary-notification.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~KnowledgePartialSyncTests"`. Wall-time under 2 minutes (5 tests × ≤ 30s).

### Implementation for User Story 5

- [ ] T038 [P] [US5] Create `tests/Mentoory.Tests.E2E/Tests/KnowledgePartialSyncTests.cs` with the xUnit + `[Collection(E2ETestCollection.Name)]` scaffold.
- [ ] T039 [US5] Add `Disconnected_SyncActionHiddenOrDisabled` to `tests/Mentoory.Tests.E2E/Tests/KnowledgePartialSyncTests.cs`: open seeded project KS (Disconnected by default), assert "Sincronizar desde plantilla" action disabled or absent; assert SyncMode toggle visible. Covers US5-1.
- [ ] T040 [US5] Add `SwitchToPartialSync_PersistsAndEnablesSyncAction` to `tests/Mentoory.Tests.E2E/Tests/KnowledgePartialSyncTests.cs`: toggle SyncMode to PartialSync, save, reload, assert sync action enabled. Covers US5-2.
- [ ] T041 [US5] Add `PartialSync_AppendsNewTemplateTopic_UnderMatchingParent` to `tests/Mentoory.Tests.E2E/Tests/KnowledgePartialSyncTests.cs`: **setup**: toggle project KS to PartialSync, call `KnowledgeIntegrationHelpers.AddTopicToTemplateAsync(seededKsExtId, moduleExtId, $"Topic_Nuevo_{Guid.NewGuid():N}")`. **act**: click "Sincronizar desde plantilla". **assert**: summary notification (Spanish) with non-zero count, reload tree, new topic present under matching clone-side parent module with `SortOrder = max+1` (assert via integration helper if the UI doesn't surface the number). Covers US5-3.
- [ ] T042 [US5] Add `PartialSync_LocalAddedTopic_Untouched` to `tests/Mentoory.Tests.E2E/Tests/KnowledgePartialSyncTests.cs`: with PartialSync enabled, add a clone-only Module M_local with unique name, trigger sync (with or without template additions), assert M_local still present (name, sort-order, children unchanged). Covers US5-4.
- [ ] T043 [US5] Add `PartialSync_LocallyRenamedTopic_RetainsLocalName` to `tests/Mentoory.Tests.E2E/Tests/KnowledgePartialSyncTests.cs`: rename a cloned-from-template topic locally, trigger sync, assert topic retains local name and position. Covers US5-5.

**Checkpoint**: US5 fully green. PartialSync semantics protected.

---

## Phase 8: User Story 6 - Authorization + tenant isolation is regression-proof (Priority: P3)

**Goal**: Every protected route denies the correct roles; tenant isolation holds across incubators; menu visibility matches role hierarchy.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~KnowledgeAuthorizationTests"`. Wall-time under 90s (most tests are theory rows with minimal browser interaction).

### Implementation for User Story 6

- [ ] T044 [P] [US6] Create `tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs` with the xUnit + `[Collection(E2ETestCollection.Name)]` scaffold, a private assertion helper `AssertDeniedAsync(IPage page, int? status, string route, string role)` replicating the pattern in `contracts/authorization-matrix.md` § Assertion Pattern, and a `TheoryData` class for protected routes.
- [ ] T045 [US6] Add `ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes` as `[Theory]` + `[InlineData]` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs` covering all rows of `contracts/authorization-matrix.md` § A (Template CRUD) for the ProjectCoordinator role. Fill `{seededKsExtId}` placeholder via `KnowledgeIntegrationHelpers.GetSeededKsTemplateExternalIdAsync` in the theory setup. Covers US6-1.
- [ ] T046 [US6] Add `ProtectedRoutes_Unauthenticated_RedirectToLogin` as `[Theory]` + `[InlineData]` in `tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs` covering ~5 representative routes from `/Coordination/Knowledge/**`. No login; assert URL ends at `/Access/Login`. Covers US6-2.
- [ ] T047 [US6] Add `TenantIsolation_CoordinatorB_CannotAccessCoordinatorAsKs` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs`: login as `coordnorte@test.mentoory.com` (Incubadora Norte), obtain `coord1`'s project KS ExternalId via integration helper, GET its detail route, assert ≥400 OR body contains no names from coord1's tree. Covers US6-3.
- [ ] T048 [US6] Add `CreateProject_CrossIncubatorForm_Denied` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs`: login as `incadmin1`, POST `/Administration/Projects/Create` with a body whose IncubatorId references `Incubadora Norte` (not incadmin1's own incubator), assert authorization rejection AND `KnowledgeIntegrationHelpers.CountProjectFormsAsync` confirms no new project row. Covers US6-4. If the form infrastructure prevents this (e.g., IncubatorId inferred from session claims not request body), document in the test the observed behavior and adapt the assertion.
- [ ] T049 [US6] Add `Menu_GlobalAdmin_ShowsBothEntries_Coordinator_ShowsOnlyProjects` to `tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs`: login as `multirole` with explicit `ContextSelection.GlobalAdmin`, assert sidebar contains `"Plantillas de conocimiento"` AND `"Estructuras del proyecto"`. Then in a second browser context login as `coord1` (FirstEnabled), assert sidebar contains only `"Estructuras del proyecto"`. Covers US6-5.

**Checkpoint**: US6 fully green. Authorization + tenant isolation surface protected.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Cleanup and cross-story verifications.

- [ ] T050 [P] Add a class-level XML doc comment at the top of each new E2E test file listing the scenario-to-method mapping from `contracts/test-files.md` so reviewers and future-devs can trace tests back to spec sections (SC-T06).
- [ ] T051 [P] Add a CI-friendly wall-time assertion: introduce a `[Trait("Category","E2E")]` attribute on every class in `tests/Mentoory.Tests.E2E/Tests/` (existing files + new ones) so CI can run them as a dedicated phase with its own timeout per FR-T31.
- [ ] T052 Run full E2E suite locally with `dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj` and confirm wall-time ≤ 6 minutes per SC-T02. If over, profile the slowest tests and either split or cache common setup via a fixture-level helper.
- [ ] T053 Manually verify SC-T04 (mutation-test smoke) by following the procedure in `quickstart.md` § Mutation-Test Smoke for at least two invariants: (a) drop UNIQUE on `knowledge.KnowledgeStructures.ProjectId`, confirm `CloneFormTemplate_TwiceForSameProject_DoesNotDuplicateProjectKs` fails; (b) remove `[Authorize(Roles="GlobalAdmin")]` on the Templates controller, confirm `ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes` fails. Revert between mutations. Document results in the PR description.
- [ ] T054 [P] Update `specs/017-knowledge-e2e-tests/checklists/requirements.md` — tick any new items and confirm all passes.
- [ ] T055 Verify `specs/017-knowledge-e2e-tests/quickstart.md` commands all work against the final state of the suite; correct any drift between plan and reality.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Independent; can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1; BLOCKS all user-story phases.
- **User Story phases (3–8)**: All depend on Phase 2. Once Phase 2 is complete, US1–US6 can proceed in parallel if staffed.
- **Polish (Phase 9)**: Depends on all user-story phases being complete.

### User Story Dependencies

- **US1 (P1)**: No dependencies on other stories.
- **US2 (P1)**: No dependencies on other stories.
- **US3 (P1)**: No dependencies on other stories; US4-5 delete-guard setup consumes `KnowledgeIntegrationHelpers.CloneFormIntoProjectAsync` but that helper comes from Phase 2 (T007), not from US3.
- **US4 (P2)**: No dependencies on other stories. T035 (delete-guard) uses a helper, not another story's output.
- **US5 (P2)**: No dependencies on other stories. Uses `KnowledgeIntegrationHelpers.AddTopicToTemplateAsync` from Phase 2.
- **US6 (P3)**: No dependencies on other stories. Uses seed data + helpers from Phase 2.

### Within Each User Story

- All tasks in a US phase edit the same file; they are sequential (no [P] markers) except the scaffold task which is [P] (different file).
- Integration-backstop tasks in US3 (T028, T029) are [P] because they live in a different file than the E2E tests.

### Parallel Opportunities

- **Phase 1**: T002, T003 are [P] (different files).
- **Phase 2**: T006, T007 can run in parallel with T004+T005 (different files). T008 is sequential after T006.
- **Phase 3–8**: Each scaffold task (T025, T030, T038, T044) is [P]. Within a phase, tasks on the same file are sequential.
- **Phase 9**: T050, T051, T054 are [P].

---

## Parallel Example: Phase 2 Kickoff

```bash
# Terminal A — SSDT seed extensions (T004 + T005 sequential because both edit the SSDT project)
(T004) edit Mentoory.Db.PostDeployment/005.SeedKnowledgeData.sql
(T005) edit Mentoory.Db.PostDeployment/004.SeedTestData.sql
dotnet build Mentoory.Db/MentooryDb.sqlproj

# Terminal B — Shared helpers (T006, T007 can parallelize because they're different files)
(T006) create tests/Mentoory.Tests.E2E/Infrastructure/KnowledgeTestHelpers.cs
(T007) create tests/Mentoory.Tests.E2E/Infrastructure/KnowledgeIntegrationHelpers.cs

# Terminal C (after T006) — Migrate existing files
(T008) edit tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs
(T008) edit tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectStructureTests.cs
(T008) edit tests/Mentoory.Tests.E2E/Tests/ProjectCreationTests.cs
(T008) edit tests/Mentoory.Tests.E2E/Tests/AvailableProjectsTests.cs
```

## Parallel Example: User Story Phases in Parallel

```bash
# After Phase 2 complete, three developers can take US1, US4, US6 concurrently
Developer A: Phase 3 (US1) — tests/Mentoory.Tests.E2E/Tests/KnowledgeTemplatesTests.cs
Developer B: Phase 6 (US4) — tests/Mentoory.Tests.E2E/Tests/KnowledgeProjectTreeEditingTests.cs
Developer C: Phase 8 (US6) — tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs

# No shared file edits → zero merge conflicts.
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Complete Phase 1 (T001–T003).
2. Complete Phase 2 (T004–T008) — unblocks every story.
3. Complete Phase 3 (T009–T018) — US1 coverage green.
4. **STOP and VALIDATE**: Run `dotnet test --filter "FullyQualifiedName~KnowledgeTemplatesTests"` and confirm all pass in ≤ 90s.
5. Merge. This is the minimum that improves the regression surface beyond today's smoke.

### Incremental Delivery

1. MVP (US1) → merge.
2. Add US2 → merge. Now Phase 9 KS binding is protected end-to-end.
3. Add US3 → merge. Cross-module cascade protected.
4. Add US4 → merge. Coordinator workflow protected.
5. Add US5 → merge. PartialSync protected.
6. Add US6 → merge. Authorization surface protected.
7. Run Phase 9 polish as a final cleanup PR.

### Parallel Team Strategy

1. All devs land Phases 1 + 2 together (seed + helpers are shared dependencies).
2. Split US1–US6 across three devs (P1 stories first):
   - Dev A: US1 then US4
   - Dev B: US2 then US5
   - Dev C: US3 then US6
3. Each story is one file (minimal merge-conflict surface — file-per-PR).
4. Phase 9 polish runs in parallel with the final user-story phase.

---

## Notes

- [P] = different files, no dependencies on incomplete tasks.
- [Story] = traces task to a `spec.md` user story.
- Every test MUST complete in ≤ 30s wall-time per FR-T30; if one balloons past that, split it.
- Every test file wraps bodies in `try/finally` with `_fixture.TakeScreenshotOnFailureAsync` + `page.Context.DisposeAsync` per EC-10 and EC-12.
- Every spec-fixed Spanish error string is asserted verbatim (substring-match OK per R6).
- Run `/simplify` (skill: `simplify`) after each phase per repo convention in `CLAUDE.md` § Code Review Standards.
- Commit one task (or logical mini-group like "scaffold + one method") per commit for reviewability.
