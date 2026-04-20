---

description: "Task list for 017-audit-e2e"
---

# Tasks: Audit Pipeline — Browser E2E Coverage

**Input**: Design documents from `/specs/017-audit-e2e/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED — this feature's sole deliverable IS test code. Every task under a user-story phase produces a test method or a shared test-infrastructure helper.

**Organization**: Tasks are grouped by user story to enable independent implementation. Each user-story phase corresponds to exactly one **session checkpoint** (P1-P4) per `contracts/session-checkpoint-protocol.md`. The session that opens a phase MUST close it with the seven-step exit ritual before ending.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel within a single session (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- All file paths are repository-relative to `/mnt/D/repos/mentoory-ps-audit/`

## Path Conventions

- Test project: `tests/Mentoory.Tests.E2E/` — all new test code lives here
- Spec artifacts: `specs/017-audit-e2e/` — RESUME files, retrospectives
- No files outside these two trees may be created or modified (SC-005)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Extend the Playwright fixture and add shared helpers needed by every subsequent phase. All Phase 1 tasks MUST complete within the **Session P1** AI session (same session that delivers US1).

- [ ] T001 Extend `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` to add a `Respawner` matching the integration fixture: `SchemasToInclude = ["access", "tenant", "diagnostic", "example", "subscription", "audit"]`. Expose a public `ResetDatabaseAsync()` method. Wire it into a new `E2ETestBase` class (or a per-class `IAsyncLifetime.InitializeAsync` call) so every test method starts against a respawned DB. Source of truth: `tests/Mentoory.Tests.Integration/Fixtures/MentooryWebApplicationFactory.cs` lines 62-67 + 141-146. Verify by running one `AuditLogViewerTests` test twice and confirming no cross-pollution.
- [ ] T002 [P] Create `tests/Mentoory.Tests.E2E/Infrastructure/LoginHelper.cs` — a static class exposing `Task LoginAsync(IPage page, string email, string password, string baseUrl)`. Copy the `LoginAsync` method body verbatim from `tests/Mentoory.Tests.E2E/Tests/AuthorizationTests.cs` (lines 124-161), including the GlobalAdmin context-selector handling. Delete the inline copy from `AuthorizationTests.cs` and make it call `LoginHelper.LoginAsync(...)` instead, preserving its screenshot-on-failure wrapper.
- [ ] T003 [P] Create `tests/Mentoory.Tests.E2E/Infrastructure/AuditSpanishCopy.cs` — a static class holding every Spanish string the audit tests will assert. Constants MUST include at minimum: column headers (`FechaUtcHeader = "Fecha (UTC)"`, `EventoHeader = "Evento"`, `UsuarioHeader = "Usuario"`, `AccionHeader = "Acción"`, `ResultadoHeader = "Resultado"`, `RolHeader = "Rol"`), filter buttons (`FiltrarButton = "Filtrar"`, `LimpiarButton = "Limpiar"`), Outcome options (`TodosOption = "Todos"`, `ExitoOption = "Éxito"`, `FalloOption = "Fallo"`), pagination labels (`Primero`, `Siguiente`, `Anterior`, `Ultimo`), empty-state copy (match existing copy rendered by `datatable-helper.js`), menu entry (`RegistroDeAuditoriaMenu = "Registro de auditoría"`), page title (`PageTitle = "Registro de auditoría"`).

**Checkpoint**: `dotnet build tests/Mentoory.Tests.E2E/` succeeds with zero new warnings. Running `AuthorizationTests` still passes using the shared `LoginHelper`. Ready to start US1 tests.

---

## Phase 2: User Story 1 - Viewer, menu, and authorization (Priority: P1) 🎯 MVP

**Session**: P1 (same session as Setup above)
**Goal**: Prove the admin audit-log viewer renders for GlobalAdmin, is denied to every lower role, and exposes the correct Spanish shell + menu visibility.
**Independent Test**: `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLogViewerTests" -p:WarningsNotAsErrors=NU1902` — green means the viewer's authorization and Spanish shell are intact.
**File produced**: `tests/Mentoory.Tests.E2E/Tests/AuditLogViewerTests.cs` (9 tests, one class)

