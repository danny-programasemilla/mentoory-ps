# Spec Review: Cascading Context Selector UX

**Spec:** specs/008-context-selector-ux/spec.md  
**Date:** 2026-04-11  
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ✅ SOUND (after revision)

**Summary:** Well-structured spec with clear requirements, strong acceptance scenarios, and full constitution alignment. Initial review found three implementability gaps (API response shape, role dropdown population, switch endpoint contract). All three have been resolved in revision — the spec now has 19 FRs, an Out of Scope section, and complete API contracts.

## Completeness: 4/5

### Structure
- [✓] All required sections present (User Scenarios, Requirements, Success Criteria)
- [✓] Recommended sections included (Edge Cases, Assumptions, Key Entities)
- [✓] No placeholder text — all sections fully populated
- [✗] Missing "Out of Scope" section (was defined during brainstorming but not included in spec)

### Coverage
- [✓] All functional requirements defined (17 FRs)
- [✓] Error cases identified (6 edge cases with error handling)
- [✓] Edge cases covered (7 cases)
- [✓] Success criteria specified (10 measurable outcomes)

**Issues:**
1. **Out of Scope section missing** — The brainstorming session defined clear boundaries (no schema changes, no auth changes, no role management UI, no real-time permission refresh, no Available Projects redesign) but these are absent from the spec. Add an "Out of Scope" section to prevent scope creep during implementation.

## Clarity: 5/5

### Language Quality
- [✓] No ambiguous language — all requirements use "MUST"
- [✓] Requirements are specific with concrete values (500ms, specific API paths, exact Spanish strings)
- [✓] No vague terms ("should", "might", "appropriately", "etc.")
- [✓] All Spanish UI strings explicitly defined

**Ambiguities Found:** None.

## Implementability: 3/5

### Plan Generation
- [✓] Can generate implementation plan (with fixes below)
- [✓] Dependencies identified (existing queries, commands, Bootstrap)
- [✓] Constraints realistic (data volume small, existing patterns)
- [✓] Scope manageable (single feature, contained changes)

**Issues:**

1. **Critical — API response shape missing `roleAssignmentExternalId`**: FR-009 and FR-010 define API endpoints returning `[{ id, name }]`. However, the `POST /Context/Select` and `POST /api/context/switch` both require a `roleAssignmentExternalId` (Guid) to activate a context. With the card-based UI, each card embedded this as a hidden field. With dropdowns, the frontend has no way to resolve a (role, incubatorId, projectId) selection back to a `roleAssignmentExternalId`. The cascade API responses must include `roleAssignmentExternalId` — either:
   - Option A: Incubators endpoint returns `[{ id, name, roleAssignmentExternalId }]` for incubator-scoped assignments; Projects endpoint returns `[{ id, name, roleAssignmentExternalId }]` for project-scoped assignments
   - Option B: Add a resolution endpoint `GET /api/context/resolve?role={role}&incubatorId={id}&projectId={id}` that returns the matching `roleAssignmentExternalId`

2. **Important — Initial Role dropdown population unspecified**: FR-001 through FR-003 describe cascade filtering, but no FR specifies how the Role dropdown itself gets populated. Options:
   - Server-rendered from the controller GET action (works for full page, but modal needs an API)
   - New endpoint `GET /api/context/roles` returning distinct roles for the user
   - Embedded as JSON in the shared partial on page load
   
   This must be specified so both the full page and modal can populate the first dropdown.

3. **Important — Extended switch endpoint is an assumption, not a requirement**: The Assumptions section states "The POST /api/context/switch endpoint is extended to accept optional incubator/project override parameters for the modal flow." This is a functional change needed for GlobalAdmin modal switching and should be an FR, not an assumption.

## Testability: 5/5

### Verification
- [✓] Success criteria measurable (500ms latency, auto-skip behavior, dropdown states)
- [✓] Requirements verifiable (all FRs are testable)
- [✓] Acceptance criteria clear (Given/When/Then format with 14 scenarios)

**Issues:** None — acceptance scenarios are well-written and cover the critical paths.

## Constitution Alignment

- [✓] Clean Architecture respected — new endpoints in Web Layer, queries in Application Layer (Principle I)
- [✓] CQRS patterns implied — new queries will follow existing patterns (Principle II)
- [✓] No new entities, no ExternalId concern (Principle III)
- [✓] Zero-warnings policy — no spec-level concern (Principle V)
- [✓] DateTime — not relevant to this feature (Principle VI)
- [✓] Spanish-first UI — all user-facing strings defined in Spanish (Principle IX)
- [✓] Role hierarchy — GlobalAdmin access to all incubators/projects, [Authorize] on new endpoints, area-role checks preserved (Principle X)
- [✓] No database changes — SSDT not affected (Principle XI)
- [✓] JavaScript in /wwwroot/js/ — implied by extending context-switcher.js (Principle VIII)

**Violations:** None.

## Recommendations

### Resolved (Fixed in Revision)
- [x] **Fixed FR-009/FR-010/FR-011 response shape** — Cascade API responses now include `roleAssignmentExternalId`. Added `GET /api/context/roles` endpoint (new FR-009). Renumbered FRs accordingly.
- [x] **Added FR for initial Role dropdown population** — New FR-009 (`GET /api/context/roles`) returns distinct roles for the authenticated user.
- [x] **Promoted switch endpoint extension to FR-016** — `POST /api/context/switch` contract extension moved from Assumptions to a numbered FR.
- [x] **Added Out of Scope section** — Boundaries from brainstorming now in spec.

### Optional (Nice to Have)
- [ ] Consider whether User Story 5 (shared partial) is better expressed as an architectural constraint within FR-019 rather than a standalone user story, since it has no end-user impact

## Conclusion

After revision, the spec is complete, clear, implementable, and fully aligned with the constitution. All critical and important issues have been resolved. The spec now has 19 well-defined functional requirements, complete API contracts with `roleAssignmentExternalId` resolution, and explicit scope boundaries.

**Ready for implementation:** Yes

**Next steps:**
1. User reviews the spec
2. Proceed to `/speckit-plan`
