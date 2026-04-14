# Data Model: Notification Domain - Email Delivery System

**Date**: 2026-04-13
**Branch**: `011-notification-email-delivery`

## Entity Relationship Overview

```
NotificationPreference (aggregate root)
  - UserId (long)
  - NotificationType (enum)
  - IsEnabled (bool)

Notification (aggregate root)
  ├── NotificationRecipient (child entity, 1:1 for email)
  │     └── DeliveryAttempt (value object, 1:N append-only)
  └── LoginContext (value object, nullable, login alerts only)
```

---

## Entities

### Notification (Aggregate Root)

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| Id | long | No | Internal PK (identity) |
| ExternalId | Guid | No | Public-facing identifier (routes use this) |
| NotificationType | byte (enum) | No | LoginAlert=0, UserRegistration=1, ProjectInvitation=2 |
| Subject | nvarchar(256) | No | Email subject line |
| HtmlBody | nvarchar(max) | No | Rendered HTML content |
| SourceEventId | uniqueidentifier | Yes | IntegrationEvent.EventId for deduplication |
| ScheduledForUtc | datetime2 | No | When to send (typically = creation time) |
| CreatedAtUtc | datetime2 | No | Record creation timestamp |

**Factory method**: `Notification.Create(type, subject, htmlBody, sourceEventId, scheduledForUtc, createdAtUtc)` with guard clauses.

**Invariants**:
- Subject must not be empty
- HtmlBody must not be empty
- ScheduledForUtc must not be in the past (at creation time)

### NotificationRecipient (Child Entity)

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| Id | long | No | Internal PK (identity) |
| NotificationId | long | No | FK to Notification |
| UserId | long | No | FK to target user (cross-aggregate by ID) |
| Email | nvarchar(256) | No | Recipient email address |
| DeliveryChannel | byte (enum) | No | Email=0 (future: Sms=1, InApp=2) |
| DeliveryStatus | byte (enum) | No | Pending=0, Sent=1, Failed=2, Suppressed=3 |
| SentAtUtc | datetime2 | Yes | When successfully sent |
| FailureReason | nvarchar(1024) | Yes | Last failure description |

**State transitions**:
- `Pending` -> `Sent` (on successful delivery)
- `Pending` -> `Failed` (after max retries exhausted)
- Created as `Suppressed` if preference check suppresses

### DeliveryAttempt (Value Object, append-only)

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| Id | long | No | Internal PK (identity) |
| NotificationRecipientId | long | No | FK to NotificationRecipient |
| AttemptNumber | int | No | 1-based attempt count |
| AttemptedAtUtc | datetime2 | No | When this attempt was made |
| Success | bit | No | Whether delivery succeeded |
| FailureReason | nvarchar(1024) | Yes | Error message if failed |

**Invariants**:
- AttemptNumber must be sequential (1, 2, 3, ...)
- Max 5 attempts (enforced by domain logic, not database)

### NotificationPreference (Aggregate Root)

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| Id | long | No | Internal PK (identity) |
| ExternalId | Guid | No | Public-facing identifier |
| UserId | long | No | FK to user (cross-aggregate by ID) |
| NotificationType | byte (enum) | No | Which notification type this preference controls |
| IsEnabled | bit | No | Whether this notification type is enabled |
| UpdatedAtUtc | datetime2 | No | Last modification timestamp |

**Constraints**:
- Unique index on `(UserId, NotificationType)` -- one preference per user per type
- Only `NotificationType.LoginAlert` can be disabled; registration and invitation are system-mandatory (enforced in domain, not database)

**Factory method**: `NotificationPreference.Create(userId, notificationType, isEnabled, utcNow)`

### LoginContext (Value Object)

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| IpAddress | nvarchar(45) | No | Client IP (IPv4 or IPv6) |
| BrowserName | nvarchar(128) | No | Parsed from User-Agent (e.g., "Chrome") |
| OperatingSystem | nvarchar(128) | No | Parsed from User-Agent (e.g., "Windows") |
| IsSuspicious | bit | No | Whether this login was flagged as suspicious |

**Stored as**: Owned entity on `Notification` (EF Core `OwnsOne`). Only populated for `NotificationType.LoginAlert`.

---

## Enums

### NotificationType
```
LoginAlert = 0
UserRegistration = 1
ProjectInvitation = 2
```

### DeliveryChannel
```
Email = 0
Sms = 1        // Future
InApp = 2      // Future
```

### DeliveryStatus
```
Pending = 0
Sent = 1
Failed = 2
Suppressed = 3
```

---

## Database Tables (SSDT)