### Tests for US1

- [ ] T004 [US1] Create `tests/Mentoory.Tests.E2E/Tests/AuditLogViewerTests.cs` as a new xUnit test class with `[Collection(E2ETestCollection.Name)]` and a constructor taking `PlaywrightFixture`. Inherit `E2ETestBase` if created in T001; otherwise call `fixture.ResetDatabaseAsync()` in the constructor. Add the `GlobalAdmin_CanOpenAuditLog_TableRenders` test: login as `admin@mentoory.com / 123abc987`, navigate to `/Administration/AuditLog`, assert response status 200, assert every Spanish column header from `AuditSpanishCopy` appears in `#auditLogTable thead th`. All Playwright waits use ≤ 15 s timeouts.
- [ ] T005 [US1] Add `IncubatorAdmin_IsDenied` test to `AuditLogViewerTests.cs`: login as `incadmin1@test.mentoory.com / Test123!@#`, navigate to `/Administration/AuditLog`, assert response status 403 OR URL contains `/AccessDenied` OR `/Access/Login`.
- [ ] T006 [US1] Add `Entrepreneur_IsDenied` test to `AuditLogViewerTests.cs` — same shape as T005 but with `entrepreneur1@test.mentoory.com / Test123!@#`.
- [ ] T007 [US1] Add `Mentor_IsDenied` test to `AuditLogViewerTests.cs` — same shape with `mentor1@test.mentoory.com / Test123!@#`.
- [ ] T008 [US1] Add `Sponsor_IsDenied` test to `AuditLogViewerTests.cs` — same shape with `sponsor1@test.mentoory.com / Test123!@#`.
- [ ] T009 [US1] Add `GlobalAdmin_SeesMenuEntryUnderPlataforma` test to `AuditLogViewerTests.cs`: login as GlobalAdmin, navigate to home `/`, locate the Plataforma menu group (existing selector pattern in the Tabler sidebar — use `nav [data-menu-group='Plataforma']` or equivalent), assert `Registro de auditoría` text is present inside that group.
- [ ] T010 [US1] Add `IncubatorAdmin_DoesNotSeeMenuEntry` test to `AuditLogViewerTests.cs`: login as IncubatorAdmin, navigate to `/`, assert the `Registro de auditoría` text is NOT present anywhere in the rendered menu (Playwright `locator(...).count()` returns 0).
- [ ] T011 [US1] Add `FilterWithNoMatch_ShowsSpanishEmptyState` test to `AuditLogViewerTests.cs`: login as GlobalAdmin, navigate to `/Administration/AuditLog`, open the filter panel, set UserEmail filter to a random GUID (guaranteed no match), click Filtrar, wait for the table to refresh, assert the empty-state Spanish copy from `AuditSpanishCopy` is visible inside `#auditLogTable tbody`.
- [ ] T012 [US1] Add `OutcomeDropdown_HasSpanishOptions` test to `AuditLogViewerTests.cs`: login as GlobalAdmin, navigate to `/Administration/AuditLog`, open the filter panel, locate `select[name='outcome']`, assert its `<option>` elements exactly match `["Todos", "Éxito", "Fallo"]` in order.

### Phase 2 Checkpoint (end of Session P1)

- [ ] T013 [US1] **Session P1 exit ritual**: run `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLogViewerTests" -p:WarningsNotAsErrors=NU1902` — must be 9/9 green. Run `dotnet test tests/Mentoory.Shared.Application.Tests/ tests/Mentoory.Tests.Architecture/ tests/Mentoory.Tests.Integration/ -p:WarningsNotAsErrors=NU1902` — must be all green. Commit with title `Add E2E coverage for audit pipeline — phase 1 (Viewer shell)` (body lists the 9 tests + 3 helpers). Push to origin. Write `specs/017-audit-e2e/RESUME-P2.md` per `contracts/resume-prompt-schema.md`. Commit + push the RESUME. **End the session — do NOT start US2 in the same context.**

