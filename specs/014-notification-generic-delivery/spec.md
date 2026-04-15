# Feature Specification: Notification Generic Delivery

**Feature Branch**: `014-notification-generic-delivery`  
**Created**: 2026-04-14  
**Status**: Draft  
**Input**: Refactor the Notification domain from a content-aware delivery system into a pure delivery service. Remove LoginContext coupling, move template rendering to originating domains, create NotificationRequestedEvent contract in Notification.Contracts, and add shared email layout engine in Mentoory.Shared.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Platform Sends Registration Welcome Email (Priority: P1)

A new user registers on the platform. The Access domain renders the welcome email using its own Razor template, wraps it in the shared brand layout, and publishes a `NotificationRequestedEvent`. The Notification domain receives the event, checks preferences (default: send), creates the notification entity, and delivers the email.

**Why this priority**: Registration emails are the first user touchpoint. This story validates the core architectural change — template rendering in the originating domain, generic delivery by Notification.

**Independent Test**: Register a new user and verify the welcome email arrives with correct content and Mentoory branding.

**Acceptance Scenarios**:

1. **Given** a new user registers, **When** the Access domain handles `UserRegisteredEvent`, **Then** it renders the welcome email HTML using its own template + shared layout and publishes `NotificationRequestedEvent`
2. **Given** the Notification domain receives a `NotificationRequestedEvent`, **When** no preference record exists for the user, **Then** it creates and delivers the notification (default: send)
3. **Given** the Notification domain receives a `NotificationRequestedEvent`, **When** the user has explicitly disabled that notification type, **Then** it skips delivery

---

### User Story 2 - Platform Sends Project Invitation Email (Priority: P1)

A coordinator reissues an invitation. The Tenant domain renders the invitation email using its own template, wraps it in the shared layout, and publishes `NotificationRequestedEvent`. The Notification domain delivers it.

**Why this priority**: Invitation emails are critical for onboarding participants. This validates that multiple domains can independently render and send notifications.

**Independent Test**: Reissue a project invitation and verify the email arrives with correct project/incubator details and Mentoory branding.

**Acceptance Scenarios**:

1. **Given** an invitation is reissued, **When** the Tenant domain handles `InvitationReissuedEvent`, **Then** it renders the invitation HTML with project name, incubator name, and action URL, and publishes `NotificationRequestedEvent`
2. **Given** the rendered email, **When** delivered, **Then** the email uses the same shared brand layout as all other notification types

---

### User Story 3 - Platform Sends Login Security Alert (Priority: P1)

A user logs in. The Access domain parses login context (IP, browser, OS), determines if the login is suspicious, renders the appropriate alert template (normal or suspicious), wraps it in the shared layout, and publishes `NotificationRequestedEvent`. The Notification domain checks preferences and delivers.

**Why this priority**: Login alerts are the most complex notification type — they require metadata parsing and conditional template selection. This validates that type-specific logic stays in the originating domain.

**Independent Test**: Trigger a login attempt and verify the correct alert email (normal or suspicious) arrives with IP/browser/OS details in the rendered body.

**Acceptance Scenarios**:

1. **Given** a login attempt with fewer than 3 failed attempts, **When** the Access domain handles `LoginAttemptEvent`, **Then** it renders a standard login alert template and publishes `NotificationRequestedEvent`
2. **Given** a login attempt with 3+ failed attempts or post-lockout, **When** the Access domain handles `LoginAttemptEvent`, **Then** it renders a suspicious login alert template and publishes `NotificationRequestedEvent`
3. **Given** login alert notifications are disabled for the user, **When** the Notification domain receives the event, **Then** it skips delivery
4. **Given** the login alert is delivered, **When** the user views the email, **Then** the IP address, browser name, and operating system are visible in the email body but NOT stored as structured metadata in the database

---

### User Story 4 - Developer Adds a New Notification Type (Priority: P2)

A developer needs to add a new notification type from any domain. They create a Razor template in the originating domain, add a handler that renders content and publishes `NotificationRequestedEvent`, and add the new type to the `NotificationType` enum. No changes are required in the Notification domain's code, schema, or services.

**Why this priority**: This is the key architectural goal — proving that the Notification domain is truly generic and extensible without modification.

**Independent Test**: Add a hypothetical notification type and verify it works without touching any Notification domain code.

**Acceptance Scenarios**:

1. **Given** a developer adds a new `NotificationType` enum value, **When** they create a handler in their domain that publishes `NotificationRequestedEvent`, **Then** the notification is delivered without any changes to the Notification domain's aggregate, queue service, or database schema
2. **Given** the new notification type, **When** no user preference exists for it, **Then** the notification is sent by default (opt-out model)

