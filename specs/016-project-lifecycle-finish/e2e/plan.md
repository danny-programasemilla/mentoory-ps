# Implementation Plan: E2E coverage for Project Lifecycle (feature 016)

**Branch**: `016-project-lifecycle-finish` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-project-lifecycle-finish/e2e/spec.md`
**Parent feature**: `specs/016-project-lifecycle-finish/` (feature 016 — product work, already shipped)

> **Layout note**: this feature is a sibling spec inside feature 016's directory (brainstorm decision Q1). `specify setup-plan` resolves plans by branch name and cannot target nested specs — the plan artifacts here are authored directly following `.specify/templates/plan-template.md`.

## Summary

Deliver a Playwright E2E suite (~16 tests) that covers every acceptance scenario, automatable edge case, and automatable success criterion from the parent feature 016 spec. Execution is chunked into six strictly-ordered sessions (C0–C5) with pre-authored resume prompts that let a fresh AI session complete each chunk without drift. Technical approach: use the existing `PlaywrightFixture` (a `WebApplicationFactory<Program>` that already exposes `.Services`) to resolve `IMediator` / `TenantDbContext` for programmatic seed + advance without touching product code; drive the UI via Playwright page objects; assert Spanish literals directly from `ProjectsController.ResolveSpanishMessage` / `StageTypeDisplay.ToSpanish` / `StageActionDisplay.ToSpanish`; reset per-test state via targeted cleanup helpers backed by the existing `TenantDbContext` (Respawn is available in the existing stack but sharing with unrelated E2E tests rules out blanket resets, so per-project cleanup is used instead).

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: xUnit, FluentAssertions, Moq (unused here), Microsoft.Playwright, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.MsSql, Respawn (available, not adopted for E2E — see research.md R3), Microsoft.SqlServer.Dac (DACPAC deploy), MediatR 14.1 (for test-time seed through `IMediator`)
**Storage**: SQL Server 2022 via Testcontainers, schema deployed from `Mentoory.Db/bin/Debug/MentooryDb.dacpac`. No schema changes in this feature.
**Testing**: xUnit is the test host. Each test class in `tests/Mentoory.Tests.E2E/Tests/Lifecycle/` shares the existing `[Collection(E2ETestCollection.Name)]` `PlaywrightFixture` (one container per test run). Per-test state isolation is handled by `LifecycleFixtures.ResetStateAsync` — not by container recreation.
**Target Platform**: Linux server + Docker daemon (for Testcontainers); Chromium headless via Playwright on the executing host.
**Project Type**: Test suite (black-box E2E). No application code shipped — the suite is an additive safety net over feature 016's Coordinator UI.
**Performance Goals**: Full E2E suite (baseline 97 + new 17 = 114 tests) runs in under 8 minutes on the executing host at p95 — below the existing suite's ~3-minute baseline plus a proportional budget for the new tests. Individual chunk exit-gate runs (`dotnet test` for only the new-chunk tests) must complete in under 2 minutes.
**Constraints**: Chromium only (matches existing convention); no new NuGet packages; Spanish UI assertions use exact product literals; no product-code changes in chunks C1–C5; zero new warnings under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; `ITimeProvider` not needed — tests don't introduce `DateTime.UtcNow` at domain/application layers; `ExternalId` on routes already enforced; SSDT-only schema changes (no schema changes here).
**Scale/Scope**: 17 new tests across 5 Walkthrough files + 1 smoke test in C0. ~800-1200 lines of test code + ~200 lines of fixture/page-object infrastructure.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The constitution (v1.1.1) defines 10 principles plus Testing Requirements and Technology Standards. Gates relevant to a **test suite** feature:

| Principle | Gate | Status |
|-----------|------|--------|
| **I. Clean Architecture Layer Boundaries** | Tests must not reach across layer boundaries in ways product code cannot. Here tests use `IMediator` + `TenantDbContext` at the same layer product code uses them — seed via application commands (`CreateProjectCommand`, `AdvanceProjectStageCommand`), read via infrastructure (`TenantDbContext`). | ✅ PASS |
| **II. CQRS Pattern Requirements** | Tests consume, not introduce, CQRS handlers. No new commands/queries. | ✅ PASS |
| **III. Domain-Driven Design Constraints** | `ExternalId` is used on every route the tests hit. Tests do not expose internal IDs in assertions. | ✅ PASS |
| **IV. Integration Events (ADR-001)** | No event publishing introduced. | ✅ PASS (N/A) |
| **V. Zero-Warnings Policy** | Spec SC-E2 enforces 0 new warnings under `TreatWarningsAsErrors`. | ✅ PASS |
| **VI. DateTime Handling** | Tests must not call `DateTime.UtcNow` in Domain/Application code. Tests are in a separate assembly; `DateTime.UtcNow` at the test layer for synthetic timestamps is allowed (matches existing E2E test usage). | ✅ PASS |
| **VII. Naming Conventions** | Fixture classes follow the `{Feature}Fixtures` / `{Feature}PageObject` pattern. Test classes follow `Walkthrough{N}{Topic}Tests`. Matches existing `DiagnosticWorkflowTests` precedent. | ✅ PASS |
| **VIII. File Organization** | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/` for tests, `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/` for fixtures. Parallel to existing `Infrastructure/` root. | ✅ PASS |
| **IX. Spanish-First UI** | All UI assertions match exact product Spanish literals. No paraphrase, no translation. | ✅ PASS |
| **X. Role Hierarchy & Session Context** | Tests cover role-based authorization (coord, mentor, IncubatorAdmin, GlobalAdmin) consistent with `[Authorize]` hierarchy. Tests for missing context use the same redirect pattern product code uses. | ✅ PASS |
| **XI. SSDT/DACPAC Database Strategy** | No schema changes. Seed SQL modifications (if any) remain idempotent `INSERT ... WHERE NOT EXISTS`. | ✅ PASS |
| **Testing Requirements** (xUnit, FluentAssertions, Respawn) | All in-stack. No new frameworks introduced. | ✅ PASS |

