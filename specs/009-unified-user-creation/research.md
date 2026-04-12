# Research: Unified User Creation with Configurable Onboarding

**Date:** 2026-04-12
**Feature:** 009-unified-user-creation

## Key Findings

### 1. Invitation Infrastructure Already Exists (~70% Complete)

The Tenant domain already has a mature invitation system built during spec 004 (Phase 1-3 hardening):

**Domain:**
- `ProjectInvitation` aggregate (`Mentoory.Tenant.Domain/Aggregates/ProjectInvitation/ProjectInvitation.cs`) — ExternalId, ProjectId, UserId, TokenHash, Status, ExpiresAtUtc, AcceptedAtUtc, CreatedByUserId, IsActive
- `InvitationStatus` enum: Pending, Accepted, Expired
- `EnrollmentVariant` enum: FullFlow (0), Bypass (1)
- `IProjectInvitationRepository` with GetByUserAsync, GetByExternalIdAsync

**Application commands/queries:**
- `CreateInvitationCommand/Handler` — creates invitation with token
- `AcceptInvitationHandler` — accepts invitation, enrolls participant as Entrepreneur
- `AdminAcceptInvitationHandler` — admin-initiated acceptance
- `ReissueInvitationHandler` — deactivates old invitation, creates new one
- `ListPendingInvitationsHandler`, `GetInvitationDetailsHandler`, `GetUserProjectAssociationsHandler`
- `RequestSelfEnrollmentHandler`

**Integration event handlers:**
- `UserRegisteredEventHandler` — branches on Bypass + !RequiresVerification for direct enrollment, otherwise creates invitation
- `UserEmailVerifiedEventHandler` — after verification, auto-accepts invitations for Bypass-variant projects

**Database:**
- `tenant.ProjectInvitations` table with indexes on (UserId, ProjectId) for active pending, (ProjectId, Status), and (UserId)

**Tests:**
- `ProjectInvitationTests.cs`

**Decision:** Extend existing infrastructure rather than building from scratch.
**Rationale:** The aggregate, repository, commands, handlers, and table all exist. Adding toggle support requires modifications, not rewrites.
**Alternatives considered:** Building a parallel invitation system in the Access domain — rejected because it would duplicate existing Tenant domain functionality.

### 2. Toggle-to-Event Mapping Already Works

The `UserRegisteredEvent` already carries: `RequiresVerification` (bool), `EnrollmentVariant` (string), `InvitationExpiryHours` (int).

The existing handler logic in `UserRegisteredEventHandler` maps cleanly to all 4 toggle combinations:

| Skip Verification | Skip Invitation | RequiresVerification | EnrollmentVariant | Handler Result |
|---|---|---|---|---|
| OFF | OFF | true | FullFlow | Creates invitation (pending until user accepts) |
| ON | ON | false | Bypass | Direct enrollment (no invitation) |
| ON | OFF | false | FullFlow | Creates invitation (user must accept) |
| OFF | ON | true | Bypass | Creates invitation; after verification, auto-accepts (Bypass) |

**Decision:** Map admin toggles to existing event fields. No new event fields needed.
**Rationale:** The existing branching logic already handles all 4 cases correctly.

### 3. Post-Verification Auto-Accept Needs Decoupling

**Current behavior:** `UserEmailVerifiedEventHandler` checks `project.EnrollmentVariant` to decide auto-accept. This means the behavior depends on the project's current setting, not the admin's toggle choice at creation time.

**Problem:** If a project's setting changes between user creation and email verification, the wrong behavior triggers.

**Decision:** Add `RequiresAcceptance` (bool) field to `ProjectInvitation` entity. The post-verification handler reads this field instead of the project setting.
**Rationale:** Decouples invitation behavior from project-level settings, making toggle state sticky per-invitation.

### 4. Email Sending Infrastructure Does Not Exist

**Finding:** The `Mentoory.Notification.*` projects are empty stubs. MailKit is referenced but has zero implementation. No email templates, no SMTP configuration, no email service.

**Existing pattern:** Token generation and storage works. Events are published. But nobody sends the actual emails.

**Decision:** Build the full creation + onboarding flow with token generation and events. Email delivery is a separate concern that integrates naturally when implemented.
**Rationale:** The spec's requirements are about WHAT should happen. The flow is correct even if emails aren't physically sent yet. Token-based links work independently of email delivery.

### 5. ConfigurationKey Already Has InvitationTokenExpiryHours

`ConfigurationKey.InvitationTokenExpiryHours` is already defined in the Access domain. The spec's 7-day (168 hours) default aligns with the existing seeded configuration.

### 6. Session Context Is Claim-Based

- `ContextController.Select` → `SetActiveContextCommand` → `UpdateAuthCookie` refreshes claims
- `GetActiveProjectId()` returns nullable long from "ActiveProjectId" claim
- `HasValidIncubatorContext()` checks ActiveIncubatorId > 0
- Controllers can check context with existing extension methods — no new infrastructure needed

### 7. DataTable Pattern for Users List

- `ListIncubatorMembersQuery` returns `IncubatorMemberListItemDto` with: ExternalId, Email, FirstName, LastName, AccountStatus, CreatedAtUtc
- Extending the DTO and query to include onboarding status requires joining ProjectInvitations table data
- The query currently fetches users by RoleAssignment incubator scope, then queries users by ID list
- Adding invitation status requires a cross-domain query (User in Access, ProjectInvitation in Tenant) — needs a dedicated query or view

### 8. Password Flow Requires New UI Pages

Current state:
- Verification page (`VerifyEmailHandler`): validates token, activates account. No password setup.
- Invitation acceptance: `AcceptInvitationHandler`: accepts, enrolls. No password setup UI.

Required:
- New verification page that combines token validation + password setup
- New invitation acceptance page that combines token validation + password setup + enrollment
- These are public-facing pages (user may not have a session) — goes in Access area

**Decision:** Create new controllers in the Access area for verification-with-password and invitation-acceptance-with-password.
**Rationale:** Cross-domain orchestration (Access for password, Tenant for enrollment) is appropriate at the controller level.
