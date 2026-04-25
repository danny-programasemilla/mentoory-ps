# Brainstorm: E2E coverage for Project Lifecycle (feature 016)

**Date:** 2026-04-19
**Status:** spec-created
**Spec:** `specs/016-project-lifecycle-finish/e2e/`

## Problem Framing

Feature 016 (project-lifecycle-finish) shipped with zero automated coverage of the user-facing surface. Tasks T042, T051, T056 in the feature's `tasks.md` were marked as manual walkthroughs and never executed on this branch. The E2E regression caught yesterday (missing `ProjectStages` rows for seed projects) was an existence proof that the UI flow needs a safety net — not just the handler-layer integration tests that were written.

Adding 15–20 Playwright tests is meaningful investment that cannot realistically be completed in one AI session. The session-drift risk is real — by test 12 the session forgets constraints established at test 3. The problem is therefore two-sided: (1) what to cover, (2) how to execute it across multiple sessions without drift.

## Approaches Considered

### A: Thin spec, heavy plan
- Spec is short: "E2E for feature 016, 5 walkthroughs, chunked per user story."
- Execution detail lives in `plan.md` + `tasks.md`.
- Pros: simplest spec, minimal upfront effort.
- Cons: pushes drift-prevention design to the plan stage, where it gets spread thin across tasks rather than authored as coherent session prompts.

### B: Spec defines the contract, plan authors the prompts
- Spec covers scope + fixtures + success criteria + checkpoint protocol as a rule.
- The six resume prompts are interpolated from a template during `/speckit-plan`.
- Pros: clean separation, lean spec.
- Cons: the drift-resistant prompts get written at the moment of maximum context drift (post-spec, pre-execution) — reduces the quality we're trying to preserve.

### C: Spec ships checkpoint artifacts up front — [chosen]
- Spec covers everything in B, PLUS ships six self-contained resume-prompt files in `checkpoints/`.
- Each prompt: prior state, this-chunk goal, fixtures/helpers, invariants, pre-flight, execution steps, exit gate, commit template, stop conditions.
- Pros: the prompts are authored while the spec-phase context is fresh and coherent; a fresh AI session can paste any prompt into an empty conversation and execute the chunk; drift is prevented by anchoring every session to a pre-authored anchor.
- Cons: more upfront work in the brainstorm/spec phase (~half a page per chunk, six chunks). Trade-off judged worthwhile.

## Decision

**C was chosen** along with these specifics:

- **Scope:** full coverage of `specs/016-project-lifecycle-finish/spec.md` — all US1/US2/US3 acceptance scenarios, 4 automatable edge cases, SC-001 + SC-004 + SC-005 (~16–17 tests). Non-automatable SCs explicitly out-of-scope.
- **Chunking:** two-tier — **C0** ships fixtures + page objects + 1 smoke test (no scenario tests); **C1-C5** ship one walkthrough per chunk. Six chunks total, strict order.
- **Checkpoint meaning:** each chunk ends with `dotnet build` green (0 new warnings) → full suite green → `/simplify` applied → one commit → push → `coverage-matrix.md` updated. Session clears; next session pastes the next resume prompt.
- **Placement:** sibling document at `specs/016-project-lifecycle-finish/e2e/`, not a new numbered feature.
- **Branch:** extend current `016-project-lifecycle-finish` branch / PR #11.
- **Product-code policy:** chunks C1-C5 are test-only. Product-code bugs surface as `[Fact(Skip=...)]` + `open-questions.md` entry + user notification.

Artifacts: `spec.md`, `review_brief.md`, `REVIEW-SPEC.md`, `coverage-matrix.md` (skeleton), `open-questions.md` (skeleton), six `checkpoints/CN-*.md` files.

Review status: ✅ SOUND (Completeness 5/5, Clarity 4.5/5, Implementability 5/5, Testability 5/5). One SC-E2 contradiction fixed inline during self-review.

## Open Threads

- **PlaywrightFixture extensibility assumption.** The spec assumes `PlaywrightFixture` can be extended to expose a service provider or HTTP client for programmatic project creation without product-code changes. C0's stop conditions handle the failure gracefully, but the assumption may prove false and re-open the spec phase. (From #11)
- **Test #3 fixture-only broken-state construction.** US1 §3 covers a "stage is Completed while not at Closure" state that the normal UI flow cannot produce. Test #3 would need a test-only fixture method that bypasses the domain to create this state. Debatable whether the test is pulling weight vs. redundant with unit coverage. (From #11)
- **Concurrency test mechanism.** Row 16 exercises the UI-level concurrency toast. Plan phase chooses between parallel Playwright contexts (flakier) and a test-time `IssueStaleAdvanceAsync` helper that bypasses the UI (more deterministic, less "E2E"). Decision deferred to plan phase. (From #11)
