# Phase 0 Research: E2E coverage for Project Lifecycle (feature 016)

**Feature**: `016-project-lifecycle-finish/e2e`
**Date**: 2026-04-19

Resolves the three open threads from `brainstorm/11-e2e-lifecycle-coverage.md` plus two test-design questions that emerged from reading `PlaywrightFixture.cs`. Each entry follows the Decision / Rationale / Alternatives format.

---

## R1. Programmatic project seeding through `PlaywrightFixture`

**Decision**: Use `fixture.Services.CreateScope()` to resolve `IMediator`, `TenantDbContext`, and `IProjectRepository`. Seed projects by sending existing application commands (`CreateIncubatorCommand`, `CreateProjectCommand`) and advance them by sending `AdvanceProjectStageCommand` as many times as needed to reach the target stage. No product-code changes; no test hook.

**Rationale**:

- `PlaywrightFixture` inherits from `WebApplicationFactory<Program>` (line 25 of `PlaywrightFixture.cs`) and calls `CreateDefaultClient()` during `InitializeAsync` (line 91), which forces the host to build. After that, `fixture.Services` is a live `IServiceProvider` backed by the real application's DI container.
- The same pattern is already used by `tests/Mentoory.Tests.Integration/Fixtures/IntegrationTestBase.SendAsync<TResponse>` (creates a scope, resolves `IMediator`, sends a request). No divergence from established test conventions.
- Going through commands rather than raw SQL preserves domain invariants — including the 7-stage `ProjectStages` seed that yesterday's seed-data regression proved is easy to forget when bypassing the factory.

**Alternatives considered**:

- **Direct raw SQL via the fixture's `ConnectionString`.** Rejected — duplicates domain invariants and is exactly the anti-pattern that produced yesterday's regression.
- **Expose a `/test-only/*` HTTP endpoint on the web host for test seeding.** Rejected — requires product-code changes, violates spec SC-E5.
- **Add a dedicated test-only IoC registration with a custom `ITestSeeder`.** Rejected — unnecessary indirection; the existing DI container already resolves everything needed.

**Implications**:

- `LifecycleFixtures` takes `PlaywrightFixture` as a constructor dependency and holds the fixture-scoped service provider internally.
- `LifecycleFixtures.CreateProjectAsync(targetStage)` loops `AdvanceProjectStageCommand` (int)targetStage times. For `Closure` that's 6 dispatches; acceptable overhead (single DB transaction per dispatch, ~50 ms each against a warm container).

---

## R2. Test #3 — the "broken-state" Completed-stage-not-at-Closure scenario

**Decision**: Keep test #3 but construct the broken state via a **reflection-based fixture helper** (`LifecycleFixtures.ForceCurrentStageStateCompletedAsync`) rather than by going through the domain. The fixture helper is documented as "test-only: creates an invariant-violating state the UI cannot produce; exists because the handler explicitly guards against it per feature 016 FR-003."

**Rationale**:

- The parent spec's US1 §3 describes the scenario verbatim ("a project's current stage has already been marked completed (edge state from an older transition)"). Deleting the test would drop coverage the spec requires.
- The handler DOES guard against this state (`if (project.CurrentStageState != StageState.InProgress) return Failure(StageNotInProgress, ...)` — `AdvanceProjectStageHandler.cs:42`). A test that never exercises the guard leaves a real code path uncovered end-to-end.
- Reflection-based property setters on private setters are already used by the existing unit tests (`AdvanceProjectStageHandlerTests.SeedProject` uses `typeof(Project).GetProperty(nameof(Project.IsActive))!.SetValue(project, false)`). Pattern established; not new.

**Alternatives considered**:

- **Drop the test, document coverage via unit tests only.** Rejected — unit coverage asserts the handler's branch but not the end-to-end flow (controller → handler → redirect → Spanish toast → rendered TempData warning). The integration gap is the one we're paid to close.
- **Introduce a domain method like `Project.MarkCurrentStageCompletedForTesting(...)`.** Rejected — product code change, violates SC-E5, and leaves a method named "ForTesting" in the domain API.
- **Produce the state via a sequence of real commands.** Rejected — no command sequence yields "Completed and not at Closure"; the domain invariant forbids it by construction.

**Implications**:

- `LifecycleFixtures.ForceCurrentStageStateCompletedAsync(Guid projectExternalId)` loads the project via DbContext (with `IgnoreQueryFilters` if tenant context is null), sets `CurrentStageState = Completed` via reflection, `SaveChangesAsync`. The helper has an XML summary comment explaining it deliberately violates domain invariants for the test #3 assertion.
- The helper is added in C1 (the chunk that needs it), not C0 — C0's stop condition #2 protects against adding fixture APIs that aren't needed yet.

---

## R3. Per-test state isolation strategy

**Decision**: Per-test isolation via `LifecycleFixtures.ResetStateAsync()` that deletes every row from `tenant.Projects` and `tenant.Incubators` (cascading to `ProjectStages`, `ProjectParticipants`, `MentorAssignments`, `RoleAssignments` scoped to projects), then re-runs seeding as needed in each test. Respawn is **not** adopted for E2E.

**Rationale**:

- The shared `PlaywrightFixture` container hosts 97 existing E2E tests that depend on the full PostDeployment seed state (users, role assignments, base seed projects). A blanket Respawn reset would blow away that seed and break every other E2E test.
- A scoped Respawn reset (schemas: `tenant` only, tables: `Projects` + `Incubators` + their cascades) would still wipe the seed projects that other tests use.
- Targeted per-test cleanup is precise: the Lifecycle tests create their own unique-named incubators and projects (prefixed `e2e-lifecycle-`); cleanup deletes only those. Other E2E tests see no change.
- The existing `Respawn` infrastructure stays available as documentation for integration tests (which use it cleanly because they own their container lifecycle).