**All gates pass.** No entries in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/016-project-lifecycle-finish/e2e/
├── spec.md                               # /spex:brainstorm output — requirements
├── plan.md                               # This file (/speckit-plan — adapted)
├── research.md                           # Phase 0 — resolves the 3 brainstorm open threads
├── data-model.md                         # Phase 1 — fixture + page object shapes
├── quickstart.md                         # Phase 1 — how to execute the suite
├── review_brief.md                       # Reviewer's guide
├── REVIEW-SPEC.md                        # spex:review-spec output (SOUND)
├── coverage-matrix.md                    # Scenario-to-test mapping (populated per chunk)
├── open-questions.md                     # Execution-time parking lot
└── checkpoints/
    ├── 00-foundation.md
    ├── 01-walkthrough-1-advance.md
    ├── 02-walkthrough-2-lifecycle-page.md
    ├── 03-walkthrough-3-gated-actions.md
    ├── 04-walkthrough-4-role-scope.md
    └── 05-walkthrough-5-audit-concurrency.md
```

No `contracts/` directory: the suite exposes no external interfaces. No `tasks.md` here: the six `checkpoints/CN-*.md` files ARE the task plan, authored at brainstorm time with the explicit goal of being drift-resistant across sessions. `/speckit-tasks` is not the right tool for this feature because its output (a flat ordered list) would duplicate and potentially drift from the checkpoint files.

### Source Code (repository root)

```text
tests/
├── Mentoory.Tests.E2E/
│   ├── Infrastructure/
│   │   ├── PlaywrightFixture.cs                      # existing — not modified
│   │   ├── E2ETestCollection.cs                      # existing — not modified
│   │   └── Lifecycle/                                # NEW (added by C0)
│   │       ├── LifecycleFixtures.cs
│   │       ├── LifecycleLoginHelpers.cs
│   │       ├── LifecyclePageObject.cs
│   │       └── CoordinationProjectsPageObject.cs
│   └── Tests/
│       ├── DiagnosticWorkflowTests.cs                # existing — not modified
│       ├── ...                                       # other existing E2E tests
│       └── Lifecycle/                                # NEW
│           ├── LifecycleSmokeTests.cs                # C0
│           ├── WalkthroughAdvanceTests.cs            # C1
│           ├── WalkthroughLifecyclePageTests.cs      # C2
│           ├── WalkthroughGatedActionsTests.cs       # C3
│           ├── WalkthroughRoleScopeTests.cs          # C4
│           └── WalkthroughAuditConcurrencyTests.cs   # C5
└── Mentoory.Tests.Integration/                       # existing — not modified
```

**Structure Decision**: Additive-only. All new files land under `tests/Mentoory.Tests.E2E/`. No product code modified across C0–C5 (per spec SC-E5). Seed SQL (`Mentoory.Db.PostDeployment/004.SeedTestData.sql`) may receive idempotent `INSERT ... WHERE NOT EXISTS` additions in C4/C5 if test users are missing — documented in those chunks' resume prompts.

## Complexity Tracking

*(Empty — all constitution gates pass without justification.)*
