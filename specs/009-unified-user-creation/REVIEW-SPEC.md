# Spec Review: Unified User Creation with Configurable Onboarding

**Spec:** specs/009-unified-user-creation/spec.md
**Date:** 2026-04-12
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** :white_check_mark: SOUND

**Summary:** Excellent spec with comprehensive coverage, clear user stories, and well-defined requirements. Initial review found 1 critical and 3 important issues — all resolved in revision. The state model now cleanly separates AccountStatus (account-level) from ProjectInvitation.Status (per-project). Spanish accent marks corrected. Existing user toggle interaction clarified. Onboarding status scoped to active project.

## Completeness: 5/5

### Structure
- [x] All required sections present
- [x] Recommended sections included (NFRs, edge cases, dependencies, out of scope, assumptions)
- [x] No placeholder text

### Coverage
- [x] All functional requirements defined (16 FRs)
- [x] Error cases identified (7 edge cases + error handling in user stories)
- [x] Edge cases covered comprehensively
- [x] Success criteria specified (10 SCs)
- [x] 8 user stories with Given/When/Then acceptance scenarios

**Issues:** None

## Clarity: 5/5 (after revision)

### Language Quality
- [x] Requirements use "MUST" consistently
- [x] No vague terms ("should", "might", "etc.")
- [ ] One critical ambiguity in status modeling (see below)

**Ambiguities Found:**

1. **CRITICAL — "status changes to PendingInvitation" (US-1 Scenario 2, US-5 Scenario 1)**
   - Issue: The spec uses "PendingInvitation" as if it were an AccountStatus value, but `AccountStatus` is account-level (PendingVerification, Active, Locked, Disabled, PasswordResetRequired) while invitation state is per-project. A user can be Active in Project A but have a pending invitation for Project B. These are two different dimensions.
   - Suggestion: Clarify that AccountStatus tracks account state only (PendingVerification -> Active after email verification). Invitation state lives on the `ProjectInvitation` entity (Pending, Accepted, Expired). The "combined onboarding status" in FR-014 is a computed/derived display value relative to the active project in session context, not a stored field.

2. **IMPORTANT — FR-011 "respecting toggle state" for existing users**
   - Issue: When an existing, already-verified user is created again for a new project with "Skip Email Verification" OFF, does the system send a verification email? Logically no (they're already verified), but "respecting toggle state" is ambiguous here.
   - Suggestion: Add explicit rule: "For existing users, email verification state is inherited from the account. If the user is already verified, no verification email is sent regardless of toggle state. Only the invitation toggle applies to enrollment."

3. **IMPORTANT — FR-014 onboarding status is project-scoped**
   - Issue: The combined onboarding status ("Pendiente verificacion", "Pendiente invitacion", etc.) must be relative to a specific project. A user's status in the users list depends on which project is active in session context.
   - Suggestion: Explicitly state: "The onboarding status column displays the user's state relative to the currently active project in session context."

## Implementability: 4.5/5

### Plan Generation
- [x] Can generate implementation plan (after clarity fixes)
- [x] Dependencies identified (Spec 008, email infra, integration events)
- [x] Constraints realistic
- [x] Scope manageable (well-prioritized P1/P2/P3)

**Issues:**
- The status modeling ambiguity (Critical #1) would cause confusion during domain modeling. Must be resolved first.

## Testability: 5/5

### Verification
- [x] Success criteria measurable
- [x] Requirements verifiable through acceptance scenarios
- [x] Each user story independently testable with Given/When/Then

**Issues:** None. Excellent test coverage across all 8 user stories.

## Constitution Alignment

- [x] Clean Architecture layers respected (Principle I)
- [x] CQRS patterns implied correctly (Principle II)
- [x] ExternalId referenced for new entities (Principle III)
- [x] Integration events correctly scoped (Principle IV)
- [x] Zero-warnings compatible (Principle V)
- [x] DateTime handling deferred to implementation (Principle VI)
- [x] Authorization includes higher-privilege roles (Principle X)
- [x] SSDT/DACPAC conventions respected (Principle XI)
- [ ] Spanish UI text missing accent marks (Principle IX) — see Important #4

**Violations:**

4. **IMPORTANT — Spanish accent marks missing (Principle IX)**
   - "verificacion" should be "verificacion" throughout
   - "invitacion" should be "invitacion" throughout
   - "Aceptar invitacion" -> "Aceptar invitacion"
   - All user-facing labels must use proper Spanish orthography

## Recommendations

### Critical (Must Fix Before Implementation)
- [x] **RESOLVED — Status modeling:** Separated AccountStatus (account-level) from ProjectInvitation.Status (per-project). Updated user stories, key entities, and FR-014. Added state model clarification section.

### Important (Should Fix)
- [x] **RESOLVED — Existing user + toggle interaction (FR-011):** Added explicit rule that verified users skip verification regardless of toggle.
- [x] **RESOLVED — Onboarding status scoped to project (FR-014):** Explicitly states computed value relative to active project in session context.
- [x] **RESOLVED — Spanish accent marks:** All user-facing text uses proper accents.

### Optional (Nice to Have)
- [ ] Consider specifying minimal email content requirements (subject line, key data fields) even if visual design is out of scope.
- [ ] Consider whether ProjectCoordinator should be able to do individual creation (currently batch only) — conscious decision is fine but worth documenting the rationale.

## Conclusion

Strong spec with thorough user stories, clear requirements, and good edge case coverage. The critical status modeling issue must be resolved before implementation — it affects domain design fundamentally. The important issues are straightforward fixes. After addressing the critical and important items, this spec will be ready for implementation.

**Ready for implementation:** Yes

**Revision History:**
- 2026-04-12: Initial review found 1 critical + 3 important issues
- 2026-04-12: All issues resolved — status modeling clarified, FR-011 tightened, FR-014 scoped, Spanish accents fixed

**Next steps:** Proceed to `/speckit-plan`
