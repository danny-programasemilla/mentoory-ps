# Integration Event Contracts: Notification Domain

**Date**: 2026-04-13

## Events Consumed by Notification Domain

### LoginAttemptEvent (enriched)

**Source**: `Mentoory.Access.Application.IntegrationEvents`
**Published by**: LoginController (web layer)
**Consumed by**: `LoginAttemptNotificationHandler`

```csharp
public sealed record LoginAttemptEvent(
    long? UserId,
    string Email,
    bool Success,
    string IpAddress,
    string? UserAgentString,       // NEW: raw User-Agent header from HttpContext
    int FailedAttemptCount,        // NEW: consecutive failed attempts before this login
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
```

**Notification behavior**:
- Only processes `Success == true` events
- Creates `NotificationType.LoginAlert` notification
- Flags as suspicious when `FailedAttemptCount >= 3`
- Parses `UserAgentString` via UAParser into `LoginContext` value object

---

### UserRegisteredEvent (existing, unchanged)

**Source**: `Mentoory.Access.Application.IntegrationEvents`
**Published by**: `CreateUserCommandHandler`, `BatchRegisterUsersHandler`
**Consumed by**: `UserRegisteredNotificationHandler`

```csharp
public sealed record UserRegisteredEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    Guid? ProjectExternalId,
    bool RequiresVerification,
    string EnrollmentVariant,
    int InvitationExpiryHours,
    DateTime CreatedAtUtc,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
```

**Notification behavior**:
- Always creates `NotificationType.UserRegistration` notification (welcome/verification email)
- If `EnrollmentVariant == "Invitation"` AND `ProjectExternalId != null`: also creates `NotificationType.ProjectInvitation` notification
- Each notification gets its own `SourceEventId` scoped by type (same `EventId`, different `NotificationType`)

---

### InvitationReissuedEvent (NEW)

**Source**: `Mentoory.Tenant.Application.IntegrationEvents`
**Published by**: `ReissueInvitationHandler`
**Consumed by**: `InvitationReissuedNotificationHandler`

```csharp
public sealed record InvitationReissuedEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    string FirstName,
    string LastName,
    Guid ProjectExternalId,
    string ProjectName,
    string IncubatorName,
    int InvitationExpiryHours,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
```

**Notification behavior**:
- Creates `NotificationType.ProjectInvitation` notification with fresh token/link
- Deduplication via `SourceEventId` prevents replay issues

---

## Events NOT Consumed (Documented for Future Reference)

### UserEmailVerifiedEvent
**Potential use**: Confirmation email ("Tu cuenta ha sido verificada")
**Status**: Out of scope for this spec

### UserLockedOutEvent
**Potential use**: Account lockout security alert
**Status**: Out of scope for this spec

---

## Deduplication Contract

All consumed events inherit from `IntegrationEvent` which provides:

```csharp
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }    // Auto-generated, unique per event instance
    public DateTime OccurredOn { get; init; }
}
```

**Deduplication key**: `(EventId, NotificationType)` -- stored as `SourceEventId` on the `Notification` entity.

**Guarantee**: If a notification with the same `SourceEventId` + `NotificationType` already exists, the handler silently skips creation.