---

### Edge Cases

- What happens when the shared layout template is missing or corrupted? Template rendering fails in the originating domain, `NotificationRequestedEvent` is never published, and the error is logged.
- What happens to existing notifications in the database when `LoginContext_*` columns are dropped? The rendered `HtmlBody` already contains the login context information in human-readable form — no user-visible content is lost.
- What happens to in-flight notifications (queued but not yet sent) during migration? They already have rendered `HtmlBody` — delivery continues unaffected since it only reads Subject and HtmlBody.
- What happens if a preference check fails due to database error? The notification is sent (fail-open for delivery), and a warning is logged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a `NotificationRequestedEvent` integration event in a new `Mentoory.Notification.Contracts` project, following the same pattern as `Mentoory.Access.Contracts` and `Mentoory.Tenant.Contracts`
- **FR-002**: `NotificationRequestedEvent` MUST carry: `NotificationType`, `Subject` (string), `HtmlBody` (string), `RecipientUserId` (long), `RecipientEmail` (string), `SourceEventId` (Guid?)
- **FR-003**: System MUST provide a shared email layout template in `Mentoory.Shared` that wraps inner content with consistent Mentoory branding (header banner, card wrapper, footer with Spanish text)
- **FR-004**: Access domain MUST render LoginAlert, SuspiciousLoginAlert, and UserRegistration email templates in its own Application layer
- **FR-005**: Tenant domain MUST render ProjectInvitation email template in its own Application layer
- **FR-006**: Each originating domain handler MUST catch its respective domain event, render the email using its own template + shared layout, and publish `NotificationRequestedEvent`
- **FR-007**: Notification domain MUST replace the three type-specific handlers (`LoginAttemptNotificationHandler`, `UserRegisteredNotificationHandler`, `InvitationReissuedNotificationHandler`) with a single generic `NotificationRequestedHandler`
- **FR-008**: `NotificationRequestedHandler` MUST check user notification preferences before creating the notification entity — if no preference exists, default to sending (opt-out model)
- **FR-009**: `Notification` aggregate MUST be simplified to remove the `LoginContext` value object — accepting only `(NotificationType, Subject, HtmlBody, SourceEventId, ScheduledForUtc, CreatedAtUtc)`
- **FR-010**: `NotificationQueueService` MUST replace type-specific methods (`QueueLoginAlertAsync`, `QueueRegistrationEmailAsync`, `QueueInvitationEmailAsync`) with a single generic `QueueAsync` method
- **FR-011**: The `LoginContext_*` columns (`LoginContext_IpAddress`, `LoginContext_BrowserName`, `LoginContext_OperatingSystem`, `LoginContext_IsSuspicious`) MUST be removed from the `notification.Notifications` database table
- **FR-012**: The `LoginContext` value object and its EF Core `OwnsOne` mapping MUST be deleted from the codebase

### Key Entities

- **Notification (simplified)**: Aggregate root representing a queued notification. Contains NotificationType, Subject, HtmlBody, SourceEventId, ScheduledForUtc, CreatedAtUtc, and a collection of Recipients. No longer contains type-specific metadata.
- **NotificationRequestedEvent**: Integration event contract in Notification.Contracts. Published by originating domains, consumed by the Notification domain. Carries the rendered email content ready for delivery.
- **Shared Email Layout**: A base HTML template providing consistent Mentoory branding. Each domain uses it to wrap their inner content before publishing the notification event.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Adding a new notification type from any domain requires zero code changes in the Notification domain (no aggregate, handler, service, or schema modifications)
- **SC-002**: All three existing notification types (LoginAlert, UserRegistration, ProjectInvitation) continue to deliver correctly with identical email content and branding
- **SC-003**: The `notification.Notifications` database table contains no `LoginContext_*` columns
- **SC-004**: The `NotificationQueueService` exposes a single generic queuing method instead of type-specific methods
- **SC-005**: The solution builds with zero warnings (`TreatWarningsAsErrors` enabled)
- **SC-006**: Email delivery, retry logic (exponential backoff), and preference checking continue to function identically to the current system

## Assumptions

- The current feature branch `013-notification-db-config` (NotificationConfiguration refactoring) will be merged before or its changes will be accounted for in this refactoring
- Existing notifications in the database already have fully rendered `HtmlBody` — dropping `LoginContext_*` columns does not lose user-visible content
- The Razor template engine (already used by `NotificationQueueService`) is available for use in other domain Application/Infrastructure layers
- The `NotificationType` enum continues to live in `Mentoory.Notification.Domain` — originating domains reference it via the `Notification.Contracts` project
