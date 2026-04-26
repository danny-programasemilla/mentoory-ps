# Review Brief: Integration Soak Bundle (016 phase wrap-up)

**Spec:** specs/019-integration-soak-bundle/spec.md
**Generated:** 2026-04-25

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Ship four feature-016 PRs (#11 lifecycle, #12 audit, #13 registration, #14 knowledge) as a single integration bundle to `develop`, gated by the full automated test suite. The bundle exists to amortize the cost of running the E2E suite (~3m50s per run) over four PRs instead of running it after each merge, and to surface cross-feature regressions in one place rather than discover them sequentially in `develop`. This is an operational/release-tactic spec — the "user" is the release manager.

## Scope Boundaries

- **In scope**: 4-way merge tactic, conflict-resolution playbook (per-file resolutions for known overlaps), automated gate definition (build / unit / integration / E2E / DACPAC publish), bisect protocol on gate failure, single bundle PR to develop with cross-reference closure of source PRs.
- **Out of scope**: New code in any of the four features (frozen), backporting conflict resolutions, **coverage-check tool enforcement** (feature 018 implementation pending — only spec/tests/CI workflow ship in this bundle), feature flagging, manual quickstart walkthroughs.
- **Why these boundaries**: The bundle is an integration tactic, not a development phase. Adding work to it (e.g., implementing feature 018's 54 tasks for coverage-check) would defeat the amortization goal.

## Critical Decisions

### Bundle merge order: smallest → largest
- **Choice:** registration (#13) → lifecycle (#11) → audit (#12) → knowledge (#14)
- **Trade-off:** Largest branch (knowledge, 22k LOC) absorbs all conflicts last when integration branch is most populated; conflicts forced to resolve against most up-to-date state.
- **Feedback:** Confirm the size-order logic is right vs. a dependency-order alternative.

### Bisect-by-revert as failure rollback
- **Choice:** If gate fails, revert merge commits in reverse order (knowledge → audit → lifecycle → registration), re-run gate after each revert.
- **Trade-off:** Up to 4 full gate runs in worst case (~30+ min each), but identifies the regression owner deterministically.
- **Feedback:** Acceptable cost ceiling, or prefer fix-forward debugging?

### Conflicts resolved on integration branch only; source branches frozen
- **Choice:** No backport to source PR branches; PRs #11–#14 are closed via cross-reference at ship time, not separately merged.
- **Trade-off:** PR review history is preserved (each PR was reviewed independently) but loses the per-PR merge commit on develop.
- **Feedback:** Acceptable for the audit trail, or prefer per-PR merges with retest?

### Drop coverage-check from this bundle's gate
- **Choice:** Coverage-check tool implementation is not on the registration branch (verified via git inspection). Feature 018 phases T001–T054 are pending. This bundle ships the spec/tests/CI workflow as forward-looking artifacts but does not gate on the tool.
- **Trade-off:** Bundle ships sooner; feature 018's enforcement gate slips to a follow-up bundle.
- **Feedback:** Confirmed — the alternative (implementing feature 018 first) violates the "frozen branches" principle.

## Areas of Potential Disagreement

### NFR-001 soak duration: 1 working day target
- **Decision:** Soft target, not hard deadline; rebase + re-gate triggered at 24h wall-clock.
- **Why this might be controversial:** The conflict surface (16+ overlapping files including 3-way conflicts on `Program.cs`, `MenuConfiguration.cs`, `IntegrationTestBase.cs`, `PlaywrightFixture.cs`) makes 1 day aggressive. A reviewer may push for 2-day target.
- **Alternative view:** Set 2 working days as the target to reduce schedule pressure-induced shortcuts.
- **Seeking input on:** Is 1 day a stretch goal or unrealistic?

### Bundle PR merge style (regular merge commit vs. squash)
- **Decision:** FR-007 recommends regular merge commit to preserve four-branch history.
- **Why this might be controversial:** Some teams prefer linear history on develop; squash gives one bundle commit instead of five.
- **Alternative view:** Squash for develop cleanliness; the integration branch retains the four-branch history for forensics.
- **Seeking input on:** OQ-1 — merge commit or squash?

### Coverage-check tool gap
- **Decision:** Spec acknowledges the implementation isn't on the registration branch and excludes coverage-check from the gate.
- **Why this might be controversial:** The feature 018 spec on the registration branch states coverage-check is required by feature 018's own delivery. Shipping its tests + CI workflow without the implementation feels half-baked.
- **Alternative view:** Hold the bundle until feature 018 implementation lands, even if it adds 54 tasks of work.
- **Seeking input on:** Is the partial feature-018 ship (spec + tests + CI but no tool) acceptable, or do we need to gate the bundle on it?

## Naming Decisions

| Item | Name | Context |
|---|---|---|
| Spec branch | `019-integration-soak-bundle` | Houses spec/plan/tasks artifacts for this bundle tactic |
| Spec directory | `specs/019-integration-soak-bundle/` | Mirrors spec branch name |
| Integration (execution) branch | `019-integration-soak` | Short-lived branch for actual merges + gate runs; deleted post-ship |
| Bundle PR | (TBD at open time) | Bundles all four source PRs into one PR to develop |

## Open Questions

- [ ] OQ-1 — Bundle PR merge style: regular merge commit vs. squash
- [ ] OQ-2 — Bundle PR description: each source PR's full body verbatim vs. cross-reference only

Both are "decide at PR open time" — neither blocks planning.

## Risk Areas

| Risk | Impact | Mitigation |
|---|---|---|
| Registration branch's `Mentoory.Specs.CoverageCheck.Tests/` may fail to build standalone (references the non-existent production project) | High — would block FR-001 standalone build verification, excluding registration from the bundle | FR-001 explicitly handles this: failed branch is excluded, bundle proceeds. Spec also calls this out in Out-of-Scope. Mitigation in execution: stub or exclude the test project from `Mentoory.sln` if needed. |
| 3-way conflicts in `Program.cs`, `MenuConfiguration.cs`, `IntegrationTestBase.cs`, `PlaywrightFixture.cs` | Medium — adds time to soak | Edge Cases section names each file with explicit resolution rule (union of all entries, verify ordering). |
| DACPAC publish fails on cross-schema FK chain (lifecycle.RowVersion + knowledge.* schema + diagnostic FKs) | High — blocks bundle ship | EC-1 names the resolution: hand-stitch SSDT files, verify with `Mentoory.Db.sqlproj` build before gate runs. |
| Audit's `AuditCoverageTests` fails on lifecycle/knowledge commands lacking `[Audited]` | Medium — adds remediation time | EC-2 explicit: add attribute during integration, document in PR. |
| E2E flake mistaken for regression triggers premature bisect | Low — wastes ~30 min per false positive | EC-5: any failing E2E re-run once before bisect; only two consecutive failures count. |
| Soak exceeds 1 day, develop diverges | Medium — stale gate evidence | NFR-001 explicit rebase + re-gate cycle at 24h wall-clock. |

---
*Share with reviewers before implementation. Spec is at `specs/019-integration-soak-bundle/spec.md`.*
