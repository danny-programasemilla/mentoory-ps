# Spec Review: Audit Pipeline Wiring

**Spec:** specs/016-audit-pipeline/spec.md
**Date:** 2026-04-18
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** SOUND

**Summary:** The specification is complete, internally consistent, implementable, and testable. It codifies the v1 decisions ratified in the PR #10 brainstorm cycle with concrete event types, column additions, a sensitive-action regex, a five-command retrofit list, and a discoverable governance rule. One important stylistic observation and a handful of optional polish items. Ready to plan.

## Completeness: 5/5

### Structure
- All required sections present (User Scenarios, Edge Cases, Functional Requirements, Key Entities, Success Criteria, Assumptions, Open Questions).
- Recommended sections included (Priority-ordered user stories, Acceptance Scenarios per story, Edge Cases).
- No placeholder text remains.

### Coverage
- All 23 functional requirements are explicit and action-scoped (attribute, behavior, redaction, persistence, correlation, retrofit, enforcement, viewer, governance).
- 10 edge cases covered (handler exception, anonymous commands, large payload, serialization failure, audit DB failure, missing correlation, concurrent commands, long-running handler, nested redaction limitation, missing attribute).
- 9 success criteria, each measurable.

**Issues:** None.

## Clarity: 4.5/5

### Language Quality
- All FRs use RFC-2119 MUST / MAY language.
- Redaction list is enumerated exactly; sensitive-action regex is verbatim; event types per retrofitted command are listed exactly.
- Acceptance scenarios use Given/When/Then uniformly.

### Minor Ambiguities

1. **Edge case "Long-running handler (>30 s)"** — ends with "Documented." but does not say where. Reader may infer intent but the record is not self-contained.
   - Suggestion: clarify location (e.g., "documented in the admin viewer's column help text" or "documented in the AuditingBehavior source comment").

2. **SC-007** — "under 1 second of perceived wait time" is clear as a user-facing SLO, but a reader could interpret "perceived" differently (time-to-first-byte vs time-to-interactive).
   - Suggestion: pin to "time from page request to rendered table on a modern browser" or adopt the project's standard UX latency definition if one exists.

3. **FR-003(h)** — "remote IP address from the current HTTP context" reaches into the web layer directly. See Constitution Alignment below.

## Implementability: 5/5

### Plan Generation
- A plan generator can derive tasks for: attribute definition, behavior implementation, schema migration, AuditEntry record update, AuditService update, correlation middleware, ICorrelationContext, five command retrofits, architecture test, admin viewer (Razor page + server-side DataTable), governance update.
- Dependencies named (ITenantContext, ITimeProvider, IHttpContextAccessor, ValidatorBehavior, TransactionBehavior, DACPAC pipeline, feature 013 DataTable pattern).
- Scope is single-sprint sized; out-of-scope items are explicitly enumerated.

**Issues:** None blocking.

## Testability: 5/5

### Verification
- Every user story has independent-test guidance.
- Every FR maps to a testable behavior.
- SCs are measurable: 100% coverage of retrofitted commands (SC-002), CI fail on missing attribute (SC-004), 1,000-row viewer < 1 s (SC-007), zero redaction leaks (SC-003), before/after text capture (SC-008).
- Architecture test itself is specified with the exact regex (FR-015) — directly testable.

**Issues:** None.

## Constitution Alignment

Checked against `.specify/memory/constitution.md` (Mentoory Constitution, v1.1.1) and referenced `.specify/memory/access-security-constitution.md` (not fully re-read here because the spec commits to augmenting it).

- **Clean Architecture Layer Boundaries (I):** `AuditingBehavior` lives in Application per MediatR convention; `AuditService` is Infrastructure; Domain is untouched. Consistent.
- **CQRS (II):** audit is orthogonal to commands; FR-002 pipeline ordering (audit after ValidatorBehavior and after TransactionBehavior) respects CQRS. Consistent.
- **DDD (III):** no aggregate changes. `CorrectAnswer` retains its `AnswerCorrection` domain-side history; audit is additive. Consistent.
- **Zero-Warnings Policy (V):** architecture test enforces build-fail on missing attribute — reinforces constitution. Consistent.
- **DateTime Handling (VI):** FR-003(e) explicitly uses `ITimeProvider`, not `DateTime.UtcNow`. Consistent.
- **Naming Conventions (VII):** follows existing `IAuditService`, `AuditEntry`, `AuditLog` pattern. Consistent.
- **SSDT/DACPAC (X):** FR-008 schema change via DACPAC, not EF migration. Consistent.

### Minor observation
- **FR-003(h) web-layer coupling:** fetching `RemoteIpAddress` from the HTTP context inside a MediatR pipeline behavior (which lives in Application) routes around the constitution's "no framework dependencies in Application" spirit. The project's existing convention is to wrap HTTP-derived values behind abstractions (the spec itself does this for correlation via `ICorrelationContext` in FR-011). Recommend extending the same abstraction approach to IP capture rather than injecting `IHttpContextAccessor` directly into the behavior.

## Recommendations

### Critical (Must Fix Before Implementation)
- None.

### Important (Should Fix)
- [ ] **IP address abstraction.** Wrap the remote-IP read in a small abstraction (e.g., extend `ICorrelationContext` with a `ClientIpAddress` property, or add `IRequestContext`) so `AuditingBehavior` does not take a direct dependency on `IHttpContextAccessor`. Revise FR-003(h) and add/adjust the relevant FR in the Correlation block.

### Optional (Nice to Have)
- [ ] Clarify "Long-running handler (>30 s)" edge-case documentation location.
- [ ] Pin SC-007's "perceived wait time" to a concrete measurement boundary.
- [ ] Consider promoting "Out of Scope" out of the Assumptions section into its own header for discoverability (cosmetic — spec-template does not mandate it).
- [ ] Add explicit cross-reference between Edge Case "Redaction of nested objects" and FR-007 (both already say the same thing; a "see FR-007" link would reduce divergence risk).

## Conclusion

The spec is ready for planning. The Important recommendation (IP abstraction) is a stylistic improvement that keeps the Application layer pure — worth addressing before `/speckit-plan` but not blocking. The Optional items can be deferred to the plan or tasks phase if convenient.

**Ready for implementation:** Yes (after addressing the IP abstraction recommendation, which is small).

**Next steps:**
1. Apply the Important recommendation (wrap IP read in an abstraction; adjust FR-003(h) and add a correlation/request-context FR).
2. Optionally apply Optional items.
3. Proceed to `/speckit-plan`.
