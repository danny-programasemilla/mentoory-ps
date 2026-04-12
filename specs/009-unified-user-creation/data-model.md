# Data Model: Unified User Creation with Configurable Onboarding

**Date:** 2026-04-12
**Feature:** 009-unified-user-creation

## Entity Changes

### ProjectInvitation (existing aggregate — MODIFY)

**Location:** `Mentoory.Tenant.Domain/Aggregates/ProjectInvitation/ProjectInvitation.cs`

**Current fields:** ExternalId, ProjectId, UserId, TokenHash, Status, ExpiresAtUtc, AcceptedAtUtc, CreatedAtUtc, CreatedByUserId, IsActive

**New field:**

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `RequiresAcceptance` | bool | true | When false, invitation is auto-accepted after email verification. Decouples toggle behavior from project-level EnrollmentVariant. |

**Factory method change:** Add `requiresAcceptance` parameter to `ProjectInvitation.Create()`.

**Database change:** Add column to `tenant.ProjectInvitations`:
```sql
ALTER TABLE [tenant].[ProjectInvitations]
    ADD [RequiresAcceptance] BIT NOT NULL DEFAULT 1
```

### User (existing aggregate — NO CHANGES)

The User aggregate in the Access domain does not change. AccountStatus stays as-is: PendingVerification, Active, Locked, Disabled, PasswordResetRequired. Invitation state lives entirely in the Tenant domain's ProjectInvitation aggregate.

### EnrollmentVariant (existing enum — NO CHANGES)

FullFlow (0) and Bypass (1) are sufficient. The 4 toggle combinations map to (RequiresVerification + EnrollmentVariant) pairs on the UserRegisteredEvent.

### InvitationStatus (existing enum — NO CHANGES)

Pending (0), Accepted (1), Expired (2) — unchanged.

## Toggle-to-Event Mapping

The admin's two toggles map to event fields as follows:

| Skip Email Verification | Skip Invitation | Event.RequiresVerification | Event.EnrollmentVariant | Invitation.RequiresAcceptance |
|---|---|---|---|---|
| OFF | OFF | true | FullFlow | true |
| ON | ON | false | Bypass | N/A (no invitation created) |
| ON | OFF | false | FullFlow | true |
| OFF | ON | true | Bypass | false |

## New Commands

### CreateUserCommand (Access domain)

Replaces `RegisterUserCommand` (admin use) and `RegisterInternalUserCommand`.

```
CreateUserCommand
├── Email: string (required)
├── Country: string (required)
├── Identification: string (required)
├── FirstName: string (required)
├── LastName: string (required)
├── SkipEmailVerification: bool
├── SkipInvitationAcceptance: bool
├── ProjectExternalId: Guid (from session context)
├── IncubatorExternalId: Guid (from session context)
└── CreatedByUserId: long (from session)
```

**Returns:** `CreateUserResult` with UserExternalId, Status (Created/Enrolled), TemporaryPassword (nullable), Warnings (list)

**Behavior:**
1. Check if user exists by email or country+identification
2. If exists → publish enrollment event (skip creation)
3. If new → create User, hash password (temp or none depending on toggles), publish UserRegisteredEvent
4. Set RequiresVerification and EnrollmentVariant based on toggles

### BatchCreateUsersCommand (Access domain)

Replaces `BatchRegisterUsersCommand`.

```
BatchCreateUsersCommand
├── Rows: IReadOnlyList<BatchUserRow> (Country, Identification, Email, FirstName, LastName)
├── SkipEmailVerification: bool (global)
├── SkipInvitationAcceptance: bool (global)
├── ProjectExternalId: Guid (from session context)
├── IncubatorExternalId: Guid (from session context)
└── CreatedByUserId: long (from session)
```

**Returns:** `BatchCreateUsersResult` with TotalCount, CreatedCount, EnrolledCount, ErrorCount, Rows (list of per-row results)

### SetInitialPasswordCommand (Access domain)

New command for users setting their password during onboarding.

```
SetInitialPasswordCommand
├── UserExternalId: Guid
├── Token: string (verification or invitation token)
├── NewPassword: string
└── ConfirmPassword: string
```

## Modified Integration Events

### UserRegisteredEvent (MODIFY)

No structural changes needed. The existing fields (`RequiresVerification`, `EnrollmentVariant`, `InvitationExpiryHours`) carry the toggle state. The change is in how these fields are populated — from admin toggles instead of project settings.

### UserEmailVerifiedEvent (NO CHANGES)

Existing event. The `UserEmailVerifiedEventHandler` needs modification to check `invitation.RequiresAcceptance` instead of `project.EnrollmentVariant`.

## Modified Queries

### ListIncubatorMembersQuery (MODIFY)

**Current DTO:** ExternalId, Email, FirstName, LastName, AccountStatus, CreatedAtUtc

**Extended DTO:** Add `OnboardingStatus` (string, computed):
- "Pendiente verificación" — AccountStatus == PendingVerification, no accepted invitation for active project
- "Pendiente invitación" — AccountStatus == Active, has pending invitation for active project
- "Pendiente ambos" — AccountStatus == PendingVerification, has pending invitation for active project
- "Activo" — AccountStatus == Active, invitation accepted or no invitation needed for active project

**Implementation:** Requires cross-domain query joining User (Access) and ProjectInvitation (Tenant) data. Options:
1. Database view joining access.Users and tenant.ProjectInvitations
2. Two-step query: fetch users, then fetch invitation status for active project

Option 2 is preferred (maintains domain separation).

## State Transitions

### Account-Level (AccountStatus)

```
RegisterUser (skip verification OFF):
  → PendingVerification
  → [user verifies email] → Active

RegisterUser (skip verification ON):
  → Active (auto-verified)

RegisterUser (both ON):
  → Active + PasswordResetRequired
```

### Project-Level (ProjectInvitation.Status)

```
CreateInvitation (RequiresAcceptance=true):
  → Pending
  → [user accepts] → Accepted → ProjectParticipant created

CreateInvitation (RequiresAcceptance=false):
  → Pending
  → [user verifies email] → auto-Accepted → ProjectParticipant created

Direct enrollment (both toggles ON):
  → No invitation created
  → ProjectParticipant created directly
```

### Combined Onboarding Flow (4 cases)

```
Case 1: Both OFF
  User → PendingVerification + Invitation(Pending, RequiresAcceptance=true)
  → verify email → Active + invitation email sent
  → accept invitation → Enrolled

Case 2: Both ON
  User → Active + PasswordResetRequired + directly enrolled
  → no emails, temp password displayed

Case 3: Skip verification, require invitation
  User → Active + Invitation(Pending, RequiresAcceptance=true)
  → invitation email sent immediately
  → accept invitation → Enrolled

Case 4: Require verification, skip invitation
  User → PendingVerification + Invitation(Pending, RequiresAcceptance=false)
  → verify email → Active + auto-accepted → Enrolled
```
