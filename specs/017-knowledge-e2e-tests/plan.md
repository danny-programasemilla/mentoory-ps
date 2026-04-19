# Implementation Plan: Knowledge Module E2E Test Coverage

**Branch**: `016-knowledge-module-core` (tests ship with the module they cover) | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/017-knowledge-e2e-tests/spec.md`

## Summary

Deliver a Playwright-driven E2E suite (plus two targeted integration-test backstops) that covers the ~33 acceptance scenarios across User Stories 1–6 of `specs/017-knowledge-e2e-tests/spec.md`. The suite builds on the existing `PlaywrightFixture` (Testcontainers SQL + DACPAC deploy + Kestrel-proxied TestServer + headless Chromium), extends the two existing smoke files in-place where practical (`KnowledgeTemplatesTests.cs`, `KnowledgeProjectStructureTests.cs`), and adds four new files scoped to (a) coordinator project-tree editing, (b) form-clone cascade happy/negative paths, (c) PartialSync lifecycle, and (d) authorization + tenant isolation. Shared helpers (login + context-select, Spanish-string locators, screenshot wrappers) extract to `tests/Mentoory.Tests.E2E/Infrastructure/` to eliminate the existing copy-paste. Two integration-test backstops ship in `tests/Mentoory.Tests.Integration/Knowledge/` to cover DB-level invariants the UI cannot observe (UNIQUE-per-project KS rowcount; null-binding cascade no-op). Seed-data additions fit inside the existing `004.SeedTestData.sql` + `005.SeedKnowledgeData.sql` PostDeployment scripts — one additional `FormTemplate` bound to the seeded KS template, and one additional coordinator in a second incubator for tenant-isolation coverage. No production-code changes; no new NuGet dependencies; target wall-time ≤ 6 minutes on the CI runner.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0 pre-release) — test projects only
**Primary Dependencies**: Microsoft.Playwright (Chromium headless), xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.MsSql, Microsoft.SqlServer.DacFx, coverlet.collector. All already declared in `tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj` and `tests/Mentoory.Tests.Integration/Mentoory.Tests.Integration.csproj`; no new packages required.
**Storage**: Ephemeral `mcr.microsoft.com/mssql/server:2022-latest` container per test collection; schema deployed from `Mentoory.Db/bin/Debug/MentooryDb.dacpac` with PostDeployment seeds `001–005`. Respawn resets DB state between integration tests (not between E2E tests — E2E tests rely on fresh-per-collection + unique-per-test identifiers).
**Testing**: Own target. The suite under specification IS the testing layer. Unit-test coverage at the handler/domain layer remains in `tests/Mentoory.Knowledge.Tests/` and `tests/Mentoory.Diagnostic.Tests/` and is not in scope for this plan.
**Target Platform**: Linux CI runner + local dev workstation. Both must run Chromium headless and Docker (for Testcontainers).
**Project Type**: Additive test suite extending an existing ASP.NET Core 10 modular monolith (Mentoory). No production code is modified except (a) optional seed-data inserts in `Mentoory.Db.PostDeployment/004.SeedTestData.sql` + `005.SeedKnowledgeData.sql`, and (b) optional helper extraction under `tests/Mentoory.Tests.E2E/Infrastructure/`.
**Performance Goals**: SC-T02 — combined new E2E suite completes in ≤ 6 minutes on the reference CI runner. FR-T30 — each individual test ≤ 30s wall-time.
**Constraints**: Zero compiler warnings (Directory.Build.props enforces `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`); all user-visible Spanish strings asserted verbatim where the spec fixes them; no locale/timezone assumptions in test assertions; no new NuGet packages.
**Scale/Scope**: ~33 acceptance scenarios → ~33 `[Fact]`/`[Theory]` methods split across 6 E2E test files + 1 integration-test extension. Total new test LOC estimate: ~1500 lines including helpers.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against `.specify/memory/constitution.md` v1.1.1. This is a test-layer feature; most principles do not apply directly, but Principles V, VIII, IX, and the Access-&-Security Constitution's compliance validation surface remain binding.

| Principle | Status | Evidence |
|---|---|---|
| I. Clean Architecture Layer Boundaries | ✅ N/A | Test code lives outside the Domain/Application/Infrastructure/Web layering. The suite drives production code at the Web layer (HTTP + browser) and occasionally at the Application layer (integration backstops via MediatR). No tests reach into internals of Domain/Infrastructure. |
| II. CQRS Pattern | ✅ N/A | Tests do not add commands/queries; they invoke existing ones via the same boundaries real users do. Integration-test backstops use `IMediator.Send` with real `CreateProjectCommand` / `CloneFormTemplateCommand` — matching production usage, not bypassing it. |
| III. DDD Constraints | ✅ N/A | Tests do not add domain types. Integration backstops that construct domain entities (`KnowledgeStructureTemplate.Create`, etc.) use the same factory methods as production handlers. |
| IV. Integration Events | ✅ PASS | Scenario US4-scenario-3's persistence of priority ranges is covered at the UI level (save + reload). The `TopicPriorityRangesChanged` INotification emission itself is already unit-tested in `tests/Mentoory.Knowledge.Tests/Handlers/UpdateTopicPriorityRangesEventTests.cs` (existing, out of scope for this spec). Tests do not re-assert event emission. |
| V. Zero Warnings | ✅ PASS | FR-T01, NFR-T01. New test code compiles under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. Existing `tests/Directory.Build.props` already propagates this. |
| VI. DateTime Handling | ✅ N/A | Test code freely uses `DateTime.UtcNow` for test-data setup (stamping `Created`, `utcNow` params passed to domain factories). Production-code `DateTime.UtcNow` prohibition does not apply to tests per the constitution's explicit Domain/Application scope. |
| VII. Naming Conventions | ✅ PASS | Test classes follow `{Concern}Tests` convention (existing). Helpers follow `{Role}HelperAsync` / `{Intent}Helper` naming in the shared Infrastructure file. |
| VIII. File Organization | ✅ PASS | One test class per file (existing pattern kept). No JS in test projects. If any test data fixtures use SQL, they go in the existing PostDeployment scripts (UTF-8 no BOM, idempotent), not inline in C#. |
| IX. Spanish-First UI | ✅ PASS | FR-T11, EC-30, EC-31. All user-visible-string assertions use Spanish strings verbatim where the spec fixes them. Code/comments remain English. |
| X. Role Hierarchy & Session Context | ✅ PASS | US6 authorization coverage explicitly verifies the hierarchical-inclusion rule: GlobalAdmin-only routes deny Coordinator + IncubatorAdmin; Coordinator routes accept Coordinator, IncubatorAdmin, GlobalAdmin. Menu-visibility assertions (US6-5) cover the MenuConfiguration side of Principle X. |
| XI. SSDT/DACPAC Strategy | ✅ PASS | NFR-T03. Any seed-data additions ship in the existing numbered PostDeployment scripts (`004.SeedTestData.sql`, `005.SeedKnowledgeData.sql`); no EF migrations introduced. Seed inserts remain idempotent (check-exists-before-insert, matches existing pattern). |

**Related — Access & Security Constitution compliance** (`.specify/memory/access-security-constitution.md` surfaces in NFR-K05 of 016 and governs this test-spec by extension):

| Surface | Coverage |
|---|---|
| GlobalAdmin-only template CRUD | US6-1 (negative-path parameterized theory across `/Coordination/Knowledge/Templates/**`) |
| Coordinator/IncubatorAdmin/GlobalAdmin project-clone CRUD | US4 positive-path + US6-3 (tenant isolation) negative-path |
| Unauthenticated access | US6-2 (every `/Coordination/Knowledge/*` → login redirect) |
| Cross-incubator IncubatorAdmin enforcement | US6-4 (create project in a non-administered incubator → denied) |
| Menu visibility per role | US6-5 |

**Result: All applicable gates pass; no violations to justify. Complexity Tracking section intentionally empty.**

### Post-Design Re-Check (after Phase 1)

Re-evaluated after `research.md`, `contracts/`, and `quickstart.md` were generated:

| Principle | Status | Post-Design Evidence |
|---|---|---|
| I–III, VI | ✅ N/A | Unchanged — test-layer only. |
| IV. Integration Events | ✅ PASS | Post-design contracts do not introduce new event assertions beyond the existing unit-test coverage. |
| V. Zero Warnings | ✅ PASS | Helper APIs in `contracts/e2e-helpers.md` are designed to be nullable-aware and `async Task`-returning; no warning-risking patterns. |
| VII. Naming | ✅ PASS | Every test method name in `contracts/test-files.md` follows `{Scenario}_{Expectation}` pattern (e.g., `CreateTemplate_OverlappingRanges_ShowsValidationError`). |
| VIII. File Organization | ✅ PASS | Contracts enumerate one class per file; no JS in test projects; seed additions confined to existing PostDeployment SQL files. |
| IX. Spanish-First UI | ✅ PASS | Every fixed Spanish error string in contracts matches the spec verbatim. |
| X. Role Hierarchy | ✅ PASS | Authorization contract (`contracts/authorization-matrix.md`) enumerates the role × route matrix and the corresponding negative-path tests. |
| XI. SSDT/DACPAC | ✅ PASS | Seed-data additions (one FormTemplate + one coordinator-in-second-incubator) ship as idempotent inserts in existing numbered scripts. |

**Post-design deltas**:
- **No violations surfaced.** The plan is approved for Phase 2 task generation.
- **One helper boundary clarified during contracts write-up**: the login-and-select-context helper will accept an explicit `targetRole` parameter (rather than "first enabled") so US6 role-based tests can unambiguously request GlobalAdmin vs. Coordinator contexts for the same multirole seed user. Reflected in `contracts/e2e-helpers.md`.

## Project Structure

### Documentation (this feature)

```text
specs/017-knowledge-e2e-tests/
├── plan.md                             # This file (/speckit.plan output)
├── research.md                         # Phase 0 output
├── spec.md                             # Approved specification
├── quickstart.md                       # Phase 1 output — how to run the new suites locally
├── contracts/                          # Phase 1 output
│   ├── e2e-helpers.md                  # Shared helper API (login, context, Spanish locators, screenshot)
│   ├── test-files.md                   # File-by-file test-method inventory with scenario → method mapping
│   ├── authorization-matrix.md         # Role × route negative-path coverage matrix
│   └── seed-additions.md               # PostDeployment SQL additions needed for US3/US6
├── tasks.md                            # Phase 2 output (NOT created by /speckit.plan)
└── checklists/
    └── requirements.md                 # Spec quality checklist (from /speckit.specify)
```

### Source Code (repository root)

```text
Mentoory.Db.PostDeployment/                   # Seed additions (idempotent INSERT guards)
├── 004.SeedTestData.sql                      # EXTENDED: add coord2 user in second incubator (US6-3)
└── 005.SeedKnowledgeData.sql                 # EXTENDED: add a FormTemplate bound to the KS template (US3)

tests/Mentoory.Tests.E2E/
├── Infrastructure/
│   ├── PlaywrightFixture.cs                  # EXISTING — unchanged
│   ├── E2ETestCollection.cs                  # EXISTING — unchanged
│   └── KnowledgeTestHelpers.cs               # NEW — extracted shared helpers (login, context, screenshot, Spanish locators)
└── Tests/
    ├── KnowledgeTemplatesTests.cs            # EXTENDED — add ~8 scenarios (priority editor, overlap, reorder, archive, hard-delete block)
    ├── KnowledgeProjectStructureTests.cs     # EXTENDED — add ~5 scenarios (CRUD per level, rename-doesn't-leak, tenant isolation)
    ├── KnowledgeProjectTreeEditingTests.cs   # NEW — priority ranges + delete-guard scenarios (US4)
    ├── KnowledgeFormCloneCascadeTests.cs     # NEW — cross-module cascade happy + mismatch negative (US3 UI scenarios)
    ├── KnowledgePartialSyncTests.cs          # NEW — SyncMode toggle + sync action + summary (US5)
    └── KnowledgeAuthorizationTests.cs        # NEW — role × route matrix + menu visibility (US6)

tests/Mentoory.Tests.Integration/
└── Knowledge/
    └── DiagnosticCascadeRoundTripTests.cs    # EXTENDED — 2 new tests: null-binding no-op cascade, PartialSync template-mutation helper
```

**Structure Decision**: E2E tests extend the existing `tests/Mentoory.Tests.E2E/Tests/` flat directory (no sub-folders per user-story — the one-file-per-concern convention already used in the project is clearer at this scale). Shared helpers extract to a single new file under `Infrastructure/` to eliminate the current duplication of `LoginAndSelectContextAsync` across `KnowledgeTemplatesTests`, `KnowledgeProjectStructureTests`, `ProjectCreationTests`, `AvailableProjectsTests`. Integration-test backstops extend the existing `DiagnosticCascadeRoundTripTests` file rather than starting a new file — the two new tests fit the same theme and share the existing `SetupProjectWithKsTemplateAsync` helper.

## Complexity Tracking

> No constitution violations to justify.

*Section intentionally empty.*
