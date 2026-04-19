# Implementation Plan: Access-Security Delivery Quality Gate

**Branch**: `018-access-security-delivery-quality-gate` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/018-access-security-delivery-quality-gate/spec.md`

## Summary

Amend the access-security constitution with six new testing floor categories and a Delivery Quality Gate section; implement a `dotnet` console tool (`Mentoory.Specs.CoverageCheck`) that parses every `spec.md` under `specs/` for `FR-###` and `SC-###` identifiers, reflects (no execution) over every test assembly for xUnit `[Trait("Spec",…)]` / `[Trait("Sc",…)]` / `[Trait("Floor",…)]` attributes, and reports Unclaimed Identifiers and Dangling Traits; wire the tool into MSBuild via a `.targets` file so `dotnet build` runs it; add a required GitHub Actions `coverage-check` stage; retrofit traits onto feature 016's existing 124 tests; and write ~7 new automated scenarios closing the gaps identified in the PR #13 analysis. Monolithic PR per the brainstorm decision. Gate binds only to specs declaring `access-security: true` in their front-matter.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0) — tool and all test projects
**Primary Dependencies**: xUnit 2.x (`TraitAttribute`), `System.Reflection.MetadataLoadContext`, `System.CommandLine` (CLI parsing), MSBuild custom `.targets`, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory` — existing), `Testcontainers.MsSql` (existing), `Microsoft.Playwright` (existing)
**Storage**: N/A for the tool. Integration tests use the existing Testcontainers SQL Server fixture; no new tables, no new seed data.
**Testing**: xUnit for tool self-tests; FluentAssertions for assertions; Moq where mocking is needed; existing Testcontainers + `WebApplicationFactory` harness for new integration scenarios; existing Playwright fixture for new E2E scenarios
**Target Platform**: Linux (Ubuntu CI runner) and Windows dev boxes — same as the rest of the repo
**Project Type**: CLI tool + test additions inside an existing modular-monolith web application. No web-tier changes.
**Performance Goals**: Coverage tool ≤ 2 s per run (NFR-001); 50-probe response-equality sweep ≤ 15 s wall time (NFR-004)
**Constraints**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` across all new code; test parallelism across the three test projects preserved (NFR-003); no touch to feature-016 production code (Out of Scope); no interactive or visual-inspection tests (Out of Scope)
**Scale/Scope**: 1 new console project, 1 new `.targets` file, 1 new GitHub Actions workflow job, retrofits onto ~50 existing test methods across 3 test projects, 7 new test methods (~400 LOC of test code total), constitution amendment (~80 lines)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against `.specify/memory/constitution.md` version 1.1.1.

| # | Principle | Relevance | Verdict | Notes |
|---|-----------|-----------|---------|-------|
| I | Clean Architecture Layer Boundaries | Tool is a standalone CLI; test retrofits are in existing test projects only | ✅ Pass | The new `tools/Mentoory.Specs.CoverageCheck/` project references **no** production code and no framework types leak into Domain/Application. |
| II | CQRS Pattern Requirements | No commands/queries added | ✅ Pass — N/A | |
| III | DDD Constraints | No domain changes | ✅ Pass — N/A | |
| IV | Integration Events (ADR-001) | No new events | ✅ Pass — N/A | |
| V | Zero-Warnings Policy | Tool + retrofits + new tests all compile under `TreatWarningsAsErrors=true` | ✅ Pass | NFR compatible. |
| VI | DateTime Handling | Tool does not deal with business time; FR-019 sweep uses `ITimeProvider`-agnostic request generation | ✅ Pass — N/A | |
| VII | Naming Conventions | Tool class names: `Program`, `SpecParser`, `TraitReflector`, `CoverageReport`, `TextReportWriter` — follow PascalCase patterns; project named `Mentoory.Specs.CoverageCheck` | ✅ Pass | |
| VIII | File Organization | One class per file; no JavaScript added; no SSDT scripts | ✅ Pass | |
| IX | Spanish-First UI | Tool is not user-facing; all output is developer-facing (English) — consistent with existing CI output | ✅ Pass — N/A for user-facing | |
| X | Role Hierarchy & Session Context | No runtime role changes; the gate governs tests, not access | ✅ Pass — N/A | |
| XI | SSDT/DACPAC Database Strategy | No schema changes | ✅ Pass — N/A | |

**Access-Security Constitution Check** (`.specify/memory/access-security-constitution.md`):

- Amendment is additive. Preserves Sections 1–12 numbering (NFR-005). Either appends as 11.9+ OR introduces a new Section 13; Phase 1 `contracts/` commits to one placement.
- Amendment scoped via an anchor phrase so it binds only access-security features (FR-004); other areas remain unaffected.

