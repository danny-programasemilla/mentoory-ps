# Quickstart: Notification Generic Delivery

## Overview

This feature refactors the Notification domain into a pure delivery service. After this change, adding a new notification type requires zero changes to the Notification domain.

## Key Concepts

1. **Each domain renders its own email content** using RazorLight templates as embedded resources
2. **A shared layout wrapper** (`IEmailLayoutWrapper`) provides consistent Mentoory branding
3. **`NotificationRequestedEvent`** is the single cross-domain contract for notification delivery
4. **The Notification domain** receives ready-made HTML and handles delivery, retries, and preferences

## Flow: How a Notification Gets Sent

```
[Access Domain]                    [Notification Domain]
     │                                     │
     │ UserRegisteredEvent                 │
     │ (domain event)                      │
     ▼                                     │
 Handler catches event                     │
     │                                     │
     ├─ Renders inner HTML                 │
     │  (RazorLight + Access template)     │
     │                                     │
     ├─ Wraps in brand layout              │
     │  (IEmailLayoutWrapper)              │
     │                                     │
     ├─ Publishes NotificationRequestedEvent
     │  (subject, htmlBody, recipient)     │
     │                    ─────────────────►│
     │                                     │ NotificationRequestedHandler
     │                                     │   ├─ Check preferences
     │                                     │   ├─ Create Notification entity
     │                                     │   └─ Queue for delivery
     │                                     │
     │                                     │ NotificationProcessorService
     │                                     │   └─ Poll + send via SMTP
```

## Adding a New Notification Type

1. Add enum value to `NotificationType` in `Mentoory.Notification.Domain`
2. Create `.cshtml` template in your domain's `Infrastructure/Templates/`
3. Create handler in your domain's `Application/IntegrationEvents/`:
   - Catch your domain event
   - Render template via `ITemplateRenderer`
   - Wrap via `IEmailLayoutWrapper`
   - Publish `NotificationRequestedEvent`
4. Done — Notification domain handles the rest automatically

## Project References

```
YourDomain.Application → Notification.Contracts (for NotificationRequestedEvent)
YourDomain.Application → Shared.Application (for ITemplateRenderer, IEmailLayoutWrapper)
YourDomain.Infrastructure → Shared.Infrastructure (for RazorLight setup)
```
