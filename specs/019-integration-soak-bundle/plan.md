# Implementation Plan: Integration Soak Bundle (016 phase wrap-up)

**Branch**: `019-integration-soak-bundle` | **Date**: 2026-04-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/019-integration-soak-bundle/spec.md`

## Summary

Ship the four ready-to-merge feature-016 PRs (#11 lifecycle, #12 audit, #13 registration, #14 knowledge) to `develop` as a single integration bundle, gated by the full automated test suite. The work is not a code feature — it's an operational/release runbook executed by a release manager. The technical approach: create a short-lived `019-integration-soak` branch off latest `origin/develop`, merge the four PRs in size order with merge commits, resolve conflicts in place (per the file-by-file playbook in the spec's Edge Cases), run the full automated gate (build / unit / Testcontainers integration / Playwright E2E / DACPAC publish / coverage-check in warn mode), and ship via one PR to `develop` referencing all four source PRs. On gate failure, bisect by reverting in reverse order until green; reverted branch goes back to its PR.

## Technical Context

**Language/Version**: N/A — operational spec; uses existing C# / .NET 10 codebase
**Primary Dependencies**: git 2.x, gh CLI 2.x, .NET 10.0 SDK with pre-release, Aspire 13.2.0, Docker (for Testcontainers + Aspire), Playwright (managed by `Mentoory.Tests.E2E` test fixture)
**Storage**: SQL Server (DACPAC publish target — Aspire-orchestrated dev DB or local instance)
**Testing**: xUnit (unit suites: Access, Tenant, Diagnostic, Knowledge, Shared, Architecture; integration: `Mentoory.Tests.Integration` with Testcontainers SQL); Playwright (E2E: `Mentoory.Tests.E2E`)
**Target Platform**: Linux dev workstation (primary execution); GitHub Actions Ubuntu runners (CI verification)
**Project Type**: operational/release-tactic spec — no code shipped from this spec; only orchestrates merging four existing PR branches into one bundle PR
**Performance Goals**: Soak ≤ 1 working day soft target. Full gate runtime budget: build ~3 min, unit tests <1 min, integration ~2 min, E2E ~4 min (per audit PR baseline 3m50s for 121 tests, plus knowledge + lifecycle test additions ≈ 6–7 min target), DACPAC publish ~1 min, coverage-check ≤ 2 s (its own NFR-001). Total wall-clock ≤ 15 min serial, less in parallel.
**Constraints**: zero compiler/StyleCop warnings (constitution V); zero pre-existing E2E regressions (NFR-003); single merge commit on `develop` (FR-007); coverage-check runs in `-p:CoverageCheckMode=warn` from registration onward (FR-005, FR-006).
**Scale/Scope**: 4 source PR branches, ~47k LOC delta combined, 16+ overlapping files (3-way conflicts on `Program.cs`, `MenuConfiguration.cs`, `IntegrationTestBase.cs`, `PlaywrightFixture.cs`), DACPAC cross-schema FK chain (lifecycle `Projects.RowVersion` + knowledge `knowledge.*` + diagnostic FKs), expected total E2E count after bundle ≈ 150–170 tests (121 baseline + 24 audit + ~15-20 lifecycle + ~20-25 knowledge — exact count pinned during execution).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Reviewed against `.specify/memory/constitution.md` v1.1.1. This spec is operational and ships no new code, so most principles are N/A. Relevant gates:

| Principle | Applies? | Status |
|---|---|---|
| I. Clean Architecture Layer Boundaries | N/A | No new code; conflict resolutions on shared files (e.g., `Program.cs`, `MenuConfiguration.cs`) preserve existing layer separation by union-merge. |
| II. CQRS Pattern Requirements | N/A | No new commands or queries. |
| III. DDD Constraints | N/A | No new domain code. |
| IV. Integration Events | N/A | No new events. |
| V. Zero-Warnings Policy | ✅ ENFORCED | FR-001, FR-005, FR-006 all require `dotnet build` zero warnings; SC-001 explicit. |
| VI. DateTime Handling | N/A | No new code. |
| VII. Naming Conventions | N/A | No new artifacts. |
| VIII. File Organization | N/A | No new files in source tree. |
| IX. Spanish-First UI | N/A | No UI changes. |
| X. Role Hierarchy & Session Context | N/A | No new controllers; conflict resolutions on `MenuConfiguration.cs` use union-merge, preserving GlobalAdmin presence in all groups (constitution requirement) by default. |
| XI. SSDT/DACPAC Database Strategy | ✅ ENFORCED | NFR-002 covers cross-schema chain; both seed scripts apply idempotently per constitution rule. EC-1 names the SSDT hand-stitch as the resolution mechanism. |

**Result**: All applicable gates pass. No violations. **Complexity Tracking section omitted (no violations to justify).**

## Project Structure

### Documentation (this feature)

```text
specs/019-integration-soak-bundle/
├── spec.md              # Feature specification
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command) — operational runbook
├── contracts/
│   └── gate-commands.md # Phase 1 output — exact command sequences for the gate
├── checklists/
│   └── requirements.md  # Spec quality checklist
├── REVIEW-SPEC.md       # Spec review record
├── review_brief.md      # Reviewer-facing summary
└── tasks.md             # Phase 2 output (/speckit.tasks command — NOT created by /speckit.plan)
```

### Execution-time artifacts (NOT in source tree)

These exist transiently during the bundle execution and either become part of `develop`'s history or are cleaned up:

```text
[git refs]
├── 019-integration-soak           # transient integration branch off origin/develop
└── 019-integration-soak-bundle    # this spec's branch (already exists)

[GitHub artifacts]
├── Bundle PR (yet to open)        # 019-integration-soak → develop, single merge commit
├── PR #11 lifecycle               # closed via cross-reference at ship time, not merged
├── PR #12 audit                   # closed via cross-reference at ship time, not merged
├── PR #13 registration            # closed via cross-reference at ship time, not merged
└── PR #14 knowledge               # closed via cross-reference at ship time, not merged

[gate evidence — attached to Bundle PR]
├── build-log.txt                  # `dotnet build -p:CoverageCheckMode=warn` output
├── unit-test-log.txt              # `dotnet test --filter Category!=Integration&Category!=E2E` output
├── integration-test-log.txt       # `dotnet test tests/Mentoory.Tests.Integration/` output
├── e2e-test-log.txt               # `dotnet test tests/Mentoory.Tests.E2E/` output
├── dacpac-publish-log.txt         # `dotnet publish Mentoory.Db/ ...` output
└── coverage-check.log             # warn-mode violations report
```

**Structure Decision**: Single-spec layout under `specs/019-integration-soak-bundle/`. No source code is created or modified by this spec; the "deliverables" are the bundle PR, its merge commit on `develop`, and the captured gate evidence. The integration branch (`019-integration-soak`) is short-lived and deleted after the bundle ships.

## Complexity Tracking

> Constitution Check has no violations. Section omitted.