---

## Phase 3: User Story 2 - UI-driven capture flows + redaction (Priority: P1)

**Session**: P2 (fresh session — claimed by reading `RESUME-P2.md`)
**Goal**: Drive the real UI for Register, Login (success + failure), Assign Role, and Correct Answer, then assert each action produces a correctly-shaped row in the viewer with correct Outcome, UserEmail, and (for Register) password redaction.
**Independent Test**: `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLogCaptureTests" -p:WarningsNotAsErrors=NU1902` — green means the capture pipeline works end-to-end from the browser.
**File produced**: `tests/Mentoory.Tests.E2E/Tests/AuditLogCaptureTests.cs` (7 tests, one class)

### Kickoff gate for Session P2

- [ ] T014 [US2] **Session P2 kickoff**: read `specs/017-audit-e2e/spec.md` + `specs/017-audit-e2e/RESUME-P2.md`. Run the baseline check `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog" -p:WarningsNotAsErrors=NU1902` — must be 9/9 green before beginning. If any P1 test is red, STOP and flag.

### Tests for US2

- [ ] T015 [US2] Create `tests/Mentoory.Tests.E2E/Tests/AuditLogCaptureTests.cs` with the fixture collection attribute and respawn setup. Add `RegisterUser_ProducesAuditRow_WithRedactedPassword` test: navigate to `/Access/Register`, fill the form (email `e2e-register@test.local`, country, identification, first/last name, password `SecureP@ss123!`), submit. Login as GlobalAdmin, navigate to `/Administration/AuditLog`, filter by UserEmail `e2e-register@test.local`, assert exactly one row with `EventType = User.Registered` and `Outcome = Éxito` (use `AuditSpanishCopy.ExitoOption`). Click the expand button, read the `<pre>` Details panel, assert it contains `***REDACTED***` AND does NOT contain the literal `SecureP@ss123!`.
- [ ] T016 [US2] Add `LoginWithInvalidPassword_ProducesFailureRow` test to `AuditLogCaptureTests.cs`: navigate to `/Access/Login`, submit with `admin@mentoory.com` and wrong password `WrongP@ss!`. Login as GlobalAdmin in a new browser context, navigate to the viewer, filter by UserEmail and EventType, assert one row with `EventType = User.LoggedIn`, `Outcome = Fallo`.
- [ ] T017 [US2] Add `LoginWithValidPassword_ProducesSuccessRow` test to `AuditLogCaptureTests.cs`: navigate to `/Access/Login`, login as `mentor1@test.mentoory.com / Test123!@#`. Then login as GlobalAdmin in a new browser context, open the viewer, filter by that UserEmail, assert one `EventType = User.LoggedIn` / `Outcome = Éxito` row.
- [ ] T018 [US2] Add `AssignRole_ProducesAuditRow` test to `AuditLogCaptureTests.cs`: login as GlobalAdmin, navigate to the admin users page (reuse the role-assignment flow from `tests/Mentoory.Tests.E2E/Tests/AdministrationUsersTests.cs`), assign a role to a test user. Navigate to the viewer, filter by `EventType = Role.Assigned`, assert one row with `Outcome = Éxito` and `UserEmail = admin@mentoory.com` (the actor).
- [ ] T019 [US2] Add `CorrectAnswer_ProducesRowWithBeforeAndAfter` test to `AuditLogCaptureTests.cs`: arrange via `DiagnosticDbContext` (seed a FormTemplate + ProjectForm + DiagnosticResponse with one text-type QuestionResponse valued `"OriginalAnswer"`) — pattern from `tests/Mentoory.Tests.Integration/Audit/CorrectAnswerAuditTests.cs`. Login as a ProjectCoordinator (or seed one), navigate to `/Coordination/AnswerCorrection/{externalId}`, submit the Correct modal with `newTextValue=FixedAnswer`. Login as GlobalAdmin in a new context, open the viewer, filter by `EventType = Answer.Corrected`, expand the row, assert the Details `<pre>` contains both `"OriginalAnswer"` and `"FixedAnswer"` (substring match, order-insensitive).
- [ ] T020 [US2] Add `CorrectAnswer_ProducesExactlyOneRow_NoDoubleWrite` test to `AuditLogCaptureTests.cs`: same arrange as T019 (fresh respawn guarantees isolation), dispatch ONE Correct action, login as GlobalAdmin, open the viewer, filter by `Action = CorrectAnswerCommand` (or by `EventType = Answer.Corrected` + the specific entity id), assert `.dt-info` shows exactly `1` record OR `tbody tr` count equals 1.
- [ ] T021 [US2] Add `ExpandButton_RevealsPrettyPrintedDetails` test to `AuditLogCaptureTests.cs`: trigger any audited command (reuse Login, simplest), login as GlobalAdmin, open the viewer, click `.audit-expand` on the first row, wait for the `<pre>` child row to appear, assert its `innerText` parses as valid JSON via `JsonDocument.Parse` and that formatted output contains newlines (pretty-printed, not one-line).

