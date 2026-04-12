# Quickstart: Unified User Creation with Configurable Onboarding

**Date:** 2026-04-12
**Feature:** 009-unified-user-creation

## What This Feature Does

Unifies admin user creation into a single form with two configurable onboarding toggles (skip email verification, skip invitation acceptance), enforces project binding via session context, and adds onboarding status visibility to the admin users list.

## Architecture Overview

```
Admin creates user (Web Layer)
    │
    ├── CreateUserCommand / BatchCreateUsersCommand (Access Application)
    │   ├── Creates or finds existing User (Access Domain)
    │   ├── Publishes UserRegisteredEvent with toggle-derived fields
    │   └── Returns result (Created/Enrolled/AlreadyEnrolled)
    │
    └── UserRegisteredEventHandler (Tenant Application)
        ├── Both ON → EnrollParticipantCommand (direct enrollment)
        └── Otherwise → CreateInvitationCommand (with RequiresAcceptance flag)
            │
            ├── RequiresAcceptance=true → user must click invitation link
            └── RequiresAcceptance=false → auto-accepted after verification
                                           (UserEmailVerifiedEventHandler)

User onboarding (Web Layer — public pages)
    │
    ├── VerifyEmail + SetPassword (Access Area)
    │   └── Publishes UserEmailVerifiedEvent
    │       └── Tenant handler checks pending invitations, auto-accepts if RequiresAcceptance=false
    │
    └── AcceptInvitation + SetPassword (Access Area)
        └── AcceptInvitationCommand (Tenant Application)
            └── EnrollParticipant
```

## Key Files to Touch

### New Files
- `Mentoory.Access.Application/Commands/CreateUser/` — Command, Handler, Validator, Result
- `Mentoory.Access.Application/Commands/BatchCreateUsers/` — Command, Handler, Validator, Result
- `Mentoory.Access.Application/Commands/SetInitialPassword/` — Command, Handler, Validator
- `Mentoory.Web/Areas/Access/Controllers/OnboardingController.cs` — Verification + invitation acceptance pages
- `Mentoory.Web/Areas/Access/Views/Onboarding/` — VerifyEmail.cshtml, AcceptInvitation.cshtml, Expired.cshtml
- `Mentoory.Web/Areas/Administration/Views/Users/Create.cshtml` — Unified creation form

### Modified Files
- `Mentoory.Tenant.Domain/Aggregates/ProjectInvitation/ProjectInvitation.cs` — Add RequiresAcceptance field
- `Mentoory.Tenant.Application/Invitations/IntegrationEventHandlers/UserEmailVerifiedEventHandler.cs` — Check invitation.RequiresAcceptance
- `Mentoory.Tenant.Application/Invitations/Commands/CreateInvitation/` — Pass RequiresAcceptance
- `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — New Create action, retire Enroll/RegisterInternal
- `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs` — Remove project dropdown, add toggles
- `Mentoory.Web/Areas/Administration/Views/BatchUpload/Index.cshtml` — Add toggles, remove project selector
- `Mentoory.Tenant.Application/Enrollment/IntegrationEventHandlers/UserRegisteredEventHandler.cs` — Pass RequiresAcceptance to CreateInvitationCommand
- `Mentoory.Access.Application/Queries/ListIncubatorMembers/` — Extend DTO with onboarding status
- `Mentoory.Db/tenant/Tables/ProjectInvitations.sql` — Add RequiresAcceptance column

### Retired (redirect only)
- `Mentoory.Web/Areas/Administration/Views/Users/Enroll.cshtml`
- `Mentoory.Web/Areas/Administration/Views/Users/RegisterInternal.cshtml`

## Prerequisites

- Spec 008 (Context Selector UX) must be implemented — session context with ActiveProjectId claim
- Branch: `009-unified-user-creation` (from develop)

## Build & Run

```bash
# Build
dotnet build

# Run with Aspire
dotnet run --project Mentoory.Aspire.AppHost

# Run Web only
dotnet run --project Mentoory.Web

# Test
dotnet test
```

## Testing Strategy

1. **Unit tests:** CreateUserCommand handler, BatchCreateUsersCommand handler, SetInitialPasswordCommand handler, toggle-to-event mapping
2. **Integration tests:** Cross-domain event flow (UserRegisteredEvent → enrollment/invitation), post-verification auto-accept
3. **E2E tests:** Full form submission → onboarding flow for each toggle combination
4. **Manual verification:** UI forms, DataTable onboarding status column, redirect routes

## Known Limitations

- **Email delivery is not implemented.** The Notification module is an empty stub. Token generation and event publishing work, but no actual emails are sent. The flow is architecturally correct and will work once email infrastructure is built.
- **Password setup during verification/invitation is new UI work** — the current verification page does not include password setup.
