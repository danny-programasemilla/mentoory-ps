# Spec Review: Audit Pipeline — Browser E2E Coverage

**Spec:** specs/017-audit-e2e/spec.md
**Date:** 2026-04-19
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ✅ SOUND

**Summary:** Complete, implementable spec with concrete acceptance scenarios, measurable success criteria, and explicit process safeguards against AI-session drift. Two minor copy-edits suggested but nothing blocking.

## Completeness: 5/5

### Structure

- ✓ All required sections present (Purpose, User Scenarios, Requirements, Success Criteria, Assumptions, Out of Scope)
- ✓ Recommended sections included (Edge Cases embedded under User Scenarios, Key Entities)
- ✓ No placeholder text — zero `TBD` / `TODO` / `NEEDS CLARIFICATION` markers

### Coverage

- ✓ 18 functional requirements covering test coverage matrix (FR-001 through FR-008), session protocol (FR-009 through FR-012, FR-018), and quality invariants (FR-013 through FR-017)
- ✓ 8 measurable success criteria — all technology-agnostic or explicitly scoped to the testing contract
- ✓ Edge cases enumerated: fixture respawn, Playwright timing, partial-phase recovery, DACPAC seed drift, Manual-mode JSON shape pinning
- ✓ 24 tests accounted for across four phases (9 + 7 + 3 + 5 = 24 — matches FR-001)

## Clarity: 4.5/5

### Language Quality

- ✓ Requirements use MUST / MUST NOT consistently
- ✓ File paths, command filters, commit-message formats are literal — no paraphrasing risk
- ⚠️ Two uses of `should` in narrative prose (lines 67, 71) — non-binding but could be tightened

**Ambiguities Found:**

1. Line 71: `"all assertions should be declarative and side-effect-free"`
   - Issue: `should` is weaker than the surrounding requirements' `MUST`.
   - Suggestion: Change to `MUST`, or leave as narrative (not a numbered FR).
   - Impact: Low — this line is under "Independent Test" prose, not the FR list.

2. Line 67: `"invariants that should never break"`
   - Issue: Colloquial usage; clear in context.
   - Suggestion: Leave as-is; attempting to formalize this sentence would add noise.
   - Impact: None.

No other ambiguity detected. Specific-copy requirements ("exactly `Todos`, `Éxito`, `Fallo`", "exactly four commits", "under 3 minutes") leave no room for interpretation.

## Implementability: 5/5

### Plan Generation

- ✓ Can generate a task-level plan directly — each FR maps to a test file and test method.
- ✓ All dependencies identified (feature 016-audit-pipeline, `PlaywrightFixture`, DACPAC-seeded users).
- ✓ Constraints realistic — existing E2E suite already runs 97 tests in ~3 min; adding 24 tests at ~7 s/test fits the budget.
- ✓ Scope explicitly bounded to `tests/Mentoory.Tests.E2E/` + `specs/017-audit-e2e/`.

**Meta-Implementation clarity** (the checkpoint protocol itself):

- ✓ The seven-step exit ritual in FR-009 is prescriptive enough to follow literally.
- ✓ The resume-prompt schema in FR-010 names five canonical sections with intentional redundancy.
- ✓ Partial-phase recovery (FR-011) covers the context-exhaustion failure mode.
- ✓ Kickoff procedure (FR-012) prevents the "new session starts writing code before verifying baseline" anti-pattern.

## Testability: 5/5

### Verification

- ✓ SC-001: literal command + literal exit code.
- ✓ SC-002: wall-clock measurement.
- ✓ SC-003: existing test-suite pass/fail.
- ✓ SC-004: literal `git log` invocation with expected output shape.
- ✓ SC-005: literal `git diff` must emit zero lines.
- ✓ SC-006: git-history artifact existence + distinct author timestamps.
- ✓ SC-007: binary — did any phase touch production code without flagging it?
- ✓ SC-008: bootstrap latency measured in tool-calls from a fresh session — novel but verifiable.

Every FR maps to at least one SC or acceptance scenario. No orphan requirements.

## Constitution Alignment

Checked against `.specify/memory/constitution.md` and `.specify/memory/access-security-constitution.md`:

- ✓ Principle IX (Spanish-First UI): spec requires verbatim Spanish copy assertions — reinforces rather than violates.
- ✓ Principle V (Zero-Warnings): spec preserves the `WarningsNotAsErrors=NU1902` escape pending MailKit resolution — consistent with 016's stance.
- ✓ Principle II (CQRS): spec does not introduce handlers; no Application-layer concerns.
- ✓ Access-security-constitution § Audit Trail Obligations: spec exercises the feature that enforces this rule; no conflict.

No violations.

**One alignment caveat worth flagging**: the spec names file paths and command-line filters. For a typical product-feature spec this would be an implementation leak, but here the testing-infrastructure surface **is** the product boundary — test files and their invocation commands are the deliverables. The accompanying `checklists/requirements.md` documents this exception explicitly.

## Recommendations

### Critical (Must Fix Before Implementation)

*(none)*

### Important (Should Fix)

*(none)*

### Optional (Nice to Have)

- [ ] Tighten line 71 from `"all assertions should be declarative"` to `"all assertions MUST be declarative"` for consistency with the rest of the spec's MUST/MUST-NOT voice.
- [ ] Add a one-line statement under § Assumptions clarifying which **reference machine** defines SC-002's "under 3 minutes" budget (e.g., developer laptop vs. CI runner) to prevent future disputes.

## Conclusion

Spec is sound and ready for implementation. The phased-checkpoint structure is the novel element and is specified clearly enough that a fresh session can execute Phase 1 from spec + `RESUME-P1.md` (not yet written — author's session creates it before handoff) alone.

**Ready for implementation:** Yes

**Next steps:** User review of the spec, then `/speckit-plan` or direct `/speckit-implement` depending on whether the user wants an intermediate task breakdown or to jump into Phase 1 writing tests.
