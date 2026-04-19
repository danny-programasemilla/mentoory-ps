# Phase 0 Research: Knowledge Module E2E Test Coverage

**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)
**Date**: 2026-04-19

This document resolves open questions from the spec's Technical Context and the Assumptions section, captures patterns found in the existing codebase that the new suite must mirror, and records the decisions that drive the Phase 1 contracts.

---

## R1 — Where should shared helpers live, and what shape should they take?

**Question**: Today `LoginAndSelectContextAsync` is copy-pasted across four E2E test files (`KnowledgeTemplatesTests`, `KnowledgeProjectStructureTests`, `ProjectCreationTests`, `AvailableProjectsTests`) with subtle signature drift (some take `roleLabel`, some don't; some drop the project dropdown, some don't). Should the new suite consolidate this?

**Decision**: Yes — extract a single `KnowledgeTestHelpers` static class under `tests/Mentoory.Tests.E2E/Infrastructure/` exposing an explicit-role API:

```csharp
public static class KnowledgeTestHelpers
{
    public static Task LoginAsync(IPage page, string baseUrl, string email, string password);
    public static Task SelectContextAsync(IPage page, ContextSelection selection);
    public static Task LoginAndSelectAsync(IPage page, string baseUrl, string email, string password, ContextSelection selection);
}

public sealed record ContextSelection(
    string? RoleLabel = null,          // null = first enabled
    string? IncubatorName = null,      // null = first enabled
    string? ProjectName = null);       // null = first enabled; omit if role doesn't require project
```

**Rationale**:
- The US6-5 menu-visibility test needs to log in the multirole seed user as `GlobalAdmin` on one request and as `ProjectCoordinator` on another. Today's helper API's "first enabled" shortcut can't express this; an explicit `RoleLabel` does.
- Keeping the helper as a static class (not a base class) means test classes can stay `public class XyzTests` and xUnit's `[Collection]` attribute keeps working without refactoring inheritance.
- Existing test files are extended in-place for US1/US2 scenarios; they switch to the new helper without changing their behavior. US4/US5/US6 new files use the helper from day one.

**Alternatives considered**:
- A base test class (`KnowledgeTestBase`) with instance helpers. Rejected — xUnit's `[Collection]` fixture model plays fine with static helpers and a base class adds an inheritance dimension with no payoff.
- Playwright's `storageState` for cookie-reuse across tests. Rejected for v1 — it couples the suite to Playwright's serialization format and obscures the per-test login path the existing suite already asserts on. Revisit if per-test login time measurably dominates the 6-minute budget.

---

## R2 — What seed data does the suite depend on, and what's missing?

**Question**: Spec `FR-T20` says the DACPAC seed MUST include GlobalAdmin, Coordinator, IncubatorAdmin, a KS template with a four-level tree, a project bound to that template, and a FormTemplate bound to the same template. What exists today, and what's missing?

**Decision**: Extend `004.SeedTestData.sql` + `005.SeedKnowledgeData.sql` with two additions:

1. **Second coordinator in second incubator** (for US6-3 tenant isolation). Call them `coord2@test.mentoory.com` / `Incubadora Norte`. The RoleAssignments table already supports multi-incubator users; the insert needs a `WHERE NOT EXISTS` guard matching the existing style in `004`.
2. **One FormTemplate bound to the seeded KS template** (for US3 cascade happy-path). Bind it via `DefaultKnowledgeStructureTemplateExternalId` to `Emprendimiento Básico`'s KS template. Give it 2–3 questions, each with `TopicId` pointing at a seeded template topic. Insert into `005.SeedKnowledgeData.sql` after the KS template seed, with the same `IF NOT EXISTS` idempotency guard.

**What already exists** (verified by reading current seed scripts):
- `multirole@test.mentoory.com` carries GlobalAdmin + IncubatorAdmin + ProjectCoordinator roles — used by US1 tests as GlobalAdmin and by US6-5 as role-switchable
- `coord1@test.mentoory.com` — single-role Coordinator in the primary incubator
- `incadmin1@test.mentoory.com` — IncubatorAdmin in the primary incubator
- `Emprendimiento Básico` KS template with modules `Ideación`, `Validación`, etc., topics, subjects, and a resource of each `ResourceType` — already populated
- `Proyecto Innovación` project — bound to `Emprendimiento Básico`; KS is materialized at seed via the new `IKnowledgeStructureProvisioner`

**Missing before this spec can green**:
- No FormTemplate exists in the DACPAC seed today (only the unit-test harness builds them in-memory)
- No second incubator/coordinator exists for tenant-isolation coverage

**Rationale**: Adding seeded rows beats per-test setup for the E2E suite because (a) DACPAC deploy runs once per fixture lifetime and the insert cost is negligible, (b) every test in the collection gets the same baseline state with no setup call, (c) repository-pattern tests at the integration layer can still build their own form templates via `SendAsync(new CloneFormTemplateCommand(...))` when they want to exercise the handler directly.

