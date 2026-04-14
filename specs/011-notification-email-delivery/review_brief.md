# Review Brief: Notification Domain - Email Delivery System

**Spec:** specs/011-notification-email-delivery/spec.md
**Generated:** 2026-04-13

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

The Notification bounded context adds centralized, auditable email delivery to the Mentoory platform. It consumes integration events from the Access and Tenant domains, checks user delivery preferences, renders branded Razor templates in Spanish, and delivers emails through a database-backed queue with exponential backoff retry. The initial scope covers three notification types: login security alerts (with suspicious login detection), user registration/verification emails, and project invitation emails. Development uses Mailtrap.io; production uses Mailgun SMTP.

## Scope Boundaries

- **In scope:** Database-backed notification queue with retry, two SMTP provider implementations (Mailtrap/Mailgun), branded Razor templates for 3 email types, notification preferences (global toggle for login alerts), login context capture (IP + User-Agent parsing)
- **Out of scope:** SMS/push/in-app channels, IP geolocation, email open/click tracking, admin notification dashboard, digest/batching, password reset email migration, template editor UI
- **Why these boundaries:** Focuses on the foundational email pipeline and the three most critical notification types. Geolocation, additional channels, and admin tooling are natural follow-up specs that build on this foundation without blocking initial delivery.

## Critical Decisions

### Database-Backed Queue Over In-Memory Channel
- **Choice:** Notifications are persisted to the database with `Pending` status; a background service polls every 15 seconds
- **Trade-off:** Slightly higher latency (15s polling) vs guaranteed delivery and crash recovery
- **Feedback:** Is 15-second polling acceptable, or should we consider a shorter interval for time-sensitive login alerts?

### Separate Provider Implementations Over Configuration-Only
- **Choice:** `MailtrapEmailService` and `MailgunEmailService` as distinct classes, not a single SMTP service with different config
- **Trade-off:** More code upfront, but enables provider-specific features (Mailgun webhooks, Mailtrap testing APIs) without modifying shared code
- **Feedback:** Does the team foresee needing provider-specific features soon, or is this premature?

### Exponential Backoff Retry (1m, 5m, 15m, 1h, 4h)
- **Choice:** 5 attempts with increasing delays, then permanent failure
- **Trade-off:** Resilient to prolonged outages (total window ~5h 21m) but a failed notification won't be retried after that
- **Feedback:** Is the 5-hour retry window sufficient, or should there be an admin resend capability?

### Suspicious Login Threshold: 3+ Failed Attempts
- **Choice:** A login after 3 or more failed attempts (or post-lockout) triggers elevated urgency alerts
- **Trade-off:** Lower threshold than the lockout (5 attempts) means more suspicious alerts, increasing awareness at the cost of potential noise
- **Feedback:** Is 3 the right threshold, or should it match the lockout threshold of 5?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Preference Check Before Queuing
- **Decision:** If a user has disabled a notification type, no `Notification` record is created at all
- **Why this might be controversial:** Some teams prefer to always create the record (with status `Suppressed`) for complete audit trail
- **Alternative view:** Queue everything, filter at delivery time -- provides full visibility into what *would* have been sent
- **Seeking input on:** Is the audit trail of suppressed notifications important for compliance or debugging?

### Caller Provides HTTP Context via Event Payload
- **Decision:** The web layer enriches `LoginAttemptEvent` with IP and User-Agent before publishing
- **Why this might be controversial:** This means the Access domain's `LoginAttemptEvent` must be modified to carry HTTP-layer data, which some may see as leaking web concerns into domain events
- **Alternative view:** A separate `LoginNotificationRequest` could be published by the web layer directly to the Notification domain, keeping Access events clean
- **Seeking input on:** Is enriching the existing event acceptable, or should there be a separate notification-specific event from the web layer?

### No Debouncing for Login Alerts
- **Decision:** Every successful login triggers its own alert, even rapid successive logins
- **Why this might be controversial:** A user opening multiple tabs could receive several nearly identical emails in quick succession
- **Alternative view:** Debounce login alerts with a 5-minute window -- only send one alert per window
- **Seeking input on:** Will rapid successive login alerts annoy users, or is every login genuinely a security event worth notifying?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Aggregate root | Notification | Queued email with lifecycle tracking |
| Aggregate root | NotificationPreference | Per-user enable/disable settings |
| Value object | LoginContext | IP + browser + OS for login alerts |
| Value object | DeliveryAttempt | Append-only delivery history |
| Enum | NotificationType | LoginAlert, UserRegistration, ProjectInvitation |
| Enum | DeliveryChannel | Email (with future SMS, InApp placeholders) |
| Interface | IEmailService | Provider abstraction (Mailtrap, Mailgun implementations) |
| Interface | ITemplateRenderer | Razor template rendering abstraction |

## Open Questions

- [ ] Should `LoginAttemptEvent` be enriched with IP/User-Agent, or should the web layer publish a separate notification-specific event?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| SMTP provider outage | High -- all notifications delayed | Exponential backoff retry (5 attempts over ~5h); database queue preserves pending items |
| Template rendering bug | Medium -- specific notification type fails | Immediate failure (no retry) with detailed logging; other notification types unaffected |
| UAParser library incompatibility with .NET 10 | Low -- login alerts degrade | Graceful fallback to "Navegador desconocido"; verify compatibility during planning |
| LoginAttemptEvent enrichment breaks existing consumers | Medium -- regression risk | Careful backward-compatible changes; new fields should be optional/nullable |

---
*Share with reviewers before implementation.*
