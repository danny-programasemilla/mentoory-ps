# Data Model: Invitation Domain Decoupling

**Date**: 2026-04-12
**Feature**: [spec.md](spec.md)

## Entity Changes

### ProjectInvitation (Tenant Domain) — MODIFIED

Remove `TokenHash` property. The aggregate no longer manages authentication tokens.

**Before:**

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| Id | long | Yes | Internal PK |
| ExternalId | Guid | Yes | Public identifier |
| ProjectId | long | Yes | FK to Project |
| UserId | long | Yes | Cross-domain reference (Access user ID) |
| **TokenHash** | **string** | **Yes** | **Base64-encoded 32-byte hash — REMOVE** |
| Status | InvitationStatus | Yes | Pending / Accepted / Expired |
| ExpiresAtUtc | DateTime | Yes | Invitation expiry |
| AcceptedAtUtc | DateTime? | No | When accepted |
| CreatedAtUtc | DateTime | Yes | Creation timestamp |
| CreatedByUserId | long | Yes | Admin who created |
| IsActive | bool | Yes | Soft-active flag |
| RequiresAcceptance | bool | Yes | Manual acceptance needed |

**After:** Same table minus `TokenHash`.

**Factory method change:**

```
Before: Create(projectId, userId, tokenHash, expiresAtUtc, createdByUserId, utcNow, requiresAcceptance)
After:  Create(projectId, userId, expiresAtUtc, createdByUserId, utcNow, requiresAcceptance)
```

### EmailVerificationToken (Access Domain) — UNCHANGED

No changes. This entity continues to serve as the sole onboarding authentication mechanism.

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| Id | long | Yes | Internal PK |
| TokenHash | string | Yes | PBKDF2 hash |
| ExpiresAtUtc | DateTime | Yes | Token expiry |
| IsUsed | bool | Yes | Single-use flag |
| CreatedAtUtc | DateTime | Yes | Creation timestamp |

### InvitationReissuedEvent (Tenant Application) — NEW

Integration event published when an invitation is reissued. Consumed by Access domain to generate a fresh `EmailVerificationToken`.

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| UserId | long | Yes | Internal user ID (for Access to look up User) |
| UserExternalId | Guid | Yes | Public user identifier |
| InvitationExpiryHours | int | Yes | Expiry duration for the new verification token |
| OccurredOnUtc | DateTime | Yes | Event timestamp (inherited from IntegrationEvent base) |

## Database Schema Change

### ProjectInvitations table (SSDT)

**File**: `Mentoory.Db/tenant/Tables/ProjectInvitations.sql`

**Change**: Remove `[TokenHash] NVARCHAR(128) NOT NULL` column definition.

No indexes reference `TokenHash`, so no index changes needed. The DACPAC publish will generate a `DROP COLUMN` statement automatically.

### EF Configuration

**File**: `Mentoory.Tenant.Infrastructure/Persistence/TenantDbContext.cs`

**Change**: Remove `entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);` from `ConfigureProjectInvitation()`.

## State Transitions

No changes to existing state transitions. `ProjectInvitation` lifecycle remains:

```
Pending → Accepted  (via Accept() method, triggered by UserEmailVerifiedEventHandler or AcceptInvitationHandler)
Pending → Expired   (via CheckExpiration() method)
Active  → Inactive  (via Deactivate() method, used by ReissueInvitationHandler)
```

The only change is that the transition from Pending to Accepted is now triggered exclusively by:
1. `UserEmailVerifiedEvent` (auto-accept when `RequiresAcceptance = false`)
2. `AcceptInvitationHandler` (manual accept when `RequiresAcceptance = true`)

Previously, `SetInitialPasswordCommandHandler` with `TokenType.Invitation` could also trigger state changes. That path is removed.
