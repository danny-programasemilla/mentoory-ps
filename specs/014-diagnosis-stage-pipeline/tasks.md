# Tasks: Configurable Stage Pipeline & Flexible Diagnosis Module

**Input**: Design documents from `/specs/014-diagnosis-stage-pipeline/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Included — spec explicitly requires "deep, meaningful E2E tests for every flow".

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Domain**: `Mentoory.{Domain}.Domain/`
- **Application**: `Mentoory.{Domain}.Application/`
- **Infrastructure**: `Mentoory.{Domain}.Infrastructure/`
- **Web**: `Mentoory.Web/Areas/{Area}/`
- **Database**: `Mentoory.Db/{schema}/Tables/`
- **PostDeployment**: `Mentoory.Db.PostDeployment/`
- **Tests**: `tests/Mentoory.{Domain}.Tests/`
- **JavaScript**: `Mentoory.Web/wwwroot/js/`

---

## Phase 1: Setup

**Purpose**: Verify clean build and prepare branch

- [x] T001 Verify solution builds cleanly on branch 014-diagnosis-stage-pipeline with `dotnet build`
- [x] T002 Run existing test suite with `dotnet test` to establish baseline (record any pre-existing failures)

---

## Phase 2: Foundational — Tenant Domain Refactoring

**Purpose**: Refactor the stage pipeline model. MUST complete before any user story work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T003 Modify StageType enum from 7 values to 4 (Registration=0, Diagnosis=1, Mentorship=2, Closure=3) in `Mentoory.Tenant.Domain/Enums/StageType.cs`
- [x] T004 Refactor ProjectStage entity: add ExternalId (Guid), Position (int), DisplayName (string), PlannedStartDate, PlannedEndDate (nullable DateTime) in `Mentoory.Tenant.Domain/Aggregates/Project/ProjectStage.cs`
- [x] T005 Refactor Project aggregate: replace fixed 7-stage creation with configurable default 5-stage pipeline (Reg→Diag→Mentorship→Diag→Closure), add AddStage(), RemoveStage(), ReorderStages(), RenameStage() methods, refactor AdvanceStage() to position-based in `Mentoory.Tenant.Domain/Aggregates/Project/Project.cs`
- [x] T006 Update TenantDbContext entity configuration for new ProjectStage properties (ExternalId unique index, Position+ProjectId unique composite, DisplayName max length) in `Mentoory.Tenant.Infrastructure/Persistence/TenantDbContext.cs`
- [x] T007 Alter SSDT table definition for ProjectStages: add ExternalId, Position, DisplayName, PlannedStartDate, PlannedEndDate columns in `Mentoory.Db/tenant/Tables/ProjectStages.sql`
- [x] T008 Create PostDeployment script to migrate existing stage data (map old 7-type values to new 4-type, generate positions and display names) in `Mentoory.Db.PostDeployment/019.MigrateStageTypes.sql`
- [x] T009 Fix all compilation errors across solution caused by StageType enum change (update all references to removed values: Forms, Analysis, LearningAssignment, Mentoring, FinalEvaluation)
- [x] T010 Write domain unit tests for Project pipeline: default creation, AddStage, RemoveStage, ReorderStages, RenameStage, AdvanceStage, boundary validation (must start with Registration, end with Closure) in `tests/Mentoory.Tenant.Tests/Domain/ProjectPipelineTests.cs`

**Checkpoint**: Tenant domain compiles, pipeline tests pass. Stage pipeline is configurable.

---

## Phase 3: Foundational — Diagnostic Domain Refactoring

**Purpose**: Remove hardcoded evaluation model and create new StageFormAssignment aggregate. MUST complete before user story work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T011 Delete EvaluationStage enum file `Mentoory.Diagnostic.Domain/Enums/EvaluationStage.cs`
- [x] T012 Delete StageApplicability enum file `Mentoory.Diagnostic.Domain/Enums/StageApplicability.cs`
- [x] T013 [P] Remove StageApplicability property from QuestionTemplate entity in `Mentoory.Diagnostic.Domain/Aggregates/FormTemplate/QuestionTemplate.cs` and its Create/CloneFromTemplate factory methods
- [x] T014 [P] Remove StageApplicability property from Question entity in `Mentoory.Diagnostic.Domain/Aggregates/ProjectForm/Question.cs` and its Create/CloneFromTemplate factory methods
- [x] T015 Create StageFormAssignment aggregate root with ExternalId, ProjectId, IncubatorId, ProjectStageId, ProjectFormId, IsActive, CreatedAtUtc and domain methods (Create, UpdateQuestionSelection, Deactivate) in `Mentoory.Diagnostic.Domain/Aggregates/StageFormAssignment/StageFormAssignment.cs`
- [x] T016 Create AssignedQuestion child entity with QuestionId and SortOrder in `Mentoory.Diagnostic.Domain/Aggregates/StageFormAssignment/AssignedQuestion.cs`
- [x] T017 Create IStageFormAssignmentRepository interface in `Mentoory.Diagnostic.Domain/Repositories/IStageFormAssignmentRepository.cs`
- [x] T018 Create ScoreDelta value object (TopicId, PreviousScore, CurrentScore, Delta, PercentageChange) in `Mentoory.Diagnostic.Domain/ValueObjects/ScoreDelta.cs`
- [x] T019 Refactor DiagnosticResponse: remove EvaluationStage property, add StageFormAssignmentId (long), update Create() factory method signature in `Mentoory.Diagnostic.Domain/Aggregates/DiagnosticResponse/DiagnosticResponse.cs`
- [x] T020 Create SSDT table definition for StageFormAssignments in `Mentoory.Db/diagnostic/Tables/StageFormAssignments.sql`
- [x] T021 Create SSDT table definition for AssignedQuestions in `Mentoory.Db/diagnostic/Tables/AssignedQuestions.sql`
- [x] T022 Alter SSDT DiagnosticResponses table: add StageFormAssignmentId column, drop EvaluationStage column in `Mentoory.Db/diagnostic/Tables/DiagnosticResponses.sql`
- [x] T023 [P] Alter SSDT QuestionTemplates table: drop StageApplicability column in `Mentoory.Db/diagnostic/Tables/QuestionTemplates.sql`
- [x] T024 [P] Alter SSDT Questions table: drop StageApplicability column in `Mentoory.Db/diagnostic/Tables/Questions.sql`
- [x] T025 Update DiagnosticDbContext: add StageFormAssignments and AssignedQuestions DbSets, configure entity mappings (cascade delete, indexes, tenant filter), update DiagnosticResponse mapping (StageFormAssignmentId FK, remove EvaluationStage), remove StageApplicability from QuestionTemplate/Question mappings in `Mentoory.Diagnostic.Infrastructure/Persistence/DiagnosticDbContext.cs`
- [x] T026 Create StageFormAssignmentRepository implementation in `Mentoory.Diagnostic.Infrastructure/Persistence/Repositories/StageFormAssignmentRepository.cs`
- [x] T027 Register StageFormAssignmentRepository in Diagnostic Infrastructure DependencyInjection in `Mentoory.Diagnostic.Infrastructure/DependencyInjection.cs`
- [x] T028 Add 3 new permissions (ManageProjectPipeline=304, AssignDiagnosticForms=305, ViewDiagnosticComparison=404) to Permission enum in `Mentoory.Access.Domain/Enums/Permission.cs`
- [x] T029 Create PostDeployment script to seed new permissions with role mappings in `Mentoory.Db.PostDeployment/020.SeedNewPermissions.sql`
- [x] T030 Fix all remaining compilation errors across solution caused by removed enums and refactored properties (update commands, queries, handlers, DTOs, controllers, view models, tests)
- [x] T031 Write domain unit tests for StageFormAssignment: Create, UpdateQuestionSelection, Deactivate, validation (non-empty questions, duplicate prevention) in `tests/Mentoory.Diagnostic.Tests/Domain/StageFormAssignmentTests.cs`
- [x] T032 Update existing DiagnosticResponseTests for new Create() signature (StageFormAssignmentId instead of EvaluationStage) in `tests/Mentoory.Diagnostic.Tests/Domain/DiagnosticResponseTests.cs`
- [x] T033 Update existing FormTemplateTests and ProjectFormTests to remove StageApplicability from test scenarios in `tests/Mentoory.Diagnostic.Tests/Domain/FormTemplateTests.cs` and `tests/Mentoory.Diagnostic.Tests/Domain/ProjectFormTests.cs`
- [x] T034 Verify full solution builds with zero warnings via `dotnet build` and all existing tests pass via `dotnet test`

**Checkpoint**: Full solution compiles with zero warnings. All domain tests pass. New aggregate and SSDT schema ready.

---

## Phase 4: User Story 1 — Configurable Stage Pipeline (Priority: P1) 🎯 MVP

**Goal**: Admins can create, customize, and advance through a configurable stage pipeline.

**Independent Test**: Create project → verify default pipeline → add/remove/reorder stages → advance through them.

### Tests for User Story 1

- [x] T035 [P] [US1] Write handler tests for AddProjectStageCommand (happy path, validation: non-Diagnosis at boundaries, position shifting) in `tests/Mentoory.Tenant.Tests/Handlers/AddProjectStageHandlerTests.cs`
- [x] T036 [P] [US1] Write handler tests for RemoveProjectStageCommand (happy path, with-data confirmation, boundary protection) in `tests/Mentoory.Tenant.Tests/Handlers/RemoveProjectStageHandlerTests.cs`
- [x] T037 [P] [US1] Write handler tests for ReorderProjectStagesCommand (happy path, invalid IDs, boundary enforcement) in `tests/Mentoory.Tenant.Tests/Handlers/ReorderProjectStagesHandlerTests.cs`
- [x] T038 [P] [US1] Write handler tests for RenameProjectStageCommand in `tests/Mentoory.Tenant.Tests/Handlers/RenameProjectStageHandlerTests.cs`
- [x] T039 [P] [US1] Write handler tests for AdvanceProjectStageCommand (position-based, closure completion, already-completed) in `tests/Mentoory.Tenant.Tests/Handlers/AdvanceProjectStageHandlerTests.cs`

### Implementation for User Story 1

- [x] T040 [P] [US1] Create AddProjectStageCommand, handler, and validator in `Mentoory.Tenant.Application/Commands/AddProjectStage/`
- [x] T041 [P] [US1] Create RemoveProjectStageCommand, handler, and validator in `Mentoory.Tenant.Application/Commands/RemoveProjectStage/`
- [x] T042 [P] [US1] Create ReorderProjectStagesCommand, handler, and validator in `Mentoory.Tenant.Application/Commands/ReorderProjectStages/`
- [x] T043 [P] [US1] Create RenameProjectStageCommand, handler, and validator in `Mentoory.Tenant.Application/Commands/RenameProjectStage/`
- [x] T044 [US1] Refactor AdvanceProjectStageCommand to position-based advancement in `Mentoory.Tenant.Application/Commands/AdvanceProjectStage/`
- [x] T045 [P] [US1] Create GetProjectPipelineQuery, handler, and DTO in `Mentoory.Tenant.Application/Queries/GetProjectPipeline/`
- [x] T046 [P] [US1] Create ListProjectStagesQuery, handler, and DTO in `Mentoory.Tenant.Application/Queries/ListProjectStages/`
- [x] T047 [P] [US1] Create ProjectStageAddedEvent and ProjectStageRemovedEvent in `Mentoory.Tenant.Application/IntegrationEvents/`
- [x] T048 [US1] Create ProjectPipelineController with Index, AddStage, RemoveStage, Reorder, Rename, Advance actions. Authorize: ProjectCoordinator, IncubatorAdmin, GlobalAdmin in `Mentoory.Web/Areas/Coordination/Controllers/ProjectPipelineController.cs`
- [x] T049 [US1] Create ProjectPipelineViewModels (PipelineViewModel, StageViewModel, AddStageViewModel, ReorderStagesViewModel) in `Mentoory.Web/Areas/Coordination/Models/ProjectPipelineViewModels.cs`
- [x] T050 [US1] Create Pipeline Editor view (ordered stage list, add/remove/reorder/rename/advance controls, validation messages) in `Mentoory.Web/Areas/Coordination/Views/ProjectPipeline/Index.cshtml`
- [x] T051 [US1] Create pipeline-editor.js (reorder interactions, AJAX stage operations, confirmation dialogs) in `Mentoory.Web/wwwroot/js/pipeline-editor.js`
- [x] T052 [US1] Add "Etapas" navigation link to project detail page for Coordination area menu

**Checkpoint**: Pipeline editor fully functional. Admin can create, customize, and advance stages. Tests pass.

---

## Phase 5: User Story 2 — Stage-Form Assignment with Question Selection (Priority: P1)

**Goal**: Coordinators can assign ProjectForms to Diagnosis stages and select which questions are active per assignment.

**Independent Test**: Assign a form to a Diagnosis stage → select questions → verify assignment persists. Assign same form to another stage with different selection.

### Tests for User Story 2

- [x] T053 [P] [US2] Write handler tests for AssignFormToStageCommand (happy path, non-Diagnosis stage rejection, default all-selected) in `tests/Mentoory.Diagnostic.Tests/Handlers/AssignFormToStageHandlerTests.cs`
- [x] T054 [P] [US2] Write handler tests for UpdateStageQuestionSelectionCommand (happy path, empty selection rejection) in `tests/Mentoory.Diagnostic.Tests/Handlers/UpdateStageQuestionSelectionHandlerTests.cs`
- [x] T055 [P] [US2] Write handler tests for RemoveFormFromStageCommand (happy path, with-responses warning) in `tests/Mentoory.Diagnostic.Tests/Handlers/RemoveFormFromStageHandlerTests.cs`

### Implementation for User Story 2

- [x] T056 [P] [US2] Create AssignFormToStageCommand, handler (validates Diagnosis stage type, creates assignment with all questions selected), and validator in `Mentoory.Diagnostic.Application/Commands/AssignFormToStage/`
- [x] T057 [P] [US2] Create UpdateStageQuestionSelectionCommand, handler, and validator in `Mentoory.Diagnostic.Application/Commands/UpdateStageQuestionSelection/`
- [x] T058 [P] [US2] Create RemoveFormFromStageCommand, handler (deactivates, checks responses), and validator in `Mentoory.Diagnostic.Application/Commands/RemoveFormFromStage/`
- [x] T059 [P] [US2] Create GetStageFormAssignmentQuery, handler, and DTO (form + selected questions with options) in `Mentoory.Diagnostic.Application/Queries/GetStageFormAssignment/`
- [x] T060 [P] [US2] Create ListStageFormAssignmentsQuery, handler, and DTO (assignments for a stage with question counts) in `Mentoory.Diagnostic.Application/Queries/ListStageFormAssignments/`
- [x] T061 [US2] Add StageConfig and QuestionSelection actions to DiagnosticsController. Authorize: ProjectCoordinator, IncubatorAdmin, GlobalAdmin in `Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs`
- [x] T062 [US2] Create/update DiagnosticViewModels: add StageConfigViewModel, QuestionSelectionViewModel, AssignedFormViewModel in `Mentoory.Web/Areas/Coordination/Models/DiagnosticViewModels.cs`
- [x] T063 [US2] Create StageConfig view (assigned forms list, add form modal, edit/remove actions) in `Mentoory.Web/Areas/Coordination/Views/Diagnostics/StageConfig.cshtml`
- [x] T064 [US2] Create QuestionSelection view (checkbox list grouped by topic, select-all toggles) in `Mentoory.Web/Areas/Coordination/Views/Diagnostics/QuestionSelection.cshtml`
- [x] T065 [US2] Create question-selection.js (topic group toggles, select all, save via AJAX) in `Mentoory.Web/wwwroot/js/question-selection.js`

**Checkpoint**: Form-to-stage assignment functional. Coordinator can assign forms, select questions. Tests pass.

---

## Phase 6: User Story 3 — Diagnosis Execution by Entrepreneur (Priority: P1)

**Goal**: Entrepreneurs see Diagnosis stages, fill questionnaires with only selected questions, and submit responses linked to StageFormAssignment.

**Independent Test**: Entrepreneur navigates to Diagnósticos → opens a pending form → fills selected questions → submits → sees confirmation → status updates to Completado.

### Tests for User Story 3

- [x] T066 [P] [US3] Write handler tests for refactored SubmitDiagnosticResponseCommand (StageFormAssignmentExternalId, validates selected questions match, uniqueness per entrepreneur+assignment) in `tests/Mentoory.Diagnostic.Tests/Handlers/SubmitDiagnosticResponseHandlerTests.cs`
- [x] T067 [P] [US3] Write handler tests for GetEntrepreneurDiagnosticStatusQuery (completion status across stages) in `tests/Mentoory.Diagnostic.Tests/Handlers/GetEntrepreneurDiagnosticStatusHandlerTests.cs`

### Implementation for User Story 3

- [x] T068 [US3] Refactor SubmitDiagnosticResponseCommand: replace EvaluationStage param with StageFormAssignmentExternalId, handler resolves assignment, validates questions match selected set, publishes updated DiagnosticCompletedEvent in `Mentoory.Diagnostic.Application/Commands/SubmitDiagnosticResponse/`
- [x] T069 [US3] Create GetEntrepreneurDiagnosticStatusQuery, handler, and DTO (per-stage, per-form completion status) in `Mentoory.Diagnostic.Application/Queries/GetEntrepreneurDiagnosticStatus/`
- [x] T070 [US3] Update DiagnosticCompletedEvent: replace EvaluationStage with StageFormAssignmentId + ProjectStageId in `Mentoory.Diagnostic.Application/IntegrationEvents/DiagnosticCompletedEvent.cs`
- [x] T071 [US3] Refactor Participant DiagnosticController: use StageFormAssignment for form rendering, update Index (landing with stage status), Fill (selected questions only), Submit, Confirmation actions in `Mentoory.Web/Areas/Participant/Controllers/DiagnosticController.cs`
- [x] T072 [US3] Update Participant DiagnosticViewModels: DiagnosticLandingViewModel (stages with form status), DiagnosticFormViewModel (selected questions only), SubmitDiagnosticViewModel (StageFormAssignmentExternalId) in `Mentoory.Web/Areas/Participant/Models/DiagnosticViewModels.cs`
- [x] T073 [US3] Update Participant Diagnostic Index view (landing: stages with per-form completion status) in `Mentoory.Web/Areas/Participant/Views/Diagnostic/Index.cshtml`
- [x] T074 [US3] Update Participant Diagnostic Fill view (questionnaire: only selected questions, grouped by topic, progress indicator) in `Mentoory.Web/Areas/Participant/Views/Diagnostic/Fill.cshtml`
- [x] T075 [US3] Update Participant Diagnostic Confirmation view in `Mentoory.Web/Areas/Participant/Views/Diagnostic/Confirmation.cshtml`

**Checkpoint**: Entrepreneur can complete a diagnosis end-to-end. Response linked to correct assignment. Tests pass.

---

## Phase 7: User Story 4 — Results Comparison Across Stages (Priority: P2)

**Goal**: Coordinators can compare topic-level scores and question-level answers across diagnosis executions for an entrepreneur.

**Independent Test**: Entrepreneur completes diagnoses at 2 stages → coordinator selects both → comparison shows topic deltas and shared question answers.

### Tests for User Story 4

- [x] T076 [P] [US4] Write handler tests for CompareDiagnosticResultsQuery (topic deltas, shared questions only, zero shared questions, percentage change calculation) in `tests/Mentoory.Diagnostic.Tests/Handlers/CompareDiagnosticResultsHandlerTests.cs`
- [x] T077 [P] [US4] Write handler tests for GetDiagnosticTimelineQuery (chronological order, all executions for entrepreneur) in `tests/Mentoory.Diagnostic.Tests/Handlers/GetDiagnosticTimelineHandlerTests.cs`

### Implementation for User Story 4

- [x] T078 [P] [US4] Create CompareDiagnosticResultsQuery, handler, and DTOs (TopicComparisonDto with ScoreDelta, QuestionComparisonDto with side-by-side answers, shared question matching by QuestionId) in `Mentoory.Diagnostic.Application/Queries/CompareDiagnosticResults/`
- [x] T079 [P] [US4] Create GetDiagnosticTimelineQuery, handler, and DTO (chronological list with stage name, form name, date, score summary) in `Mentoory.Diagnostic.Application/Queries/GetDiagnosticTimeline/`
- [x] T080 [US4] Create DiagnosticResultsController with Timeline, Detail, Compare actions. Authorize: ProjectCoordinator, Mentor, IncubatorAdmin, GlobalAdmin in `Mentoory.Web/Areas/Coordination/Controllers/DiagnosticResultsController.cs`
- [x] T081 [US4] Create DiagnosticResultsViewModels (TimelineViewModel, CompareViewModel, TopicComparisonViewModel, QuestionComparisonViewModel) in `Mentoory.Web/Areas/Coordination/Models/DiagnosticResultsViewModels.cs`
- [x] T082 [US4] Create Timeline view (chronological execution list, selection checkboxes, "Comparar" button) in `Mentoory.Web/Areas/Coordination/Views/DiagnosticResults/Timeline.cshtml`
- [x] T083 [US4] Create Compare view (topic-level tab with score deltas and visual indicators, question-level tab with shared questions only) in `Mentoory.Web/Areas/Coordination/Views/DiagnosticResults/Compare.cshtml`
- [x] T084 [US4] Create diagnostic-comparison.js (tab switching, selection management, delta visualization) in `Mentoory.Web/wwwroot/js/diagnostic-comparison.js`

**Checkpoint**: Comparison view functional. Topic deltas and question drill-down work correctly. Tests pass.

---

## Phase 8: User Story 5 — Answer Correction with Audit Trail (Priority: P2)

**Goal**: Authorized users can correct answers with full audit trail, and corrections update topic aggregations.

**Independent Test**: Submit response → correct an answer → verify audit trail records previous value → verify topic scores update.

### Tests for User Story 5

- [x] T085 [US5] Update existing CorrectAnswerHandlerTests for new model (StageFormAssignment-based DiagnosticResponse) in `tests/Mentoory.Diagnostic.Tests/Handlers/CorrectAnswerHandlerTests.cs`

### Implementation for User Story 5

- [x] T086 [US5] Update CorrectAnswerCommand handler: resolve DiagnosticResponse via StageFormAssignment reference, ensure AnswerCorrectedEvent unchanged in `Mentoory.Diagnostic.Application/Commands/CorrectAnswer/`
- [x] T087 [US5] Refactor AnswerCorrectionController: remove EvaluationStage references, add stage display name from StageFormAssignment context in `Mentoory.Web/Areas/Coordination/Controllers/AnswerCorrectionController.cs`
- [x] T088 [US5] Update AnswerCorrection views to show stage name context instead of EvaluationStage label in `Mentoory.Web/Areas/Coordination/Views/AnswerCorrection/`

**Checkpoint**: Answer correction works with new model. Audit trail preserved. Tests pass.

---

## Phase 9: User Story 6 — Single Result Detail View (Priority: P2)

**Goal**: Coordinators can view full detail of a single diagnosis execution with topic aggregation and per-question answers.

**Independent Test**: Submit response → view detail → verify topic scores and individual answers displayed with stage context.

### Implementation for User Story 6

- [x] T089 [US6] Update GetDiagnosticResponseQuery handler and DTO to include StageFormAssignment context (stage name, form name) in `Mentoory.Diagnostic.Application/Queries/GetDiagnosticResponse/`
- [x] T090 [US6] Add Detail action to DiagnosticResultsController in `Mentoory.Web/Areas/Coordination/Controllers/DiagnosticResultsController.cs`
- [x] T091 [US6] Create Detail view (stage name, form name, completion date, topic score aggregation, per-question answers with scores, correction flags) in `Mentoory.Web/Areas/Coordination/Views/DiagnosticResults/Detail.cshtml`

**Checkpoint**: Detail view functional. Topic scores and individual answers display correctly.

---

## Phase 10: User Story 7 — Template Sync with Assignment Awareness (Priority: P3)

**Goal**: Template sync adds questions to ProjectForm but does NOT auto-add to existing StageFormAssignments.

**Independent Test**: Sync form from updated template → verify new questions in form → verify existing assignments unchanged → manually add new questions to assignment.

### Tests for User Story 7

- [x] T092 [US7] Write handler tests verifying template sync does not modify existing StageFormAssignment question selections in `tests/Mentoory.Diagnostic.Tests/Handlers/SyncFromTemplateHandlerTests.cs`

### Implementation for User Story 7

- [x] T093 [US7] Review and update SyncFromTemplateCommand handler: ensure synced questions are added to ProjectForm only, not propagated to StageFormAssignments in `Mentoory.Diagnostic.Application/Commands/SyncFromTemplate/`
- [x] T094 [US7] Update CustomizeProjectFormCommand handler: remove StageApplicability from question add/modify operations in `Mentoory.Diagnostic.Application/Commands/CustomizeProjectForm/`
- [x] T095 [US7] Update GetProjectFormQuery DTO: remove StageApplicability from QuestionDto in `Mentoory.Diagnostic.Application/Queries/GetProjectForm/`

**Checkpoint**: Template sync works correctly with assignment model. New questions don't leak into existing assignments.

---

## Phase 11: E2E Integration Tests

**Purpose**: Deep, meaningful end-to-end tests covering all complete flows.

- [ ] T096 [P] Write E2E test: full diagnosis flow (create project → configure pipeline → assign forms to stages → entrepreneur submits diagnosis → advance stage → submit second diagnosis → compare results) in `tests/Mentoory.Diagnostic.Tests/Integration/DiagnosisFullFlowTests.cs`
- [ ] T097 [P] Write E2E test: mid-project diagnosis (add Diagnosis stage mid-pipeline → assign different form → submit → compare with shared questions only) in `tests/Mentoory.Diagnostic.Tests/Integration/MidProjectDiagnosisTests.cs`
- [ ] T098 [P] Write E2E test: answer correction flow (submit → correct → verify audit trail and updated aggregation) in `tests/Mentoory.Diagnostic.Tests/Integration/AnswerCorrectionFlowTests.cs`
- [ ] T099 [P] Write E2E test: pipeline modification (add/remove/reorder stages with existing assignments and responses → verify data integrity) in `tests/Mentoory.Diagnostic.Tests/Integration/PipelineModificationTests.cs`
- [ ] T100 [P] Write E2E test: multi-tenant isolation (two incubators, same project names → verify complete data separation at every layer) in `tests/Mentoory.Diagnostic.Tests/Integration/MultiTenantIsolationTests.cs`
- [ ] T101 [P] Write E2E test: template sync with assignments (connected form → template update → sync → verify assignment selections unchanged → manually add new questions) in `tests/Mentoory.Diagnostic.Tests/Integration/TemplateSyncAssignmentTests.cs`
- [ ] T102 [P] Write E2E test: concurrent submissions (two entrepreneurs submitting to same StageFormAssignment simultaneously → verify independent responses) in `tests/Mentoory.Diagnostic.Tests/Integration/ConcurrentSubmissionTests.cs`

**Checkpoint**: All 7 E2E tests pass. Complete vertical slice verified.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup, navigation, and validation.

- [x] T103 Add "Resultados diagnósticos" navigation link to Coordination area menu in `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs`
- [x] T104 Add "Diagnósticos" navigation link to Participant area menu (if not already present) in `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs`
- [x] T105 Verify all [Authorize] attributes include higher-privilege roles per constitution Principle X across all new/modified controllers
- [x] T106 Verify all user-facing text is in Spanish across all new views, validation messages, and toast notifications
- [x] T107 Run full solution build with `dotnet build` — verify zero warnings
- [x] T108 Run full test suite with `dotnet test` — verify all tests pass (unit + handler + E2E)
- [ ] T109 Run DACPAC build with `cd Mentoory.Db && ./publish-mentoorydb.sh` — verify schema deploys cleanly
- [ ] T110 Run quickstart.md verification steps manually: pipeline config → form assignment → diagnosis execution → results comparison

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Tenant Refactoring)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (Diagnostic Refactoring)**: Depends on Phase 2 — BLOCKS all user stories
- **Phase 4-10 (User Stories)**: All depend on Phase 2 + 3 completion
  - US1 (Pipeline UI): Can start immediately after Phase 3
  - US2 (Form Assignment): Can start immediately after Phase 3
  - US3 (Execution): Depends on US2 (needs assignments to exist)
  - US4 (Comparison): Depends on US3 (needs responses to compare)
  - US5 (Correction): Depends on US3 (needs responses to correct)
  - US6 (Detail View): Depends on US3 (needs responses to view)
  - US7 (Template Sync): Can start immediately after Phase 3
- **Phase 11 (E2E Tests)**: Depends on US1-US7 completion
- **Phase 12 (Polish)**: Depends on Phase 11

### User Story Dependencies

```
Phase 2 + 3 (Foundational)
    ├── US1 (Pipeline UI) ──────────────────┐
    ├── US2 (Form Assignment) ──► US3 (Execution) ──► US4 (Comparison)
    │                                    ├──► US5 (Correction)
    │                                    └──► US6 (Detail View)
    └── US7 (Template Sync) ────────────────┘
                                            ▼
                                     Phase 11 (E2E Tests)
                                            ▼
                                     Phase 12 (Polish)
