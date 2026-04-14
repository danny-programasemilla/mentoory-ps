# Feature Specification: Notification Domain - Email Delivery System

**Feature Branch**: `011-notification-email-delivery`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "Notification domain with Mailtrap (dev) and Mailgun (prod), professional branded Razor templates in Spanish, covering login alerts, registration/verification, and project invitation emails"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - User Registration Email (Priority: P1)

A project coordinator creates a new user (individual or batch). The system sends a branded welcome email in Spanish containing the user's name, project name, a verification link with token, and the link expiration timeframe. The user receives the email in their inbox and clicks the verification link to complete onboarding.

**Why this priority**: Registration is the entry point for all users. Without this email, no user can verify their account and access the platform. This is the foundational notification that unblocks all other platform usage.

**Independent Test**: Can be fully tested by creating a user via the admin interface and confirming the email arrives in Mailtrap with correct content, working verification link, and branded Spanish template.

**Acceptance Scenarios**:

1. **Given** a project coordinator creates a new user with email `juan@example.com`, **When** the `UserRegisteredEvent` is published, **Then** a welcome/verification email is queued with status `Pending` and delivered within 2 minutes
2. **Given** a registration email is queued, **When** the background service processes it, **Then** the email contains the user's first name, project name, a clickable verification URL, and expiration timeframe -- all in Spanish
3. **Given** the SMTP server is temporarily unreachable, **When** delivery fails, **Then** the system records a `DeliveryAttempt` with the failure reason and schedules a retry at the next backoff interval (1m)
4. **Given** the same `UserRegisteredEvent` is replayed (duplicate), **When** the handler processes it, **Then** no duplicate notification is created (idempotency via `SourceEventId`)

---

### User Story 2 - Project Invitation Email (Priority: P1)

A project coordinator invites a user to a project. The system sends a branded invitation email containing the inviter context, project name, incubator name, an acceptance/verification link, and expiration timeframe. When an invitation is reissued, a fresh email with a new token is sent.

**Why this priority**: Invitations are the primary mechanism for onboarding users into specific projects. Tied closely to registration flow and equally critical for platform adoption.

**Independent Test**: Can be tested by creating an invitation via the admin interface and confirming the email arrives with correct project/incubator context and a working action link.

**Acceptance Scenarios**:

1. **Given** a user is registered with `EnrollmentVariant = Invitation`, **When** `UserRegisteredEvent` is published, **Then** an invitation email is queued containing project name, incubator name, and acceptance URL
2. **Given** an admin reissues an invitation, **When** `InvitationReissuedEvent` is published, **Then** a new invitation email is queued with the fresh token and expiration
3. **Given** the original invitation email is still pending when a reissue occurs, **When** both emails are eventually delivered, **Then** both arrive but only the new invitation link is valid (old token deactivated by Access domain)

---

### User Story 3 - Login Security Alert (Priority: P2)

A user logs in to the platform. The system sends a security alert email with the login timestamp, IP address, and browser/OS information parsed from the User-Agent header. For suspicious logins (new IP after failed attempts, login after lockout release), the email uses an elevated urgency tone with a distinct subject line.

**Why this priority**: Security alerts enhance account protection and user trust but are not blocking for core platform functionality. Users can use the platform without them.

**Independent Test**: Can be tested by performing a login and confirming the alert email arrives in Mailtrap with correct IP, browser, OS, and timestamp. Suspicious login can be tested by triggering failed attempts first, then succeeding.

**Acceptance Scenarios**:

1. **Given** a user logs in successfully from IP `203.0.113.42` using Chrome on Windows, **When** `LoginAttemptEvent` is published with success and client context, **Then** a login alert email is queued with subject "Nuevo inicio de sesion" containing the IP, "Chrome", "Windows", and formatted timestamp
2. **Given** a user logs in after 3 or more failed attempts, **When** the login succeeds, **Then** the email uses subject "Actividad sospechosa detectada" with elevated urgency styling
3. **Given** a user logs in after a lockout was released (5 failed attempts), **When** `LoginAttemptEvent` is published, **Then** the email is flagged as suspicious with urgency tone
4. **Given** User-Agent parsing fails for an unknown agent string, **When** the notification is processed, **Then** the email renders with "Navegador desconocido" and "Sistema operativo desconocido" instead of blocking delivery

