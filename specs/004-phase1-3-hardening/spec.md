# Feature Specification: Phase 1–3 Hardening

**Feature Branch**: `004-phase1-3-hardening`  
**Created**: 2026-04-03  
**Status**: Draft  
**Input**: User description: "Hardening cycle for Phases 1-3: registration, verification, authentication, invitation, batch onboarding, and project enrollment flows to production-ready quality"

## Clarifications

### Session 2026-04-03

- Q: What happens after a user initiates a self-enrollment request for a public project? → A: The enrollment follows the project's configured enrollment variant (full flow or bypass), consistent with the internal enrollment model.
- Q: How should administrators access temporary passwords for batch-created users? → A: Removed. Temporary password visibility is not a requirement. Batch-created users get a generated temporary password (hashed, never stored in plaintext) and are forced to change it on first login. Admins can regenerate a new temporary password if needed via password reset.
- Q: What specific password complexity rules should be enforced? → A: OWASP standard: minimum 8 characters, at least one uppercase, one lowercase, one digit, and one special character.
- Q: What are the batch CSV file format requirements? → A: UTF-8 encoding, comma-delimited, first row as header, maximum 500 rows per file.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Public Registration with Email Verification (Priority: P1)

A person visits the public registration page and creates an account. They provide their country, national identification number, email, and password. The system validates uniqueness of both country+identification and email globally, then creates the account in a pending verification state. The person cannot use the account as a fully active user until email verification is completed. Since email delivery is not yet implemented, an administrator must be able to manually progress the verification state for testing and onboarding purposes.

**Why this priority**: Registration is the entry point to the entire platform. Without a correct, deterministic registration flow with proper account states, nothing else works.

**Independent Test**: Can be fully tested by registering a new user via the public form and verifying the resulting account state is PendingVerification, that login is blocked until verification occurs, and that an administrator can manually verify the account.

**Acceptance Scenarios**:

1. **Given** the public registration page is displayed, **When** a person fills in country, identification, email, and a valid password and submits, **Then** the account is created with status PendingVerification, an email verification token is generated, and the person sees a confirmation message indicating they must verify their email.
2. **Given** a person registers successfully, **When** they attempt to log in before verification, **Then** the system rejects the login and displays a message about pending verification.
3. **Given** a person has registered, **When** an administrator uses the admin panel to manually verify that person's email, **Then** the account status transitions to Active and the person can now log in.
4. **Given** a user with country "Costa Rica" and identification "123456789" already exists, **When** another person attempts to register with the same country and identification, **Then** registration is rejected with a specific error indicating that the identification already exists.
5. **Given** the email "user@example.com" already exists in the system, **When** another person attempts to register with the same email (regardless of case), **Then** registration is rejected with a specific error indicating the email already exists.
6. **Given** the registration form is displayed, **When** the person selects a country, **Then** the identification field applies the appropriate input mask and validation rules for that country's national ID format.

---

### User Story 2 — Authentication and Session Lifecycle (Priority: P1)

A verified user logs in with email and password. The system validates credentials, checks account status, enforces lockout after repeated failures, creates a server-validated session, and redirects to the appropriate landing experience. Session tokens must be validated server-side on every request. Users with PasswordResetRequired status must be forced to change their password before accessing any other functionality.

**Why this priority**: Authentication is co-equal with registration. Without correct session validation and status enforcement, the entire security model is broken.

**Independent Test**: Can be fully tested by logging in with valid/invalid credentials and verifying session creation, lockout behavior, server-side session validation, and forced password change enforcement.

**Acceptance Scenarios**:

1. **Given** a verified active user, **When** they log in with correct email and password, **Then** a server-side session is created and the user is redirected based on their role context.
2. **Given** a user enters incorrect credentials, **When** they fail the configured maximum number of times (default: 5), **Then** the account is locked for the configured lockout duration (default: 15 minutes) and further login attempts are rejected.
3. **Given** a session has been created, **When** the session token is validated on each subsequent request, **Then** the system checks the session record server-side (not just cookie presence) and rejects requests with revoked or expired sessions.
4. **Given** a user with AccountStatus = PasswordResetRequired, **When** they log in with their current password, **Then** they are immediately redirected to the password change screen and cannot access any other page until the password is changed.
5. **Given** a locked account, **When** the lockout duration expires, **Then** the account automatically unlocks on the next login attempt.
6. **Given** a user is logged in, **When** they log in from another location, **Then** the previous session is invalidated (single active session enforcement).

