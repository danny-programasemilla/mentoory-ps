# Brainstorm: Notification Domain - Email Delivery System

**Date:** 2026-04-13
**Status:** spec-created
**Spec:** specs/011-notification-email-delivery/

## Problem Framing

The Mentoory platform has empty Notification module projects (Domain, Application, Infrastructure) with MailKit already declared as a dependency, but no email delivery implementation exists. Users need to receive transactional emails for critical platform events: account registration/verification, project invitations, and login security alerts. The system needs professional branded templates in Spanish, environment-based provider switching (Mailtrap for dev, Mailgun for prod), and user-configurable notification preferences.

## Approaches Considered

### A: Monolithic Notification Service
- Pros: Fast to build, fewer files, easy to understand initially
- Cons: Violates SRP, hard to test individual concerns, template changes risk breaking sending logic, retry logic entangled with SMTP code

### B: Layered Domain with Aggregate-Driven Queue (chosen)
- Pros: Clean separation of concerns, testable at every layer, retry logic in domain (pure), provider swap is DI change, templates isolated from sending logic
- Cons: More upfront structure, more files

### C: Event-Sourced Notifications
- Pros: Perfect audit history, natural fit for retry tracking, temporal queries
- Cons: Significant complexity overhead, event sourcing infrastructure doesn't exist in project, overkill for transactional emails

## Decision

Chose **Approach B** -- Layered DDD with database-backed queue. Aligns with the project's established Clean Architecture and DDD patterns. The empty Notification module projects provide natural landing zones.

### Key design choices:
- **Razor templates** for email rendering (team knows the syntax, strongly typed)
- **Branded/rich style** (colored header banner, card layout, footer with links)
- **Full notification preferences from day one** (login alerts = global toggle, registration/invitation = mandatory)
- **Database-backed queue** with exponential backoff retry (1m, 5m, 15m, 1h, 4h, max 5 attempts)
- **Separate provider implementations** (MailtrapEmailService + MailgunEmailService) for future extensibility
- **IP + User-Agent capture** for login alerts (no geolocation yet), caller provides via event payload
- **Suspicious login threshold**: 3+ failed attempts or post-lockout

## Open Threads

- Should `LoginAttemptEvent` be enriched with IP/User-Agent, or should the web layer publish a separate notification-specific event? (deferred to implementation planning)
- Geolocation for login alerts deferred to a future spec
- Password reset email migration from Access domain to Notification domain is a future spec
- Admin dashboard for notification monitoring is a future spec