### Phase 3 Checkpoint (end of Session P2)

- [ ] T022 [US2] **Session P2 exit ritual**: run US2 tests — must be 7/7 green. Run lower-layer suites — all green. Commit with title `Add E2E coverage for audit pipeline — phase 2 (Capture flows)`. Push. Write `specs/017-audit-e2e/RESUME-P3.md`. Commit + push RESUME. **End the session.**

---

## Phase 4: User Story 3 - Correlation propagation (Priority: P2)

**Session**: P3 (fresh — claimed by reading `RESUME-P3.md`)
**Goal**: Prove the correlation middleware is wired in the production pipeline, visible from the browser.
**Independent Test**: `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLogCorrelationTests" -p:WarningsNotAsErrors=NU1902` — green means the correlation contract holds at the HTTP boundary.
**File produced**: `tests/Mentoory.Tests.E2E/Tests/AuditLogCorrelationTests.cs` (3 tests, one class)

### Kickoff gate for Session P3

- [ ] T023 [US3] **Session P3 kickoff**: read spec + `RESUME-P3.md`. Run `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog" -p:WarningsNotAsErrors=NU1902` — must be 16/16 green (P1's 9 + P2's 7). If any red, STOP.

### Tests for US3

- [ ] T024 [US3] Create `tests/Mentoory.Tests.E2E/Tests/AuditLogCorrelationTests.cs` with the fixture collection attribute. Add `AnyGetResponse_CarriesValidGuidCorrelationId` test: navigate to `fixture.BaseUrl + "/"` without setting any headers, capture the navigation `IResponse`, assert `response.Headers` contains key `"x-correlation-id"` (case-insensitive) and that its value parses as a valid `Guid`.
- [ ] T025 [US3] Add `ClientProvidedCorrelationId_IsEchoedVerbatim` test to `AuditLogCorrelationTests.cs`: create a new Playwright browser context, set `ExtraHTTPHeaders = { ["X-Correlation-Id"] = "{specific known guid}" }` via `CreateBrowserContextAsync` options, navigate to `/`, capture the response, assert the returned `x-correlation-id` header equals the known GUID.
- [ ] T026 [US3] Add `TwoRequestsWithoutHeader_GetDistinctCorrelationIds` test to `AuditLogCorrelationTests.cs`: issue two sequential navigations without the header (two separate pages, or one page and one `page.RequestAsync(...)` for a raw request), capture both responses, assert both carry valid-GUID `x-correlation-id` headers AND the two values differ.

### Phase 4 Checkpoint (end of Session P3)