---

### User Story 3 — Internal One-by-One User Registration (Priority: P2)

An authorized internal user (IncubatorAdmin or GlobalAdmin) registers a new user into a specific project within their incubator. The internal registration form collects the same minimal data (country, identification, email, password) plus an explicit option to require or skip email verification. When verification is required, the user is created in PendingVerification state. When verification is not required, the user is created as immediately Active. In both cases, the user is associated with the target project through the invitation/enrollment flow.

**Why this priority**: Internal enrollment is the primary mechanism for onboarding users into specific projects and must produce correct account and project association states.

**Independent Test**: Can be fully tested by an admin creating a user with both verification-required and verification-not-required options and verifying the resulting account states and project associations.

**Acceptance Scenarios**:

1. **Given** an IncubatorAdmin is on the internal registration screen for a project, **When** they fill in the user data with "require email verification" checked and submit, **Then** the user is created with status PendingVerification and an email verification token is generated.
2. **Given** an IncubatorAdmin is on the internal registration screen, **When** they fill in the user data with "require email verification" unchecked and submit, **Then** the user is created with status Active (immediately verified).
3. **Given** a user is created through internal registration, **When** the creation succeeds, **Then** the user is associated with the target project through the invitation/enrollment flow according to the configured variant (full flow or bypass).
4. **Given** a user with the same email already exists, **When** the admin attempts to register them, **Then** the admin receives a specific, field-level error message identifying the email conflict (not a generic error).
5. **Given** a user with the same country+identification already exists, **When** the admin attempts to register them, **Then** the admin receives a specific, field-level error message identifying the identification conflict.

---

### User Story 4 — Batch User Registration and Project Enrollment (Priority: P2)

An authorized internal user uploads a CSV file (typically exported from Excel) to onboard multiple users into a specific project within an incubator. Each row must contain at minimum: country, identification, and email. Optional demographic fields (first name, last name, province, canton, district, address, postal code) may be included. The system processes each row idempotently, generating a per-row result report. Temporary passwords are generated for newly created users. The operation must handle existing users, existing project associations, and validation failures gracefully per row.

**Why this priority**: Batch enrollment is essential for real-world project launches where dozens or hundreds of participants are onboarded simultaneously.

**Independent Test**: Can be fully tested by uploading a CSV with a mix of new users, existing users, and invalid rows, then verifying the per-row result report and the resulting system state.

**Acceptance Scenarios**:

1. **Given** an admin uploads a valid CSV with 10 new users for a project, **When** processing completes, **Then** all 10 users are created with temporary passwords, associated with the project, and the result report shows "created" and "enrolled" for each row.
2. **Given** a CSV contains a row for a user who already exists in the system, **When** that row is processed, **Then** the user is not recreated, but the system checks project association and proceeds accordingly, reporting "already existed" in the result.
3. **Given** a CSV contains a row for a user who already exists and is already in the target project, **When** that row is processed, **Then** no changes are made and the result reports "already existed, already in project."
4. **Given** a CSV contains a row for a user who already exists but is NOT in the target project, **When** that row is processed, **Then** the user is associated with the project through the invitation/enrollment flow, reporting "already existed, newly enrolled/invited."
5. **Given** a CSV contains a row with invalid or missing mandatory data, **When** that row is processed, **Then** that row fails with a specific error, and processing continues for remaining rows.
6. **Given** batch-created users, **When** they log in for the first time using their temporary password, **Then** they are immediately forced to change their password before accessing any other functionality.
7. **Given** an admin uploads the same CSV file twice, **When** the second upload is processed, **Then** the operation is idempotent — no duplicate users or associations are created, and the result report reflects the already-existing state for each row.

---

### User Story 5 — Project Invitation and Enrollment Lifecycle (Priority: P2)

Users are associated with projects through an invitation/enrollment system with explicit states. The flow can be configured per project: either a full flow (email verification → invitation acceptance → active participation) or a bypass flow (user appears immediately verified and enrolled). Invitations have explicit states: Pending, Accepted, Expired. Administrators can inspect users in incomplete states and manually progress them through the workflow.

**Why this priority**: Project participation must be modeled with deterministic state transitions, not implicit membership. This is the core of multi-project, multi-incubator governance.

**Independent Test**: Can be fully tested by creating invitations, verifying state transitions, testing expiration, and confirming that only accepted invitations result in active project participation.

**Acceptance Scenarios**:

