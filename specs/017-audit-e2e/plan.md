# Implementation Plan: Audit Pipeline — Browser E2E Coverage

**Branch**: `017-audit-e2e` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/017-audit-e2e/spec.md`

## Summary

Add 24 Playwright tests under `tests/Mentoory.Tests.E2E/Tests/AuditLog*.cs` covering feature 016-audit-pipeline's user-visible surface (viewer, capture flows, correlation, regressions). Delivery is split into four phases, each ending in a mandatory commit+push+context-clear+resume-prompt handoff to prevent AI-session drift. The technical approach reuses the existing `PlaywrightFixture` (Kestrel + TestServer proxy, DACPAC-seeded SQL container, Chromium headless) with one fixture-level extension confirmed in Phase 1: `audit` schema respawn between tests.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: Microsoft.Playwright (already referenced by `Mentoory.Tests.E2E`), xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, existing `PlaywrightFixture` + Testcontainers (MsSql)
**Storage**: Read-only access to `[audit].[AuditLog]` via the existing admin viewer endpoints; no schema changes, no writes outside the feature 016 pipeline
**Testing**: xUnit + Playwright Chromium (headless); Respawn between tests; DACPAC-seeded users
**Target Platform**: Linux/Windows wherever `Mentoory.Tests.E2E` already runs (Docker-backed MsSql container required)
**Project Type**: Test project (existing — `tests/Mentoory.Tests.E2E/`)
**Performance Goals**: All 24 new `AuditLog*` tests complete in under 3 minutes wall-clock (SC-002); each Playwright wait capped at ≤15 s (FR-015)
**Constraints**: Zero production-code changes across the four phases (SC-005); no new fixtures (FR-014); one commit per phase, no squashes (SC-004)
**Scale/Scope**: 24 tests across 4 files (P1: 9, P2: 7, P3: 3, P4: 5); 4 resume-prompt files + 1 terminal retrospective

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Test-infrastructure feature — most principles apply trivially (they govern production code; this deliverable is test-only). Explicit check:

| Principle | Applies? | Compliance |
|-----------|----------|------------|
| I. Clean Architecture Layers | Negative only | ✅ FR-017 forbids modifying anything outside `tests/Mentoory.Tests.E2E/` + `specs/017-audit-e2e/`. No production layer is touched. |
| II. CQRS Patterns | No new handlers | ✅ No handlers introduced. Tests dispatch existing commands via the real UI or the Web HTTP surface. |
| III. DDD Constraints | No domain changes | ✅ No aggregates, value objects, or entities introduced. `AuditLogReadEntity` already exists from 016. |
| IV. Integration Events | N/A | ✅ No events published by tests. |
| V. Zero-Warnings Policy | Applies | ✅ Test project inherits `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. MailKit CVE (NU1902) still carries the pre-existing `WarningsNotAsErrors=NU1902` escape from 016 — same posture, no regression. |
| VI. DateTime Handling | Applies | ✅ Tests may read `DateTime` for assertions but introduce no production `DateTime.UtcNow` calls. Time-sensitive assertions compare to `DateTime.UtcNow` in the test body only. |
| VII. Naming Conventions | Applies | ✅ Test classes follow `{Feature}Tests` — `AuditLogViewerTests`, `AuditLogCaptureTests`, `AuditLogCorrelationTests`, `AuditLogRegressionTests`. |
| VIII. File Organization | Applies | ✅ One class per file; test files under `tests/Mentoory.Tests.E2E/Tests/` matching existing convention. |
| IX. Spanish-First UI | Validates | ✅ Tests assert Spanish copy VERBATIM — reinforces rather than violates. Test code itself is English, matching the "code in English, UI strings in Spanish" rule. |
| X. Role Hierarchy & Session Context | Validates | ✅ P1 asserts that IncubatorAdmin / Entrepreneur / Mentor / Sponsor are denied the GlobalAdmin-only viewer. Reinforces X. |
| XI. SSDT/DACPAC Strategy | N/A | ✅ No schema changes. Tests rely on existing DACPAC seed. |
| Item #10 (audit obligations) | Validates | ✅ P2 proves that retrofitted commands produce the audit rows obligated by access-security-constitution § Audit Trail Obligations. |