- [ ] T027 [US3] **Session P3 exit ritual**: run US3 tests — 3/3 green. Lower-layer suites — green. Commit with title `Add E2E coverage for audit pipeline — phase 3 (Correlation)`. Push. Write `specs/017-audit-e2e/RESUME-P4.md`. Commit + push RESUME. **End the session.**

---

## Phase 5: User Story 4 - Negative regressions + Spanish QA (Priority: P3)

**Session**: P4 (fresh — claimed by reading `RESUME-P4.md`)
**Goal**: Encode invariants that should never regress. Most overlap with unit/integration coverage; existing at the E2E layer catches regressions that slip past lower tiers.
**Independent Test**: `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLogRegressionTests" -p:WarningsNotAsErrors=NU1902` — green means the defensive invariants hold at the browser layer.
**File produced**: `tests/Mentoory.Tests.E2E/Tests/AuditLogRegressionTests.cs` (5 tests, one class)

### Kickoff gate for Session P4

- [X] T028 [US4] **Session P4 kickoff**: read spec + `RESUME-P4.md`. Run full `AuditLog*` suite — must be 19/19 green. Lower-layer suites green. If any red, STOP.

### Tests for US4

- [X] T029 [US4] Create `tests/Mentoory.Tests.E2E/Tests/AuditLogRegressionTests.cs`. Add `NoPlaintextPasswordAppearsInAnyDetails` test: register a test user via `/Access/Register` with password `SecureP@ss123!`, login as GlobalAdmin, open the viewer, enumerate ALL `.audit-expand` buttons on the current page, click each, concatenate every `<pre>` Details body text, assert the concatenation does NOT contain the literal `SecureP@ss123!`.
- [X] T030 [US4] Add `FilterBarButtons_HaveSpanishLabels` test to `AuditLogRegressionTests.cs`: login as GlobalAdmin, open the viewer filter panel, assert `button[type='submit']` text exactly equals `AuditSpanishCopy.FiltrarButton` and `.filter-clear-btn` text equals `AuditSpanishCopy.LimpiarButton`.
- [X] T031 [US4] Add `PaginationControls_HaveSpanishLabels` test to `AuditLogRegressionTests.cs`: login as GlobalAdmin, open the viewer (seed enough audit rows via repeated command dispatch if needed to surface pagination — or assert on `.dt-paging` container text patterns regardless of row count), assert the rendered pagination controls contain `Primero`, `Siguiente`, `Anterior`, `Ultimo` (from `AuditSpanishCopy` constants).
- [X] T032 [US4] Add `OutcomeBadges_RenderSpanishTextAndColor` test to `AuditLogRegressionTests.cs`: arrange one success and one failure audit row (dispatch a successful login + an invalid login), login as GlobalAdmin, open the viewer, locate each row's Outcome cell. Assert the success row's badge contains `AuditSpanishCopy.ExitoOption` AND carries `.status-success` class; the failure row contains `AuditSpanishCopy.FalloOption` AND carries `.status-danger` class.
- [X] T033 [US4] Add `ValidationRejectedCommand_ProducesNoAuditRow` test to `AuditLogRegressionTests.cs`: navigate to `/Access/Register`, submit with a deliberately invalid email (e.g., `not-an-email`) so FluentValidation rejects before the handler fires, observe the form validation error on-page. Login as GlobalAdmin, open the viewer, assert NO audit row with that email exists. This proves the pipeline order `Validator → Auditing → Transaction` — validation short-circuits before Auditing runs.

### Phase 5 Polish (same Session P4)

- [X] T034 [US4] Run the full `AuditLog*` E2E suite end-to-end: `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog" -p:WarningsNotAsErrors=NU1902`. Confirm 24/24 green. Confirm wall-clock runtime ≤ 3 minutes (SC-002). Document actual runtime in the commit body and in `RESUME-COMPLETE.md`.
- [X] T035 [US4] Run `git diff develop..HEAD -- ':!tests' ':!specs'` and confirm zero output (SC-005). If output exists, flag the deviation in the commit body AND in `RESUME-COMPLETE.md § Deviations from spec`.

