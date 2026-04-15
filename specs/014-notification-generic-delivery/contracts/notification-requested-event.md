# Contract: NotificationRequestedEvent

**Type**: Integration Event (MediatR INotification)  
**Project**: `Mentoory.Notification.Contracts`  
**Namespace**: `Mentoory.Notification.Contracts.IntegrationEvents`

## Purpose

Generic cross-domain integration event that requests the Notification domain to deliver a notification. Published by any domain that needs to send a notification. Consumed exclusively by the Notification domain.

## Event Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| NotificationType | NotificationType (enum) | Yes | Identifies the notification category for preference checking |
| Subject | string | Yes | Email subject line (Spanish) |
| HtmlBody | string | Yes | Fully rendered email HTML including brand layout |
| RecipientUserId | long | Yes | Internal user ID for preference lookup |
| RecipientEmail | string | Yes | Target email address |
| SourceEventId | Guid? | No | Deduplication key — prevents duplicate notifications for the same source event + type |
| EventId | Guid | Yes | Inherited from IntegrationEvent base — unique event instance ID |
| OccurredOn | DateTime | Yes | Inherited from IntegrationEvent base — when the event was created |

## Publishers

| Domain | Handler | Trigger Event |
|--------|---------|---------------|
| Access | `UserRegisteredNotificationHandler` | `UserRegisteredEvent` |
| Access | `LoginAttemptNotificationHandler` | `LoginAttemptEvent` |
| Tenant | `InvitationReissuedNotificationHandler` | `InvitationReissuedEvent` |

## Consumer

| Domain | Handler | Action |
|--------|---------|--------|
| Notification | `NotificationRequestedHandler` | Check preferences → Create Notification entity → Queue for delivery |

## Deduplication

The `SourceEventId` + `NotificationType` combination is unique-indexed in the `notification.Notifications` table. If a `NotificationRequestedEvent` arrives with a `SourceEventId` that already has a notification of the same type, it is silently ignored.

## Preference Check

Before creating the Notification entity, the handler checks `notification.NotificationPreferences` for the recipient + notification type. If no preference record exists, the default is **send** (opt-out model).