1. **Given** a user is invited to a project with full flow configured, **When** the invitation is created, **Then** the invitation is in Pending state and the user is not yet an active participant.
2. **Given** a user has a pending invitation and has verified their email, **When** they accept the invitation, **Then** the invitation transitions to Accepted and the user becomes an active project participant.
3. **Given** a user has a pending invitation, **When** the configured validity period expires, **Then** the invitation transitions to Expired and can no longer be accepted.
4. **Given** a project is configured with bypass flow, **When** a user is enrolled, **Then** the user appears immediately as an active participant (verification and invitation acceptance are auto-completed).
5. **Given** an administrator views users with pending or expired invitations, **When** they choose to reissue an invitation, **Then** a new invitation token is generated (previous one is invalidated) and the user can accept the new invitation.
6. **Given** a user has not yet verified their email (full flow), **When** they attempt to accept a project invitation, **Then** the system blocks acceptance and indicates that email verification must happen first.

---

### User Story 6 — First Login Experience for Users Without Project Context (Priority: P3)

When a newly registered (and verified) user logs in and does not yet belong to any active project, the system must not lead them to a broken or empty dashboard. Instead, they see a listing of all public projects currently in Registration stage that accept self-enrollment requests. The user can initiate a request to register into one of those projects.

**Why this priority**: Without a meaningful first experience, verified users hit a dead end. Showing available public projects provides an organic path forward.

**Independent Test**: Can be fully tested by logging in as a verified user with no project associations and verifying that public registration-stage projects are displayed.

**Acceptance Scenarios**:

1. **Given** a verified user with no active project associations, **When** they log in, **Then** they are shown a page listing all public projects in Registration stage.
2. **Given** public projects are displayed, **When** the user selects a project and requests enrollment, **Then** the system follows the project's configured enrollment variant: in full flow, a pending invitation is created and the user is informed of the verification/acceptance steps; in bypass flow, the user is immediately enrolled as an active participant.
3. **Given** a project exists but is not marked as public or is not in Registration stage, **When** a user without project context views the dashboard, **Then** that project does not appear in the listing.
4. **Given** no public projects in Registration stage exist, **When** a user without project context views the dashboard, **Then** a meaningful empty-state message is shown (not a broken page or error).

---

### User Story 7 — Configurable System Settings in Database (Priority: P3)

All operational flow parameters that are currently hardcoded must be stored in the database and be configurable without code changes. This includes: email verification token expiry, password reset token expiry, invitation token expiry, maximum failed login attempts, lockout duration, session timeout, password history depth, and any invitation/verification behavior toggles. This is one of the genuinely new capabilities required by this hardening phase.

**Why this priority**: Hardcoded values prevent operational flexibility and create deployment friction. Database-driven configuration is a prerequisite for production readiness.

**Independent Test**: Can be fully tested by changing configuration values in the database and verifying that the system behavior changes accordingly without restart or redeployment.

**Acceptance Scenarios**:

1. **Given** a configuration entry for email verification token expiry exists in the database with value 48 hours, **When** a new verification token is generated, **Then** the token expires after 48 hours (not the previously hardcoded 24 hours).
2. **Given** a configuration entry for max failed login attempts is set to 3, **When** a user fails login 3 times, **Then** the account is locked (not after the previously hardcoded 5 attempts).
3. **Given** a configuration entry for session timeout is set to 4 hours, **When** a session is created, **Then** it expires after 4 hours.
4. **Given** an administrator changes a configuration value, **When** the next relevant operation occurs, **Then** the new value is used without requiring application restart.

---

### User Story 8 — Manual Verification and Administrative User Management (Priority: P3)

Because email delivery is not yet implemented, administrators must be able to search for and inspect users in incomplete states (unverified email, pending invitation, incomplete onboarding). They must be able to manually progress users through workflow states: verify an email, accept an invitation on behalf of a user, regenerate tokens, reset a user's password, and unlock accounts. These capabilities serve both as operational tools for the current phase and as the administrative interface for ongoing user management.

**Why this priority**: Without administrative override capabilities, the system is untestable and inoperable while email delivery is not yet built.

**Independent Test**: Can be fully tested by creating users in various incomplete states and verifying that an administrator can search for them, view their state, and progress them through each workflow step.

**Acceptance Scenarios**:

