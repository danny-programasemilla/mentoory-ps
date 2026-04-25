# Review Brief: Audit Pipeline Wiring

**Spec:** specs/016-audit-pipeline/spec.md
**Generated:** 2026-04-18

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Closes FR-045 (audit trails) and FR-018b (answer-correction traceability) by establishing a uniform way to capture security-sensitive commands across the modular monolith. Ships a hybrid capture mechanism: a `[Audited]` attribute on command classes with a MediatR pipeline behavior (`AuditingBehavior`) that handles the default payload automatically, plus a `Mode = Manual` escape hatch for handlers (like `CorrectAnswerHandler`) that need domain-specific detail. Extends `[audit].[AuditLog]` with five new columns (CorrelationId, Outcome, ExceptionType, UserEmail, RoleContext), retrofits five already-shipped commands, enforces coverage on future commands via an architecture test, adds a Platform Admin read-only viewer at `/Admin/AuditLog`, and binds future sensitive commands to the same rule through a governance update in `access-security-constitution.md`.

## Scope Boundaries

- **In scope:** attribute + pipeline behavior, redaction, schema extension, request-context abstraction (for IP + correlation), retrofit of 5 shipped commands, architecture test, admin viewer, governance update.
- **Out of scope:** entity-level before/after diffs for arbitrary aggregates; audit retention / TTL; notifications on audit events; CSV/SIEM export; real-time tailing; auditing query handlers; retrofit of `AdvanceProjectStageCommand` and `ApproveMentoringPlanCommand` (deferred to their own feature specs, but the governance rule added here will CI-fail the moment they land without `[Audited]`).
- **Why these boundaries:** the cross-cutting hardening brainstorm (#10) explicitly ratified the v1 payload scope, rejected entity diffs as out-of-proportion for v1, and separated audit-for-read-back from notification-for-alerting. The retrofit list is kept to shipped code to tighten blast radius; governance closes the gap for future commands.

## Critical Decisions

### Hybrid capture mechanism (attribute + pipeline behavior + explicit escape hatch)
- **Choice:** `[Audited]` attribute drives an `AuditingBehavior` for the automatic path; `Mode = Manual` opts out of automatic capture so handlers can write their own entries with domain-specific detail.
- **Trade-off:** two paths instead of one. Gains uniform coverage for the 95% case and preserves flexibility for the 5% (e.g., before/after answer text in `CorrectAnswer`) without coupling the behavior to aggregate knowledge.
- **Feedback:** Are there existing sensitive commands beyond the five listed where `Mode = Manual` is clearly needed, or can Automatic cover them all?

### Typed schema columns vs JSON-in-Details
- **Choice:** promote `CorrelationId`, `Outcome`, `ExceptionType`, `UserEmail`, `RoleContext` to first-class columns with a new index on `CorrelationId`.
- **Trade-off:** a one-time schema migration in exchange for indexable filters in the admin viewer and durable, self-documenting structure.
- **Feedback:** any concern about the migration running through the existing DACPAC pipeline, or preference to hold the schema change behind a feature flag?

### Architecture-test enforcement + governance update
- **Choice:** regex-matched command names (`^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$`) must carry `[Audited]`. Enforced by a test that fails CI. Paired with a new "Audit Trail Obligations" section in `access-security-constitution.md`.
- **Trade-off:** the regex is an imperfect proxy for "sensitive." False positives possible (a command name matching the regex that genuinely is not audit-worthy); false negatives likely (sensitive commands whose names don't match the pattern).
- **Feedback:** are there known-sensitive command name shapes outside this regex (e.g., `DeleteUser*`, `Revoke*`, `Reset*`) that should be added before shipping?

### Request-context abstraction for IP + correlation
- **Choice:** extend `ICorrelationContext` (or add a co-located `IRequestContext`) so `AuditingBehavior` reads client IP and correlation id through a single abstraction. No direct `IHttpContextAccessor` dependency in Application.
- **Trade-off:** one more abstraction member. Preserves the constitution's "no framework dependencies in Application" rule and keeps the behavior testable without spinning up `HttpContext` in unit tests.

### Best-effort audit write outside the business transaction
- **Choice:** preserve the existing `AuditService` ADO.NET-direct write path. Audit DB failure is caught and logged; business transaction unaffected.
- **Trade-off:** an audit row can go missing silently during a DB outage. The alternative — same-transaction write with a shared DbContext — was explicitly rejected in brainstorm #10 (breaks module boundaries) and same-transaction via interceptor was reserved for the Notification outbox pattern (different trust budget).

## Areas of Potential Disagreement

### Priority of the Manual-mode handler (User Story 4) vs correlation ID (User Story 3)
- **Decision:** US4 (correction before/after text) is P2; US3 (correlation id) is P3.
- **Why this might be controversial:** security reviewers may argue correlation is more valuable than before/after text for incident response, and should be P2.
- **Alternative view:** "incident reconstruction" benefits directly from correlation; "content correction traceability" is satisfied by the aggregate itself even without audit capture.
- **Seeking input on:** is correlation actually the second-most-important story, or is the current P2/P3 split right for this org?

### Retrofit list stops at 5
- **Decision:** retrofit only the 5 already-shipped sensitive handlers; `AdvanceProjectStage` and `ApproveMentoringPlan` apply the attribute when their own features ship.
- **Why this might be controversial:** a reviewer may argue the architecture test should ship with placeholder commands (no-op) to exercise enforcement end-to-end before those features land.
- **Alternative view:** placeholders couple this spec to unshipped work and introduce dead code. The governance rule + CI test is enough to guarantee coverage at landing time.
- **Seeking input on:** do we trust the CI gate, or do we want a placeholder command to smoke-test the gate?

### Admin viewer scope
- **Decision:** in-scope for v1 as a thin read-only DataTable (`/Admin/AuditLog`), following feature-013 patterns.
- **Why this might be controversial:** adds UI scope to what is otherwise a backend feature. Some shops would defer the viewer until capture is proven in production.
- **Alternative view:** capture without a viewer means admins file tickets for raw SQL access — most of the audit-trail value is never realized.
- **Seeking input on:** is the thin viewer the right v1 shape, or do we want it richer (CSV export, saved filters) or thinner (last-100-events stub)?

### Redaction depth
- **Decision:** top-level properties only in v1. Nested objects containing sensitive fields will NOT be redacted at depth.
- **Why this might be controversial:** a command carrying a value object with a `NationalId` could leak the raw value into `Details`.
- **Alternative view:** recursive walk from the start avoids the leak entirely.
- **Seeking input on:** do we have a known command today that passes a sensitive value via nested object? If yes, add it to scope now.

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Attribute | `[Audited(EventType, EntityType, Mode)]` | Applied to command classes |
| Mode enum values | `Automatic`, `Manual` | Controls whether the behavior writes the entry |
| Pipeline behavior | `AuditingBehavior<TRequest, TResponse>` | MediatR pipeline, registered after TransactionBehavior |
| Abstraction | `ICorrelationContext` (+ `IRequestContext` member) | Wraps correlation id + client IP for Application layer |
| Middleware | `X-Correlation-Id` header | Read from request, attached to response |
| Admin route | `/Admin/AuditLog` | Platform Admin read-only DataTable |
| Constitution section | "Audit Trail Obligations" | New section in `access-security-constitution.md` |
| Event types | `Context.Activated`, `Role.Assigned`, `User.Registered`, `User.LoggedIn`, `Answer.Corrected` | Five retrofitted commands |
| Redaction sentinel | `***REDACTED***` | Replaces sensitive values in `Details` |
| Regex | `^(Assign\|Approve\|Correct\|Advance\|Login\|Register\|SetActive).*Command$` | Architecture test sensitive-action matcher |

## Open Questions

- [ ] **OQ-1** Nested-object redaction — revisit when a command carries sensitive nested values.
- [ ] **OQ-2** Correlation-id propagation from hosted-service background jobs — accept explicit parameter vs derive from Activity?
- [ ] **OQ-3** Audit retention / TTL — defer to a future operational feature (90-day / 1-year / indefinite).
- [ ] Should the sensitive-action regex include additional stems (`Delete*`, `Revoke*`, `Reset*`) before v1 ships?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Audit DB outage silently drops rows | Medium — compliance gaps during incidents | Best-effort is intentional; add operational alerting on ILogger audit errors as a follow-up ops ticket |
| Regex misses a sensitive command | Medium — uncovered command ships without enforcement | Constitution documents the extension procedure; code review catches new sensitive commands; architectural test regex is a living list |
| Top-level-only redaction leaks nested sensitive values | High (if triggered) — PII in audit log | No known triggering command today; if one lands, extend redaction before merging |
| Schema migration in DACPAC conflicts with in-flight PRs | Low | Single-PR ship; coordinate with any parallel schema work |
| Admin viewer performance on large tables | Low-Medium — usability only | Server-side DataTable + index on CorrelationId and existing indexes on EventType/UserId/EntityType keep typical filters fast |
| Developers bypass `[Audited]` by naming a command outside the regex | Medium — silent coverage gap | Code review + constitution section; follow-up enhancement could move the list into a `[SensitiveCommand]` marker type for broader enforcement |

---
*Share with reviewers before implementation.*