**Alternatives considered**:

- **Respawn with schema scope `tenant`.** Rejected — would delete seeded base projects used by `DiagnosticWorkflowTests`, `ProjectIsolationTests`, and others.
- **One test-container per test class.** Rejected — Testcontainers startup is ~15 s; multiplied by 5 walkthrough files = 75 s wasted per suite run. No isolation benefit.
- **One test-container per test method.** Rejected — same cost problem at larger scale (~17 × 15 s = ~4 min pure overhead).
- **Use `IDisposable.Dispose` per test to delete the test's own projects.** Accepted as a secondary pattern within `ResetStateAsync` — cleanup is both `ResetStateAsync` at the start (defensive) and within each fixture helper's `try/finally` for projects it created.

**Implications**:

- Naming convention: every fixture-created project is prefixed `e2e-lifecycle-{testName}-{Guid[:8]}`. The reset query is `DELETE FROM tenant.Projects WHERE [Name] LIKE 'e2e-lifecycle-%'` (cascade deletes stages/participants/assignments via FK).
- Incubators: prefixed `e2e-lifecycle-inc-{Guid[:8]}`. Same LIKE-based cleanup.
- Test users (coord1, coord2, etc.) are part of the PostDeployment seed and never touched.

---

## R4. Concurrency test (row #16) mechanism

**Decision**: Use a test-time helper `LifecycleFixtures.IssueAdvanceAsync(Guid projectExternalId, long actingUserId)` that issues the `AdvanceProjectStageCommand` directly through the shared `IMediator` (NOT through the UI). The test: (a) opens the Lifecycle page in the Playwright browser (captures the server-rendered RowVersion implicitly via the next submit's anti-forgery token + session state); (b) calls `IssueAdvanceAsync` from the test to bump the DB RowVersion behind the browser's back; (c) clicks the Advance button in the browser — the handler now sees a stale entity via EF's identity map inside its own scope, throws `DbUpdateConcurrencyException`, returns `LifecycleConcurrencyConflict`, and the controller renders the Spanish toast.

**Rationale**:

- Parallel Playwright contexts (two browsers, one project) is flaky: the two submits land in arbitrary order, and the test has no hook to ensure the "stale" one lands second.
- The existing integration test `AdvanceProjectStageConcurrencyTests.Handle_WhenProjectMovesUnderneathStaleTracker_ReturnsLifecycleConcurrencyConflict` already uses the exact same pattern (scope A holds stale tracker, scope B commits, scope A's handler sees stale via identity map). Applying the same pattern at E2E level is consistent.
- The UI assertion (the Spanish toast's exact text) is what this test adds — the handler-level behavior is already covered by the integration test. The E2E test's value is the browser-rendered toast plus the redirect back to Lifecycle.

**Alternatives considered**:

- **Two parallel Playwright contexts.** Rejected — flakiness risk per spec R1 NFR "Determinism". Would need timeouts and retries, both anti-patterns.
- **Skip the E2E concurrency test entirely, rely on integration coverage.** Rejected — the parent spec's edge case explicitly names the Spanish toast rendering, which integration tests don't assert.
- **Use Playwright's `route()` to intercept and delay one request.** Rejected — couples test to HTTP plumbing the product code doesn't know about; brittle under routing changes.

**Implications**:

- `LifecycleFixtures.IssueAdvanceAsync` is added in C5 only. Not a C0 artifact — it's specific to one test.
- The test must open the Lifecycle page BEFORE calling `IssueAdvanceAsync`, to ensure the browser session's pending form submission has the stale state. If the DB advance happens before the page renders, the Lifecycle page sees the fresh state and no concurrency race occurs.

---

## R5. Build-time prerequisite: DACPAC must exist before E2E runs

**Decision**: Each chunk's pre-flight checklist includes `ls Mentoory.Db/bin/Debug/MentooryDb.dacpac` and instructs the session to run `cd Mentoory.Db && ./publish-mentoorydb.sh` if the file is missing. No test-time auto-build.

**Rationale**:

- `PlaywrightFixture.FindDacpac()` throws `FileNotFoundException` at fixture init time if the DACPAC is absent — an opaque failure mode when the root cause is a stale build.
- Yesterday's seed-data fix rebuilt the DACPAC; on a fresh clone or after a clean, the file won't exist.
- Auto-building in the test would obscure state drift: if the DACPAC doesn't match current SSDT sources, it should fail visibly so the operator knows.

**Alternatives considered**:

- **Build DACPAC in `[Fact]` attributes via a collection fixture.** Rejected — silent auto-build hides stale-source issues.
- **Document once in quickstart.md, omit from per-chunk pre-flight.** Rejected — a drifted-away session might not have read quickstart; the pre-flight is the authoritative self-check.

**Implications**:

- The pre-flight in `checkpoints/00-foundation.md` already includes `ls .../MentooryDb.dacpac`. Same step is inherited by all later chunks.

---

## Summary of resolutions

| Brainstorm open thread | Status |
|---|---|
| PlaywrightFixture extensibility assumption | ✅ Resolved — R1 |
| Test #3 broken-state fixture — pull weight or redundant? | ✅ Keep via reflection helper — R2 |
| Concurrency UI test mechanism — parallel contexts vs helper? | ✅ Test-time `IssueAdvanceAsync` — R4 |
| Per-test isolation — Respawn or targeted cleanup? | ✅ Targeted cleanup, not Respawn — R3 |
| DACPAC build prerequisite | ✅ Pre-flight only, no auto-build — R5 |

All NEEDS CLARIFICATION items from `plan.md` Technical Context resolved. Phase 1 (data-model + quickstart) unblocked.
