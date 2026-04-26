# Phase 1 Data Model — Integration Soak Bundle

**Date:** 2026-04-25
**Spec:** [spec.md](./spec.md)

This spec ships **no application data model changes**. The bundle merges four source branches whose individual data-model deliverables are documented in their respective `data-model.md` files and ship as part of those branches' merges. This file captures the *operational* entities that the bundle introduces and consumes — refs, PRs, evidence files — and how they relate.

---

## Operational Entities

### Integration Branch

| Field | Value | Notes |
|---|---|---|
| Name | `019-integration-soak` | Distinct from the spec branch `019-integration-soak-bundle` |
| Origin | `origin/develop` (latest at creation time) | Created via `git checkout -b 019-integration-soak origin/develop` |
| Lifetime | Transient (≤ 1 working day target, per NFR-001) | Deleted from `origin` after bundle PR merges |
| Visible state | Public on `origin` (so CI can run gate) | Required for CI hooks (`coverage-check.yml`, etc.) |
| Allowed commits | Merge commits only (one per source PR) + minimal conflict-resolution commits | No feature commits |

**State transitions:**
```
created → registration-merged → lifecycle-merged → audit-merged → knowledge-merged → gate-running → gate-green → bundle-pr-open → merged-to-develop → deleted
                                                                                  ↓
                                                                           gate-red → bisect-revert → gate-rerunning → … (loop) → partial-bundle-shipped OR aborted
```

### Source PR (×4)

| Field | Lifecycle (#11) | Audit (#12) | Registration (#13) | Knowledge (#14) |
|---|---|---|---|---|
| GitHub PR | https://github.com/daperezu/mentoory-ps/pull/11 | https://github.com/daperezu/mentoory-ps/pull/12 | https://github.com/daperezu/mentoory-ps/pull/13 | https://github.com/daperezu/mentoory-ps/pull/14 |
| Branch | `016-project-lifecycle-finish` | `016-audit-pipeline` | `016-registration-access-hardening` | `016-knowledge-module-core` |
| HEAD SHA at brainstorm | `ac4e6b7` | `55875f2` | `90f6592` | `d97b3f2` |
| Bundle merge order | 2 | 3 | 1 | 4 |
| Frozen during soak? | Yes (FR-003) | Yes | Yes | Yes |
| Final disposition | Closed via cross-reference comment, **not** merged | Closed via cross-reference | Closed via cross-reference | Closed via cross-reference |

**State transitions per source PR:**
```
non-draft → frozen-during-soak → (one of:)
                                    ├→ standalone-build-passed → merged-into-integration → bundle-shipped → closed-via-crossreference
                                    └→ standalone-build-failed → excluded-from-bundle → returned-to-pr-with-comment
                                    └→ regression-owner-after-bisect → reverted-from-integration → returned-to-pr-with-comment + bisect-evidence
```

### Bundle PR

| Field | Value | Source |
|---|---|---|
| Base | `develop` | FR-007 |
| Head | `019-integration-soak` | FR-007 |
| Title | "Bundle: 016 ship — registration + lifecycle + audit + knowledge" | Recommended; OQ-1 confirms exact format |
| Body | Cross-references PRs #11/#12/#13/#14, links to this spec, attaches gate evidence | FR-007 + FR-010 |
| Merge style | Regular merge commit (preserves 4-branch history) | FR-007 (recommended); OQ-1 may flip to squash |
| Approval requirement | Standard repo review process; release manager opens; reviewer(s) sign off | Implicit |

### Gate Evidence Files

Attached to the bundle PR as either inline content or upload artifacts. Each file is the captured stdout+stderr of the corresponding gate step, plus a one-line summary at the top.

| File | Content | Source command |
|---|---|---|
| `build-log.txt` | `dotnet build` output | `dotnet build Mentoory.sln -c Release -p:CoverageCheckMode=warn` (post-registration merge onward) |
| `unit-test-log.txt` | All non-Integration / non-E2E test projects | `dotnet test Mentoory.sln -c Release --filter "Category!=Integration&Category!=E2E"` |
| `integration-test-log.txt` | Testcontainers integration tests | `dotnet test tests/Mentoory.Tests.Integration/ -c Release` |
| `e2e-test-log.txt` | Playwright E2E tests | `dotnet test tests/Mentoory.Tests.E2E/ -c Release` |
| `dacpac-publish-log.txt` | DACPAC publish to integration DB | `dotnet publish Mentoory.Db/MentooryDb.sqlproj` + `sqlpackage /Action:Publish /SourceFile:... /TargetServerName:...` |
| `coverage-check.log` | Warn-mode violations report | Auto-emitted by MSBuild `AfterTargets="Build"` target |
| `bisect-log.txt` (only if gate failed) | Sequence of revert commits + post-revert gate results | Manual — release manager records each iteration |

---

## Relationships

```
[Integration Branch] ─merges─> [Source PR #13 registration] ─frozen─> [Bundle PR ─references─> Source PR #13 (then closes it)]
                ├─merges─> [Source PR #11 lifecycle]    ─frozen─> [Bundle PR ─references─> Source PR #11 (then closes it)]
                ├─merges─> [Source PR #12 audit]        ─frozen─> [Bundle PR ─references─> Source PR #12 (then closes it)]
                ├─merges─> [Source PR #14 knowledge]    ─frozen─> [Bundle PR ─references─> Source PR #14 (then closes it)]
                └─runs────> [Gate] ─produces─> [Gate Evidence Files] ─attached-to─> [Bundle PR]

[Bundle PR] ─merges-into─> [develop] (single merge commit preserving the four-branch history)
[Integration Branch] ─deleted-from-origin (after Bundle PR merges)
```

## Validation Rules

These are checked by the operational tasks (in `tasks.md`) and reflected in success criteria:

- **VR-1** (FR-001): Each source PR branch builds standalone with zero warnings before merging. If not, branch is excluded from the bundle.
- **VR-2** (FR-005): After each merge, `dotnet build -p:CoverageCheckMode=warn` returns 0 with zero compiler/StyleCop warnings. Coverage-check warnings are tolerated.
- **VR-3** (FR-006): After all four merges, the full gate (build + unit + integration + E2E + DACPAC publish) returns green.
- **VR-4** (NFR-003): Pre-soak `develop` E2E test count (pinned at execution start) ≤ post-bundle E2E test count, with zero pre-existing failures.
- **VR-5** (NFR-002): DACPAC publishes cleanly to an integration DB; both seed scripts apply idempotently (verify by re-running once, expecting a no-op).
- **VR-6** (FR-007): Bundle PR merges as a regular merge commit; source PRs are closed with cross-reference comments within 6 hours (SC-003).
- **VR-7** (FR-009): Post-bundle `develop` contains all four `specs/{NNN}-*/` directories intact (per `git ls-tree develop -- specs/`).
- **VR-8** (SC-006): If gate fails, bisect reaches a stable state (full ship, partial ship, or full abort) within 4 revert iterations.

## Out of Application Data Model

The bundle does NOT introduce or modify:
- Domain aggregates, value objects, entities, or enums
- DbContext schemas (other than the cross-branch DACPAC merge which already lives in source PRs)
- Application commands, queries, handlers, or DTOs
- Web controllers, views, or view models
- Any production C# / SQL / JS / Razor file (other than conflict resolutions on merge)

For the application data model deltas the four source PRs introduce, see each branch's own `data-model.md`:
- `specs/016-project-lifecycle-finish/data-model.md`
- `specs/016-audit-pipeline/data-model.md`
- `specs/016-registration-access-hardening/data-model.md`
- `specs/016-knowledge-module-core/data-model.md`