```

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Domain/application layer before web layer
- Commands/queries before controllers
- Controllers before views
- Views before JavaScript

### Parallel Opportunities

- **Phase 2**: T003-T008 largely sequential (enum change cascades)
- **Phase 3**: T013+T014 parallel (different files), T020+T021+T023+T024 parallel (different SQL files)
- **Phase 4**: T035-T039 parallel (test files), T040-T043+T045-T047 parallel (different command/query dirs)
- **Phase 5**: T053-T055 parallel (test files), T056-T060 parallel (different command/query dirs)
- **Phase 11**: ALL E2E tests (T096-T102) are parallel — different test classes

---

## Parallel Example: User Story 2

```bash
# Launch all tests for US2 together:
Task: "Handler tests for AssignFormToStageCommand" (T053)
Task: "Handler tests for UpdateStageQuestionSelectionCommand" (T054)
Task: "Handler tests for RemoveFormFromStageCommand" (T055)

# Launch all commands/queries for US2 together:
Task: "AssignFormToStageCommand" (T056)
Task: "UpdateStageQuestionSelectionCommand" (T057)
Task: "RemoveFormFromStageCommand" (T058)
Task: "GetStageFormAssignmentQuery" (T059)
Task: "ListStageFormAssignmentsQuery" (T060)
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Tenant Refactoring (CRITICAL — blocks everything)
3. Complete Phase 3: Diagnostic Refactoring (CRITICAL — blocks everything)
4. Complete Phase 4: User Story 1 (Pipeline UI)
5. Complete Phase 5: User Story 2 (Form Assignment)
6. Complete Phase 6: User Story 3 (Diagnosis Execution)
7. **STOP and VALIDATE**: Test all 3 stories independently
8. Deploy/demo MVP

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 (Pipeline) → Test → Demo
3. Add US2 (Assignment) → Test → Demo
4. Add US3 (Execution) → Test → Demo (MVP!)
5. Add US4 (Comparison) → Test → Demo
6. Add US5+US6 (Correction+Detail) → Test → Demo
7. Add US7 (Template Sync) → Test → Demo
8. E2E Tests + Polish → Release

### Parallel Team Strategy

With multiple developers after foundational phases:

- Developer A: US1 (Pipeline UI) + US7 (Template Sync)
- Developer B: US2 (Form Assignment) → US3 (Execution)
- Developer C: US4 (Comparison) + US5 (Correction) + US6 (Detail)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Spec requires deep E2E tests — Phase 11 is mandatory, not optional
