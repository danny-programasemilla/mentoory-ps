# Data Model: Notification Generic Delivery

**Date**: 2026-04-14

## Entity Changes

### Notification (Simplified Aggregate Root)

**Project**: `Mentoory.Notification.Domain/Aggregates/Notification/Notification.cs`

| Field | Type | Change |
|-------|------|--------|
| Id | long | Unchanged |
| ExternalId | Guid | Unchanged |
| NotificationType | NotificationType (enum) | Unchanged |
| Subject | string | Unchanged |
| HtmlBody | string | Unchanged |
| SourceEventId | Guid? | Unchanged |
| ScheduledForUtc | DateTime | Unchanged |
| CreatedAtUtc | DateTime | Unchanged |
| ~~LoginContext~~ | ~~LoginContext?~~ | **REMOVED** |
| Recipients | IReadOnlyCollection\<NotificationRecipient\> | Unchanged |

**Factory method change**:
- Before: `Create(notificationType, subject, htmlBody, sourceEventId, scheduledForUtc, createdAtUtc, loginContext?)`
- After: `Create(notificationType, subject, htmlBody, sourceEventId, scheduledForUtc, createdAtUtc)`

### LoginContext (Value Object) — DELETED

**Project**: `Mentoory.Notification.Domain/ValueObjects/LoginContext.cs`

Entire file deleted. No replacement needed — login context data is rendered into HtmlBody by the Access domain before delivery.

### NotificationRequestedEvent (New Integration Event)

**Project**: `Mentoory.Notification.Contracts/IntegrationEvents/NotificationRequestedEvent.cs`

| Field | Type | Description |
|-------|------|-------------|
| NotificationType | NotificationType | Type of notification (LoginAlert, UserRegistration, ProjectInvitation) |
| Subject | string | Email subject line |
| HtmlBody | string | Fully rendered email HTML (inner content wrapped in brand layout) |
| RecipientUserId | long | Internal user ID for preference lookup |
| RecipientEmail | string | Delivery email address |
| SourceEventId | Guid? | Deduplication key to prevent duplicate notifications |
| OccurredOnUtc | DateTime | Inherited from IntegrationEvent base |

## Database Schema Changes

### notification.Notifications — Column Removal

```sql
-- REMOVE these columns from Notifications.sql:
[LoginContext_IpAddress]       NVARCHAR(45)   NULL,
[LoginContext_BrowserName]     NVARCHAR(128)  NULL,
[LoginContext_OperatingSystem] NVARCHAR(128)  NULL,
[LoginContext_IsSuspicious]    BIT            NULL,
```

**Migration safety**: Existing rows retain their `HtmlBody` which already contains the rendered login context information. No data migration script needed — just column removal via SSDT schema update.

### Tables Unchanged

- `notification.NotificationRecipients` — generic, no changes
- `notification.DeliveryAttempts` — generic, no changes
- `notification.NotificationPreferences` — generic, no changes
- `notification.NotificationConfigurations` — new from 013, no changes

## EF Core Mapping Changes

### NotificationDbContext

**Remove**:
```csharp
builder.OwnsOne(e => e.LoginContext, lc =>
{
    lc.Property(x => x.IpAddress).HasColumnName("LoginContext_IpAddress");
    lc.Property(x => x.BrowserName).HasColumnName("LoginContext_BrowserName");
    lc.Property(x => x.OperatingSystem).HasColumnName("LoginContext_OperatingSystem");
    lc.Property(x => x.IsSuspicious).HasColumnName("LoginContext_IsSuspicious");
});
```

No replacement mapping needed — the columns are removed entirely.

## New Interfaces

### IEmailLayoutWrapper

**Project**: `Mentoory.Shared.Application/Notifications/IEmailLayoutWrapper.cs`

```
WrapInBrandLayout(string innerHtml) → string
```

Takes rendered inner content HTML and wraps it in the standard Mentoory email layout (header banner, card wrapper, footer).

### ITemplateRenderer (Relocated)

**From**: `Mentoory.Notification.Infrastructure/Services/ITemplateRenderer.cs`
**To**: `Mentoory.Shared.Application/Notifications/ITemplateRenderer.cs`

```
RenderAsync(string templateName, object model) → Task<string>
```

No signature change — just relocated for shared access.

## State Transitions

No state transition changes. The Notification entity's recipient delivery lifecycle (Pending → Sent / Failed → retry) remains identical.

## Validation Rules

No new validation rules. The `NotificationRequestedEvent` is an integration event (not a command) and does not require FluentValidation. The Notification aggregate's `Create()` factory method retains its existing guard clauses (non-empty subject, non-empty body).
