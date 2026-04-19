# Feature Specification: Knowledge Module E2E Test Coverage

**Feature Branch**: `016-knowledge-module-core` (shared — tests land in the same PR as the module they cover)
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "analyze and create high quality and coverage e2e tests required for the work done in this branch"

## Overview

Branch `016-knowledge-module-core` (plus its Phase 9 amendment "Project-owned KS binding") introduces a large, cross-schema slice of product behavior — new `knowledge` schema, Template + Project-Clone domains, project-creation KS materialization, form-clone cascade with `Question.TopicId` rewriting, partial-sync, priority-range events, and a Spanish-UI coordinator surface under `Mentoory.Web/Areas/Coordination/Knowledge`. Domain and integration tests cover handlers in isolation, but browser-level E2E coverage today consists of two smoke files (`KnowledgeTemplatesTests`, `KnowledgeProjectStructureTests`) with a total of 8 tests — not enough to detect UI regressions, authorization drift, or cross-module cascades breaking at the HTTP boundary.

This feature specifies the **E2E test coverage the team wants added before merging 016 to `develop`**. The deliverable is a new and/or extended set of Playwright-driven test files under `tests/Mentoory.Tests.E2E/Tests/` (and one reinforcement to the `DiagnosticCascadeRoundTripTests` integration suite where a pure-UI test is impractical), plus whatever PostDeployment seed data those tests depend on.

The spec is scoped to **test-suite behavior only** — it specifies *what scenarios the automated tests must exercise and assert*. It does not re-specify the module's business rules (those live in `specs/016-knowledge-module-core/spec.md`); it references them where coverage is required.

## User Scenarios & Testing *(mandatory)*

> These "user stories" treat the CI pipeline and future-developer-modifying-the-module as the consumers. Each story is a test-suite slice that can land independently and immediately starts catching regressions on every PR that touches the knowledge or diagnostic modules.

### User Story 1 - Global-admin template curation is regression-proof end-to-end (Priority: P1)

Every branch that changes `KnowledgeController` template actions, `TopicTemplate` invariants, `KnowledgeStructureTemplate` CRUD handlers, or the template tree editor JS (`wwwroot/js/knowledge/template-editor.js`, `templates-list.js`) automatically fails CI if any of the US1 acceptance scenarios in `specs/016-knowledge-module-core/spec.md` regresses.

**Why this priority**: US1 is the foundational catalog surface — if template CRUD breaks, every project onboard breaks with it. Today's coverage (3 tests) only checks list-renders-with-seed, detail-renders, and "Nueva plantilla" happy path; the priority-range editor, overlapping-range validation, archive lifecycle, reorder, and hard-delete block are completely untested in the browser.

**Independent Test**: Can be verified by running the new template test file in isolation — it authenticates a GlobalAdmin, walks through template list → detail → tree editing → archive → unarchive → re-edit, and exits green on a clean seeded database.

**Acceptance Scenarios**:

