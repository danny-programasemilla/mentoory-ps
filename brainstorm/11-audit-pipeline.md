# Brainstorm: Audit Pipeline Wiring

**Date:** 2026-04-18
**Status:** spec-created
**Spec:** specs/016-audit-pipeline/
**Parent seed:** [10-cross-cutting-hardening.md § Section 3](./10-cross-cutting-hardening.md)

## Problem Framing

The platform has `IAuditService` / `AuditService` / `[audit].[AuditLog]` already built, but no handler anywhere calls `LogAsync`. FR-045 (audit trails), FR-018b (answer-correction traceability), and SC-010 (traceable within 24h) are therefore all unsatisfied despite the scaffolding being in place. The #10 seed ratified the v1 payload shape (command type + redacted payload + user/tenant/role context + correlation + outcome; no entity diffs) but left the capture mechanism, schema shape, retrofit scope, and admin viewer scope unresolved.

This session resolved all four open threads and produced a concrete spec (016) ready to plan.

## Approaches Considered

### A: Attribute + pipeline behavior (fully automatic)

- **Pros:** zero per-handler boilerplate; uniform payload; enforceable via architecture test; the handler source stays clean.
- **Cons:** invisible at handler-read time (reader doesn't see audit happening); domain-specific captures (like before/after answer text) are impossible because the behavior has no aggregate knowledge.

### B: Explicit `IAuditService.LogAsync` in every handler

- **Pros:** visible at call site; each handler chooses exactly what to log; naturally supports domain-specific detail.
- **Cons:** verbose; easy to forget; no enforcement path short of manual code review; boilerplate multiplies across dozens of future sensitive commands.

### C: Hybrid — attribute + behavior as default, `Mode = Manual` escape hatch (chosen)

- **Pros:** covers the 95% uniform case automatically; keeps the 5% domain-specific path available; single enforcement point (architecture test can check for `[Audited]` regardless of mode); aligns with the existing MediatR pipeline pattern (ValidatorBehavior, TransactionBehavior).
- **Cons:** two paths instead of one (minor cognitive overhead); `Mode = Manual` handlers carry per-handler responsibility to actually call `LogAsync` — not architecture-test enforceable, only integration-test checkable.

## Decision

**Hybrid (Approach C).** All resolved decisions crystallized in the spec:

- **Capture mechanism:** `[Audited(EventType, EntityType, Mode)]` attribute + `AuditingBehavior<TRequest, TResponse>` MediatR pipeline behavior. `Mode = Manual` opts out of automatic capture so handlers can write domain-specific detail themselves (e.g., `CorrectAnswer` emits before/after answer text).
- **Schema:** extend `[audit].[AuditLog]` with typed columns — `CorrelationId`, `Outcome`, `ExceptionType`, `UserEmail`, `RoleContext` — plus a non-clustered index on `CorrelationId`. Rejected JSON-in-Details because the admin viewer needs indexable filters on outcome and correlation.
- **Retrofit scope:** 5 shipped handlers — `SetActiveContext` → `Context.Activated`, `AssignRole` → `Role.Assigned`, `RegisterUser` → `User.Registered`, `LoginUser` → `User.LoggedIn`, `CorrectAnswer` → `Answer.Corrected` (Manual mode). `AdvanceProjectStage` and `ApproveMentoringPlan` deferred to their own feature specs; governance rule catches them at landing time via CI.
- **Admin viewer:** in v1 as a thin server-side DataTable at `/Admin/AuditLog`, GlobalAdmin-only, following feature-013's pattern. Filters on EventType, UserId, Outcome, date range; expandable detail row with pretty-printed `Details` JSON.
- **Governance:** new "Audit Trail Obligations" section in `access-security-constitution.md` + an architecture test asserting every command matching `^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$` carries `[Audited]`. Future features extending the category list must extend both the regex and the constitution together.
- **Request-context abstraction:** during spec review, the `AuditingBehavior` was initially designed to read client IP from `IHttpContextAccessor` directly. Reviewer flagged the Clean Architecture violation. Resolved by extending `ICorrelationContext` (FR-011a) to expose the client IP, so Application-layer code never takes a direct HTTP dependency.

**Out of scope (explicitly):** entity-level before/after diffs, audit retention/TTL, audit-driven notifications, CSV/SIEM export, real-time tailing, auditing query handlers.

## Open Threads

- Nested-object redaction depth — v1 is top-level only. Revisit when a `Manual`-mode handler passes a sensitive value via nested object. (Tracked as OQ-1 in the spec.)
- Correlation-id propagation from hosted-service background jobs — v1 derives from `Activity.Current?.Id`. Revisit when the first non-trivial background command lands. (OQ-2.)
- Audit retention / TTL — deferred to a future operational feature (90-day vs 1-year vs indefinite). (OQ-3.)
- Sensitive-action regex completeness — should stems like `Delete*`, `Revoke*`, `Reset*` be added before v1 ships? Left for review feedback on `review_brief.md`.
- Priority ordering of US4 (correction detail) vs US3 (correlation) — reviewer may push to elevate US3 to P2.
