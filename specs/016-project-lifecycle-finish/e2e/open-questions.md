# Open Questions — E2E for feature 016

Execution-time parking lot. Populated as chunks run. **Not** used during the spec phase.

## Cannot-test-as-specified

| Scenario | Reason | Noted by |
|---|---|---|
| Legacy stage-gated action started before feature 016 shipped (spec.md §Edge Cases item 4) | No realistic seed path exists in the current codebase for "a diagnostic response created before the feature existed." Would require either backdated seed data (fragile) or a schema-level change to bypass the aggregate invariant (out-of-scope). | Spec phase (C0 predecessor) |

## Cross-chunk refactoring opportunities

*(Populated when `/simplify` reveals duplication across chunks. Post-suite cleanup pass, not during the chunks themselves.)*

## Product-code issues surfaced during testing

*(Populated if a test reveals a real bug. Each entry: test name, expected behavior, observed behavior, chunk, severity. Triage separately — do NOT fix during the E2E execution per R1/E2.)*

### US2 §3 — "all seven stages completed" state is unreachable (C2)

- **Test**: `WalkthroughLifecyclePageTests.Lifecycle_ClosedProject_PriorStagesCompleted_ClosureInProgress`
- **Expected per parent spec US2 §3 / FR-010-FR-011**: "all seven stages appear in the completed state with their timestamps."
- **Observed**: Closure stage renders as `En progreso`. `AdvanceProjectStageHandler.cs:48` explicitly rejects a 7th advance (`ProjectAlreadyClosed`), so no UI or command path completes Closure. The domain (`Project.AdvanceStage`) *does* support a 7th advance that would complete Closure — exercised by `Mentoory.Tenant.Tests.Domain.ProjectTests.AdvanceStage_AtFinalStage_StopsInClosureWithCompletedState` — but that path is unreachable from the application layer.
- **Severity**: Spec/product discrepancy, not a regression. FR-004 explicitly forbids any advance at Closure, which is consistent with the handler behavior. Parent-spec US2 §3 and FR-010/FR-011 read as if Closure can be completed, which no product flow supports.
- **Resolution options for triage**:
  1. Add a "finalize project" application-layer command that performs the 7th domain-level advance. US2 §3 then becomes achievable verbatim.
  2. Amend parent spec US2 §3 to describe the actually-achievable "closed" state (6 completed + Closure in progress + no advance).
- **Current coverage** (C2): test asserts the renderable reality. See coverage-matrix note A.

## Fixture extensions proposed but deferred

*(Populated when a test would benefit from a fixture method that doesn't warrant adding in the current chunk.)*