1. **Given** users exist with PendingVerification status, **When** an administrator searches for users by state, **Then** all unverified users are listed.
2. **Given** a user has PendingVerification status, **When** an administrator manually verifies their email, **Then** the account transitions to Active.
3. **Given** a user has a pending project invitation, **When** an administrator manually accepts the invitation on behalf of the user, **Then** the invitation transitions to Accepted and the user becomes an active project participant.
4. **Given** a user has an expired verification token, **When** an administrator regenerates the token, **Then** a new token is created with a fresh expiry period and the previous token is invalidated.
5. **Given** a user has an expired project invitation, **When** an administrator reissues the invitation, **Then** a new invitation is created with a fresh expiry period.
6. **Given** an administrator needs to reset a user's password, **When** they trigger a password reset from the admin panel, **Then** the user's account is set to PasswordResetRequired with a new temporary password, and the temporary password is shown once in the admin response.

---

### Edge Cases

- What happens when a user registers, gets a verification token, and then the same email is used to register a different account before verification? The second registration must be rejected — email uniqueness is enforced at registration time, not at verification time.
- What happens when a batch CSV contains duplicate rows (same person twice in the same file)? The operation must process rows sequentially and apply idempotency — the second occurrence is treated as "already exists."
- What happens when a user has PasswordResetRequired status and their session is also expired? They must re-authenticate with the temporary password first, then be forced to change it.
- What happens when a token is regenerated while the old token has not expired? The old token must be invalidated, and only the new token is accepted.
- What happens when an administrator tries to verify an already-verified user? The operation should be idempotent — no error, no state change, just acknowledgment.
- What happens when a project is switched from public to non-public while users are viewing the public listing? The project disappears from the listing on next load. No enrollment requests in progress are affected.
- What happens when a user is enrolled in multiple projects across different incubators? Each enrollment is independent. The user selects their active context after login.
- What happens when a batch CSV contains a valid existing user but with a different email than what is on file? The system matches by country+identification. The email in the CSV is not used to update the existing user's email. A warning is included in the per-row result.

## Requirements *(mandatory)*

### Functional Requirements

#### Registration

- **FR-001**: System MUST provide a public registration page accessible without authentication.
- **FR-002**: Public registration MUST collect exactly: country of identification, identification number, email, and password. No additional fields are required at registration time.
- **FR-003**: The registration page MUST display a country dropdown sourced from database values.
- **FR-004**: When a country is selected, the identification field MUST apply the appropriate input mask and validation rules for that country's national ID format.
- **FR-005**: System MUST validate user uniqueness in this order: (1) country + identification must not already exist, (2) email must not already exist globally. Both validations must pass before the user is created.
- **FR-006**: For public registration, each uniqueness violation MUST produce a specific, user-facing error message identifying which field conflicts.
- **FR-007**: For internal (admin) registration, each uniqueness violation MUST produce a specific, field-level error message so the administrator knows exactly which field conflicts.
- **FR-008**: Password MUST comply with OWASP standard rules: minimum 8 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character.
- **FR-009**: Upon successful public registration, the account MUST be created with status PendingVerification and an email verification token MUST be generated.
- **FR-010**: *(Consolidated into FR-022)* Users with PendingVerification status MUST NOT be able to log in — see FR-022 for the complete login rejection rule.

#### Email Verification

- **FR-011**: The system MUST model email verification as an explicit state lifecycle with states: unverified (PendingVerification), verified (Active).
- **FR-012**: Email verification MUST be completable via a token-based mechanism (for future email delivery) and via manual administrative action (for current-phase operability).
- **FR-013**: Email verification tokens MUST have a configurable expiry period stored in the database.
- **FR-014**: When a verification token expires, the user remains in PendingVerification state and a new token must be regenerable.
- **FR-015**: An administrator MUST be able to regenerate a verification token for a user, invalidating any previously active token.
- **FR-016**: An administrator MUST be able to directly verify a user's email (bypassing the token mechanism) for manual testing and onboarding.

#### Authentication

- **FR-017**: System MUST authenticate users via email and password.
- **FR-018**: Session tokens MUST be validated server-side on every authenticated request — cookie possession alone MUST NOT grant access.
- **FR-019**: The system MUST enforce single active session per user — a new login invalidates any existing session.
- **FR-020**: After the configured maximum failed login attempts (stored in database), the account MUST be locked for the configured lockout duration (stored in database).
- **FR-021**: Locked accounts MUST automatically unlock after the lockout duration expires, on the next login attempt.
- **FR-022**: The system MUST reject login attempts for users with PendingVerification or Disabled status with appropriate messages.
- **FR-023**: The system MUST detect users with PasswordResetRequired status at login and redirect them to a forced password change flow before granting access to any other functionality.
- **FR-024**: Session timeout MUST be configurable via database setting.