---

### User Story 4 - Notification Preferences Management (Priority: P2)

A user configures their notification preferences to disable login alerts globally. The system respects this preference and stops queuing login alert notifications for that user. Registration and invitation emails are system-mandatory and cannot be disabled.

**Why this priority**: Preferences give users control over potentially noisy notifications (login alerts). Important for user experience but not blocking for the core notification pipeline.

**Independent Test**: Can be tested by disabling login alerts for a user, performing a login, and confirming no login alert email is queued. Then verifying that registration/invitation emails still send regardless of preference settings.

**Acceptance Scenarios**:

1. **Given** a user has login alerts disabled globally, **When** they log in and the preference is checked, **Then** no login alert notification is created
2. **Given** a user has no preference record (new user), **When** a notification is triggered, **Then** the system treats all notifications as enabled (default)
3. **Given** a user tries to disable registration or invitation emails, **When** the preference update is attempted, **Then** the system rejects the change -- these are system-mandatory
4. **Given** the preference lookup fails (database error), **When** a notification is triggered, **Then** the system defaults to sending (fail-open) and logs the error

---

### User Story 5 - Email Provider Switching (Priority: P3)

The development team uses Mailtrap.io in the development environment for safe email testing. In production, the system uses Mailgun SMTP via `mail.mentoory.com`. Switching between providers requires only environment-specific configuration -- no code changes.

**Why this priority**: Infrastructure concern that supports all other stories. Lower priority because it can be validated after the core notification pipeline works with a single provider.

**Independent Test**: Can be tested by running the application with `appsettings.Development.json` (Mailtrap credentials) and confirming emails arrive in Mailtrap, then switching to production config and confirming emails route through Mailgun.

**Acceptance Scenarios**:

1. **Given** the application runs in Development environment, **When** an email is sent, **Then** MailKit connects to Mailtrap SMTP (`sandbox.smtp.mailtrap.io:2525`) and the email appears in the Mailtrap inbox
2. **Given** the application runs in Production environment, **When** an email is sent, **Then** MailKit connects to Mailgun SMTP (`smtp.mailgun.org:587`) via `mail.mentoory.com` credentials
3. **Given** SMTP credentials are missing or invalid, **When** the email service attempts to connect, **Then** the delivery attempt fails with a clear error message and the notification is marked for retry

---

### Edge Cases

- **User registers but verification token expires before email is delivered**: Email still sends with the original token. The verification flow handles expired tokens with a clear user-facing message.
- **Rapid successive logins**: Each login generates its own notification. No debouncing -- every login is a security event worth recording.
- **Invitation reissued while original email is still pending**: Both emails may be delivered. The old link won't work (token deactivated by Access domain). Acceptable behavior.
- **User has no notification preferences record**: Treat as "all enabled" (default). Preference record created lazily on first preference change.
- **Background service picks up notification for a deleted user**: Mark as `Failed` with reason "Recipient not found". No retry.
- **Multiple notification types from same event**: `UserRegisteredEvent` may trigger both registration and invitation emails. Each produces a separate notification with its own `SourceEventId` scoped by notification type.
- **Background service crashes mid-processing**: `IHostedService` restarts automatically. Pending notifications remain in database with `Pending` status and are picked up on next poll cycle.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST queue notification records with `Pending` status when integration events are received from other domains
- **FR-002**: System MUST process pending notifications via a background service that polls every 15 seconds
- **FR-003**: System MUST record each delivery attempt with timestamp, success/failure status, and failure reason
- **FR-004**: System MUST retry failed deliveries using exponential backoff (1m, 5m, 15m, 1h, 4h) with a maximum of 5 attempts before marking as `Failed`
- **FR-005**: System MUST detect duplicate events via `SourceEventId` and silently skip re-processing
- **FR-006**: System MUST support two email provider implementations (Mailtrap for development, Mailgun for production) registered via dependency injection based on environment
- **FR-007**: System MUST configure email providers via environment-specific settings (host, port, credentials, from address)
- **FR-008**: System MUST render email content using Razor templates with strongly-typed models
- **FR-009**: System MUST use a shared base layout with branded header (logo, color banner), card-based content area, and footer with platform links
- **FR-010**: All email content MUST be in Spanish
- **FR-011**: System MUST send a login security alert on every successful login containing IP address, browser name, OS name (parsed from User-Agent via UAParser), and timestamp
- **FR-012**: System MUST send suspicious login alerts with elevated urgency tone and distinct subject ("Actividad sospechosa detectada") for logins following 3 or more failed attempts or after lockout release
- **FR-013**: The caller (web layer) MUST provide IP address and User-Agent string via the event payload -- the Notification domain has zero HTTP awareness
- **FR-014**: System MUST send a welcome/verification email on user registration containing user's name, project name, verification URL, and expiration timeframe
- **FR-015**: System MUST send an invitation email on project invitation containing inviter context, project name, incubator name, action URL, and expiration
- **FR-016**: System MUST send a fresh invitation email with new token when an invitation is reissued
- **FR-017**: System MUST allow users to enable/disable login alert notifications via a global toggle (not scoped per incubator, since login is a platform-level event)
- **FR-018**: Registration and invitation emails MUST be system-mandatory and cannot be disabled by user preferences
- **FR-019**: Default notification preference for new users MUST be "all enabled"
- **FR-020**: Preference check MUST happen before queuing -- no notification record created if the notification type is suppressed for that user/context
- **FR-021**: System MUST degrade gracefully when User-Agent parsing fails, using "Navegador desconocido" / "Sistema operativo desconocido" in the template
- **FR-022**: System MUST default to sending (fail-open) when preference lookup fails, and log the error

