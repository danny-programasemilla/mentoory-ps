# Spec Review: E2E coverage for Project Lifecycle (feature 016)

**Spec:** `specs/016-project-lifecycle-finish/e2e/spec.md`
**Date:** 2026-04-19
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ✅ SOUND

**Summary:** The spec is complete, concrete, implementable, and testable. The checkpoint protocol is the dominant design decision and is described with enough specificity that a fresh AI session can execute each chunk unattended. One medium-severity risk is called out (R3 below) but is already mitigated inside C0's stop conditions.

## Completeness: 5/5

### Structure
- ✓ All required sections present (Purpose, FR, Success Criteria, Error Handling)
- ✓ All recommended sections present (NFR, Edge Cases, Dependencies, Out of Scope, Open Questions, Assumptions)
- ✓ No placeholder text, no TBD, no unresolved "to be decided" markers

### Coverage
- ✓ Every parent-spec acceptance scenario mapped to a test row in R1
- ✓ Every parent-spec edge case evaluated (4 automated, 1 parked to `open-questions.md` with rationale)
- ✓ Non-automatable success criteria explicitly called out in both §Out of Scope and the coverage matrix
- ✓ Error handling covers all 7 execution-time failure modes (E1-E7)
- ✓ Execution edge cases enumerate interrupted sessions, test-count drift, concurrent branch activity, infeasible scenarios (EC1-EC5)

**Issues:** None.

## Clarity: 4.5/5

### Language Quality
- ✓ Requirements are specific (test file paths, test method names, exact Spanish literals to assert)
- ✓ Ambiguous test counts are resolved in context: "16–17 tests" means "16 guaranteed + 1 conditional (test #17)"
- ✓ "Mechanism is a plan-phase decision" deferrals are paired with guidance in C0 (prefer scoped DbContext over raw SQL)

**Ambiguities Found:**

1. "The test asserts whichever is the consistent user-facing behavior on this branch." (Test #17 description)
   - Issue: Two possible observed behaviors (button hidden vs. button present with toast on click), which one is correct depends on the current code.
   - Suggestion: Acceptable deferral — the test's job is to pin down whichever behavior exists today. Not a blocker.

2. "fixture extension — allowed per E3" appears in multiple checkpoint files.
   - Issue: Very slight conceptual overlap between "fixture" (test infrastructure) and "seed data" (database content). E3 explicitly covers both.
   - Suggestion: The E3 text already resolves this. Not a blocker.

## Implementability: 5/5

### Plan Generation
- ✓ Six chunks defined with explicit deliverables
- ✓ Test file paths, directory layout, class names, method names all named
- ✓ Dependencies identified (D1-D6, all verified against current commit `7a7942d`)
- ✓ Prerequisites for each chunk encoded in checkpoint pre-flight checklists
- ✓ Constraints realistic (no new NuGet packages, no new CI config, existing Testcontainers infra)

### Implementability Risks

- **R3 — PlaywrightFixture extension risk.** The spec assumes `PlaywrightFixture` can be extended to expose a service provider or HTTP client for programmatic project creation without product-code changes. If this assumption fails, C0 cannot complete as specified.
  - **Mitigation in spec:** C0's Stop Conditions explicitly handle this: "The existing `PlaywrightFixture` cannot be extended for programmatic project creation without product-code changes" ⇒ stop and surface. The spec is not silent on this risk.
  - **Severity:** Medium — if triggered, the spec phase re-opens. But the outcome is graceful (surface, not crash).

## Testability: 5/5

### Verification
- ✓ SC-E1 measurable (test count + green state)
- ✓ SC-E2 measurable (build warnings count, test failure/skip count, `open-questions.md` entries)
- ✓ SC-E3 measurable (commit count, commit messages, order)
- ✓ SC-E4 measurable via review (a human reads each resume prompt and judges self-containment; acceptance gate is in workflow step 9 of the brainstorm skill)
- ✓ SC-E5 measurable (product-code paths in `git diff` — should be empty outside `tests/` and specs)
- ✓ SC-E6 measurable via `coverage-matrix.md` completeness — every 🚧 row must flip before C5 closes
- ✓ SC-E7 measurable (tasks.md T042/T051/T056 state)

## Constitution Alignment

The project's constitution (v1.1.1) defines 10 principles plus Testing Requirements and Technology Standards. The spec aligns with all relevant ones:

- ✓ **V. Zero-Warnings Policy** — SC-E2 enforces "0 new warnings," matching `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- ✓ **IX. Spanish-First UI** — All UI assertions use exact Spanish literals from product code; no paraphrase allowed.
- ✓ **Testing Requirements** — Uses xUnit, FluentAssertions, Respawn (all already in-stack). No new test framework introduced.
- ✓ **Technology Standards** — No new NuGet packages. Reuses `PlaywrightFixture`, `MentooryWebApplicationFactory`, Testcontainers.MsSql.
- ✓ **I. Clean Architecture Layer Boundaries** — The spec explicitly forbids product-code changes (SC-E5), which means no risk of layer violations introduced by this work.
- ✓ **VIII. File Organization** — Test files placed under `tests/Mentoory.Tests.E2E/Tests/Lifecycle/`, fixtures under `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/` — matches established convention.

**Violations:** None.

## Recommendations

### Critical (Must Fix Before Implementation)

None.

### Important (Should Fix)

None — the one contradiction found during review (SC-E2 simultaneously saying "0 skipped" and permitting skip-marked tests per E2) was fixed inline before this report was written.

### Optional (Nice to Have)

- **Definition of Done for the whole suite**, explicit in one place: "The E2E suite is done when C5 has committed AND `coverage-matrix.md` has no 🚧 rows AND `open-questions.md` has been triaged by the user." Currently spread across SC-E1/SC-E6/SC-E7 — functionally equivalent but less glanceable.
- **One explicit counter-example for resume-prompt self-containment.** SC-E4 says a prompt must be self-contained but doesn't show what that looks like. The six checkpoint files themselves are the worked example, so this is mostly style.

Neither of these blocks implementation.

## Conclusion

The spec is ready for implementation. The six checkpoint files are already written, the coverage matrix and open-questions.md skeletons are in place, and the review_brief.md is ready for stakeholder review.

**Ready for implementation:** Yes.

**Next steps:**

1. User reviews `spec.md` + `review_brief.md` and approves (brainstorm step 9).
2. Commit the spec directory and the brainstorm document.
3. Execute C0 (foundation chunk) by pasting `checkpoints/00-foundation.md` into a fresh session.
