# Brainstorm Seed: Cross-Cutting — Subscription + Notification + Audit Wiring

**Date:** 2026-04-18
**Status:** parked
**Spec:** —
**Parent roadmap:** [06-platform-roadmap-gap-analysis.md](./06-platform-roadmap-gap-analysis.md) (Phases A–B, warm streams A-β, B-α, B-β)

> This is a **seed document** covering three independent-but-related cross-cutting areas that run as warm AI streams alongside the hot feature work. Each section below is a future brainstorm candidate; they can be combined or split depending on ship cadence.

---

## Why Grouped

Subscription, Notification, and Audit share characteristics that make them suitable for AI-subagent-driven warm streams:
- Each is **horizontally cross-cutting** (touches many modules via integration points)
- Each is **isolated in its own module / behavior** (no shared-file contention with hot streams)
- Each is **security/compliance-adjacent** (wrong design = latent bugs, not user-visible failures)
- Each has **explicit FR anchors** but moderate design latitude

---

## Section 1 — Subscription Module (US6)

### Current State
- `Mentoory.Subscription.Domain/.Application/.Infrastructure` empty
- `SubscriptionPlans.sql` table exists with `Name`, `Description`, `Version`, `IsActive` only — **missing features + overrides tables**
- `Incubator.AssignSubscriptionPlan()` domain method exists but no command handler
- No enforcement in `CreateProjectHandler` — incubator can exceed project limits freely

### Key FRs
- **FR-032** — Versioned plans with boolean + quantitative features; new version archives previous; incubators remain until reassigned
- **FR-033** — Positive-only accumulative overrides per incubator with no expiration
- **FR-034** — Feature limits enforced based on effective plan (base + overrides)
- **FR-035** — Admin-managed, no payment gateway
- **FR-014** — FormTemplates filtered by subscription tier (currently `FormTemplates.SubscriptionTier` column exists, no filter)

### Scope for the first spec
- Domain: `SubscriptionPlan` (aggregate root, with `PlanFeature` collection), `IncubatorOverride`
- Application: `CreatePlan`, `UpdatePlan`, `AssignPlanToIncubator`, `ApplyOverride` commands
- Application: `GetEffectiveFeaturesQuery` returning computed features (base + overrides) per incubator
- Integration: `Shared.Application.ISubscriptionFeatureService` consumed by Tenant (`CreateProjectHandler`) and Diagnostic (FormTemplate filter)
- Platform Admin UI: plan CRUD + override management

### Open Questions
1. Feature key/value schema: structured (typed features) vs bag-of-strings? Typed gives compile-time safety; bag gives extensibility.
2. Override semantics on booleans: if base is false and override is true, result is true. What about overriding a quantitative value from 50 to "unlimited"? Null/magic value?
3. Plan change behavior: spec says "take effect immediately, no grace period" (assumption). What about features that downgrade — if base drops from 50→30 projects and incubator has 45, spec edge case says existing projects remain, only new creation blocked. Implement that.
4. Where does enforcement actually live — validator on commands, or pipeline behavior? Validator is per-command-local and clearer; pipeline behavior is DRY but opaque.

### Integration Seams with Hot Streams
- `CreateProjectHandler` (Tenant, already shipped) — add validator step calling `ISubscriptionFeatureService.CheckProjectLimit()`
- `CloneFormTemplateHandler` (Diagnostic, already shipped) — filter templates by incubator's effective tier
- Platform Admin area new controller — does not collide with existing work

---

## Section 2 — Notification Module (US8)

### Current State
- `Mentoory.Notification.Domain/.Application/.Infrastructure` empty
- `Mentoory.Notification.Tests` empty
- `notification/Schema.sql` contains only schema declaration
- MailKit/MimeKit listed as "Active Technologies" in CLAUDE.md but not referenced anywhere
- No event handlers anywhere subscribe to domain events

