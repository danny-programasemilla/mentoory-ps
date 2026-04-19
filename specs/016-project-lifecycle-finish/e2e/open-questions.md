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

## Fixture extensions proposed but deferred

*(Populated when a test would benefit from a fixture method that doesn't warrant adding in the current chunk.)*
