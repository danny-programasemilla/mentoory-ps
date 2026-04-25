# Review Brief: Audit Pipeline — Browser E2E Coverage

**Spec:** specs/017-audit-e2e/spec.md
**Generated:** 2026-04-19

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Add 24 browser-driven Playwright tests that prove feature 016-audit-pipeline's user-visible surface (admin viewer + menu + filters + Spanish copy) works end-to-end, and that auditable UI actions (register, login, assign role, correct answer) produce correctly-shaped audit rows with password redaction intact. The work is deliberately split into four phases, each ending in a mandatory commit+push+clear+resume checkpoint to prevent AI-session drift on multi-hour test-writing work.

## Scope Boundaries

- **In scope:** 24 E2E tests under `tests/Mentoory.Tests.E2E/Tests/AuditLog*.cs`, four resume-prompt files, one final retrospective.
- **Out of scope:** performance/load on the viewer (016 carry-over), visual regression, E2E for commands with no UI (`AssignMentor`, `RegisterInternalUser`), background-service dispatches, cross-browser.
- **Why these boundaries:** the E2E layer's unique value is catching regressions invisible at the integration layer (CSS, auth filters, Spanish copy, redaction path). Commands without a UI surface cannot regress at the UI layer, so integration coverage is sufficient for them.

## Critical Decisions

### Four-phase split with mandatory checkpoints
- **Choice:** split delivery into P1 (viewer shell, 9 tests) → P2 (capture flows, 7 tests) → P3 (correlation, 3 tests) → P4 (regressions, 5 tests). Each phase ends in a commit+push+clear+resume handoff.
- **Trade-off:** four commits instead of one, and slight overhead per session boundary, in exchange for bounded context per AI session and resumable handoffs.
- **Feedback:** is this phasing the right slice? Would you re-sequence or merge phases?

### Resume-prompt schema with duplicated invariants
- **Choice:** every `RESUME-PN.md` file carries a full copy of the Invariants block (seeded users, fixture conventions, Spanish-copy rule). Not a reference to a shared section.
- **Trade-off:** storage redundancy for self-sufficiency — any RESUME file alone is enough to start the next session even if the spec has evolved.
- **Feedback:** is duplication-as-drift-signal worth the noise, or would you prefer a single authoritative invariants file referenced from each RESUME?

### Test-infrastructure as product boundary
- **Choice:** the spec deliberately names specific file paths, class names, and command-line filters — things that a product-feature spec would treat as implementation details.
- **Trade-off:** the spec's Purpose is test coverage itself, so the test files ARE the delivery artifact. Refusing to name them would leave the spec hollow.
- **Feedback:** comfortable with this deviation from the usual "no implementation details in spec" rule?

## Areas of Potential Disagreement

### Choice of phase boundaries (by user story vs. by complexity)

- **Decision:** Phases map to user stories from the original 016 feature (viewer surface → capture flows → correlation → regressions).
- **Why this might be controversial:** a complexity-ramp (simple tests first, hardest last) would be more conventional and easier for less-experienced sessions to ride. User-story phasing can put a "capture flow" P2 right after the "viewer shell" P1, even though P2 is technically more complex than P3 correlation.
- **Alternative view:** a reviewer could argue P3 (correlation, just 3 simple header assertions) should be P2 for momentum.
- **Seeking input on:** do you want phases by user-story clarity (current) or by implementation-complexity ramp?

### SC-008 "bootstrap within 5 tool calls"

- **Decision:** success criterion is that a fresh session given only `spec.md + RESUME-P{N}.md` bootstraps Phase N+1 within 5 tool calls.
- **Why this might be controversial:** "tool calls" is a novel unit — not a standard CI metric.
- **Alternative view:** could be reframed as "the session reads no files beyond the spec and RESUME before writing its first test" — same intent, cleaner measurement.
- **Seeking input on:** keep the tool-call counter, or tighten to "reads no extra files"?

### Phase 4's Spanish-copy assertions overlap with Phase 1

- **Decision:** P1 asserts *some* Spanish copy (column headers, empty state, Outcome dropdown options); P4 asserts *more* Spanish copy (filter buttons, pagination, badges).
- **Why this might be controversial:** a reviewer could argue Spanish-copy checks are one coherent concern and should live in a single file, not split by phase.
- **Alternative view:** move all Spanish-copy assertions into a single `AuditLogSpanishCopyTests.cs` in P4.
- **Seeking input on:** keep the copy assertions distributed (current) or consolidate into one file?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Feature branch | `017-audit-e2e` | sequential numbering; next available after 016 |
| Spec directory | `specs/017-audit-e2e/` | matches branch |
| Test file — P1 | `AuditLogViewerTests.cs` | matches existing convention (`Tests` suffix) |
| Test file — P2 | `AuditLogCaptureTests.cs` | `Capture` = UI-driven command dispatch |
| Test file — P3 | `AuditLogCorrelationTests.cs` | self-evident |
| Test file — P4 | `AuditLogRegressionTests.cs` | defensive/invariant assertions |
| Resume prompt | `RESUME-P{N}.md` | directly in spec directory |
| Terminal retrospective | `RESUME-COMPLETE.md` | replaces the "next handoff" block on the final phase |
| Commit message prefix | `Add E2E coverage for audit pipeline — phase N ({theme})` | parseable, greppable |

## Open Questions

- [ ] Should `RESUME-COMPLETE.md` be Markdown (current default) or structured JSON for a possible future CI gate?
- [ ] Should Spanish-copy assertions consolidate into a single file, or stay distributed across P1 and P4?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| `PlaywrightFixture` does not respawn the `audit` schema between tests | High — tests pollute each other | FR-016 makes verifying this the FIRST task of Phase 1; extend the fixture if needed. |
| Playwright flakiness bleeds into CI | Medium — intermittent test failures | FR-015 caps every wait at ≤15 s with explicit timeouts. |
| Session drift mid-phase | Medium — partial work, ambiguous state | FR-011 mandates a partial `RESUME-P{N}.md` with `status: in-progress` and a pending-tests list. |
| Invariants block drifts between resume files | Low — signals the spec or fixture changed | Intentional: drift between duplicated invariants is a review trigger. |
| Production code accidentally modified | Medium — undermines scope discipline | SC-005 enforces zero lines of production diff across the delivery. |

---
*Share with reviewers before implementation.*
