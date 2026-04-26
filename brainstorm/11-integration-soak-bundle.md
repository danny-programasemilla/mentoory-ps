# Brainstorm: Integration Soak Bundle (016 phase wrap-up)

**Date:** 2026-04-25
**Status:** spec-created
**Spec:** specs/019-integration-soak-bundle/

## Problem Framing

Four feature-016 PRs (#11 lifecycle, #12 audit, #13 registration, #14 knowledge) were authored in parallel worktrees and are now non-draft, individually clean against develop, and awaiting merge. Merging them one-by-one would re-run the full E2E suite (~3m50s) after each ship, plus risk introducing cross-feature regressions sequentially in develop. The user wanted a tactic that:

- Ships all four atomically
- Gates on the full automated suite once at the end
- Has an explicit failure-rollback plan
- Doesn't lose work or quality if something goes wrong

The non-trivial complications:
- 16+ files overlap pairwise across the four branches (3-way conflicts on `Program.cs`, `MenuConfiguration.cs`, test infrastructure, `RegisterUserCommand.cs`)
- DACPAC has cross-branch schema work (lifecycle's `Projects.RowVersion` + knowledge's `knowledge.*` schema with cross-FKs to diagnostic)
- Audit's `[Audited]` coverage architecture test will trip on commands lifecycle/knowledge introduced without the attribute
- Registration ships feature 018's spec + tests + CI workflow, but the coverage-check tool **production project is not built yet** — the test project can't reference a non-existent production project, which would break standalone build

## Approaches Considered

### A: Release-train soak (chosen)
- Create `019-integration-soak` off develop, merge the 4 PRs in size order (smallest → largest), gate once, ship as one PR.
- Pros: Amortizes E2E cost (1 full gate vs. 4); cross-feature regressions found in one place; PR review history preserved (source PRs were reviewed independently); rollback = revert one merge commit on develop.
- Cons: Source PRs become stale on conflict resolution; if gate fails, bisect requires up to 4 full gate reruns; "bundle" reviews on develop combine 47k LOC.

### B: Stacked rebase chain
- Rebase each branch on the previous one's resolution: lifecycle → audit → registration → knowledge, each rebase against the cumulative state.
- Pros: Cleaner linear history; each PR can be re-reviewed in its rebased state.
- Cons: O(N) conflict resolutions instead of O(1); the whole point was to amortize; rebasing freezes nothing — every rebase re-creates commit hashes and breaks existing PR review threads.

### C: Octopus merge (single commit with 4 parents)
- `git merge` with all 4 branches as parents at once.
- Pros: Single atomic commit; preserves all 4 lineages.
- Cons: Git's octopus strategy refuses to auto-resolve any conflicts; given the file overlap, this just won't work without splitting back into sequential merges anyway.

## Decision

**Approach A — release-train soak with bisect-by-revert rollback.** Confirmed via the brainstorm session:
- Bundle ships as a single merge commit to develop (preserves 4-branch history)
- New branch `019-integration-soak` off develop (not the existing 017 phase0 branch)
- Conflicts resolved exclusively on integration branch; source PRs frozen
- Full automated gate (build + unit + Testcontainers integration + Playwright E2E + DACPAC publish) — coverage-check explicitly excluded because feature 018 implementation isn't ready
- On failure, bisect by reverting in reverse order (knowledge → audit → lifecycle → registration); first revert that turns gate green identifies the regression owner; partial bundle ships if remaining branches stay green
- Soak target 1 working day, soft cap with rebase + re-gate at 24h wall-clock

The first review pass caught a critical assumption error: the spec referenced the coverage-check tool as a gate step, but the production project doesn't exist on the registration branch (only the spec, tests, and CI workflow ship in this bundle). Fix: removed coverage-check from FR-006 / Key Entities / SC-002; added explicit Out-of-Scope bullet citing the implementation gap. Also added a pre-flight FR-001 standalone-build verification (excludes any branch that fails standalone build from the bundle).

## Open Threads

- OQ-1: Bundle PR merge style — regular merge commit (preserves 4-branch history) vs. squash (single line on develop). Decision deferred to PR open time.
- OQ-2: Bundle PR description — each source PR's body verbatim (self-contained for reviewers) vs. cross-reference only (cleaner). Decision deferred to PR open time.
- Verification needed during execution: registration branch's standalone build — the new `Mentoory.Specs.CoverageCheck.Tests/` project may fail to compile if it references the non-existent production project. If it does, exclude or stub the test project before merging registration.
- Follow-up bundle: once feature 018 phases T001–T054 implementation lands, a second bundle gates on coverage-check.
