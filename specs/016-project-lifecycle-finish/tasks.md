---
description: "Task list for feature 016-project-lifecycle-finish"
---

# Tasks: Project Lifecycle Application + UI Completion

**Input**: Design documents from `/specs/016-project-lifecycle-finish/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Included. The Mentoory project has an established test convention (constitution § Testing Requirements — xUnit, Moq, FluentAssertions, `{Domain}.Tests`); handler and domain tests exist for every comparable feature. Test tasks mirror that convention rather than introducing a new discipline.

**Organization**: Tasks are grouped by user story so each can be implemented, tested, demoed, and merged independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (`[US1]`, `[US2]`, `[US3]`) — absent for Setup/Foundational/Polish
- Paths are relative to repository root `/mnt/D/repos/mentoory-ps-lifecycle-finish/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: The solution is pre-existing; "setup" here means confirming the feature branch + updating shared touch-points that aren't specific to any user story.

- [X] T001 Verify feature branch `016-project-lifecycle-finish` is checked out and `dotnet build` succeeds at baseline before starting (no code changes — validation step only).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core primitives that every user story depends on: the concurrency-token schema change, the stage-action policy types, and the Spanish display helpers. None of these primitives are story-specific; all three user stories need one or more of them.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [X] T002 Add `[RowVersion] ROWVERSION NOT NULL` column to `tenant.Projects` in `Mentoory.Db/tenant/Tables/Projects.sql` and run `cd Mentoory.Db && ./publish-mentoorydb.sh` to validate the DACPAC builds cleanly.
- [X] T003 Add `public byte[] RowVersion { get; private set; } = null!;` property to `Mentoory.Tenant.Domain/Aggregates/Project/Project.cs` (private setter, positioned alongside existing scalar properties).
- [X] T004 Map `Project.RowVersion` as the optimistic-concurrency token inside `ConfigureProject` in `Mentoory.Tenant.Infrastructure/Persistence/TenantDbContext.cs` using `entity.Property(p => p.RowVersion).IsRowVersion();`.
- [X] T005 [P] Create `StageGatedAction` enum in `Mentoory.Access.Application/StageActions/StageGatedAction.cs` with members `DiagnosticForms`, `AnswerCorrection`, `LearningAssignment`, `MentoringCoordination`, `FinalEvaluation`, `Closure` (per data-model.md § 2.1).
- [X] T006 [P] Create `StageGatedActionState` enum in `Mentoory.Access.Application/StageActions/StageGatedActionState.cs` with members `Available`, `Locked`, `Past` (per data-model.md § 2.2).
- [X] T007 Create `StageActionRegistry` static class in `Mentoory.Access.Application/StageActions/StageActionRegistry.cs` implementing `ActionsByStage`, `GetGatingStage(StageGatedAction)`, and `GetState(StageType currentStage, StageGatedAction action)` with the stage→actions mapping from data-model.md § 3.1.
- [X] T008 [P] Create `StageTypeDisplay` static helper in `Mentoory.Web/Infrastructure/Display/StageTypeDisplay.cs` with `ToSpanish(StageType)` returning the seven Spanish display names from research.md § R4.
- [X] T009 [P] Create `StageActionDisplay` static helper in `Mentoory.Web/Infrastructure/Display/StageActionDisplay.cs` returning the Spanish label for each `StageGatedAction` enum value (e.g., `Diagnósticos`, `Corrección de Respuestas`).
- [X] T010 [P] Create `StageActionLinks` static helper in `Mentoory.Web/Infrastructure/Display/StageActionLinks.cs` mapping each `StageGatedAction` to its Coordination-area entry URL (e.g., `DiagnosticForms` → `/Coordination/Diagnostics`, `AnswerCorrection` → `/Coordination/AnswerCorrection`).
- [X] T011 Add new `ResultErrorCodes` entries (or feature-local `LifecycleErrorCodes` constants class if the platform uses per-feature error codes) for `ProjectNotFound`, `ProjectOutOfScope`, `ProjectInactive`, `StageNotInProgress`, `ProjectAlreadyClosed`, `LifecycleConcurrencyConflict` in the appropriate shared file (grep `ResultErrorCodes` in `Mentoory.Shared.Application/` to locate).
- [X] T012 [P] Add unit tests for `StageActionRegistry.GetState` and `GetGatingStage` covering all 7 stages × all 6 actions in `tests/Mentoory.Access.Tests/StageActions/StageActionRegistryTests.cs` (Available/Locked/Past state transitions).
- [X] T013 [P] Add unit tests for `StageTypeDisplay.ToSpanish` covering all 7 `StageType` values in `tests/Mentoory.Access.Tests/StageActions/StageTypeDisplayTests.cs` (placed in Access.Tests since it's a shared display helper; adjust to `Mentoory.Web.Tests` if a Web test project exists).

**Checkpoint**: Database schema updated, policy enums/registry in place, display helpers available. User story implementation can now begin.

---

## Phase 3: User Story 1 — Advance a project through the lifecycle (Priority: P1) 🎯 MVP

**Goal**: Coordinators can trigger stage advancement from a UI entry point, producing a correct audit trail (who, when, which stages) and rejecting invalid transitions with clear Spanish messages.

**Independent Test**: Per spec Story 1 — a coordinator opens a project in Registration, clicks Advance, confirms, and the project moves to Formularios with timestamps + user recorded. All failure flows (inactive project, already-closed, concurrent double-advance) reject with the correct Spanish toast. Verifiable on a single seed project without Stories 2 or 3.

### Tests for User Story 1

- [X] T014 [P] [US1] Add domain tests for `Project.AdvanceStage` covering: normal Registration→Forms advance, invalid advance when `CurrentStageState` is Completed, no-op/rejection at Closure, and AdvancedByUserId/timestamps set correctly. Extend `tests/Mentoory.Tenant.Tests/Domain/ProjectTests.cs`.
- [X] T015 [P] [US1] Add handler tests for `AdvanceProjectStageHandler` in `tests/Mentoory.Tenant.Tests/Handlers/AdvanceProjectStageHandlerTests.cs` covering: success path, `ProjectNotFound`, `ProjectOutOfScope` (non-matching incubator, non-global-admin), `ProjectInactive`, `StageNotInProgress`, `ProjectAlreadyClosed`.
- [ ] T016 [P] [US1] Add a dedicated concurrency-conflict test for `AdvanceProjectStageHandler` in `tests/Mentoory.Tenant.Tests/Handlers/AdvanceProjectStageHandlerConcurrencyTests.cs` using a relational test provider (SQLite in-memory or Testcontainers SQL Server) since `RowVersion` is not simulated by EF Core InMemory — per research.md § R1.
- [X] T017 [P] [US1] Add validator tests for `AdvanceProjectStageValidator` in `tests/Mentoory.Tenant.Tests/Validators/AdvanceProjectStageValidatorTests.cs` covering empty `ProjectExternalId` and non-positive `ActingUserId`.

### Implementation for User Story 1

- [X] T018 [US1] Create `AdvanceProjectStageCommand` record in `Mentoory.Tenant.Application/Commands/AdvanceProjectStage/AdvanceProjectStageCommand.cs` matching the shape in `contracts/advance-project-stage-command.md`.
- [X] T019 [P] [US1] Create `AdvanceProjectStageResult` record in `Mentoory.Tenant.Application/Commands/AdvanceProjectStage/AdvanceProjectStageResult.cs` (fields: `NewCurrentStageType`, `NewCurrentStageState`).
- [X] T020 [P] [US1] Create `AdvanceProjectStageValidator` in `Mentoory.Tenant.Application/Commands/AdvanceProjectStage/AdvanceProjectStageValidator.cs` with the two rules from the contract.
- [X] T021 [US1] Create `AdvanceProjectStageHandler` in `Mentoory.Tenant.Application/Commands/AdvanceProjectStage/AdvanceProjectStageHandler.cs` implementing steps 1–7 from `contracts/advance-project-stage-command.md` — loads project via `IProjectRepository.GetByExternalIdAsync`, enforces scope, validates state, calls `project.AdvanceStage(userId, timeProvider.UtcNow)`, catches `DbUpdateConcurrencyException`, returns typed failures. Uses `ITimeProvider`, `ILogger`, `[LoggerMessage]` partial methods (per existing handler pattern in `CreateProjectHandler`).
- [X] T022 [US1] Add the `AdvanceStage` action (POST `/Coordination/Projects/AdvanceStage/{externalId}`) to `Mentoory.Web/Areas/Coordination/Controllers/ProjectsController.cs` per `contracts/coordination-ui-routes.md` § `AdvanceStage` action. Controller class is created here with `[Area("Coordination")]`, `[Route("[area]/[controller]")]`, `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`, and constructor-injected `MediatRExecutor`. The controller file will be extended in Phase 4 with additional actions — US1 adds only `AdvanceStage` + the containing class.
- [X] T023 [US1] Implement `ResolveSpanishMessage(errorCode)` helper (private method on the controller or static helper in `Mentoory.Web/Infrastructure/Display/`) translating the six advance-command failure codes to the Spanish toast strings from `contracts/advance-project-stage-command.md` § Failure codes.
- [X] T024 [US1] Register the `AdvanceProjectStage` handler assembly in the MediatR pipeline if it is not already auto-discovered (grep existing handler registration in `Mentoory.Tenant.Application` DI module to confirm no manual step is required).

**Checkpoint**: `AdvanceProjectStageCommand` + handler ship. Even without the new Coordination UI, the command is invocable from the existing `AnswerCorrectionController` / any admin harness and the full flow is unit-testable. User Story 1 is complete once a coordinator can trigger the action by URL.

---

## Phase 4: User Story 2 — Coordination area project overview (Priority: P2)

**Goal**: A dedicated Coordination-area page that lists the coordinator's projects and, for each one, shows the full 7-stage lifecycle timeline with states, timestamps, advanced-by names, and surfaces the advance button (re-using the US1 command) when advancement is allowed.

**Independent Test**: Per spec Story 2 — a coordinator lands on `/Coordination/Projects`, clicks a project, sees the 7-stage timeline with correct states/timestamps for a mid-lifecycle project, and can advance via the now-visible button (re-using US1). A brand-new project shows only Registration in progress; a closed project shows all 7 complete with no advance button. Testable without Story 3.

### Tests for User Story 2

- [X] T025 [P] [US2] Add handler tests for `GetProjectLifecycleHandler` in `tests/Mentoory.Tenant.Tests/Handlers/GetProjectLifecycleHandlerTests.cs` covering: success for a mid-lifecycle project, `ProjectNotFound`, `ProjectOutOfScope` (non-global-admin, non-matching incubator), GlobalAdmin cross-tenant read success, `CanAdvance`/`CannotAdvanceReason` correctness across all five reasons (not in progress, closure, inactive, allowed).
- [ ] T026 [P] [US2] Add integration tests in `tests/Mentoory.Tests.Integration/Coordination/CoordinationProjectsControllerTests.cs` for the `Index`, `Data`, and `Lifecycle/{externalId}` endpoints covering: missing context redirect, role-based authorization denial for non-coordinator roles, happy-path page render, tenant-isolation (incubator A user cannot fetch incubator B's project).

### Implementation for User Story 2

- [X] T027 [P] [US2] Create `ProjectLifecycleStageDto` record in `Mentoory.Tenant.Application/Projects/Queries/GetProjectLifecycle/ProjectLifecycleStageDto.cs` per data-model.md § 4.2.
- [X] T028 [P] [US2] Create `StageActionDto` record in `Mentoory.Tenant.Application/Projects/Queries/GetProjectLifecycle/StageActionDto.cs` per data-model.md § 4.3.
- [X] T029 [P] [US2] Create `ProjectLifecycleDto` record in `Mentoory.Tenant.Application/Projects/Queries/GetProjectLifecycle/ProjectLifecycleDto.cs` per data-model.md § 4.1.
- [X] T030 [US2] Create `GetProjectLifecycleQuery` record in `Mentoory.Tenant.Application/Projects/Queries/GetProjectLifecycle/GetProjectLifecycleQuery.cs` per `contracts/get-project-lifecycle-query.md`.
- [X] T031 [US2] Create `GetProjectLifecycleHandler` in `Mentoory.Tenant.Application/Projects/Queries/GetProjectLifecycle/GetProjectLifecycleHandler.cs` implementing all 8 steps from the contract. Use `AsNoTracking()` + `Include("_stages")`. Compose actions from `StageActionRegistry` + `StageActionDisplay` + `StageActionLinks` + `StageTypeDisplay`.
- [X] T032 [US2] Add a lightweight user-display-name lookup used by step 4 of the handler (resolve `AdvancedByUserId` → display name). If a user-directory query already exists (grep `ListPlatformUsersQuery` / `IUserRepository`), reuse it; otherwise add a targeted `IUserReadService.GetDisplayNamesAsync(IReadOnlyCollection<long> userIds)` method in `Mentoory.Access.Application` (or the equivalent existing namespace used by other handlers for user lookups) and call it once per request.
- [X] T033 [P] [US2] Create `LifecycleStageViewModel` in `Mentoory.Web/Areas/Coordination/Models/LifecycleStageViewModel.cs` mirroring `ProjectLifecycleStageDto`.
- [X] T034 [P] [US2] Create `StageActionViewModel` in `Mentoory.Web/Areas/Coordination/Models/StageActionViewModel.cs` mirroring `StageActionDto`.
- [X] T035 [P] [US2] Create `LifecycleProjectViewModel` in `Mentoory.Web/Areas/Coordination/Models/LifecycleProjectViewModel.cs` mirroring `ProjectLifecycleDto` (references the two above view models).
- [X] T036 [US2] Create a Mapperly mapper `LifecycleMapper` in `Mentoory.Web/Areas/Coordination/Models/LifecycleMapper.cs` with a `ToViewModel(ProjectLifecycleDto)` method producing `LifecycleProjectViewModel`. Mark the class `[Mapper]` per approved mapper.
- [X] T037 [US2] Extend `Mentoory.Web/Areas/Coordination/Controllers/ProjectsController.cs` (created in T022) with `Index()`, `Data([FromForm] DataTableServerRequest request, CancellationToken ct)`, and `Lifecycle(Guid externalId, CancellationToken ct)` actions per `contracts/coordination-ui-routes.md`. Inject the new `LifecycleMapper`. Reuse `ListProjectsQuery` for the `Data` endpoint.
- [X] T038 [P] [US2] Create `Mentoory.Web/Areas/Coordination/Views/Projects/Index.cshtml` — DataTables list with columns Nombre, Descripción, Etapa Actual (Spanish via `StageTypeDisplay`), Estado, Acciones (link button to `Lifecycle/{externalId}`). Model is unset (AJAX-driven) — follow the pattern in `Mentoory.Web/Areas/Administration/Views/Projects/Index.cshtml`.
- [X] T039 [P] [US2] Create `Mentoory.Web/Areas/Coordination/Views/Projects/Lifecycle.cshtml` — project header card (name, description, incubator, Spanish current-stage badge), 7-stage timeline section (each stage: `StageTypeDisplay.ToSpanish`, state icon/color, timestamps formatted `dd/MM/yyyy HH:mm`, `AdvancedByDisplay` line). Do NOT render the Actions grid in this task — that is US3. Render the advance button + confirmation modal conditionally on `Model.CanAdvance`; when `!CanAdvance`, show `Model.CannotAdvanceReason` as muted help text.
- [X] T040 [P] [US2] Create `Mentoory.Web/wwwroot/js/coordination-lifecycle.js` — wire the Bootstrap confirmation modal to the advance form. Confirmation text interpolates current stage and next stage names. Follows platform JS-in-wwwroot convention (constitution VIII).
- [X] T041 [US2] Update `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` — add a new `MenuItem("Proyectos", "/Coordination/Projects", "ti ti-sitemap")` at the top of the existing Coordinación `MenuGroup`'s items array. Keep the existing Diagnósticos entry below it. Roles array already includes `"ProjectCoordinator", "Mentor", "IncubatorAdmin", "GlobalAdmin"` per constitution X — no change needed.
- [ ] T042 [US2] Manual verification: run the app, follow Walkthrough 2 in `quickstart.md`, confirm timeline renders correctly across Registration, mid-lifecycle, and Closed projects.

**Checkpoint**: Coordinator has a working overview + advance button. Stage-gated actions still render as plain anchors inside the Lifecycle view if at all — Actions grid is added in US3. User Stories 1 and 2 both stand alone.

---

## Phase 5: User Story 3 — Stage-gated actions and guidance (Priority: P3)

**Goal**: Server-side filter that rejects out-of-stage action attempts; Lifecycle-view Actions grid that renders every registered action as Available / Locked / Past with accessible helper text naming the unlocking stage.

**Independent Test**: Per spec Story 3 — on a project in Registration, all six action cards render locked with "Disponible desde la etapa X" tooltips. Direct URL to a locked action redirects to the Lifecycle page with a Spanish toast. Advancing through stages flips cards from Locked → Available → Past consistently. Two coordination actions (`AnswerCorrection`, `DiagnosticForms`) get server-side gating.

### Tests for User Story 3

- [ ] T043 [P] [US3] Add filter tests for `RequiresStageAttribute` in `tests/Mentoory.Tests.Integration/Filters/RequiresStageAttributeTests.cs` (or `Mentoory.Web.Tests` if a web test project exists): covering all three states (Available passes through, Locked redirects with toast, Past redirects with toast) across a matrix of action × currentStage combinations. Also cover the two project-id resolution sources (`projectExternalId` route value and `User.GetActiveProjectId()` claim) and the fallback redirect when no project id is resolvable.
- [ ] T044 [P] [US3] Add integration test verifying that hitting `/Coordination/AnswerCorrection` while the active project is in Registration is rejected by the filter (redirect + Spanish toast), and hitting it while in Analysis is allowed.

### Implementation for User Story 3

- [X] T045 [US3] Create `RequiresStageAttribute` action filter in `Mentoory.Web/Infrastructure/Filters/RequiresStageAttribute.cs` implementing the behavior from `contracts/coordination-ui-routes.md` § `RequiresStageAttribute`. Implement as `IAsyncActionFilter` that accepts a `StageGatedAction` in its constructor. Resolves project id from `projectExternalId` route value first, then `User.GetActiveProjectId()`. Uses `MediatRExecutor` (via `context.HttpContext.RequestServices`) to run a minimal `GetProjectCurrentStageQuery`. Redirects with Spanish toast in `TempData` when the state is not `Available`.
- [X] T046 [US3] Create `GetProjectCurrentStageQuery` + handler in `Mentoory.Tenant.Application/Projects/Queries/GetProjectCurrentStage/` returning `{ StageType CurrentStageType, bool IsActive, long IncubatorId }` for the filter to consume without loading the full aggregate. One table round-trip, `AsNoTracking()`.
- [X] T047 [P] [US3] Extend `Mentoory.Web/Areas/Coordination/Views/Projects/Lifecycle.cshtml` with the Actions grid section — iterate `Model.Actions`, render each as a Tabler card with CSS class per `StageGatedActionState`, icon, link, and subtitle. Locked cards: `aria-disabled="true"`, `tabindex="-1"`, `data-bs-toggle="tooltip" data-bs-title="Disponible desde la etapa {GatingStageDisplayName}"`. Past cards: muted + checkmark badge. Available cards: primary button styling with action URL.
- [X] T048 [P] [US3] Extend `Mentoory.Web/wwwroot/js/coordination-lifecycle.js` — initialize Bootstrap tooltips for `[data-bs-toggle="tooltip"]` inside the actions grid container on page load.
- [X] T049 [P] [US3] Apply `[RequiresStage(StageGatedAction.AnswerCorrection)]` to `Mentoory.Web/Areas/Coordination/Controllers/AnswerCorrectionController.cs` — class-level attribute so every action requires Analysis stage.
- [X] T050 [P] [US3] Apply `[RequiresStage(StageGatedAction.DiagnosticForms)]` to the `Clone` and `Configure` actions (and any other form-management actions) of `Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs`. Leave read-only listing actions unchanged (coordinators should still be able to browse the diagnostic-forms list regardless of stage).
- [ ] T051 [US3] Manual verification: run the app, follow Walkthrough 3 in `quickstart.md`, confirm cards flip states correctly as stages advance and direct-URL attempts to locked actions get rejected with the Spanish toast.

**Checkpoint**: All three user stories are functional and independently demoable. The full acceptance scenario set in `spec.md` can be walked through.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Spanish-copy consistency, code review against the Mentoory Code Review Standards, quickstart validation, and any constitution-enforcement fixes that surfaced during implementation.

- [X] T052 [P] Run the Mentoory Code Review Standards pass (per `CLAUDE.md`): check every new method is under 30 lines, extract helpers where needed, verify `AsNoTracking()` is applied on every read-only query path introduced, remove any dead switch branches or unused values, confirm no magic strings remain (enum usage everywhere).
- [X] T053 [P] Run `/simplify` on the changeset to collapse any accidental duplication and normalize style (per `CLAUDE.md` instruction "Run /simplify before presenting code to the user").
- [X] T054 [P] Fix the pre-existing bug in `Mentoory.Web/Areas/Administration/Views/Projects/Details.cshtml` where the stage is rendered as `@Model.CurrentStageType` (enum literal) — replace with `@StageTypeDisplay.ToSpanish(Model.CurrentStageType)` for Spanish consistency (constitution IX). This fix is in-scope because we introduced the helper and the inconsistency is visible from the new Coordination view.
- [X] T055 Verify `dotnet build` passes with zero warnings (constitution V) and `dotnet test` passes fully.
- [ ] T056 Execute all five walkthroughs in `specs/016-project-lifecycle-finish/quickstart.md` against a freshly built stack and check them off. Validate each Success Criterion (SC-001..SC-007) per the quickstart's validation table.
- [X] T057 [P] Update the project's `README.md` / developer docs if there is a section enumerating coordination-area pages or listing lifecycle behavior — otherwise skip. (No `CHANGELOG.md` maintenance required by the project.)
- [X] T058 Constitution compliance self-check: re-run the 11 checks in `plan.md` § Constitution Check against the final code, plus the four Access & Security checks (tenant isolation, scope enforcement, backend authority, audit trail). Document any unexpected deviations in a new `specs/016-project-lifecycle-finish/implementation-notes.md` file (only if any arose — do not create the file for zero-deviation outcomes).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Baseline verification only — no blockers.
- **Foundational (Phase 2)**: Depends on Phase 1. **BLOCKS all user stories.** Inside Phase 2, T002–T004 (schema + EF concurrency mapping) must complete before any handler test touching concurrency (T016). T005–T010 are `[P]`-parallel. T007 depends on T005+T006 (registry uses the enums). T011 is independent. Tests T012–T013 can run alongside T005–T010 once their sources exist.
- **Phase 3 (US1)**: Depends on Phase 2. T018 (command record) → T019, T020, T021 ([P] except T021 depends on T018). T021 → T022 (controller needs the handler wired). T023, T024 after T021.
- **Phase 4 (US2)**: Depends on Phase 2. Independent of US1 in theory, but T037 extends the controller file created in T022 — so US2 requires US1 T022 to have landed (or the tasks must coordinate on the single controller file). All DTO tasks T027–T029 are `[P]`-parallel. T030 → T031. T032 is `[P]` with the rest of US2. View-model tasks T033–T035 `[P]`-parallel. T036 after T035. T037 after T030+T031+T036. Views T038–T039 and JS T040 are `[P]`-parallel with each other but depend on view models. T041 is independent. T042 is the final manual check.
- **Phase 5 (US3)**: Depends on Phase 2 and on US2's Lifecycle view (T039) existing for T047 to extend. T045 depends on T046 (filter calls the query). T047 and T048 edit files created in US2 — coordinate or land after US2 merges. T049 and T050 are `[P]`-parallel.
- **Phase 6 (Polish)**: Depends on all user stories being complete.

### User Story Dependencies

- **US1 (P1)**: No dependencies on other stories. Fully functional at merge.
- **US2 (P2)**: Structurally extends the controller file introduced by US1 T022. If US2 lands first, adjust T022 to scaffold the controller at that time. Recommended order: US1 → US2 so the controller file has a clear merge owner.
- **US3 (P3)**: Depends on US2 (the Lifecycle view is the rendering surface for the Actions grid). Can be developed in parallel after US2's shell view is in place.

### Within Each User Story

- Tests first where they can be written without the implementation (record shapes, happy paths).
- Application layer (DTOs, command/query, handler, validator) before Web layer (controller actions, views, JS).
- Views and JS are file-parallel with each other; same-file tasks (Lifecycle.cshtml in US2 T039 and US3 T047) must serialize.

### Parallel Opportunities

- Phase 2: T005, T006, T008, T009, T010, T012, T013 all `[P]`. T007 serializes after T005+T006.
- US1: T019, T020 parallel with T021 blocked on T018. Tests T014–T017 `[P]` with each other.
- US2: All DTO files (T027–T029), all view-model files (T033–T035), all view files (T038–T039), JS (T040), menu config (T041) can be done in parallel after their prerequisites.
- US3: T049 and T050 edit different controllers — `[P]`-parallel. View extension (T047) and JS extension (T048) are `[P]` because they touch different files.
- Polish: T052, T053, T054, T057 all `[P]`. T055 and T056 are sequential validation steps.

---

## Parallel Example: User Story 2

```bash
# After T030 (query) and T031 (handler) land, launch in parallel:
Task: "Create LifecycleStageViewModel in Mentoory.Web/Areas/Coordination/Models/LifecycleStageViewModel.cs"
Task: "Create StageActionViewModel in Mentoory.Web/Areas/Coordination/Models/StageActionViewModel.cs"
Task: "Create LifecycleProjectViewModel in Mentoory.Web/Areas/Coordination/Models/LifecycleProjectViewModel.cs"

# After view models and controller land, launch view + JS + menu in parallel:
Task: "Create Mentoory.Web/Areas/Coordination/Views/Projects/Index.cshtml"
Task: "Create Mentoory.Web/Areas/Coordination/Views/Projects/Lifecycle.cshtml"
Task: "Create Mentoory.Web/wwwroot/js/coordination-lifecycle.js"
Task: "Update Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs to add the new menu item"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 (validation only).
2. Complete Phase 2 (schema + registry + helpers).
3. Complete Phase 3 (US1 — AdvanceProjectStageCommand end-to-end, including the POST endpoint).
4. **STOP and VALIDATE**: Demonstrate advancing a project via `curl` or a minimal admin button, confirming database state and audit trail. This MVP ships without the new Coordination-area overview but proves the lifecycle advancement works correctly and safely.
5. Deploy/demo if ready.

### Incremental Delivery

1. Setup + Foundational → primitives in place.
2. Add US1 → advance works → Deploy/Demo (MVP).
3. Add US2 → coordinators have a proper overview page and UI button → Deploy/Demo.
4. Add US3 → action grid + server-side stage gating → Deploy/Demo.
5. Polish → final code-review + Spanish consistency + quickstart validation.

### Parallel Team Strategy

- Developer A: US1 (command + handler + validator + controller stub).
- Developer B: US2 DTOs/query/handler — can start as soon as Phase 2 is green, does not need US1 code.
- Developer C: US3 filter + `GetProjectCurrentStageQuery` — can draft in isolation; integration into controllers waits for US2.
- All three merge-coordinate on the one Coordination `ProjectsController.cs` file, preferably via short-lived branches rebased in priority order.

---

## Notes

- Every `[P]` task is in a distinct file with no runtime dependency on other `[P]` tasks in the same group.
- Story labels (`[US1]`, `[US2]`, `[US3]`) appear on every user-story-phase task; Setup/Foundational/Polish tasks have no story label per the format rules.
- Tests in each user-story phase follow the platform's existing convention (xUnit + Moq + FluentAssertions). Concurrency test (T016) intentionally uses a relational provider because EF Core InMemory does not emulate `ROWVERSION`.
- Commit after each checkpoint at minimum; prefer commit-per-task for reviewability.
- Stop at any checkpoint to demo the story.
- The Access & Security Constitution requires tenant isolation checks in the handlers (not only in the controller) — T021 and T031 enforce this at the Application layer, which is the non-negotiable location per both constitutions.