#### Password Management

- **FR-025**: Users MUST be able to change their password by providing the current password and a new one.
- **FR-026**: Password change MUST check against a configurable number of previous passwords (history depth stored in database).
- **FR-027**: The system MUST support a password reset flow via token (for future email-based delivery) and via manual administrative action.
- **FR-028**: Password reset tokens MUST have a configurable expiry period stored in the database.
- **FR-029**: Batch-created users MUST be assigned a system-generated temporary password.
- **FR-030**: Batch-created users MUST have their account status set to PasswordResetRequired, forcing a password change on first login.
- **FR-031**: *(Consolidated into FR-023)* Users with PasswordResetRequired status MUST NOT access any functionality other than the password change screen — see FR-023 for the complete enforcement rule including redirect mechanism.

#### Internal Registration

- **FR-032**: The system MUST provide an internal registration screen accessible only to IncubatorAdmin and GlobalAdmin roles.
- **FR-033**: Internal registration MUST collect the same minimal data as public registration: country, identification, email, and password.
- **FR-034**: Internal registration MUST include an explicit UI control (checkbox or equivalent) to indicate whether email verification is required for the new user.
- **FR-035**: When email verification is required, the user MUST be created with PendingVerification status and a verification token generated.
- **FR-036**: When email verification is not required, the user MUST be created with Active status (immediately verified).
- **FR-037**: Internal registration MUST associate the created user with the target project through the invitation/enrollment flow.

#### Batch Registration

- **FR-038**: The system MUST provide a batch registration flow where an admin uploads a CSV file to onboard multiple users into a specific project within a specific incubator.
- **FR-039**: The CSV MUST be UTF-8 encoded, comma-delimited, with a required header row and a maximum of 500 rows per file. Required columns: country, identification, and email. Optional columns: first name, last name, province, canton, district, address line, postal code.
- **FR-040**: The batch operation MUST be idempotent: existing users are not recreated; existing project associations are not duplicated.
- **FR-041**: For each row, the system MUST generate a temporary password for newly created users.
- **FR-042**: The batch operation MUST return a per-row result report indicating: whether the user already existed, whether the user was created, whether the user was already in the project, whether the user was newly associated/invited, whether an invitation was generated, and the final result for that row.
- **FR-043**: Row-level failures MUST NOT prevent processing of remaining rows.
- **FR-044**: When a CSV row matches an existing user by country+identification but the email differs from the existing record, the system MUST include a warning in the per-row result and NOT update the existing email.

#### Project Visibility and Self-Enrollment

- **FR-045**: Projects MUST have an explicit, configurable flag that determines whether they appear in the public listing for self-registration.
- **FR-046**: Only projects that are both public AND in Registration stage MUST appear on the public listing.
- **FR-047**: Verified users without an active project association MUST see a landing page listing available public projects in Registration stage.
- **FR-048**: Users MUST be able to initiate an enrollment request for a public project from the landing page. The enrollment request MUST follow the project's configured enrollment variant (full flow or bypass), so the project controls whether the user is auto-enrolled or must complete verification and invitation acceptance steps.

#### Project Invitation and Enrollment

- **FR-049**: Project participation MUST be modeled as an invitation/enrollment flow with explicit states: Pending, Accepted, Expired.
- **FR-050**: The invitation/enrollment behavior MUST be configurable per project with two variants: (A) Full flow — email verification first, then invitation acceptance; (B) Bypass — user appears immediately verified and enrolled.
- **FR-051**: In full flow (Variant A), the system MUST enforce the sequence: email verification MUST happen before invitation acceptance. A user who has not verified their email MUST NOT be able to accept a project invitation.
- **FR-052**: In bypass flow (Variant B), the system MUST auto-complete verification and invitation acceptance, resulting in the user being immediately linked to the project.
- **FR-053**: Invitation tokens MUST have a configurable expiry period stored in the database.
- **FR-054**: Expired invitations MUST NOT be acceptable. A new invitation must be issued.
- **FR-055**: An administrator MUST be able to reissue an invitation (generating a new token, invalidating the previous one).
- **FR-056**: An administrator MUST be able to manually accept an invitation on behalf of a user.

#### Configuration