### Phase 5 Terminal Checkpoint (end of Session P4)

- [X] T036 [US4] **Session P4 terminal exit ritual**: run all 24 `AuditLog*` tests — green. Run lower-layer suites — green. Run full E2E suite (`tests/Mentoory.Tests.E2E/`) — green (prove no regression in the existing 97 tests). Commit with title `Add E2E coverage for audit pipeline — phase 4 (Regressions + Spanish QA)`. Push. Write `specs/017-audit-e2e/RESUME-COMPLETE.md` (retrospective structure per `contracts/resume-prompt-schema.md § RESUME-COMPLETE.md special structure`) — include coverage delivered, deviations, aggregated gotchas from P1-P4, suggested follow-ups. Commit + push `RESUME-COMPLETE.md`. **End the session.**

---

## Dependencies & Execution Order

### Session-level dependencies (HARD)

```
Session P1 (Setup + US1) ──►  Session P2 (US2) ──►  Session P3 (US3) ──►  Session P4 (US4 + Polish)
                                      ▲                  ▲                         ▲
                                      │                  │                         │
                                 reads RESUME-P2    reads RESUME-P3           reads RESUME-P4
```

Each session ends with a checkpoint commit + RESUME write; the next session MUST be a fresh AI context.

### Within-session task ordering

- **Phase 1 (Session P1)**: T001 before everything (fixture respawn is a prerequisite for every test isolation). T002 and T003 can run [P] in parallel with each other (different files) but both after T001. T004-T012 after all helpers exist. T013 is the final session task.
- **Phase 3 (Session P2)**: T014 kickoff, then T015-T021 (each test independent — could be [P] if different engineers, but single session = sequential). T022 final.
- **Phase 4 (Session P3)**: T023 kickoff, T024-T026 (sequential), T027 final.
- **Phase 5 (Session P4)**: T028 kickoff, T029-T033, T034-T035 polish, T036 terminal.

### Parallel opportunities (within a single session)

- T002 || T003 (different new files, both after T001)
- Within each US test class, tests that share arrangement can be marked [P] but within one session they run sequentially anyway. The [P] marker here is mostly cosmetic.

---

## Implementation Strategy

### Four-session delivery (the checkpoint protocol)

1. **Session P1**: claim the feature. Complete Phase 1 (T001-T003 setup) + Phase 2 (T004-T013 US1). Session ends with commit `phase 1 (Viewer shell)` + `RESUME-P2.md`.
2. **Session P2**: claim by reading `RESUME-P2.md`. Complete Phase 3 (T014-T022). Session ends with `phase 2 (Capture flows)` + `RESUME-P3.md`.
3. **Session P3**: claim by reading `RESUME-P3.md`. Complete Phase 4 (T023-T027). Session ends with `phase 3 (Correlation)` + `RESUME-P4.md`.
4. **Session P4**: claim by reading `RESUME-P4.md`. Complete Phase 5 (T028-T036). Session ends with `phase 4 (Regressions + Spanish QA)` + `RESUME-COMPLETE.md`.

### MVP (if resources constrained)

Stop after Session P1. 9 viewer/authorization/menu tests ship; the critical user-visible surface is covered. The remaining phases are additive value.

### Incremental Delivery

Each session-commit could be merged to `develop` independently once reviewed — the feature branch holds four reviewable increments, not one monolithic PR.

---

## Notes

- Every test task produces exactly one test method (or a small cluster of closely-related methods) in a named file. File paths are absolute-from-repo-root.
- The checkpoint protocol is load-bearing: the commit titles are greppable per SC-004; the RESUME files are the handoff contract per `contracts/resume-prompt-schema.md`.
- Partial-phase recovery is covered by FR-011 — a session that runs out of context MUST write `RESUME-P{N}.md` with `Status: in-progress` and the pending test-method list.
- All Playwright waits MUST specify explicit timeouts ≤ 15 seconds (FR-015).
- No production code changes are permitted across the four phases (SC-005, FR-017).