### Key Entities

- **Notification**: Represents a queued email to be delivered. Tracks notification type, subject, rendered body, source event ID (for deduplication), scheduled delivery time, and lifecycle status (Pending, Sent, Failed). Aggregate root.
- **NotificationRecipient**: Tracks per-recipient delivery state. Contains user reference, delivery channel (Email), delivery status (Pending, Sent, Failed, Suppressed), sent timestamp, and failure reason.
- **DeliveryAttempt**: Records each attempt to deliver a notification. Contains attempt number, timestamp, success/failure, and failure reason. Append-only.
- **NotificationPreference**: Stores per-user, per-notification-type enable/disable settings. Login alerts use a global toggle; future notification types may add incubator-scoped preferences. Aggregate root.
- **LoginContext**: Value object capturing IP address, browser name, and OS name for login alert emails. Designed to accommodate future geolocation data.
- **NotificationType**: Enumeration of supported notification types (LoginAlert, UserRegistration, ProjectInvitation). Extensible for future types.
- **DeliveryChannel**: Enumeration of delivery channels (Email). Includes placeholders for future SMS and InApp channels.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Notification emails are delivered to the recipient's inbox within 2 minutes of the triggering event under normal SMTP conditions
- **SC-002**: Failed deliveries are automatically retried up to 5 times with exponential backoff; no manual intervention required for transient failures
- **SC-003**: Duplicate events produce zero duplicate emails -- idempotency is enforced at the queue level
- **SC-004**: Users can disable login alerts and immediately stop receiving them; system-mandatory emails (registration, invitation) are unaffected by preference changes
- **SC-005**: Adding a new notification type requires only a new template file, a model class, and an event handler -- no modifications to existing infrastructure code
- **SC-006**: Switching between development (Mailtrap) and production (Mailgun) email providers requires only configuration changes, no code changes
- **SC-007**: All email content is rendered in Spanish with professional branded layout consistent across all notification types
- **SC-008**: Login alert emails accurately display the user's IP address, browser name, and OS name; unknown User-Agents degrade to placeholder text without blocking delivery

## Assumptions

- Mailtrap.io account credentials will be provided for the development environment configuration
- Mailgun SMTP is already configured with sending domain `mail.mentoory.com` and credentials are available
- The `LoginAttemptEvent` will be enriched by the web layer to include IP address and User-Agent string before publishing
- The platform base URL for constructing verification/invitation links is available via configuration
- UAParser NuGet package is compatible with the project's .NET 10.0 target and does not conflict with existing dependencies
- The existing `UserRegisteredEvent` payload contains sufficient data (email, name, project, verification token, expiration) for both registration and invitation email templates
- Email template branding assets (logo image, brand colors) will be provided or are derivable from the existing Phoenix Admin Template theme