1. **Given** a GlobalAdmin logs in and opens the templates list, **When** the page settles, **Then** the seeded `Emprendimiento Básico` template is visible AND (new coverage) the list exposes its archived/active state visually AND a "Nueva plantilla" action is reachable.
2. **Given** the GlobalAdmin opens a template's detail view, **When** the tree has loaded, **Then** all four hierarchy levels render (Modules, Topics, Subjects, Resources) with seeded rows labeled in Spanish.
3. **Given** a template detail view, **When** the admin adds a Module, then a Topic under it, then a Subject under the topic, then a Resource with `ResourceType=Video`, **Then** each new node appears in the tree after save and survives a full-page reload.
4. **Given** a Topic detail/panel, **When** the admin fills High 80–100, Medium 50–79, Low 0–49 and saves, **Then** the three bands persist and the UI re-renders them in the priority-range editor on reload.
5. **Given** the same topic, **When** the admin enters overlapping bands (e.g., High 70–100, Medium 65–80) and attempts to save, **Then** the UI surfaces a field-level validation error (Spanish) AND the previously-saved bands are preserved.
6. **Given** a template with two modules, **When** the admin reorders them (drag/drop or up/down control), **Then** the new order persists across a full-page reload.
7. **Given** the template list with archived rows hidden by default, **When** the admin archives a template AND toggles "Show archived" (or equivalent Spanish-labeled control), **Then** the archived row reappears AND can be unarchived AND unarchiving restores it to the default view.
8. **Given** a template that has at least one project clone (the seeded `Emprendimiento Básico` fits), **When** the admin triggers hard-delete, **Then** the delete is blocked with a Spanish error message naming the "archivar en lugar de eliminar" guidance, AND the template row remains in the list.
9. **Given** a GlobalAdmin in the templates surface, **When** a ProjectCoordinator-only user tries to navigate to any `/Coordination/Knowledge/Templates/*` URL, **Then** they receive a 403/redirect (current `Templates_CoordinatorCannotAccess` passes; coverage must extend to the `Create`, `Detail`, `Edit` routes for each of Modules/Topics/Subjects/Resources).

---

### User Story 2 - Project-creation-with-KS-binding is regression-proof end-to-end (Priority: P1)

Every change to `CreateProjectCommand`, `IKnowledgeStructureProvisioner`, the Administration `Projects/Create` view, or any DACPAC constraint on `knowledge.KnowledgeStructures` or `tenant.Projects.KnowledgeStructureTemplateId` triggers a failing E2E assertion if the Phase 9 invariants regress.

**Why this priority**: This is the Phase 9 amendment's heart — the project MUST gain exactly one `KnowledgeStructure` atomically with its creation. If this slips (creation without KS, creation with two KS rows, non-matching bound template), downstream form-clone cascade and coordinator UX fall apart. Existing `ProjectCreationTests.cs` covers the form fields and redirect but does not verify the materialized KS or that the legacy clone-from-template UI is gone.

**Independent Test**: Can be verified by running the new project-KS-binding test file — it logs in as an IncubatorAdmin, creates a fresh project with a KS template selection, navigates to `/Coordination/Knowledge/Projects`, confirms a KS row was materialized for that project, confirms no "Clonar desde plantilla" action is visible, and confirms `/Coordination/Knowledge/Projects/Clone` returns a non-200-form response.

**Acceptance Scenarios**:

1. **Given** the seeded `coord1@test.mentoory.com` coordinator, **When** they open `/Coordination/Knowledge/Projects` after login with context selected, **Then** a row is visible for the seeded `Proyecto Innovación` with a KS already materialized (name column non-empty, detail link present).
2. **Given** an IncubatorAdmin on the project create form, **When** they omit the KS-template dropdown selection and submit, **Then** the form re-renders with a Spanish validation error citing "plantilla de conocimiento" AND no project row is created (verifiable via the projects list count before/after).
3. **Given** an IncubatorAdmin on the project create form, **When** they fill a unique name, select a KS template, and submit, **Then** the projects list shows the new row AND the coordinator (or the same admin using the coord-scoped view) can navigate to `/Coordination/Knowledge/Projects` AND sees a KS detail link for the new project.
4. **Given** the new project exists, **When** the coordinator opens the KS detail, **Then** the tree renders with the Modules/Topics/Subjects/Resources cloned from the selected template AND every node's name matches the template's seeded names AND the root `SyncMode` is displayed as `Desconectado` (Disconnected).
5. **Given** any coordinator on `/Coordination/Knowledge/Projects`, **When** the page settles, **Then** NO "Clonar desde plantilla" / "Clone from template" action is visible anywhere on the page (current `Projects_NoCloneFromTemplateButton_Phase9Regression` passes this; keep).
6. **Given** any authenticated user, **When** they GET `/Coordination/Knowledge/Projects/Clone`, **Then** the response is either a ≥400 status OR the body contains no clone form markup (no `SourceTemplateExternalId` input, no "Clonar desde plantilla" heading). (Current `CloneFromTemplate_LegacyRoute_NoLongerAccessible` passes this; keep.)

