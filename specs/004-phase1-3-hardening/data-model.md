# Data Model: Phase 1–3 Hardening

**Date**: 2026-04-03  
**Feature**: [spec.md](spec.md)

## Entity Summary

| Entity | Domain | Status | Description |
|--------|--------|--------|-------------|
| User | Access | MODIFIED | Add token generation at registration; enforce PasswordResetRequired |
| EmailVerificationToken | Access | EXISTING | Child of User — no structural changes |
| PasswordResetToken | Access | EXISTING | Child of User — no structural changes |
| Credential | Access | EXISTING | No structural changes |
| AuthSession | Access | EXISTING | No structural changes, but validation logic changes |
| RoleAssignment | Access | EXISTING | No changes |
| UserProfile | Access | MODIFIED | Keep in sync with User status changes |
| SystemConfiguration | Access | NEW | Database-driven operational configuration |
| Country | Shared | NEW | Supported countries with ID mask/validation |
| Project | Tenant | MODIFIED | Add IsPublic flag, EnrollmentVariant |
| ProjectInvitation | Tenant | NEW | Invitation lifecycle for project enrollment |
| ProjectParticipant | Tenant | EXISTING | No structural changes |

---

## New Entities

### SystemConfiguration (Access Domain)

Database-stored key-value pairs for operational parameters.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | long | PK, identity | Internal identifier |
| ExternalId | Guid | Unique, not null | Public-facing identifier |
| Key | string | Unique, not null, max 100 | Configuration key (maps to ConfigurationKey enum) |
| Value | string | Not null, max 500 | Configuration value (string, parsed by consumers) |
| Description | string | Nullable, max 500 | Human-readable description of the setting |
| DataType | string | Not null, max 50 | Expected data type (Integer, Decimal, Boolean, String) |
| CreatedAtUtc | DateTime | Not null | When the entry was created |
| UpdatedAtUtc | DateTime | Not null | When the entry was last modified |

**ConfigurationKey enum values**:

| Key | Default | DataType | Used by |
|-----|---------|----------|---------|
| EmailVerificationTokenExpiryHours | 24 | Integer | User.GenerateEmailVerificationToken |
| PasswordResetTokenExpiryHours | 1 | Integer | User.GeneratePasswordResetToken |
| InvitationTokenExpiryHours | 72 | Integer | ProjectInvitation.Create |
| MaxFailedLoginAttempts | 5 | Integer | LoginUserHandler |
| LockoutDurationMinutes | 15 | Integer | LoginUserHandler |
| SessionTimeoutHours | 8 | Integer | LoginUserHandler |
| PasswordHistoryDepth | 5 | Integer | ChangePasswordHandler |

**Validation rules**:
- Key must be unique
- Value must be parseable to the declared DataType
- Numeric values must be > 0

**Database**: `[access].[SystemConfigurations]`

---

### Country (Shared Domain)

Reference data for supported countries with identification format rules.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | long | PK, identity | Internal identifier |
| ExternalId | Guid | Unique, not null | Public-facing identifier |
| Name | string | Not null, max 100 | Display name (Spanish) |
| Code | string | Unique, not null, max 3 | ISO 3166-1 alpha-3 code |
| IdentificationLabel | string | Not null, max 100 | Label for the ID field in UI (e.g., "Cedula Nacional") |
| IdentificationMask | string | Nullable, max 50 | Input mask pattern for client-side formatting (e.g., "0-0000-0000") |
| IdentificationRegex | string | Nullable, max 200 | Server-side validation regex |
| IdentificationMaxLength | int | Not null | Maximum character length for identification |
| IsActive | bool | Not null, default true | Whether the country is available for registration |
| CreatedAtUtc | DateTime | Not null | When the entry was created |

**Validation rules**:
- Code must be unique and match ISO 3166-1 alpha-3 format
- Name must be non-empty
- IdentificationMaxLength must be > 0

**Initial seed data**:

| Name | Code | Label | Mask | Regex | MaxLength |
|------|------|-------|------|-------|-----------|
| Costa Rica | CRI | Cedula Nacional | 0-0000-0000 | ^\d{1}-\d{4}-\d{4}$ | 11 |