**Constitution gate status**: PASS (no violations, nothing to justify).

## Project Structure

### Documentation (this feature)

```text
specs/017-audit-e2e/
├── plan.md                    # This file (/speckit-plan output)
├── spec.md                    # Feature specification (already created)
├── REVIEW-SPEC.md             # Spec review report (already created)
├── review_brief.md            # Reviewer guide (already created)
├── research.md                # Phase 0 output (/speckit-plan — generated now)
├── data-model.md              # Phase 1 output (/speckit-plan — generated now)
├── quickstart.md              # Phase 1 output (/speckit-plan — generated now)
├── contracts/
│   ├── resume-prompt-schema.md       # Canonical RESUME-P{N}.md structure
│   └── session-checkpoint-protocol.md # Seven-step exit ritual + kickoff procedure
├── checklists/
│   └── requirements.md        # Quality checklist (already created)
├── RESUME-P1.md               # Created at end of this session (kickoff for Phase 1)
├── RESUME-P2.md               # Created by Phase 1 session at its checkpoint
├── RESUME-P3.md               # Created by Phase 2 session at its checkpoint
├── RESUME-P4.md               # Created by Phase 3 session at its checkpoint
├── RESUME-COMPLETE.md         # Created by Phase 4 session (terminal retrospective)
└── tasks.md                   # Created by /speckit-tasks (NOT this command)
```

### Source Code (repository root)

All changes confined to the existing `Mentoory.Tests.E2E` project:

```text
tests/Mentoory.Tests.E2E/
├── Infrastructure/
│   ├── E2ETestCollection.cs                (existing — no changes)
│   └── PlaywrightFixture.cs                (existing — Phase 1 adds `audit` to Respawn schemas IF needed)
├── Tests/
│   ├── AuditLogViewerTests.cs              (NEW — Phase 1, 9 tests)
│   ├── AuditLogCaptureTests.cs             (NEW — Phase 2, 7 tests)
│   ├── AuditLogCorrelationTests.cs         (NEW — Phase 3, 3 tests)
│   ├── AuditLogRegressionTests.cs          (NEW — Phase 4, 5 tests)
│   ├── AuthorizationTests.cs               (existing — referenced for LoginAsync pattern)
│   ├── AdministrationUsersTests.cs         (existing — referenced for role-assignment UI path)
│   ├── RegistrationTests.cs                (existing — referenced for Register UI path)
│   ├── LoginFlowTests.cs                   (existing — referenced for Login UI path)
│   └── ...                                 (existing — untouched)
└── Mentoory.Tests.E2E.csproj               (existing — no changes expected)
```

**Structure Decision**: single test-project extension — all 24 new tests are xUnit classes in the existing `tests/Mentoory.Tests.E2E/Tests/` directory, each sharing the same `PlaywrightFixture` via `[Collection(E2ETestCollection.Name)]`. No new fixtures, no new projects, no changes outside the test tree (enforced by SC-005 and FR-017).

## Complexity Tracking

No constitution violations; no justifications required.

---

**Phase 0 and Phase 1 artifacts follow in sibling documents:**

- `research.md` — unknowns resolved (fixture respawn, CorrectAnswer trigger path, correlation-header interception, etc.)
- `data-model.md` — Phase / Checkpoint / ResumePrompt logical model (no persistence)
- `contracts/resume-prompt-schema.md` — canonical five-section handoff file
- `contracts/session-checkpoint-protocol.md` — seven-step exit ritual + kickoff procedure
- `quickstart.md` — how a fresh session starts Phase 1 from this branch
