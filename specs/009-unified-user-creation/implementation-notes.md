# Implementation Notes: Unified User Creation with Configurable Onboarding

## Design Decisions

### Decision: Unified Command Architecture (Approach A)
- Replace `RegisterUserCommand` (admin use only), `RegisterInternalUserCommand`, and `BatchRegisterUsersCommand` with two commands: `CreateUserCommand` (single) and `BatchCreateUsersCommand` (CSV)
- Rationale: DRY logic, single code path for onboarding behavior, easiest to maintain
- Rejected Approach B (extend existing): Two parallel command paths for the same behavior, harder to keep in sync
- Rejected Approach C (event-driven enrollment service): More async complexity, harder to show immediate results to admin

### Decision: Session Context for Project Binding
- Both individual and batch creation derive the target project from the active session context (claims)
- No project selector on either form — consistent with how all other context-scoped features work
- Depends on Spec 008 (Context Selector UX) being implemented
- Rejected: Form-level project dropdown (duplication with top-bar selector, inconsistent mental model)

### Decision: Global Toggles on Batch
- Onboarding toggles apply uniformly to all rows in a CSV upload
- Per-row toggles (CSV columns) rejected: adds complexity to CSV format, error handling, and UX with marginal benefit
- Admins can do two separate uploads if they need different toggle states for different user groups

### Decision: Adaptive Password Flow
- Users going through verification/invitation set their own password during that flow
- Temporary passwords only generated when both toggles are ON (fully admin-provisioned)
- Rationale: Better UX — users in an onboarding flow are already interacting with the system, so let them choose their password. Temporary passwords are only needed when there's no user interaction.

### Decision: Simple Invitation Acceptance Page
- Shows project name + "Aceptar invitacion" button + password form (if needed)
- No "Decline" button — users who don't want to join simply don't click
- Rejected: Detailed landing page with role info and decline option (complexity for little value; decline raises questions about what happens to the account)

### Decision: 7-Day Invitation Token Expiration
- Longer than email verification (24h) because invitations are less urgent
- Admins can regenerate expired tokens
- Rejected: Configurable expiration (YAGNI — adds settings UI complexity)
- Rejected: No expiration (security concern — stale tokens accumulate)

### Decision: Existing User Auto-Enrollment
- Same behavior for both individual and batch: detect existing user, enroll in project, show informational message
- Rejected: Confirmation dialog for individual creation (adds friction; the admin's intent is clear — they want the user in this project)
- Special case: User already enrolled in the same project shows "Ya inscrito" message, not an error

### Decision: Retire Enroll + RegisterInternal Forms
- Single unified form replaces both
- Old routes redirect to new form to avoid broken bookmarks
- `RegisterUserCommand` remains for public self-registration only (unchanged)

## Technical Context

### Existing Patterns to Follow
- `EmailVerificationToken` pattern for `ProjectInvitation` tokens
- `UserRegisteredEvent` for cross-domain enrollment (extend with onboarding config)
- `BatchRowResult` pattern for batch results (extend with new statuses)
- `CsvUserMap` with flexible column aliases for CSV parsing (no changes to CSV format)

### Key Files to Modify
- `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — new Create action
- `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs` — add toggles, remove project dropdown
- `Mentoory.Access.Application/Commands/` — new `CreateUser/` and `BatchCreateUsers/` directories
- `Mentoory.Access.Domain/Aggregates/User/User.cs` — invitation token generation methods
- New entity/VO for `ProjectInvitation` within User aggregate or as separate tracking