---

### User Story 3 - Cross-module form-clone cascade is regression-proof end-to-end (Priority: P1)

Every change to `CloneFormTemplateHandler`, `FormTemplate.DefaultKnowledgeStructureTemplateExternalId`, `diagnostic.Questions.TopicId` FK, or the project's KS-template binding must keep the topic-id rewrite invariant enforced — the test suite catches any drift before it reaches production.

**Why this priority**: This closes the `Questions.TopicId → knowledge.Topics.Id` FK hazard that previously silently tolerated dangling references. A regression here would let projects ship with questions pointing at template-topic ids (not project-topic ids), breaking future Mentoring Plan score aggregation at the root. No pure UI test exists for this cascade today; the only coverage is the handler-level `DiagnosticCascadeRoundTripTests.cs`.

**Independent Test**: Can be verified by running the new cascade test file — it creates (or reuses) a seeded `FormTemplate` bound to the same KS template the project is bound to, clones the form into the project via the coordinator UI, and then asserts (via a follow-up HTTP GET of a diagnostic detail endpoint OR an accompanying integration test that round-trips the cascade at the handler layer) that every `Question.TopicId` on the resulting `ProjectForm` resolves to a `knowledge.Topics` row whose `KnowledgeStructureId` belongs to the target project.

**Acceptance Scenarios**:

1. **Given** a seeded `FormTemplate` whose `DefaultKnowledgeStructureTemplateExternalId` matches the seeded project's bound KS template AND all its `Question.TopicId`s resolve to topics in that template, **When** the coordinator clones the form into the project (via the Diagnostic clone UI) and the operation returns success, **Then** the new `ProjectForm` is visible in the project's forms list AND (integration-test backstop) every `Question.TopicId` on the `ProjectForm` belongs to the project's `KnowledgeStructure`.
2. **Given** a seeded `FormTemplate` whose `DefaultKnowledgeStructureTemplateExternalId` points at a *different* KS template than the project's bound template, **When** the coordinator attempts to clone, **Then** the UI surfaces the exact Spanish error message `"Este formulario está diseñado para una estructura de conocimiento diferente a la del proyecto."` AND no `ProjectForm` row is created for the attempted clone.
3. **Given** a seeded `FormTemplate` with `DefaultKnowledgeStructureTemplateExternalId = NULL`, **When** the coordinator clones it into the project, **Then** the clone succeeds AND no new `KnowledgeStructure` row is created for the project (the project's KS count remains exactly 1, asserted either via an admin diagnostic-listing endpoint, via DB query in the integration backstop, or by absence of a new row in the coordinator's KS list after the clone).
4. **Given** the seeded project (which already has a KS materialized at creation), **When** the coordinator clones the same compatible form twice, **Then** both clones succeed AND exactly one `knowledge.KnowledgeStructures` row exists for that project (UNIQUE-constraint invariant; asserted via the integration backstop, since the coordinator UI does not expose row counts directly).

**Implementation note**: Scenarios 3 and 4 require DB-level assertions that are not exposed in the browser UI. The spec allows these to be covered by extending `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs` with the two missing cases (null-binding cascade, double-clone idempotency) — integration tests running against the same Testcontainers SQL instance as the E2E suite.

---

### User Story 4 - Coordinator project-tree editing (priority ranges, CRUD, delete-guard) is regression-proof end-to-end (Priority: P2)

Every change to the project-side knowledge tree editor (`wwwroot/js/knowledge/project-structure-editor.js`), `KnowledgeController` project-clone actions, or domain invariants on `Topic`/`Subject`/`Resource` triggers CI if a coordinator-level regression slips.