**Database**: `[shared].[Countries]`

---

### ProjectInvitation (Tenant Domain)

Invitation for a user to participate in a project with explicit lifecycle.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | long | PK, identity | Internal identifier |
| ExternalId | Guid | Unique, not null | Public-facing identifier |
| ProjectId | long | FK → Projects, not null | Target project |
| UserId | long | Not null | User being invited (cross-aggregate reference by ID) |
| TokenHash | string | Not null, max 128 | Hashed invitation token |
| Status | InvitationStatus | Not null, default Pending | Current lifecycle state |
| ExpiresAtUtc | DateTime | Not null | When the invitation expires |
| AcceptedAtUtc | DateTime | Nullable | When the invitation was accepted |
| CreatedAtUtc | DateTime | Not null | When the invitation was created |
| CreatedByUserId | long | Not null | Admin who created the invitation |
| IsActive | bool | Not null, default true | Soft-active flag (false when superseded by reissue) |

**InvitationStatus enum**:

| Value | Numeric | Description |
|-------|---------|-------------|
| Pending | 0 | Awaiting user acceptance |
| Accepted | 1 | User has accepted the invitation |
| Expired | 2 | Token validity period has passed |

**State transitions**:

```
Pending → Accepted  (user accepts, or admin accepts on behalf)
Pending → Expired   (checked lazily when accessed after ExpiresAtUtc)
Expired → [reissue creates NEW invitation; old one set IsActive=false]
```

**Validation rules**:
- Only one active invitation per user per project at a time (unique filtered index on UserId + ProjectId WHERE IsActive = 1 AND Status = 0)
- Cannot transition from Accepted to any other state
- Cannot accept an expired invitation
- Acceptance requires user's email to be verified (in full-flow variant)

**Domain methods**:
- `Create(projectId, userId, tokenHash, expiresAtUtc, createdByUserId, utcNow)` — factory method
- `Accept(utcNow)` — validates not expired, transitions to Accepted
- `CheckExpiration(utcNow)` — if Pending and past ExpiresAtUtc, transitions to Expired
- `Deactivate()` — sets IsActive = false (called when reissuing)

**Database**: `[tenant].[ProjectInvitations]`

---

## Modified Entities

### User (Access Domain)

**Changes**:
1. `Register` factory method must now also call `GenerateEmailVerificationToken` to create a token at registration time (currently skipped)
2. New method: `AdminVerifyEmail(utcNow)` — bypasses token mechanism, directly sets AccountStatus to Active and EmailVerifiedAtUtc
3. `GenerateEmailVerificationToken` and `GeneratePasswordResetToken` must accept expiry duration as a parameter (instead of hardcoded values) — the calling handler reads the value from SystemConfiguration
4. New method: `SetPasswordResetRequired()` — sets AccountStatus to PasswordResetRequired (used by batch registration)
5. `ChangePassword` already clears PasswordResetRequired status — no change needed

**Modified method signatures**:
- `GenerateEmailVerificationToken(utcNow, expiryHours)` — was: `GenerateEmailVerificationToken(utcNow)` with hardcoded 24h
- `GeneratePasswordResetToken(utcNow, expiryHours)` — was: `GeneratePasswordResetToken(utcNow)` with hardcoded 1h

---

### UserProfile (Access Domain — Read Model)

**Changes**:
1. `AccountStatus` field must be updated whenever the User aggregate's AccountStatus changes
2. Affected handlers: `VerifyEmailHandler`, `AdminVerifyEmailHandler`, `LoginUserHandler` (lockout), `ChangePasswordHandler` (clears PasswordResetRequired)

No schema changes — the `NVARCHAR(50)` column is sufficient.

---

### Project (Tenant Domain)

**New fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| IsPublic | bool | Not null, default false | Whether the project appears in the public listing |
| EnrollmentVariant | EnrollmentVariant | Not null, default FullFlow | Controls enrollment behavior |

