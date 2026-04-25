# Spec Review: Access-Security Delivery Quality Gate

**Spec:** specs/018-access-security-delivery-quality-gate/spec.md
**Date:** 2026-04-19
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ✅ SOUND

**Summary:** Specification is complete, unambiguous, and implementable. 21 functional requirements are grouped by concern (constitution, tool, CI, retrofit, new scenarios) and each is testable. Success criteria are measurable and include two canary tests that exercise the gate mechanism itself. Three Open Questions are explicitly flagged as non-blocking. Minor clarity improvements are possible but not required for implementation.

## Completeness: 5/5

### Structure
- ✓ All required sections present (User Scenarios & Testing, Requirements, Success Criteria)
- ✓ Recommended sections included (NFRs, Edge Cases, Dependencies, Assumptions, Out of Scope, Open Questions, Key Entities)
- ✓ No placeholder text (verified by grep for `TBD`, `TODO`, `NEEDS CLARIFICATION`, `[xxx]`, `$ARGUMENTS`)

### Coverage
- ✓ Functional requirements defined (21 FRs + 5 NFRs)
- ✓ Error cases identified (implicit via Edge Cases section + Acceptance Scenarios)
- ✓ Edge cases explicit (10 cases covering tool behaviour, flaky tests, renumbering, deletion, rate-limit override, local dirty trees)
- ✓ Success criteria specified (6 SCs, each measurable)

**Issues:** None.

## Clarity: 4.5/5

### Language Quality
- ✓ No ambiguous modal verbs — requirements use `MUST` consistently; non-requirements use declarative language
- ✓ Concrete specifics throughout — file paths, technology names, assertion shapes, timing budgets (`≤ 2 s`, `≤ 15 s`)
- ✓ No weasel words (`fast`, `slow`, `appropriate`, `user-friendly`, `etc.`) except where `e.g.` is used correctly

**Ambiguities Found:**

1. **FR-017** — `"HTTP 401 or a redirect to the login route (the test pins whichever the deployed policy produces)"`
   - Issue: The `OR` is technically an ambiguity — implementers cannot know from the spec alone which one to assert against.
   - Mitigation in spec: The parenthetical explicitly defers the choice to the test, which is reasonable because the deployed `[Authorize]` policy's behaviour is an existing property of the system, not a decision this spec makes.
   - Suggestion (non-blocking): Add a one-line note to the Assumptions section stating `FR-017 must be pinned to the current deployed behaviour of [Authorize] on /Administration/* during implementation; if that behaviour changes, the test and spec must both be updated in lockstep.`

2. **US1 Acceptance Scenario 4** combines public-path and admin-path checks in one `Given/Then; and given/Then` clause.
   - Issue: Slightly harder to read than two separate scenarios.
   - Severity: Low. The content is clear; splitting would be stylistic.
   - Suggestion: Optional — split into 4a (public) and 4b (admin) for symmetry with other scenarios.

## Implementability: 5/5

### Plan Generation
- ✓ Implementation plan can be generated directly from the FR grouping (constitution → tool → retrofit → new scenarios → CI wiring)
- ✓ Dependencies identified (xUnit 2.x, .NET 10 `MetadataLoadContext`, existing test fixtures, GitHub Actions, branch protection)
- ✓ Constraints realistic (2 s tool runtime, 15 s sweep, existing fixtures suitable)
- ✓ Scope manageable — single-PR monolithic landing is explicitly chosen and consistent across the FRs

**Issues:** None.

**Notes:**
- OQ-002 (tool project location) is correctly deferred to implementation — all three options (`tools/`, `build/`, `specs/tooling/`) satisfy the functional contract.
- FR-013 deliberately does not prescribe which specific FR identifier each existing 016 test should claim; that mapping is an implementation-plan activity, not a spec requirement. This is the correct boundary.

## Testability: 5/5

### Verification
- ✓ Every SC is measurable (exit codes, zero-counts, timing budgets, canary behaviours)
- ✓ Every FR is verifiable (observable HTTP responses, tool outputs, attribute presence, CI stage outcomes)
- ✓ Acceptance scenarios use Given/When/Then consistently

**Issues:** None.

**Strong points:**
- SC-005 and SC-006 are *canary tests of the gate itself* — the spec does not just assert its features work; it asserts that deliberately breaking the coverage produces the expected failure. This is a high-quality testability choice.
- FR-019 pins the response-equality comparison dimensions precisely (`status`, `Location`, body bytes with antiforgery stripped, `Cache-Control`, `Content-Type`, `Set-Cookie` cookie-name set) — no room for implementer drift.

## Constitution Alignment

The project has both `.specify/memory/constitution.md` and `.specify/memory/access-security-constitution.md`. This spec directly amends the latter, so its compliance checks are against both.

- ✓ Follows Clean Architecture layer boundaries (testing infrastructure, no production-code changes in access layer)
- ✓ Spec targets the existing `access-security-constitution.md` which it explicitly lists as an amendment target in Dependencies
- ✓ Preserves existing section numbering (NFR-005 explicit)
- ✓ Compatible with the zero-warnings policy (coverage tool is additive; it fails builds, doesn't warn)
- ✓ Spanish-first UI unaffected (no user-facing copy introduced)
- ✓ SSDT/DACPAC strategy unaffected (no schema change)
- ✓ No forbidden patterns (no AutoMapper, no static business logic, no service locator)

**Violations:** None detected.

## Recommendations

### Critical (Must Fix Before Implementation)

None.

### Important (Should Fix)

None blocking. Two low-severity items worth an inline clarification pass:

- [ ] Add a sentence to Assumptions (or a new note next to FR-017) stating that `FR-017`'s "401-or-redirect" must be pinned to the deployed `[Authorize]` policy behaviour, and that any change to that policy requires updating both the test and this spec.

### Optional (Nice to Have)

- [ ] Split US1 Acceptance Scenario 4 into 4a (public) and 4b (admin) for symmetry.
- [ ] Consider adding a concrete example of a `Coverage: N/A` justification clause to FR-006 or EC so implementers have a canonical shape to follow. (Could live in implementation-notes.md instead; not a spec requirement.)
- [ ] OQ-003 (nightly 500-probe deep run) — could be promoted to an explicit non-requirement "future consideration" rather than an open question, since it's already non-blocking.

## Conclusion

Excellent specification. Well-scoped, rigorously testable, and directly amendable to an implementation plan. The monolithic-PR rollout choice is consistent across the FRs, and the canary-SC approach for verifying the gate itself is a mark of a mature spec.

**Ready for implementation:** Yes.

**Next steps:**
1. User reviews `spec.md` and either approves or requests edits.
2. Generate `review_brief.md` for reviewer handoff.
3. Commit spec + checklist + review artifacts to the `018-access-security-delivery-quality-gate` branch.
4. Transition to `/speckit-plan` (recommended — the FR grouping is ready to become an implementation-phase plan) or `/speckit-implement` (acceptable if plan is considered overhead for this scope).
