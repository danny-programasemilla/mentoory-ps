# Feature Specification: Integration Soak Bundle (016 phase wrap-up)

**Feature Branch**: `019-integration-soak-bundle`
**Created**: 2026-04-25
**Status**: Draft
**Input**: User description: "Merge the four ready-to-ship 016-* PRs (#11 lifecycle, #12 audit, #13 registration, #14 knowledge) as a single integration bundle to develop, gated by the full automated test suite, to avoid the cost of re-running E2E after each individual merge and to surface cross-feature regressions in one place."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bundle ships green on the first gate run (Priority: P1)

The release manager creates the integration branch, merges the four PR branches in size order, runs the full automated gate, and ships the bundle to develop as a single merge commit referencing all four source PRs.

**Why this priority**: This is the happy path the entire tactic is designed for. If it works, it eliminates ~3× redundant E2E runs and surfaces cross-feature integration issues only once.

**Independent Test**: Can be fully tested by creating the integration branch, performing the four merges, running the full gate, and confirming a single PR opens and merges to develop with the four source PRs closed via cross-reference comments.

**Acceptance Scenarios**:

1. **Given** the four source PR branches each build standalone with zero warnings, **When** the release manager creates `019-integration-soak` from `origin/develop` and merges the four branches in order (registration → lifecycle → audit → knowledge) with merge commits, **Then** the integration branch contains all four feature sets with no leftover conflict markers and `dotnet build` passes with zero warnings.
2. **Given** the integration branch is built and tests are runnable, **When** the full gate runs (unit + Testcontainers integration + Playwright E2E + DACPAC publish), **Then** every step is green and the test logs are captured as evidence.
3. **Given** the gate is green, **When** a single PR opens from `019-integration-soak` to `develop` and is merged as a regular merge commit, **Then** `develop` contains all four features and the four source PRs are closed with comments cross-referencing the bundle PR (not separately merged).

---

### User Story 2 - Bundle fails the gate; bisect identifies the regression owner (Priority: P2)

The bundle fails one or more gate steps after all four merges land. The release manager bisects by reverting merge commits in reverse order until the gate is green again, ships the partial bundle, and routes the reverted branch back to its PR for follow-up.

**Why this priority**: This is the safety valve. Without a clear bisect protocol, a failed bundle becomes a debugging session of unknown duration; with one, the worst case is a partial ship plus a known-bad branch.

**Independent Test**: Can be tested by injecting a known regression on one branch, running the bundle, and confirming the bisect protocol identifies the correct branch and produces a green partial bundle.

**Acceptance Scenarios**:

1. **Given** all four merges have landed but the gate fails, **When** the release manager reverts the most recent merge (knowledge), re-runs the gate, and observes whether it passes, **Then** either the gate passes (knowledge owns the regression) or the next revert proceeds (audit, then lifecycle, then registration).
2. **Given** the bisect identifies a single regression-owning branch, **When** the partial bundle (the remaining 3 merges) re-runs the gate green, **Then** the partial bundle ships via PR to develop and the reverted branch is left open with a comment explaining the regression and the bisect evidence.
3. **Given** an E2E test fails once, **When** the release manager re-runs that single test before triggering the bisect, **Then** a one-time pass is treated as flake (no bisect) and a second consecutive failure is treated as a real regression (bisect proceeds).

---

### User Story 3 - Soak exceeds one working day; integration branch is rebased (Priority: P3)

The soak runs longer than one working day (conflict resolution took longer, gate flakes required investigation, etc.). The release manager rebases the integration branch on the latest `origin/develop` and re-runs the full gate before shipping.

**Why this priority**: Rare but critical edge case. If the integration branch goes stale, the gate evidence becomes stale too; shipping without a re-gate hides regressions caused by intervening develop activity.

**Independent Test**: Can be tested by simulating a develop commit during the soak, confirming the integration branch can rebase cleanly (or surface conflicts), and confirming a fresh full-gate run produces fresh evidence.

**Acceptance Scenarios**:

1. **Given** the soak has run for more than one working day and `origin/develop` has new commits, **When** the release manager rebases `019-integration-soak` onto `origin/develop`, **Then** any conflicts are resolved on the integration branch and the build is green before the gate runs.
2. **Given** the rebase is complete, **When** the full gate re-runs end-to-end, **Then** fresh test logs are captured as the canonical ship evidence, replacing the stale pre-rebase logs.

---

### Edge Cases