- **FR-057**: All operational flow parameters MUST be stored in the database and be configurable without code changes or application restart.
- **FR-058**: Configurable parameters MUST include at minimum: email verification token expiry, password reset token expiry, invitation token expiry, maximum failed login attempts, lockout duration, session timeout, and password history depth. Invitation/enrollment behavior is configured per project via the EnrollmentVariant field (FullFlow or Bypass), not via global system configuration toggles.
- **FR-059**: The system MUST read configuration values at the time of each relevant operation (not cached at startup).

#### Administrative User Management

- **FR-060**: Administrators MUST be able to search for and list users by account state (PendingVerification, Active, Locked, Disabled, PasswordResetRequired).
- **FR-061**: Administrators MUST be able to view detailed user state including: account status, verification state, pending invitations, and project associations.
- **FR-062**: Administrators MUST be able to perform the following manual actions: verify email, regenerate verification token, accept invitation on behalf of user, reissue invitation, unlock account, and reset a user's password (setting the account to PasswordResetRequired and generating a new temporary password).
- **FR-063**: The UserProfile read model MUST be kept in sync with the User aggregate. Changes to account status, email verification, or other profile-relevant fields MUST be reflected in the read model.

### Key Entities

- **User**: Global platform account. Key attributes: country+identification (unique pair), email (globally unique), account status, verification state, credentials, password history.
- **EmailVerificationToken**: Token for verifying user email. Attributes: token hash, expiry, used flag. Belongs to User.
- **PasswordResetToken**: Token for resetting password. Attributes: token hash, expiry, used flag. Belongs to User.
- **ProjectInvitation**: Invitation for a user to participate in a project. Attributes: token hash, expiry, invitation status (Pending/Accepted/Expired), project reference, user reference.
- **Country**: Supported country for registration. Attributes: name, code, identification mask/validation rules. Sourced from database.
- **SystemConfiguration**: Database-stored configuration key-value pairs. Attributes: configuration key, value, description, last modified timestamp.
- **Project** (existing): Extended with public visibility flag and enrollment behavior configuration.
- **UserProfile** (existing): Read model that must stay synchronized with User aggregate state changes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of public registrations produce the correct account state (PendingVerification) and a verification token, verifiable by checking the database state after registration.
- **SC-002**: 100% of authenticated requests are validated server-side against the session store — no request succeeds based solely on cookie presence with a revoked or expired session.
- **SC-003**: Users with PasswordResetRequired status are blocked from accessing any page other than the password change screen, verified through navigation attempts.
- **SC-004**: Batch upload of 100 users completes successfully with a per-row result report where each row accurately reflects the operation performed.
- **SC-005**: All configurable parameters (token expiry, lockout attempts, session timeout, etc.) can be changed in the database and take effect on the next relevant operation without application restart.
- **SC-006**: Administrators can search, inspect, and manually progress users through all lifecycle states (verify email, accept invitation, regenerate tokens, unlock accounts) using the administrative interface.
- **SC-007**: Invitation lifecycle states (Pending → Accepted, Pending → Expired) are deterministic, with no implicit transitions. Auditability is provided by timestamps on each state change (CreatedAtUtc, AcceptedAtUtc, ExpiresAtUtc) and the IsActive flag for tracking superseded invitations.
- **SC-008**: Verified users without project associations see a listing of public registration-stage projects and can initiate enrollment, confirmed through end-to-end flow testing.
- **SC-009**: All uniqueness violations during registration produce specific, actionable error messages (not generic errors), verified for both public and internal registration flows.
- **SC-010**: End-to-end test suite covers all happy paths and failure paths without relying on email delivery, achieving zero false-positive test results.

## Assumptions

- Email delivery (actual sending of verification emails, invitation emails, welcome emails) is out of scope for this hardening phase and will be implemented in a future Notification domain phase.
- The system will initially support Costa Rica-specific location fields (province, canton, district) for demographic data. Other country-specific location structures may be added later.
- Country data (names, codes, identification masks) will be seeded in the database as part of this feature.
- The existing Access bounded context (merged Identity + Authorization) is the correct home for all user, authentication, and verification domain logic.
- The existing Tenant bounded context is the correct home for project participation, invitation, and enrollment domain logic.
- Rate limiting for registration, login, and password reset endpoints already exists and is adequate for this phase.
- The UserProfile read model synchronization approach (synchronous within the same transaction) is acceptable for the current scale. Event-driven synchronization may be introduced in a future phase.
- The forced password change flow uses a redirect-based mechanism — middleware or filters intercept requests from users with PasswordResetRequired status and redirect them to the password change page.
