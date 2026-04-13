# Internal Contracts: Invitation Domain Decoupling

**Date**: 2026-04-12
**Feature**: [spec.md](../spec.md)

## Integration Event Contracts

### InvitationReissuedEvent (NEW)

**Publisher**: `ReissueInvitationHandler` (Tenant.Application)
**Consumer**: `InvitationReissuedEventHandler` (Access.Application)
**Location**: `Mentoory.Tenant.Application/IntegrationEvents/InvitationReissuedEvent.cs`

```
InvitationReissuedEvent
├── UserId: long                  — Internal user ID for Access lookup
├── UserExternalId: Guid          — Public user identifier
├── InvitationExpiryHours: int    — Token expiry aligned to invitation expiry
└── OccurredOnUtc: DateTime       — Event timestamp (base class)
```

**Contract guarantees**:
- Published after the old invitation is deactivated and the new invitation is persisted
- `UserId` corresponds to an existing User in the Access domain
- `InvitationExpiryHours` reflects the system configuration at time of reissue

**Consumer behavior**:
- Looks up User by `UserId`
- Generates a new `EmailVerificationToken` with `InvitationExpiryHours` as expiry
- Invalidates any previous unused tokens (handled by `User.GenerateEmailVerificationToken()`)
- Saves the user

### Existing Events (UNCHANGED)

#### UserEmailVerifiedEvent

**Publisher**: `SetInitialPasswordCommandHandler`, `VerifyEmailHandler`, `AdminVerifyEmailHandler` (Access.Application)
**Consumer**: `UserEmailVerifiedEventHandler` (Tenant.Application)

No changes. Continues to trigger auto-acceptance of non-`RequiresAcceptance` pending invitations.

#### UserRegisteredEvent

**Publisher**: `RegisterUserHandler` (Access.Application)
**Consumer**: `UserRegisteredEventHandler` (Tenant.Application)

No changes. Continues to create `ProjectInvitation` and trigger enrollment logic.

## Command Contract Changes

### SetInitialPasswordCommand (MODIFIED)

**Before**:
```
SetInitialPasswordCommand
├── UserExternalId: Guid
├── Token: string
├── TokenType: TokenType (Verification | Invitation)
├── NewPassword: string
└── ConfirmPassword: string
```

**After**:
```
SetInitialPasswordCommand
├── UserExternalId: Guid
├── Token: string
├── NewPassword: string
└── ConfirmPassword: string
```

`TokenType` removed. Handler always validates the token as an `EmailVerificationToken`.

## Removed Contracts

### IInvitationTokenValidator (DELETED)

```
IInvitationTokenValidator
└── IsActiveForUserAsync(invitationExternalId: Guid, userId: long, ct: CancellationToken) → bool
```

This interface and its implementation are deleted entirely. No replacement needed — the functionality is handled by the event-driven `UserEmailVerifiedEvent` flow.