**Result**: All gates pass. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/018-access-security-delivery-quality-gate/
├── plan.md              # This file
├── spec.md              # Feature specification
├── review_brief.md      # Reviewer digest
├── REVIEW-SPEC.md       # Spec review report (status: SOUND)
├── research.md          # Phase 0 — decisions + rationale
├── data-model.md        # Phase 1 — logical entities the tool reasons about
├── quickstart.md        # Phase 1 — verification walkthrough
├── contracts/           # Phase 1 — CLI, MSBuild, CI, trait, spec-front-matter contracts
│   ├── coverage-check-cli.md
│   ├── msbuild-integration.md
│   ├── github-actions-stage.md
│   ├── trait-conventions.md
│   └── spec-front-matter.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 — /speckit-tasks output (NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
# NEW — coverage tool
tools/
└── Mentoory.Specs.CoverageCheck/
    ├── Mentoory.Specs.CoverageCheck.csproj
    ├── Program.cs                       # CLI entry (System.CommandLine)
    ├── Parsing/
    │   ├── SpecParser.cs               # regex-based FR/SC + Coverage: N/A + access-security front-matter
    │   └── SpecModels.cs               # FeatureSpec, RequirementId, ExclusionMarker records
    ├── Reflection/
    │   ├── TraitReflector.cs           # MetadataLoadContext over test assemblies
    │   └── TestClaimModels.cs          # TestClaim, TraitKind records
    ├── Coverage/
    │   ├── CoverageAnalyzer.cs         # joins specs + traits → reports
    │   ├── CoverageReport.cs           # Unclaimed, Dangling, ExcludedIds, FloorCategoryGaps
    │   └── FloorCategories.cs          # the 6 canonical names + trigger conditions
    └── Output/
        └── TextReportWriter.cs         # deterministic CLI output for CI logs

tools/Mentoory.Specs.CoverageCheck/build/
└── CoverageCheck.targets                # MSBuild integration

tests/Mentoory.Specs.CoverageCheck.Tests/      # NEW — self-tests for SC-001 canary
├── Mentoory.Specs.CoverageCheck.Tests.csproj
├── Parsing/SpecParserTests.cs
├── Reflection/TraitReflectorTests.cs
├── Coverage/CoverageAnalyzerTests.cs
├── Coverage/FloorCategoriesTests.cs
└── Fixtures/                                   # synthetic specs + test-assembly fixture
    ├── specs/                                  # hand-written sample spec.md files
    └── assemblies/                             # xunit test assemblies built on demand

# EXISTING — retrofit targets (add [Trait] attributes; write new test methods)
tests/Mentoory.Access.Tests/
├── Handlers/
│   ├── RegisterUserHandlerTests.cs           # +[Trait] claims for 016 FR/SC
│   └── AdminEnrollUserHandlerTests.cs        # +[Trait] claims
├── Services/UserProvisioningServiceTests.cs  # +[Trait] claims
└── Validators/
    ├── RegisterUserValidatorTests.cs         # +[Trait] claims
    ├── AdminEnrollUserValidatorTests.cs      # +[Trait] claims
    └── PasswordIdentifyingDataRuleTests.cs   # +[Trait] claims

tests/Mentoory.Tests.Integration/Identity/
├── RegistrationTests.cs                      # +[Trait]; add FR-015 (dup-NID admin)
├── AdminEnrollmentTests.cs                   # +[Trait]; add FR-016 (admin fresh), FR-017 (unauth)
├── PublicRegistrationSweepTests.cs           # NEW — FR-019 50-probe equality sweep
├── DefenseInDepthTests.cs                    # NEW — FR-021 antiforgery + rate-limit
└── PasswordIdentifyingDataAdminIntegrationTests.cs  # NEW — FR-018 admin-side national-ID rejection

tests/Mentoory.Tests.E2E/Tests/
├── RegistrationTests.cs                      # +[Trait]; FR-018 E2E admin/public parity; FR-020 form-state
└── AdministrationUsersTests.cs               # +[Trait] claims

# EXISTING — constitution amendment
.specify/memory/
└── access-security-constitution.md           # amended: new Section 11.9 (floor categories)
                                              #          + new Section 13 (Delivery Quality Gate)

# EXISTING — spec front-matter retrofit
specs/016-registration-access-hardening/spec.md   # add `access-security: true` front-matter
specs/018-access-security-delivery-quality-gate/spec.md   # add `access-security: true`

# EXISTING — solution + build integration
Mentoory.sln                                  # add Mentoory.Specs.CoverageCheck + its Tests project
Directory.Build.targets                       # NEW (or extended) — import CoverageCheck.targets at solution scope

# NEW — CI wiring
.github/workflows/
└── coverage-check.yml                        # OR integrate into existing workflow as a new job
```

**Structure Decision**: Single-repo monolith. Coverage tool lives at `tools/Mentoory.Specs.CoverageCheck/` (OQ-002 resolved in research.md). The tool's MSBuild integration sits at `tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets` and is imported from the repo-root `Directory.Build.targets` so `dotnet build` from any working directory fires the check. Trait retrofits and new test methods stay inside the three existing test projects — no new test project is introduced *for production tests*; a separate `tests/Mentoory.Specs.CoverageCheck.Tests/` project exists only for the tool's own unit tests (required by SC-001's self-test).

## Complexity Tracking

*All gates pass. No violations to justify.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