**Why this priority**: This is the coordinator's day-one workflow after project creation. Breaks here don't brick the platform (the project still exists), but they block every project onboard and every `TopicPriorityRangesChanged` downstream consumer when Mentoring Plan ships. P2 because it layers on top of US2's materialization guarantee.

**Independent Test**: Can be verified by running the new project-structure test file — it logs in as a coordinator, navigates to a project's KS detail, adds one node at each level, renames one cloned node, edits one topic's priority ranges, attempts to delete a topic referenced by a diagnostic question (must be blocked), then deletes a topic with no references (must succeed).

**Acceptance Scenarios**:

1. **Given** a coordinator on the project KS detail, **When** they add a new Module under the root, then a Topic under that module, then a Subject under the topic, then a Resource under the subject (each with a unique Spanish name), **Then** every node appears in the tree after save AND survives a full-page reload AND is flagged as clone-only (no "imported from template" badge, since no `SourceTemplateXExternalId` was stamped).
2. **Given** a coordinator on the project KS detail, **When** they rename a cloned-from-template topic (e.g., the seeded `Finanzas` → `Finanzas Básicas`) and save, **Then** the rename persists on the clone AND a subsequent visit to the template detail view (impersonating a GlobalAdmin in a separate browser context) shows the original template topic name unchanged (`Finanzas`).
3. **Given** a project topic with empty priority bands, **When** the coordinator enters High 80–100 / Medium 50–79 / Low 0–49 and saves, **Then** all three bands persist across reload AND the UI renders them in the order High → Medium → Low.
4. **Given** the same project topic, **When** the coordinator enters overlapping bands (High 70–100, Medium 65–80) and saves, **Then** the save is rejected with a Spanish field-level error AND the previously-saved bands are preserved.
5. **Given** a project topic that IS referenced by at least one diagnostic `Question.TopicId` (achievable by first running the US3 cascade happy-path), **When** the coordinator attempts to delete that topic, **Then** the UI surfaces a Spanish error naming the referencing-question count AND the topic row is still visible in the tree after the failed attempt.
6. **Given** a project topic that is NOT referenced by any diagnostic question, **When** the coordinator deletes it, **Then** the topic disappears from the tree AND survives a full-page reload as deleted.
7. **Given** two coordinators scoped to *different* projects, **When** coordinator A loads their project's KS detail via its ExternalId, **Then** the tree contents reflect only A's project AND attempting to GET coordinator B's KS detail by ExternalId returns a 404/403 (tenant isolation; currently no test covers this in the browser).

---

### User Story 5 - PartialSync from template is regression-proof end-to-end (Priority: P2)

Every change to `SyncFromTemplateHandler`, the `SyncMode` toggle, or the "Sincronizar desde plantilla" action triggers CI if sync semantics regress.

**Why this priority**: Useful for long-running projects when the catalog evolves. P2 because no v1 downstream consumer depends on it, but untested sync behavior today means silent drift later.

**Independent Test**: Can be verified by running the new sync test file — it uses a helper (either a seeded state, or an integration-test driven setup) to put the project KS in `PartialSync` mode, add one new topic at the template level, trigger sync from the coordinator UI, and then assert that the new topic appears under its matching parent in the clone AND that a pre-existing locally-added topic is untouched AND that a pre-existing locally-renamed topic retains its renamed value.

**Acceptance Scenarios**:

1. **Given** a project KS in `Disconnected` mode, **When** the coordinator opens the KS detail, **Then** the "Sincronizar desde plantilla" action is disabled OR absent AND the SyncMode toggle is visible.
2. **Given** the same KS, **When** the coordinator switches SyncMode to `PartialSync` and saves, **Then** the sync action becomes enabled AND survives a full-page reload.
3. **Given** a project KS in `PartialSync` mode AND a template that has gained a new topic since the clone was created (setup via integration-test harness or a second seed pass), **When** the coordinator triggers sync, **Then** the new topic appears under its matching clone-side parent module AND its position is `max(SortOrder)+1` AND the summary notification reports at least one topic added (text in Spanish, numeric count visible).
4. **Given** the project KS has a locally-added Module M_local (no `SourceTemplateModuleExternalId`), **When** sync runs, **Then** M_local remains present in the tree unchanged (name, sort-order, children).
5. **Given** the project KS has a topic T_renamed that was cloned from template and later renamed locally, **When** sync runs, **Then** T_renamed retains its local name AND its position is unchanged (sync never mutates existing cloned items).