- **DACPAC schema collision** between lifecycle's `Projects.RowVersion` column and knowledge's `knowledge.*` schema with cross-FKs to `diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId` and `diagnostic.Questions.TopicId`. Resolution: hand-stitch SSDT files so both column add and cross-schema FK ship; verify by building `Mentoory.Db/MentooryDb.sqlproj`. Both seed scripts (`004.SeedTestData.sql` and `005.SeedKnowledgeData.sql`) must apply idempotently in deploy order.
- **`[Audited]` coverage gap** on lifecycle/knowledge commands. Audit's `AuditCoverageTests` will fail on any qualifying command (regex `^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$`) lacking the attribute. Resolution: add `[Audited]` to qualifying commands during integration; document the additions in the bundle PR.
- **3-way conflict** in `Program.cs`, `MenuConfiguration.cs`, `IntegrationTestBase.cs`, `PlaywrightFixture.cs`. Resolution: union of all entries (DI registrations, menu items, test setup); verify menu order and DI ordering are sane.
- **`RegisterUserCommand.cs` semantic conflict**: registration restructures the handler, audit decorates the command. Resolution: apply registration's restructure first, then re-apply audit's `[Audited]` attribute on the post-restructure command (and on `AdminEnrollUserCommand` if its name pattern qualifies).
- **E2E flake vs real regression**: any failing E2E test is re-run once before triggering the bisect. Two consecutive failures = real regression. One pass = flake; continue.
- **Two independent regressions** in different branches. Both reverts ship; the remaining 2-branch bundle ships if the gate is green; the two reverted branches go back to their PRs.
- **Post-merge regression discovered on develop** after the bundle ships. Default action: revert the bundle merge commit (one revert undoes all four) and triage in a follow-up bundle. Fix-forward only if the regression is small and isolated.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each source PR branch (#11, #12, #13, #14) is verified to build standalone (`dotnet build`, zero warnings) on its own tip before any merging begins. The registration branch (#13) builds in strict coverage-check mode (its own retrofit must pass); the other three branches do not contain the coverage-check tool, so their standalone builds run without it. A branch that fails standalone build is removed from the bundle and its owner is notified; the soak proceeds with the remaining green branches.
- **FR-002**: Integration branch `019-integration-soak` is created from latest `origin/develop` with a clean working tree.
- **FR-003**: Source PR branches merge into the integration branch in order: registration (#13) → lifecycle (#11) → audit (#12) → knowledge (#14). Each merge is its own merge commit (no fast-forward, no squash).
- **FR-004**: Conflicts are resolved exclusively on the integration branch. The four source branches are frozen for the duration of the soak; no commits land on them.
- **FR-005**: Each merge step is followed by a build check (`dotnet build -p:CoverageCheckMode=warn`, zero warnings excluding coverage-check violations) before the next merge proceeds. A merge that breaks build is fixed in place, not reverted. Coverage-check warn mode is mandatory from the moment registration's coverage-check tool is on the integration branch (i.e., from step 1 onward); without warn mode, dangling-trait violations from the other three branches would block the gate.
- **FR-006**: After all four are merged and building, the full gate covers: all unit test projects pass, `Mentoory.Tests.Integration` (Testcontainers SQL) passes, `Mentoory.Tests.E2E` (Playwright) passes, DACPAC publish against an integration DB succeeds, and the coverage-check tool runs in warn mode and produces a recorded violations report attached to the bundle PR as evidence. Coverage-check must complete in ≤ 2 seconds (its own NFR); coverage violations do NOT block the bundle but ARE captured for follow-up. Steps within the gate may run in parallel where the toolchain supports it; the spec does not mandate ordering between independent steps.
- **FR-007**: On green, a single PR opens from `019-integration-soak` to `develop`. The PR body cross-references PRs #11/#12/#13/#14 and includes the gate evidence (test counts, log links, DACPAC publish output). Merge style is a regular merge commit (preserves the four-branch history). PRs #11–#14 are closed with a comment pointing at the bundle PR — not merged.
- **FR-008**: On red, bisect by reverting merge commits in reverse order (knowledge → audit → lifecycle → registration), re-running the full gate after each revert. The first revert that returns the gate to green identifies the regression owner. The reverted branch goes back to its PR for follow-up; the remaining bundle ships if still green.
- **FR-009**: All four features' `specs/{NNN}-*/` artifacts (spec, plan, tasks, research, contracts, quickstart, implementation-notes, brainstorm) must reach `develop` intact. No documentation loss is acceptable as a conflict-resolution shortcut.
- **FR-010**: Bundle PR title and body identify it as a bundle ship and list the four source PR numbers + spec directories. The body links to this spec (`specs/019-integration-soak-bundle/`) as the operational source of truth.

### Non-Functional Requirements

- **NFR-001**: Soak duration target is 1 working day from integration branch creation to bundle PR opening. This is a soft target, not a hard deadline. If the soak exceeds 24 hours of wall-clock time, rebase on the latest `origin/develop`, re-run the full gate, and capture the rebase + re-gate evidence before shipping.
- **NFR-002**: DACPAC publishes cleanly against an integration DB. Specifically: lifecycle's `Projects.RowVersion` column + knowledge's `knowledge.*` schema (10 tables) + cross-FKs to `diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId` and `diagnostic.Questions.TopicId` apply in deploy order. Both seed scripts (`004.SeedTestData.sql`, `005.SeedKnowledgeData.sql`) apply idempotently.
- **NFR-003**: Zero regression invariant: every E2E test that passed on `origin/develop` immediately before the soak began must still pass on the integration branch. New E2E tests introduced by the four source branches (lifecycle ~6 files, audit 24 tests across 4 files, knowledge ~6 files) must all pass. The bundle's total E2E pass count must be strictly greater than the pre-soak `develop` pass count.
- **NFR-004**: The gate is reproducible by a single command sequence documented in the spec's quickstart (a future planning artifact), runnable locally and in CI.

### Key Entities

- **Integration branch (`019-integration-soak`)**: Short-lived branch off `origin/develop` that holds the four-way merge and serves as the gate target. Distinct from this spec's branch (`019-integration-soak-bundle`), which holds spec/plan/tasks artifacts.
- **Bundle PR**: The single PR from `019-integration-soak` to `develop` that ships all four features atomically. Cross-references but does not consume PRs #11–#14.
- **Source PR branches**: PR #11 (`016-project-lifecycle-finish`), PR #12 (`016-audit-pipeline`), PR #13 (`016-registration-access-hardening`), PR #14 (`016-knowledge-module-core`). Frozen during soak, closed via cross-reference at ship time.
- **Gate evidence**: Captured test logs, build output, DACPAC publish output, and coverage-check warn-mode violations report attached to the bundle PR as ship justification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All source branches that passed standalone build (FR-001) merge into `019-integration-soak` with no leftover conflict markers and a clean `dotnet build` (zero warnings, `TreatWarningsAsErrors=true` per constitution).
- **SC-002**: Full gate is green on the integration branch — every unit test project passes (Access, Tenant, Diagnostic, Knowledge, Shared, Architecture), Testcontainers integration tests pass, all Playwright E2E tests pass (NFR-003 invariant: pre-soak baseline preserved + new tests green), DACPAC publish succeeds.
- **SC-003**: Bundle PR merges to `develop` as a single merge commit preserving the four-branch history. PRs #11/#12/#13/#14 are closed with cross-reference comments within 6 hours of the bundle merge — not separately merged.
- **SC-004**: Post-merge `develop` contains all four (or remaining, if any branch was excluded) `specs/{NNN}-*/` directories, both DACPAC seed scripts, and the four feature implementations interoperate (audit captures lifecycle/knowledge commands; knowledge cascade respects lifecycle stage gates if applicable).
- **SC-005**: Soak target is 1 working day; if exceeded, rebase + re-gate evidence is documented in the bundle PR before ship.
- **SC-006**: If the gate fails, the bisect protocol identifies the regression owner within at most four revert-and-regate iterations, and either ships a partial bundle of the remaining green branches or aborts cleanly with no commits landing on `develop`.

## Out of Scope

- Implementing or modifying any code in the four features — they are frozen.
- Backporting conflict resolutions to source PR branches (they will be closed at ship time, not merged).
- **Strict coverage-check enforcement.** Feature 018's coverage-check tool ships fully built on the registration branch (`tools/Mentoory.Specs.CoverageCheck/` with Program.cs, CoverageAnalyzer, and an MSBuild `AfterTargets="Build"` target that auto-fires on every solution-level `dotnet build`). The bundle's gate therefore *runs* coverage-check, but in **warn mode** (`-p:CoverageCheckMode=warn`) — the lifecycle, audit, and knowledge specs introduce SC-### / FR-### identifiers whose tests have not yet been retrofitted with `[Trait("Spec",..)("Sc",..)]` attributes. Strict enforcement would block the bundle on dangling-trait / unclaimed-identifier violations whose remediation is *not* in this bundle's scope. The bundle records the warn-mode violations as evidence in the PR body; a follow-up spec backports the trait retrofits and re-gates in strict mode.
- Deferred items inside each PR's description (audit's manual Spanish QA, lifecycle's manual quickstart W1–W5, knowledge's deferred T092 UI, registration's feature-018 implementation phases T001–T054). Those remain TODO post-merge or are tracked in their own follow-up specs.
- The five cross-cutting features tracked elsewhere in roadmap brainstorm #06 (subscription, notification, plus the rest).
- Feature flagging the bundle (no flag infra in the project).
- Manual quickstart walkthroughs from each spec — only the **automated** gate is the ship criterion.

## Assumptions

- The four source PR branches are non-draft and merge cleanly into `develop` individually — verified via `git merge-tree` against current `develop` (no conflict markers in any pairwise probe).
- Each source PR branch builds standalone with zero warnings (verified by FR-001 before the soak begins). If any branch fails standalone build, it is excluded from the bundle and the bundle proceeds with the remaining branches.
- CI workflows for build / unit / integration / E2E exist and run on push.
- A DACPAC publish target is available (Aspire dev DB or a local SQL Server instance) and the publishing pipeline is wired.
- `gh` CLI is authenticated for PR operations (open, comment, close).
- The integration branch (`019-integration-soak`) and this spec branch (`019-integration-soak-bundle`) coexist briefly but never collide on `develop` (the integration branch is deleted post-ship; only the spec dir remains).

## Open Questions

- **OQ-1**: Bundle PR merge style — regular merge commit (preserves 4-branch history; FR-007's recommendation) vs. squash (single line on `develop`). Confirm before locking the merge button.
- **OQ-2**: Bundle PR description — include each source PR's full body verbatim (self-contained for reviewers) or cross-reference only (cleaner)?