**EnrollmentVariant enum**:

| Value | Numeric | Description |
|-------|---------|-------------|
| FullFlow | 0 | Email verification required, then invitation acceptance |
| Bypass | 1 | User immediately enrolled as active participant |

**Database changes**: Add `IsPublic BIT NOT NULL DEFAULT 0` and `EnrollmentVariant TINYINT NOT NULL DEFAULT 0` to `[tenant].[Projects]`.

---

## State Machines

### User AccountStatus Lifecycle

```
                         ┌──────────────────────────┐
                         │                          │
    Register ──────► PendingVerification            │
                         │                          │
                    VerifyEmail /                    │
                    AdminVerify                     │
                         │                          │
                         ▼                          │
                       Active ◄─────────────────────┤
                         │                          │
              ┌──────────┼──────────┐               │
              │          │          │               │
         FailedLogin  AdminDisable BatchCreate      │
         (max attempts)  │          │               │
              │          │          │               │
              ▼          ▼          ▼               │
           Locked     Disabled  PasswordReset       │
              │                  Required           │
              │                     │               │
         AutoUnlock            ChangePassword       │
         (lockout expires)          │               │
              │                     │               │
              └─────────────────────┘───────────────┘
```

**Transition rules**:
- PendingVerification → Active: via token verification or admin manual verify
- Active → Locked: when failed login attempts reach configured maximum
- Locked → Active: when lockout duration expires (checked at next login attempt)
- Active → Disabled: admin action only
- Active → PasswordResetRequired: batch registration or admin action
- PasswordResetRequired → Active: when user successfully changes password
- Disabled: terminal state (admin can only re-enable by setting Active)

### ProjectInvitation Lifecycle

```
    Create ──────► Pending
                      │
           ┌──────────┼──────────┐
           │                     │
      Accept (user            ExpiresAtUtc
      or admin)               has passed
           │                     │
           ▼                     ▼
        Accepted              Expired
                                 │
                            Reissue
                            (creates NEW,
                             deactivates old)
                                 │
                                 ▼
                              Pending
                            (new invitation)
```

### Email Verification Token Lifecycle

```
    Generate ──────► Valid (not used, not expired)
                         │
              ┌──────────┼──────────┐
              │                     │
         Use (verify)         ExpiresAtUtc
              │               has passed
              ▼                     │
            Used                    ▼
                              Expired
                                 │
                            Regenerate
                            (creates NEW,
                             old stays expired)
                                 │
                                 ▼
                              Valid
                            (new token)
```

---

## Indexes

### New Indexes

| Table | Index | Columns | Filter | Purpose |
|-------|-------|---------|--------|---------|
| SystemConfigurations | UX_SystemConfigurations_Key | Key | — | Unique configuration keys |
| Countries | UX_Countries_Code | Code | — | Unique country codes |
| ProjectInvitations | UX_ProjectInvitations_ActivePending | UserId, ProjectId | WHERE IsActive = 1 AND Status = 0 | One active pending invitation per user per project |
| ProjectInvitations | IX_ProjectInvitations_ProjectStatus | ProjectId, Status | WHERE IsActive = 1 | List invitations by project and status |
| ProjectInvitations | IX_ProjectInvitations_UserId | UserId | WHERE IsActive = 1 | Find invitations for a user |

### Modified Indexes

None — existing indexes on Users, AuthSessions, RoleAssignments remain unchanged.

---

## Cross-Domain References

| From | To | Reference Type | Mechanism |
|------|-----|---------------|-----------|
| ProjectInvitation.UserId | User.Id | ID reference (long) | Cross-aggregate, cross-domain |
| ProjectInvitation.CreatedByUserId | User.Id | ID reference (long) | Cross-aggregate, cross-domain |
| Project.EnrollmentVariant | — | Local enum | Within Tenant domain |
| User registration | ProjectInvitation creation | Integration event | UserRegisteredEvent → UserRegisteredEventHandler |
| Batch registration | ProjectInvitation creation | Orchestrated in handler | BatchRegisterUsersHandler calls both domains |