**Implementation note**: Acceptance scenarios 3/4/5 require manipulating the template after the clone exists. Since the template-edit action is GlobalAdmin-scoped, the E2E test will either (a) log in as GlobalAdmin in a separate browser context and add the template row, then switch back to coordinator and sync; or (b) drive the template mutation through a handler-layer integration test and only use Playwright for the sync-trigger + visible-summary assertion. Choose whichever keeps the test robust and under 30s of wall time.

---

### User Story 6 - Authorization and tenant isolation are regression-proof end-to-end (Priority: P3)

Any change to `[Authorize(Roles=...)]` attributes on `KnowledgeController`, `ProjectsController.Create`, or the tenant query filter on the project-clone repository triggers CI if a role-scope or project-scope hole opens up.

**Why this priority**: Access-control regressions are the highest-severity failure mode (data leakage across tenants, template edits by unauthorized users) but the smallest incremental delta — a few well-placed negative-path tests cover the surface. P3 because each individual test is cheap but the cumulative coverage is strategic.

**Independent Test**: Can be verified by running the authorization test file — it drives unauthorized users against every protected route and asserts HTTP ≥ 400 or a redirect away from the protected URL.

**Acceptance Scenarios**:

1. **Given** a ProjectCoordinator user, **When** they GET each of `/Coordination/Knowledge/Templates`, `/Coordination/Knowledge/Templates/{externalId}`, and the equivalent `Create`/`Edit`/`Delete` routes for Modules/Topics/Subjects/Resources under templates, **Then** every response is ≥ 400 OR redirects away from the protected URL (current coverage handles only the list root).
2. **Given** an unauthenticated user, **When** they GET any `/Coordination/Knowledge/*` URL, **Then** the response redirects to `/Access/Login`.
3. **Given** coordinator A scoped to project P_A, **When** they GET `/Coordination/Knowledge/Projects/{P_B_KS_ExternalId}` (coordinator B's project KS detail by ExternalId), **Then** the response is ≥ 400 OR the view body does not render any of P_B's KS content (no module/topic names from P_B's clone appear).
4. **Given** an IncubatorAdmin with a known incubator scope, **When** they attempt to create a project under an incubator they do NOT administer (via form-hack / direct POST), **Then** the create is rejected with an authorization error AND no project row is created.
5. **Given** the menu (sidebar/navbar) rendered for each role, **When** a ProjectCoordinator logs in, **Then** the "Plantillas de conocimiento" entry is NOT visible AND the "Estructuras del proyecto" entry IS visible; **When** a GlobalAdmin logs in, **Then** both entries are visible.

---

### Edge Cases

**Seed data & test determinism:**

- **EC-01** — Every E2E test MUST reset its relevant slice of the DB before running (either by relying on the Testcontainers/DACPAC fresh-deploy-per-collection behavior, or by creating all test-local entities with unique GUIDs/names so they don't collide across runs). Tests MUST NOT depend on execution order within the collection.
- **EC-02** — Tests that mutate the seeded templates (e.g., US1 hard-delete block) MUST either run against a test-local template they themselves created OR restore the seed state on teardown. No test may leave the seeded `Emprendimiento Básico` template archived/deleted.
- **EC-03** — Tests that create new projects (US2 create happy path, US5 sync) MUST use a unique project name per run (e.g., `$"E2E Project {Guid.NewGuid():N}"`) to avoid colliding with repeat runs in the same DB.

**Browser / Playwright reliability:**

- **EC-10** — Every test MUST take a screenshot on failure (existing pattern: `_fixture.TakeScreenshotOnFailureAsync`). Screenshot filenames include the test-method name and UTC timestamp; they land in `screenshots/` next to the test binaries.
- **EC-11** — Every test MUST await `LoadState.NetworkIdle` after navigation AND use `WaitForFunctionAsync` on dropdowns that populate via XHR before interacting with them (existing pattern in `LoginAndSelectContextAsync`). Raw `Task.Delay` / fixed-interval sleeps are forbidden.
- **EC-12** — Tests MUST NOT share a `Page` across test methods; each test creates a page and disposes its context in the `finally` block (existing pattern).

**Cross-module coordination:**

- **EC-20** — US3 (cascade) and US4 (delete-guard) both require a `Question.TopicId` reference to exist against a project topic. Tests MUST either set this up via the UI (clone form with bound KS) or via an integration-test harness; they MUST NOT poke the DB directly via raw SQL because the FK + trigger logic is part of the behavior under test.
- **EC-21** — US5 (PartialSync) requires mutating the template after a clone exists. If done via UI, the test runs two browser contexts (GlobalAdmin + Coordinator) sequentially or in parallel, and waits for template mutation to commit before triggering the sync.

**Language & UI conventions:**

- **EC-30** — Every user-visible string the tests assert on MUST be the Spanish text. Tests MAY additionally assert on HTML element attributes (`name=`, `data-*`) that are not user-visible.
- **EC-31** — Error-path assertions MUST match the exact Spanish error message where the spec fixes it verbatim (e.g., US3 scenario 2's `"Este formulario está diseñado para una estructura de conocimiento diferente a la del proyecto."`). For other validation messages, tests MAY match a substring that identifies the field.

## Requirements *(mandatory)*

### Functional Requirements

**Test organization & location:**

- **FR-T01** — New Playwright test files MUST live under `tests/Mentoory.Tests.E2E/Tests/` and use the `[Collection(E2ETestCollection.Name)]` attribute so they share the `PlaywrightFixture` (SQL container, DACPAC, Kestrel, browser).
- **FR-T02** — New test files MUST be named by the user-story they cover, following the pattern `KnowledgeTemplate<Concern>Tests.cs` for US1, `Knowledge<Concern>Tests.cs` for US2/US3/US4/US5, and `Knowledge<Concern>AuthorizationTests.cs` for US6. Existing files `KnowledgeTemplatesTests.cs` and `KnowledgeProjectStructureTests.cs` MAY be extended in-place (preferred when the new scenarios share helpers) OR split into new files (preferred when the file would exceed ~250 lines).
- **FR-T03** — Integration-level backstops (US3 scenarios 3+4, US5 template-mutation helper) MUST live under `tests/Mentoory.Tests.Integration/Knowledge/` and extend the existing `DiagnosticCascadeRoundTripTests` suite OR create a new sibling file (e.g., `ProjectKsPartialSyncTests.cs`).

**Scenario coverage — must-pass matrix:**

- **FR-T10** — Every numbered acceptance scenario from User Stories 1 through 6 above MUST be covered by at least one automated test. "Covered" means: the test drives the same action described in the scenario, reaches the same end state, and asserts on the same observable outcome.
- **FR-T11** — Tests MUST distinguish positive-path (success) and negative-path (validation/authorization failure) assertions; negative-path tests MUST assert BOTH the error surface (HTTP status, Spanish error text, or redirect target) AND the absence of side-effects (no row created, previous state preserved).
- **FR-T12** — Tests asserting absence of UI elements (e.g., US2 scenario 5: no "Clonar desde plantilla" button) MUST use `.CountAsync() == 0` against a broadly-selected locator, not a narrow one, to avoid false greens when the selector changes.

**Data & state management:**

- **FR-T20** — The shared `PlaywrightFixture` DACPAC deploy MUST include (via the existing `004.SeedTestData.sql` / `005.SeedKnowledgeData.sql`) the entities these tests depend on: GlobalAdmin user, at least one Coordinator user, at least one IncubatorAdmin user, at least one KnowledgeStructureTemplate with a complete four-level tree, at least one project bound to that template, and at least one FormTemplate bound to that same template. Tests MUST NOT require the developer to run any manual SQL to enable them.
- **FR-T21** — Tests that create new state (new template, new project, new module/topic/subject/resource, new form clone) MUST use unique identifiers (name includes `Guid.NewGuid():N`) to avoid collisions across reruns inside the same container lifetime.
- **FR-T22** — Tests MUST NOT assume any specific order of records in lists they query; assertions MUST match against entity names/ExternalIds, not list indices.

**Reliability & performance budgets:**

- **FR-T30** — Each test MUST complete in under 30 seconds of wall time on the reference CI runner. Tests exceeding this limit MUST be split or the setup MUST be moved into a fixture-level cache (e.g., pre-logged-in browser contexts shared across tests).
- **FR-T31** — The total new E2E test wall-time for this spec (US1–US6 combined) SHOULD be under 6 minutes on the reference CI runner. The PlaywrightFixture's one-time DACPAC + Kestrel + browser boot cost (~30–60s) counts once, not per-test.
- **FR-T32** — Flaky tests (fail rate > 2% over 50 consecutive CI runs) MUST be quarantined via `[Trait("Quarantine","true")]` and a tracking issue opened; they MUST NOT be silently retried.

**Observability:**

- **FR-T40** — On failure, every test MUST capture a full-page screenshot using the existing `_fixture.TakeScreenshotOnFailureAsync` helper, named for the test method.
- **FR-T41** — Tests that perform multi-step flows (login + context select + navigate + interact + assert) SHOULD log a one-line breadcrumb at each step (e.g., `Console.WriteLine`) so that CI logs make failure points diagnosable without rerunning locally.

**Authorization coverage:**

- **FR-T50** — The authorization test file MUST drive at least one negative-path request per protected route under `/Coordination/Knowledge/Templates/**` and `/Coordination/Knowledge/Projects/**`. The test MAY use a parameterized theory (xUnit `[Theory]` + `[InlineData]`) to keep the file concise.
- **FR-T51** — Tenant-isolation tests (US6 scenario 3) MUST use two seeded projects under two seeded coordinators, each in a different incubator, so that cross-project leakage actually tests the tenant filter (not an incidental incubator filter).

### Non-Functional Requirements

- **NFR-T01** — New tests MUST compile with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (repo convention).
- **NFR-T02** — New tests MUST NOT introduce dependencies on additional NuGet packages beyond those already in `Mentoory.Tests.E2E.csproj` (Playwright, xUnit, FluentAssertions, Testcontainers, DacFx).
- **NFR-T03** — The DACPAC + seed data changes required to support these tests (if any) MUST ship in the same PR as the tests themselves so the PR is self-contained and revertable.
- **NFR-T04** — Helpers shared across test files (login-and-select-context, assertion helpers, screenshot wrappers) MUST live in a dedicated helpers class under `tests/Mentoory.Tests.E2E/Infrastructure/` so they are reused, not copy-pasted. Current `LoginAndSelectContextAsync` copies are acceptable as a baseline; the PR MUST either extract them to a shared helper OR document the duplication as intentional.
- **NFR-T05** — Tests MUST be independent of local machine locale or timezone; any date/time assertions MUST work identically in UTC and in non-UTC runners.

### Key Entities

- **Test file** — A `.cs` file under `tests/Mentoory.Tests.E2E/Tests/` or `tests/Mentoory.Tests.Integration/Knowledge/`, containing one xUnit test class scoped to a single user story.
- **Test scenario** — A single `[Fact]` or `[Theory]` test method that covers one numbered acceptance scenario from this spec. One-to-one mapping, with exceptions allowed when two sibling scenarios naturally collapse into a single Given/When/Then flow.
- **Seed fixture** — The DACPAC's `004.SeedTestData.sql` + `005.SeedKnowledgeData.sql` PostDeployment scripts, deployed once per `PlaywrightFixture` lifetime, providing users, incubator, project, KS template, project KS, and form template rows these tests depend on.
- **Spanish error string** — A verbatim user-visible error message anchored in a specific FR/EC of `specs/016-knowledge-module-core/spec.md`, asserted by exact match in the corresponding negative-path test.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-T01** — Every numbered acceptance scenario in User Stories 1 through 6 (a total of ~33 scenarios) is covered by at least one passing automated test; the coverage matrix is documented in the test file header (XML doc comment) so reviewers can verify one-to-one alignment with this spec.
- **SC-T02** — The combined new E2E suite (Template + Project + Cascade + Coordinator-Tree + PartialSync + Authorization) runs green on a fresh DACPAC deploy in under 6 minutes of wall time on the reference CI runner.
- **SC-T03** — The combined suite exercises at least one positive-path AND one negative-path assertion for every writable surface in `KnowledgeController` (template CRUD, project-clone CRUD, sync) and for the Phase 9 project-creation KS binding (`ProjectsController.Create`).
- **SC-T04** — Mutation testing (or manual regression via reverting one behavior at a time on a scratch branch) demonstrates that removing any single Phase 9 invariant — e.g., dropping the UNIQUE constraint on `KnowledgeStructures.ProjectId`, or returning success instead of the mismatch error in `CloneFormTemplateHandler`, or removing the `[Authorize(Roles="GlobalAdmin")]` on templates — causes at least one new test in this suite to fail. If no test catches the mutation, coverage MUST be extended before this spec is considered complete.
- **SC-T05** — Over 50 consecutive CI runs on unrelated PRs, the combined suite has a pass rate ≥ 98% (at most one flake). Flakes above this threshold trigger FR-T32 quarantine.
- **SC-T06** — A new developer who has never touched the Knowledge module can read the test-file XML doc comments and the scenario names AND identify, within 15 minutes, which business invariant is under test and where the corresponding spec section lives (spec → test traceability is an explicit deliverable, not implicit).

## Assumptions

- **Playwright/Chromium headless in CI** — The reference CI runner has Playwright's Chromium browser bundled (existing fixture already launches `Chromium.LaunchAsync(new { Headless = true })`). No additional browser setup is required by this spec.
- **Testcontainers MSSQL image availability** — The CI environment can pull `mcr.microsoft.com/mssql/server:2022-latest` on-demand (existing fixture does this). Offline runners are out of scope.
- **Seeded users' passwords** — The seed uses `Test123!@#` across all test-user accounts (current convention in `004.SeedTestData.sql` and the existing tests). Changes to the seed password are out of scope for this spec and would require updating every E2E test file's login helper.
- **Spanish strings are stable** — User-visible Spanish strings the negative-path tests assert on will not be retranslated inside the 016-branch PR window. If later retranslation occurs, the test-suite maintainer MUST update the test assertions as part of the retranslation PR.
- **Phase 9 amendment is in scope** — This spec assumes the Phase 9 Project-owned KS binding lands with the 016-branch PR. If the amendment is split out to a separate PR, User Stories 2, 3, 4, and the US5 setup path MUST be re-scoped accordingly (legacy "Clone from template" UI would reappear and would need its own coverage).
- **Cross-browser coverage is out of scope for v1** — Tests run against Chromium only. Firefox / WebKit coverage MAY be added in a follow-up spec if cross-browser regressions emerge; it is NOT required by this spec.
- **Mobile viewport coverage is out of scope for v1** — The fixture's fixed `1280 × 720` viewport is the only covered form factor. Mobile-responsive assertions belong in the design-system polish specs (010–012), not here.
