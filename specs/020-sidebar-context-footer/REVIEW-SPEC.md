# Spec Review: Sidebar Context Footer

**Spec:** specs/020-sidebar-context-footer/spec.md
**Date:** 2026-05-22
**Reviewer:** Claude (speckit-spex-gates-review-spec)

## Overall Assessment

**Status:** SOUND

**Summary:** A tightly-scoped, read-only UI relocation with one behavioral change (single switch entry point). Requirements are specific and testable, edge cases cover the degenerate context states, and the one genuine unknown (switchability detection) is correctly deferred to planning rather than guessed.

## Completeness: 5/5

### Structure
- Purpose (via Input + User Story 1), Functional Requirements, Success Criteria, Edge Cases, Assumptions, Out of Scope, Open Questions all present.
- No placeholder/TBD text remaining.

### Coverage
- Display, header simplification, switch entry, single-context, and reuse/integration constraints all covered (FR-001..FR-014).
- No dedicated "Error Handling" heading, which is acceptable here: the feature is a read-only display + a trigger for an unchanged modal. The only failure surfaces are missing context values, all enumerated under Edge Cases (no incubator, no project, no active context → card not rendered).

## Clarity: 5/5

### Language Quality
- Requirements use MUST and name concrete, observable outcomes.
- The subjective term "professional UI/UX" lives only in Purpose; testable requirements (SC-004) translate it into an objective inventory of allowed header elements.

**Ambiguities Found:**
1. FR-010 "sufficient contrast for legibility"
   - Issue: "sufficient" is not quantified.
   - Suggestion (optional): bind to WCAG AA (≥ 4.5:1 for text). Not blocking — the dark sidebar already hosts legible nav text.

## Implementability: 5/5

- A plan can be generated directly: relocate markup into the sidebar partial, strip the header partial, repoint one E2E test.
- Dependencies named by stable identifiers (`current-context` test hook, the switcher modal, the shared selector partial).
- The single open question (switchability detection) is scoped with three candidate approaches and a safe fallback (always-clickable, since the switcher already handles single-option as read-only), so it cannot block implementation.

## Testability: 5/5

- SC-001..SC-005 are inspectable or automatable.
- SC-002 ("same number of confirmation steps") and SC-005 (existing tests pass + zero warnings) give concrete regression gates.
- FR-014 makes the test repointing an explicit, verifiable deliverable.

## Constitution Alignment

- **IX. Spanish-First UI** — FR-008 requires Spanish for all introduced/relocated text. ✓
- **X. Role Hierarchy & Session Context** — read-only display of existing claims; missing-context handled by not rendering the card (graceful), and the GlobalAdmin global-scope case (no incubator) is an explicit edge case. ✓
- **V. Zero-Warnings Policy** — SC-005 requires a zero-warning build. ✓
- **Web Layer / UI Framework** — reuses the existing Tabler/Bootstrap switcher and partials; no new dependencies. ✓

**Violations:** None.

## Recommendations

### Critical (Must Fix Before Implementation)
- None.

### Important (Should Fix)
- None.

### Optional (Nice to Have)
- [ ] Quantify FR-010 contrast against WCAG AA (≥ 4.5:1).
- [ ] In `/speckit-plan`, resolve the switchability-detection open question before writing tasks.

## Conclusion

The spec is sound, implementable, and free of blocking ambiguity. The scope is appropriate for a single plan/implementation cycle.

**Ready for implementation:** Yes (after the optional plan-time resolution of switchability detection).

**Next steps:** Proceed to `/speckit-plan` (recommended, to settle switchability detection and produce tasks) or `/speckit-implement` directly.