**Alternatives considered**:
- Per-test fixture that builds a FormTemplate in C# via `IMediator.Send`. Rejected for E2E — it adds ~1s per test (DB round-trips for insert + query) and the existing tests rely on seeded data for consistency. Keep this pattern for integration-test backstops where per-test data is genuine (Respawn already resets, so seed isn't usable).

---

## R3 — How should the US5 PartialSync setup work without driving the full template-mutation UI?

**Question**: US5 requires the template to gain a new topic AFTER the project clone exists, then sync picks it up. Driving this end-to-end through two browser contexts (GlobalAdmin opens templates, adds a topic, logs out; Coordinator logs in, triggers sync) adds ~15s per test.

**Decision**: Use a hybrid approach:

- **US5 scenarios 1–2** (SyncMode toggle, action visibility): pure Playwright, fast.
- **US5 scenarios 3–5** (template adds topic → sync picks it up → locally-renamed topic preserved): drive the template mutation through an **integration-test extension**, not through the UI. The E2E portion only drives the SyncMode toggle + the "Sincronizar desde plantilla" click + asserts on the summary notification + asserts on the resulting tree after sync.

Implementation pattern:

```csharp
[Fact]
public async Task Sync_AppendsNewTemplateTopic()
{
    // Arrange: coordinator logs in, opens project KS, switches to PartialSync
    // Arrange (integration helper, same DB container):
    //   AddTopicToTemplateAsync(ksTemplateExternalId, moduleName, newTopicName);
    // Act: coordinator clicks "Sincronizar desde plantilla"
    // Assert: summary notification + tree contains new topic node
}
```

The integration-helper path lives in a `public static class KnowledgeIntegrationHelpers` under `tests/Mentoory.Tests.E2E/Infrastructure/` and uses the `WebApplicationFactory.Services.CreateScope()` pattern to dispatch a command handler against the same DB container Playwright is talking to.

**Rationale**: Keeps the E2E assertion surface (what the coordinator sees in the browser) while avoiding the wall-time cost of a second browser context performing template mutation. The DB mutation is a one-line `IMediator.Send`; it's as trustworthy as the production handler because it IS the production handler.

**Alternatives considered**:
- Pure UI (two browser contexts). Rejected for v1 — wall-time cost, second context's login flake risk, and the scenario's focus is sync behavior, not template-admin UI.
- Raw SQL insert into `knowledge.TopicTemplates`. Rejected — bypasses domain invariants (`Version` bump, `SortOrder` computation) that production enforces; a regression in the domain factory would not surface.

---

## R4 — How do we avoid flaky XHR-timing on the context-selector dropdowns?