### Key FRs
- **FR-040** — Centralized, auditable, no-duplication
- **FR-041** — Immediate + scheduled types
- **FR-042** — Track content, recipients, channel, delivery status
- **FR-043** — Per-role + per-context user preferences
- **FR-044** — Email initial, extensible

### Scope for the first spec
- Domain: `Notification` (with recipients collection), `NotificationPreference`, `NotificationOutboxEntry`
- Application: `EnqueueNotification` command, `ProcessOutbox` scheduled background service
- Application: `NotificationEventDispatcher` that listens to domain events and maps them to notifications
- Channel: `EmailChannelAdapter` using MailKit (SMTP config via `IOptions`)
- Dedup: idempotency key per (event, recipient) — no duplicate send for the same key
- Scheduled notifications: Quartz.NET-lite or hand-rolled cron (constitution prefers minimalism — prefer hand-rolled hosted service for the first spec)
- Preferences UI: stub with defaults; per-role + per-context preferences

### Decided v1 Patterns

- **Outbox — `SaveChangesInterceptor` per module DbContext:** each module's DbContext registers a `SaveChangesInterceptor` that detects outbox-worthy events (domain events emitted by aggregates) and writes outbox entries **within the same transaction** as the triggering handler's `SaveChanges`. Outbox table lives in a shared `[notification]` schema. A single hosted background service polls and dispatches. **Dual-write across independent DbContexts is explicitly rejected** — it is not atomic and cannot be made durable without distributed transactions, which the stack does not support. Shared-DbContext approaches are also rejected because they break module boundaries.

### Open Questions

1. Scheduled dispatch: hosted background service polling the outbox on interval, vs an explicit scheduler library? (v1 recommendation: hand-rolled hosted service polling at a configurable interval; library adoption is a later optimization.)
2. Dedup key strategy: `(eventType, entityId, recipientId)` — sufficient? Does not cover cases like "weekly agenda on Monday" that fires at cadence, not per-event. Need a second key shape for cadence-based notifications.
3. Preferences granularity: per-role-per-context (spec says both) creates a combinatorial space. Store defaults + overrides?
4. Template rendering: inline strings, Razor views, or a template engine (Scriban, etc.)?
5. Email delivery failure semantics: retries + backoff + dead-letter?

### Integration Seams
- Subscribes to domain events from all modules (MediatR notifications)
- Emits no outgoing cross-module events
- Uses `IAuditService` from FR-045 to log notification send events

### Event Inventory (Target Subscriptions)
- `UserRegisteredEvent` → verification email
- `EmailVerificationRequestedEvent` → verification code
- `PasswordResetRequestedEvent` → reset link
- `DiagnosticCompletedEvent` → notify coordinator + mentor
- `MentoringPlanApprovedEvent` → notify entrepreneur
- `MentoringSessionScheduledEvent` → reminder to mentor + entrepreneur
- `AssignmentCreatedEvent` → notify entrepreneur
- `AssignmentSubmittedEvent` → notify mentor
- `AssignmentReviewedEvent` → notify entrepreneur
- `ProjectStageAdvancedEvent` → notify participants

---

## Section 3 — Audit Pipeline Wiring (FR-045 + FR-018b)

### Current State
- `IAuditService` interface in `Mentoory.Shared.Application/Audit/IAuditService.cs` (single `LogAsync` method)
- `AuditService` implementation in `Mentoory.Shared.Infrastructure/Audit/AuditService.cs` (writes via ADO.NET to `[audit].[AuditLog]`)
- Database table exists with proper indexes on `EventType`, `UserId`, `EntityType`
- **No handler anywhere calls `LogAsync`**
- `CorrectAnswerHandler` records audit in the `AnswerCorrection` aggregate itself but does not write to the central audit log

### Key FRs
- **FR-045** — Full audit trails for security events, context changes, plan approvals, stage transitions
- **FR-018b** — Answer corrections require audit trail (who/when/previous) — **already in domain via AnswerCorrection** but not centrally logged