### [notification].[Notifications]

```sql
CREATE TABLE [notification].[Notifications]
(
    [Id]               BIGINT            IDENTITY(1,1) NOT NULL,
    [ExternalId]       UNIQUEIDENTIFIER  NOT NULL,
    [NotificationType] TINYINT           NOT NULL,
    [Subject]          NVARCHAR(256)     NOT NULL,
    [HtmlBody]         NVARCHAR(MAX)     NOT NULL,
    [SourceEventId]    UNIQUEIDENTIFIER  NULL,
    [ScheduledForUtc]  DATETIME2(7)      NOT NULL,
    [CreatedAtUtc]     DATETIME2(7)      NOT NULL,
    -- LoginContext (owned value object, nullable for non-login notifications)
    [LoginContext_IpAddress]       NVARCHAR(45)   NULL,
    [LoginContext_BrowserName]     NVARCHAR(128)  NULL,
    [LoginContext_OperatingSystem] NVARCHAR(128)  NULL,
    [LoginContext_IsSuspicious]    BIT            NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Notifications_ExternalId] UNIQUE ([ExternalId])
);

CREATE UNIQUE NONCLUSTERED INDEX [IX_Notifications_SourceEventId_Type]
    ON [notification].[Notifications] ([SourceEventId], [NotificationType])
    WHERE [SourceEventId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Notifications_ScheduledForUtc]
    ON [notification].[Notifications] ([ScheduledForUtc])
    INCLUDE ([NotificationType]);
```

### [notification].[NotificationRecipients]

```sql
CREATE TABLE [notification].[NotificationRecipients]
(
    [Id]              BIGINT           IDENTITY(1,1) NOT NULL,
    [NotificationId]  BIGINT           NOT NULL,
    [UserId]          BIGINT           NOT NULL,
    [Email]           NVARCHAR(256)    NOT NULL,
    [DeliveryChannel] TINYINT          NOT NULL,
    [DeliveryStatus]  TINYINT          NOT NULL,
    [SentAtUtc]       DATETIME2(7)     NULL,
    [FailureReason]   NVARCHAR(1024)   NULL,
    CONSTRAINT [PK_NotificationRecipients] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_NotificationRecipients_Notifications]
        FOREIGN KEY ([NotificationId]) REFERENCES [notification].[Notifications]([Id])
);

CREATE NONCLUSTERED INDEX [IX_NotificationRecipients_Status]
    ON [notification].[NotificationRecipients] ([DeliveryStatus])
    INCLUDE ([NotificationId], [SentAtUtc])
    WHERE [DeliveryStatus] = 0; -- Pending only
```

### [notification].[DeliveryAttempts]

```sql
CREATE TABLE [notification].[DeliveryAttempts]
(
    [Id]                       BIGINT          IDENTITY(1,1) NOT NULL,
    [NotificationRecipientId]  BIGINT          NOT NULL,
    [AttemptNumber]            INT             NOT NULL,
    [AttemptedAtUtc]           DATETIME2(7)    NOT NULL,
    [Success]                  BIT             NOT NULL,
    [FailureReason]            NVARCHAR(1024)  NULL,
    CONSTRAINT [PK_DeliveryAttempts] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_DeliveryAttempts_NotificationRecipients]
        FOREIGN KEY ([NotificationRecipientId]) REFERENCES [notification].[NotificationRecipients]([Id])
);
```

### [notification].[NotificationPreferences]

```sql
CREATE TABLE [notification].[NotificationPreferences]
(
    [Id]               BIGINT           IDENTITY(1,1) NOT NULL,
    [ExternalId]       UNIQUEIDENTIFIER NOT NULL,
    [UserId]           BIGINT           NOT NULL,
    [NotificationType] TINYINT          NOT NULL,
    [IsEnabled]        BIT              NOT NULL,
    [UpdatedAtUtc]     DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_NotificationPreferences_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_NotificationPreferences_User_Type] UNIQUE ([UserId], [NotificationType])
);
```

---

## Indexes Summary

| Table | Index | Type | Purpose |
|-------|-------|------|---------|
| Notifications | IX_Notifications_SourceEventId_Type | Unique filtered | Deduplication: prevent duplicate sends from event replay |
| Notifications | IX_Notifications_ScheduledForUtc | Non-clustered | Background service polling: find pending notifications by schedule |
| NotificationRecipients | IX_NotificationRecipients_Status | Filtered (Pending) | Background service: find recipients needing delivery |
| NotificationPreferences | UQ_NotificationPreferences_User_Type | Unique | One preference per user per notification type |