**Question**: The context selector populates `[data-cs='role']`, `[data-cs='incubator']`, `[data-cs='project']` dropdowns via XHR. Premature interaction causes intermittent flake (option value 0 when the list hasn't loaded).

**Decision**: Reuse and formalize the existing pattern — every dropdown interaction waits on `sel => sel.options.length > 1` via `page.WaitForFunctionAsync` before calling `SelectOptionAsync`. Codify this in the shared helper so no test has the raw wait-pattern inline.

**Rationale**: The existing helper in `KnowledgeTemplatesTests.cs:187–218` has proved reliable across ~30 PRs. Formalizing it in the shared helper removes the risk of a copy-paste omission in new tests.

**Alternatives considered**:
- Increase `SelectOptionAsync` timeout. Rejected — masks flake without fixing the underlying race.
- Server-side render the dropdown options. Out of scope (would change production behavior).

---

## R5 — How does the suite reset state between tests?

**Question**: `PlaywrightFixture` provisions a SQL container + DACPAC deploy **once per collection** — it does not call `Respawn.ResetAsync` between tests. Some tests mutate seeded rows (US1 archive, US1 hard-delete block); how do we keep them idempotent?

**Decision**: Two-pronged:

1. **Tests that CREATE new rows** use unique names/ExternalIds (`$"E2E Project {Guid.NewGuid():N}"` pattern already in `ProjectCreationTests`). No teardown needed — the extra rows persist until the collection ends.
2. **Tests that MUTATE seeded rows** (US1-7 archive/unarchive, US1-8 hard-delete block) use a **test-local template** the test itself creates first, then mutates. The seeded `Emprendimiento Básico` is read-only from the test suite's perspective. US1-8 specifically (hard-delete block) uses the seeded project's already-existing reference to it, so the seed stays intact.

**Rationale**: This matches the existing `ProjectCreationTests.cs` pattern (every test creates its own project with a unique name). It's cheaper than Respawn + DACPAC re-deploy per test (~30s each), and cheaper than isolated `IClassFixture` per test class (loses the shared browser + container benefit).

**Alternatives considered**:
- Respawn between tests. Rejected — the fixture runs `IntegrationTestBase`-style reset for the integration suite but not the E2E suite. Retrofitting Respawn to E2E would rebuild seeded state each run and add ~5s per test; over 33 scenarios that's ~3 extra minutes, blowing the 6-minute budget.
- Per-collection snapshot + restore via Testcontainers. Rejected — MSSQL container snapshot support is available but adds moving parts and .dacpac-versus-snapshot staleness risk.

---

## R6 — How do we assert on the mismatch-KS error string in US3 scenario 2?

**Question**: The spec fixes the exact Spanish error: `"Este formulario está diseñado para una estructura de conocimiento diferente a la del proyecto."` The user-visible surface is the clone-form UI in the Diagnostic area, not the KnowledgeController. Where does the error appear?

**Decision**: Confirmed by reading `Mentoory.Diagnostic.Application/Commands/CloneFormTemplate/CloneFormTemplateHandler.cs` and the corresponding view: the handler returns `Result<...>` with the Spanish error; the controller surfaces it via `this.MapErrorsToModelStateAndSetErrorToast<CloneFormTemplateCommand>(result)`. The toast text is rendered into a `.toast` element (Tabler convention) after the form POST redirects back to the clone view.

**Assertion pattern**:
```csharp
var content = await page.ContentAsync();
content.Should().Contain("Este formulario está diseñado para una estructura de conocimiento diferente",
    "the exact Phase 9 mismatch error must surface in the UI");
```

Substring-match (not full-string) because the toast may add leading/trailing whitespace or a dismiss button. The unique first clause is specific enough to avoid false positives.

**Alternatives considered**:
- Assert on toast DOM structure (`.toast-body`). Rejected — depends on Tabler internals that could drift in future polish work.
- Assert on HTTP status. Rejected — the handler returns 302 + toast (standard MVC PRG), not a 4xx; status won't distinguish validation-error from happy-path redirect.

---

## R7 — Should we run Playwright in `Headed` mode for local debugging?

**Question**: CI runs headless; local debug benefits from `Headless = false`. Should the fixture expose this?

**Decision**: Gate on an env var:

```csharp
Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = Environment.GetEnvironmentVariable("E2E_HEADED") != "1",
    SlowMo = Environment.GetEnvironmentVariable("E2E_HEADED") == "1" ? 250 : 0,
});
```

Patch applied in `PlaywrightFixture.cs` as part of this spec. No impact on CI (var unset → headless), immediate local debuggability without per-dev overrides.

**Rationale**: One-line change, zero CI risk, material dev-experience win when investigating flakes.

**Alternatives considered**:
- Per-test `[Fact(Skip = ...)]` controls. Rejected — too granular and test-level noise.

---

## R8 — What's the coverage matrix the tasks.md will consume?

**Decision** (mapping spec scenarios → test methods for Phase 2):

| Spec | Scenarios | Target file | Method count |
|---|---|---|---|
| US1 | 9 scenarios | `KnowledgeTemplatesTests.cs` (extended) | 9 tests (3 existing renamed/kept, 6 new) |
| US2 | 6 scenarios | `KnowledgeProjectStructureTests.cs` + `ProjectCreationTests.cs` (both extended) | 6 tests (4 existing kept, 2 new) |
| US3 | 4 scenarios | `KnowledgeFormCloneCascadeTests.cs` (new) + `DiagnosticCascadeRoundTripTests.cs` (extended) | 2 E2E + 2 integration = 4 tests |
| US4 | 7 scenarios | `KnowledgeProjectTreeEditingTests.cs` (new) + `KnowledgeProjectStructureTests.cs` (extended) | 7 tests (5 new E2E + 2 in the extended existing file) |
| US5 | 5 scenarios | `KnowledgePartialSyncTests.cs` (new) + integration helper extension | 5 tests |
| US6 | 5 scenarios | `KnowledgeAuthorizationTests.cs` (new) | 5 tests (scenario 1 as parameterized theory over ~10 routes) |
| **Total** | **36 scenarios** | 6 E2E files + 1 integration file | **~33–36 test methods** |

(Spec lists ~33 scenarios; the mapping above lands at 36 because US6-1 covers multiple routes in one theory, which is counted once in the spec but generates multiple `[InlineData]` inputs here. The integration backstops cover US3-3 and US3-4 which the spec also counts as "covered" without being distinct UI tests.)

---

## Summary of Decisions

| # | Decision | Drives |
|---|---|---|
| R1 | Extract `KnowledgeTestHelpers` with explicit-role API | `contracts/e2e-helpers.md`, helper extraction task in `tasks.md` |
| R2 | Extend seed scripts with FormTemplate + second coordinator/incubator | `contracts/seed-additions.md`, SSDT tasks |
| R3 | Hybrid E2E + integration-helper for US5 | `KnowledgePartialSyncTests.cs` structure |
| R4 | Formalize dropdown-XHR wait in shared helper | Flake prevention across all new tests |
| R5 | Unique-name-per-test for create; test-local templates for mutation | Every test's setup pattern |
| R6 | Substring-match on the Phase 9 mismatch error | US3-2 assertion shape |
| R7 | Env-gated `Headless=false` for local debug | `PlaywrightFixture.cs` patch |
| R8 | Spec-scenario → test-method mapping | Direct input to `/speckit.tasks` |

No [NEEDS CLARIFICATION] markers remain. Proceed to Phase 1 (contracts + quickstart).
