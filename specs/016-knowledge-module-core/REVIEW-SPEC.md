# Spec Review: Knowledge Module Core

**Spec:** specs/016-knowledge-module-core/spec.md
**Date:** 2026-04-18
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ⚠️ NEEDS WORK → ✅ SOUND (after fixes applied)

**Summary:** Comprehensive, implementable spec with clear user stories, exhaustive edge cases, and concrete acceptance scenarios. A first-pass review surfaced several Important-severity gaps against the Mentoory Constitution (role-hierarchy authorization, integration-event placement, menu configuration) plus a minor technical leak and one typo. All Important issues have been addressed inline; the spec is now sound for planning.

## Completeness: 5/5

### Structure
- ✓ All required sections present (Overview, User Scenarios, Edge Cases, Requirements, Success Criteria, Assumptions)
- ✓ Recommended sections included (Non-Functional Requirements, Key Entities, Dependencies inline in Overview)
- ✓ No placeholder text, TBDs, or `[NEEDS CLARIFICATION]` markers

### Coverage
- ✓ All 44 functional requirements defined across templates/clones/integration/events/UI
- ✓ 9 non-functional requirements cover architecture, CQRS, testing, security, build quality
- ✓ Edge cases cover clone lifecycle, priority ranges, cross-module cascade, deletion safety, tenant isolation (4 groups, 15 items)
- ✓ 6 success criteria, each with measurable outcome and test-type attribution (unit/integration)

## Clarity: 4.5/5

### Language Quality
- ✓ Requirements use MUST / MUST NOT consistently
- ✓ Each FR is specific and bounded
- ⚠ One implementation-detail leak (addressed below)
- ⚠ One minor ordering ambiguity (addressed below)
- ⚠ One typo in SC-K04 (addressed)

### Ambiguities Found

1. **FR-K21 names a concrete data structure** (`Dictionary<Guid, long>`)
   - Issue: Spec-level requirements should describe behavior, not data structures
   - Suggestion: "an in-memory lookup from template-topic `ExternalId` to project-topic id"
   - **Status:** FIXED

2. **FR-K15 says "walks the template tree in order"**
   - Issue: "In order" is ambiguous (SortOrder? Pre-order? Depth-first?)
   - Suggestion: "walks the template tree depth-first by ascending `SortOrder` at each level"
   - **Status:** FIXED

3. **SC-K04: "`Question.TopicId` values every resolve to..."** (grammatical)
   - Issue: Typo
   - Suggestion: "values every resolve" → "values all resolve"
   - **Status:** FIXED

## Implementability: 5/5

### Plan Generation
- ✓ Can generate a detailed implementation plan — aggregate structure, command/handler list, DB tables, FK points, UI surface are all concrete
- ✓ Dependencies identified (Diagnostic module files enumerated; `ITenantContext` and `ITimeProvider` called out)
- ✓ Constraints realistic — mirrors an already-shipped pattern (ProjectForm)
- ✓ Scope manageable — one domain module + one cross-module cascade + one event

### Concurrency/Transactionality
- ⚠ Previous spec did not explicitly state that the cascade in FR-K21 and the sync in FR-K15 run inside a single transaction
- **Status:** FIXED (explicit "within a single transaction; no partial commit" language added)

## Testability: 5/5

### Verification
- ✓ Every success criterion is objectively measurable
- ✓ SC-K01 has a concrete stopwatch measurement; SC-K02–K06 cite specific test types
- ✓ Each acceptance scenario is a clean Given/When/Then

## Constitution Alignment

Checked against `.specify/memory/constitution.md` v1.1.1.

| Principle | Status | Notes |
|---|---|---|
| I. Clean Architecture | ✓ | NFR-K01 explicit |
| II. CQRS | ✓ | NFR-K03 explicit |
| III. DDD (ExternalId, cross-agg refs by id) | ✓ | NFR-K02; cross-module ref uses ExternalId |
| IV. Integration Events placement | ⚠ → ✓ | Initial spec did not specify `Application/IntegrationEvents/` placement; FIXED |
| V. Zero warnings | ✓ | NFR-K06 |
| VI. DateTime injection | ✓ | NFR-K07 |
| VII. Naming conventions | ✓ | Commands follow `{Verb}{Entity}Command/Handler` |
| VIII. File organization | — | Implementation-phase concern |
| IX. Spanish UI | ✓ | FR-K44 |
| X. Role Hierarchy + Session Context + Menu | ⚠ → ✓ | Initial NFR-K05 mentioned layering but did not enumerate required role sets or mandate menu-group inclusion of GlobalAdmin; FIXED |
| XI. SSDT / PostDeployment seeds | ⚠ → ✓ | Initial spec had SSDT atomicity (NFR-K08) but omitted the PostDeployment seed requirement for sample templates; FIXED |

### Violations Found and Fixed

1. **Principle X — Authorization role sets not named**
   - The constitution explicitly requires `[Authorize]` to include the target role AND all higher roles, and that menu groups include GlobalAdmin.
   - NFR-K05 only said "Authorization uses `[Authorize(Roles=...)]` additively combined with `CheckPermission`".
   - **FIX:** NFR-K05 now names the exact role sets: global-template CRUD `"GlobalAdmin"`; project-clone CRUD `"ProjectCoordinator, IncubatorAdmin, GlobalAdmin"`. A new NFR-K10 requires `MenuConfiguration.cs` to include Knowledge menu groups with GlobalAdmin in all role arrays.

2. **Principle IV — Integration event placement**
   - The constitution requires events in `{Domain}/Application/IntegrationEvents/`.
   - FR-K30 named the event but not its placement.
   - **FIX:** FR-K30 now specifies `Mentoory.Knowledge.Application/IntegrationEvents/`.

3. **Principle XI — PostDeployment seeds missing**
   - The brainstorm seed called for 1–2 sample global KnowledgeStructureTemplates for smoke testing; this dropped out of the final spec.
   - **FIX:** New FR-K50 mandates idempotent PostDeployment seed scripts for at least one sample global template.

## Recommendations

### Critical (Must Fix Before Implementation)
- None

### Important (Should Fix)
- All addressed inline (Constitution Principle X/IV/XI gaps, data-structure leak in FR-K21, ordering ambiguity in FR-K15, SC-K04 typo, transactionality language).

### Optional (Nice to Have)
- Concurrent-edit semantics (two coordinators editing the same clone simultaneously) are out of scope and not explicitly noted. Consider calling this out in Assumptions or Out-of-Scope for future readers. **Not blocking.**
- UI indicator for `SourceTemplateVersion` drift (EC-03) could have its own FR for concreteness. Currently it's only in an edge-case line. **Not blocking — the EC captures the requirement.**

## Conclusion

After applying all Important-severity fixes, the spec is implementable, testable, and constitution-aligned.

**Ready for implementation:** Yes (after fixes applied)

**Next steps:**
1. Fixes applied inline to `spec.md` in this review cycle
2. User reviews the updated spec
3. Proceed to `/speckit-clarify` (optional) or `/speckit-plan`