### Scope for the first spec
- MediatR `AuditingBehavior<TRequest, TResponse>` pipeline behavior
- Opt-in mechanism: `[Audited(EventType, EntityType)]` attribute on command classes
- Payload capture: command metadata + user + tenant context + before/after state for writes
- Retrofit 5–7 existing handlers: `SetActiveContext`, `AssignRole`, `ApproveMentoringPlan` (future), `AdvanceProjectStage` (Phase A), `CorrectAnswer`, `RegisterUser`, `LoginUser`
- Architecture test: assert every write handler whose command matches a set of security-sensitive prefixes has `[Audited]`
- Audit log view in Platform Admin area (read-only table)

### Decided v1 Scope

- **Audit payload for v1:** `AuditingBehavior` captures (a) command type name, (b) command payload serialized with sensitive-field redaction (`Password`, `PasswordHash`, `NationalId`, `VerificationToken`, etc.), (c) active user ID + email, (d) active tenant + project + role context, (e) correlation ID, (f) UTC timestamp from `ITimeProvider`, (g) command outcome (success or failure + exception type if failure).
- **Entity-level before/after diffs are OUT OF SCOPE for v1.** Fetching the pre-state inside a pipeline behavior requires DbContext coordination that is out of proportion to v1 value. Aggregate state changes remain traceable via domain events if needed.
- **Audit log is NOT an event store** — it is a denormalized append-only log for read-back. Keep writes simple.

### Open Questions

1. **Mechanism — automatic vs explicit:** pipeline behavior via `[Audited]` attribute is invisible and easy to forget; explicit `IAuditService.LogAsync` in each handler is verbose. Trade-off still to resolve; both are compatible with the v1 payload scope.
2. **Audit retention:** SC-010 says "traceable within 24 hours" — implies available, not necessarily forever. Leave TTL as a future concern.
3. **Cross-cutting with Notification:** should audit log send a notification to platform admin on sensitive events? Likely no — audit is for read-back, not alerting.

---

## Integration Risk Register (Cross-Cutting)

1. **Subscription `ISubscriptionFeatureService` injection into Tenant:** `Mentoory.Shared.Application` is the right place for the abstraction so Tenant doesn't depend on Subscription.
2. **Notification outbox transactional consistency:** if the triggering handler's DbContext and the outbox writer's DbContext differ, dual-write fails. Decision: notification outbox writes go through Shared DbContext OR via Unit of Work that spans modules. Discuss.
3. **AuditingBehavior + multiple DbContexts:** if each module has its own DbContext, the audit log write is a separate transaction. Acceptable — audit is best-effort logging, not part of the business transaction.
4. **Subscription plan change during active project:** spec edge case (plan limit reduced below current project count) → existing projects remain, only creation blocked. Implement.
5. **Notification dedup edge cases:** "canceled session reminder" — outbox entry must be revoked, not just ignored. Needs design.

---

## Suggested Ship Order

1. **Subscription** can ship independently — smallest blast radius, most valuable gate for enforcing business rules.
2. **Audit pipeline behavior** ships next and is retroactively applied across modules; it unlocks compliance.
3. **Notification** ships last among these three — most integration points, so it benefits from having events from other modules already defined.

Alternative: ship **Audit first** to unlock compliance logging during Subscription + Notification development.

## Suggested Next Steps (per sub-stream)

- **Subscription:** open `/spex:brainstorm`, resolve open questions, `/speckit-specify`
- **Notification:** open `/spex:brainstorm`, inventory events, `/speckit-specify`
- **Audit:** smaller — can skip brainstorm if open questions resolve quickly, go straight to `/speckit-specify`

## Open Threads

- Subscription feature schema: typed vs bag
- Subscription override value semantics for booleans + unlimited quantitative
- Notification outbox transactional approach (shared DbContext vs separate)
- Notification scheduled-dispatch mechanism (hosted service vs library)
- Notification template engine (inline vs Razor vs Scriban)
- Audit pipeline behavior: automatic via attribute vs explicit `IAuditService.LogAsync` in handlers
- Audit before/after state capture strategy
- Ship order: Subscription first vs Audit first
