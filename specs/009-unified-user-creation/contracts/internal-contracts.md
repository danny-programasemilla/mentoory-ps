# Internal Contracts: Unified User Creation

**Date:** 2026-04-12
**Feature:** 009-unified-user-creation

## Command Contracts

### CreateUserCommand → CreateUserResult

**Input:**
```
CreateUserCommand(
    Email: string,              // required, email format
    Country: string,            // required, valid country code
    Identification: string,     // required, valid per country rules
    FirstName: string,          // required
    LastName: string,           // required
    SkipEmailVerification: bool,
    SkipInvitationAcceptance: bool,
    ProjectExternalId: Guid,    // from session context
    IncubatorExternalId: Guid,  // from session context
    CreatedByUserId: long       // from session
)
```

**Output:**
```
CreateUserResult(
    UserExternalId: Guid,
    Outcome: CreateUserOutcome,    // Created, Enrolled, AlreadyEnrolled
    TemporaryPassword: string?,    // only when both toggles ON + new user
    Warnings: IReadOnlyList<string>
)
```

**Outcomes:**
- `Created` — new user account + enrollment process started per toggles
- `Enrolled` — existing user, enrolled in project per toggles
- `AlreadyEnrolled` — existing user, already in this project

**Errors:**
- Duplicate email with different identification → validation error
- Invalid country/identification → validation error
- Project not found → error

### BatchCreateUsersCommand → BatchCreateUsersResult

**Input:**
```
BatchCreateUsersCommand(
    Rows: IReadOnlyList<BatchUserRow>,
    SkipEmailVerification: bool,
    SkipInvitationAcceptance: bool,
    ProjectExternalId: Guid,
    IncubatorExternalId: Guid,
    CreatedByUserId: long
)

BatchUserRow(
    Country: string,
    Identification: string,
    Email: string,
    FirstName: string,
    LastName: string
)
```

**Output:**
```
BatchCreateUsersResult(
    TotalCount: int,
    CreatedCount: int,
    EnrolledCount: int,
    ErrorCount: int,
    Rows: IReadOnlyList<BatchRowResult>
)

BatchRowResult(
    RowNumber: int,
    Country: string,
    Identification: string,
    Email: string,
    Outcome: CreateUserOutcome,
    TemporaryPassword: string?,
    Warnings: IReadOnlyList<string>,
    Errors: IReadOnlyList<string>
)
```

### SetInitialPasswordCommand

**Input:**
```
SetInitialPasswordCommand(
    UserExternalId: Guid,
    Token: string,          // verification token or invitation token
    TokenType: TokenType,   // Verification or Invitation
    NewPassword: string,
    ConfirmPassword: string
)
```

**Output:** `Result` (success/failure)

**Behavior:** Validates token, sets password, marks token as used. If TokenType is Verification, activates account.

### AcceptInvitationCommand (existing — NO CHANGES)

Already handles: validate invitation, accept, enroll participant.

### ReissueInvitationCommand (existing — NO CHANGES)

Already handles: deactivate old, create new invitation.

## Integration Event Contracts

### UserRegisteredEvent (existing — FIELD POPULATION CHANGES)

```
UserRegisteredEvent(
    UserId: long,
    UserExternalId: Guid,
    Email: string,
    FirstName: string,
    LastName: string,
    AccountStatus: string,
    ProjectExternalId: Guid?,
    RequiresVerification: bool,     // ← now set from admin toggle
    EnrollmentVariant: string,      // ← now set from admin toggle
    InvitationExpiryHours: int,
    CreatedAtUtc: DateTime,
    OccurredOnUtc: DateTime
)
```

**Toggle mapping:**
- `RequiresVerification` = !SkipEmailVerification
- `EnrollmentVariant` = SkipInvitationAcceptance ? "Bypass" : "FullFlow"

### UserEmailVerifiedEvent (existing — NO CHANGES)

Published after email verification. The handler in Tenant domain will be modified to check `invitation.RequiresAcceptance` instead of `project.EnrollmentVariant`.

## Web Endpoint Contracts

### Admin Endpoints (authenticated, role-gated)

| Method | Route | Roles | Purpose |
|--------|-------|-------|---------|
| GET | `/Administration/Users/Create` | IncubatorAdmin, GlobalAdmin | Unified creation form |
| POST | `/Administration/Users/Create` | IncubatorAdmin, GlobalAdmin | Submit new user |
| GET | `/Administration/BatchUpload` | ProjectCoordinator, IncubatorAdmin, GlobalAdmin | Batch upload form (modified) |
| POST | `/Administration/BatchUpload` | ProjectCoordinator, IncubatorAdmin, GlobalAdmin | Submit CSV (modified) |
| POST | `/Administration/Users/ReissueInvitation` | IncubatorAdmin, GlobalAdmin | Regenerate invitation token |

### Public Onboarding Endpoints (token-based, no authentication)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/Access/Onboarding/VerifyEmail?token={token}` | Email verification + password setup |
| POST | `/Access/Onboarding/VerifyEmail` | Submit verification + password |
| GET | `/Access/Onboarding/AcceptInvitation?token={token}` | Invitation acceptance + password setup |
| POST | `/Access/Onboarding/AcceptInvitation` | Submit acceptance + password |
| GET | `/Access/Onboarding/Expired` | Token expired message page |

### Redirect Routes (backward compatibility)

| Old Route | Redirects To |
|-----------|-------------|
| `/Administration/Users/Enroll` | `/Administration/Users/Create` |
| `/Administration/Users/RegisterInternal` | `/Administration/Users/Create` |
