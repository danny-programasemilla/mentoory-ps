# Implementation Notes: Notification Domain - Email Delivery System

## Design Decisions

### Decision: MailKit SMTP for Both Providers
- Both Mailtrap (dev) and Mailgun (prod) use standard SMTP protocol
- MailKit 4.15.1 is already an approved dependency in the project
- Separate `IEmailService` implementations allow provider-specific features later (e.g., Mailgun API for bounce tracking)
- Mailtrap: `sandbox.smtp.mailtrap.io:2525`
- Mailgun: `smtp.mailgun.org:587` via sending domain `mail.mentoory.com`

### Decision: Separate Implementations vs Configuration-Only
- Chose separate `MailtrapEmailService` and `MailgunEmailService` over configuration-only switching
- Rationale: Enables provider-specific features (Mailgun webhooks, Mailtrap testing APIs) without modifying a shared implementation
- Trade-off: Slightly more code, but cleaner extensibility

### Decision: Razor Templates Over HTML Token Replacement
- Razor templates chosen for type safety, C# power, and team familiarity
- Templates live in Infrastructure layer, rendered via `ITemplateRenderer` service
- Shared base layout ensures consistent branding (rich/branded style with color banner, card layout, footer)
- Rejected MJML: adds Node.js build step dependency
- Rejected plain HTML tokens: no type safety, no conditional logic

### Decision: Database-Backed Queue Over In-Memory Channel
- Chose database-backed queue over `Channel<T>` with `IHostedService`
- Rationale: Survives process crashes, provides full audit trail, enables retry with backoff
- Trade-off: Slightly higher latency (polling every 15s) vs immediate in-memory processing
- Rejected inline MediatR: blocks the request pipeline during SMTP round-trip

### Decision: Exponential Backoff Retry Strategy
- Schedule: 1m, 5m, 15m, 1h, 4h (5 attempts max)
- Chosen over simple fixed retry for better resilience against prolonged outages
- After 5 failures: marked `Failed` permanently
- Template rendering errors: immediate failure (code bug, not transient)

### Decision: Full Notification Preferences From Day One
- Build `NotificationPreference` aggregate immediately rather than deferring
- Only login alerts are user-configurable; registration/invitation are system-mandatory
- Default: all notifications enabled; lazy creation on first preference change
- Fail-open: if preference lookup fails, default to sending

### Decision: Login Context - IP + User-Agent Only (No Geolocation)
- Capture IP and User-Agent from caller (web layer provides via event payload)
- Parse User-Agent via UAParser NuGet package into browser name + OS name
- Geolocation deferred to future spec -- `LoginContext` value object designed to accommodate it
- Graceful degradation: unknown agents show "Navegador desconocido" / "Sistema operativo desconocido"

### Decision: Caller Provides HTTP Context, Not Notification Domain
- Notification domain has zero HTTP awareness
- Web layer enriches `LoginAttemptEvent` with IP and User-Agent before publishing
- Aligns with Clean Architecture: domain should not know about transport layer

## Architecture Notes

### Domain Structure (Approach B - Layered DDD)
- `Notification` aggregate root: tracks lifecycle (Pending -> Sent/Failed)
- `NotificationPreference` aggregate root: per-user, per-incubator settings
- `DeliveryAttempt`: value object, append-only delivery history
- `LoginContext`: value object for IP + browser + OS
- `NotificationType` enum: LoginAlert, UserRegistration, ProjectInvitation
- `DeliveryChannel` enum: Email (with future SMS, InApp placeholders)

### Integration Events Consumed
- `UserRegisteredEvent` -> registration email + invitation email (if applicable)
- `InvitationReissuedEvent` -> fresh invitation email
- `LoginAttemptEvent` -> login alert (standard or suspicious)
- `UserLockedOutEvent` -> potential future use

### Email Template Design
- Style: Rich/branded with colored header banner, card-based layout, footer with social/platform links
- Language: All Spanish
- Base layout shared across all notification types
- Future geolocation data can be added to login alert template without structural changes
