# Spec Review: Invitation Domain Decoupling

**Spec:** specs/010-invitation-domain-decoupling/spec.md
**Date:** 2026-04-12
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ✅ SOUND

**Summary:** Specification is well-structured, complete, and directly implementable. Requirements are specific, numbered, and use MUST language consistently. All edge cases are covered with concrete expected behaviors. One minor clarity improvement recommended.

## Completeness: 5/5

### Structure
- [✓] All required sections present (User Scenarios, Requirements, Success Criteria)
- [✓] Recommended sections included (Edge Cases, Assumptions, Key Entities)
- [✓] No placeholder text

### Coverage
- [✓] All functional requirements defined (13 requirements: FR-001 through FR-013)
- [✓] Error cases identified (4 edge cases with explicit behaviors)
- [✓] Edge cases covered (token/invitation expiry mismatch, handler failure, already-active user)
- [✓] Success criteria specified (6 measurable outcomes)
- [✓] Guardrail requirements explicitly mark what must NOT change (FR-011 through FR-013)

## Clarity: 4.5/5

### Language Quality
- [✓] No ambiguous language — all requirements use MUST
- [✓] Requirements are specific with exact handler/interface/file names
- [✓] No vague terms ("fast", "appropriate", etc.)

**Ambiguities Found:**

1. FR-010: "Any controller or frontend endpoint that sends `SetInitialPasswordCommand` with `TokenType.Invitation` MUST be updated to use the verification token flow."
   - Issue: "Use the verification token flow" is slightly vague — does this mean redirect to the VerifyEmail endpoint, remove the endpoint entirely, or restructure the form?
   - Impact: Low — the research.md (Decision 1) already clarifies this as "unify the onboarding flow" and the plan/tasks specify the exact controller changes.
   - Suggestion: Could be more explicit (e.g., "MUST be removed or redirected to the VerifyEmail endpoint"), but the supporting artifacts resolve this.

## Implementability: 5/5

### Plan Generation
- [✓] Can generate implementation plan — plan.md already generated successfully
- [✓] Dependencies identified (notification module coordination noted in Assumptions)
- [✓] Constraints realistic (zero warnings, existing test suite)
- [✓] Scope manageable (3 deletions, ~10 modifications, 2 creations)

**Notes:**
- The spec is an architectural refactoring with clearly bounded scope
- Every FR maps to specific files and handlers — no ambiguity about what to change
- Guardrail requirements (FR-011 to FR-013) prevent scope creep

## Testability: 5/5

### Verification
- [✓] Success criteria measurable (SC-001 through SC-006 are all binary pass/fail)
- [✓] Requirements verifiable (each FR can be checked by inspecting code or running build)
- [✓] Acceptance criteria clear (Given/When/Then format for all user stories)

**Notes:**
- SC-001 is verifiable via grep: zero references to invitation concepts in Access.Application
- SC-005 is verifiable via `dotnet build` with TreatWarningsAsErrors
- SC-006 is verifiable via `dotnet test`
- User story acceptance scenarios are concrete and independently testable

## Constitution Alignment

- [✓] Follows Clean Architecture layer boundaries (Principle I) — actually **fixes** an existing violation
- [✓] CQRS patterns followed (Principle II) — existing handler patterns preserved
- [✓] DDD constraints respected (Principle III) — **fixes** cross-domain dependency
- [✓] Integration events correctly placed (Principle IV) — `InvitationReissuedEvent` in originating domain
- [✓] Zero-warnings policy (Principle V) — SC-005 enforces this
- [✓] DateTime handling (Principle VI) — ITimeProvider used, event timestamps as parameters
- [✓] Naming conventions (Principle VII) — event/handler names follow patterns
- [✓] File organization (Principle VIII) — one class per file
- [✓] Spanish-first UI (Principle IX) — N/A (no new user-facing text)
- [✓] Role hierarchy (Principle X) — N/A (no authorization changes)
- [✓] SSDT/DACPAC (Principle XI) — schema change via SSDT table edit

**Violations:** None. This spec fixes existing violations of Principles I and III.

## Recommendations

### Important (Should Fix)
- [ ] Clarify FR-010: Specify whether the AcceptInvitation password-setting endpoint should be removed entirely or redirected to the VerifyEmail flow. (Note: research.md Decision 1 already resolves this — consider pulling that clarity into the spec itself.)

### Optional (Nice to Have)
- [ ] Consider adding an explicit "Out of Scope" section to the spec (currently in the brainstorm discussion but not in the spec file itself)
- [ ] Consider adding a note about coordinating token expiry alignment (edge case 3 mentions admin reissue, but doesn't explicitly state the token expiry will match invitation expiry — the research.md Decision 3 covers this)

## Conclusion

This is a well-crafted spec for an architectural refactoring. Requirements are specific, success criteria are measurable, and edge cases are thoroughly covered. The guardrail requirements (FR-011 to FR-013) are a particularly good practice — they prevent accidental scope creep during implementation.

The one minor ambiguity (FR-010) is resolved by supporting artifacts (research.md, plan.md), so it does not block implementation.

**Ready for implementation:** Yes

**Next steps:**
- Tasks already generated at `specs/010-invitation-domain-decoupling/tasks.md`
- Proceed with `/speckit-implement` when ready
