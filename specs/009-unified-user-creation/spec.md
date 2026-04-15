# Feature Specification: Unified User Creation with Configurable Onboarding

**Feature Branch**: `009-unified-user-creation`
**Created**: 2026-04-12
**Status**: Draft
**Input**: Brainstorm session from `.claude/brainstorm/user-registration.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Individual User Creation with Full Onboarding (Priority: P1)

An IncubatorAdmin navigates to `/Administration/Users/Create`, sees a unified form with fields for Email, Country, Identification, FirstName, and LastName, plus two toggles: "Omitir verificación de correo" and "Omitir aceptación de invitación". Both toggles are OFF by default. The admin fills the form and submits. The system creates the user with status `PendingVerification` and sends a verification email. After the user verifies their email, the system sends an invitation email. The user clicks the invitation link, sees the project name and an "Aceptar invitación" button alongside a password setup form. Upon acceptance, the user is enrolled in the project and can log in.

**Why this priority**: This is the core flow — most admin user creation will use the default toggles. It validates the end-to-end onboarding pipeline.

**Independent Test**: Create a user with both toggles OFF, verify that two emails are sent in sequence, and confirm the user can only access the project after completing both steps.

**Acceptance Scenarios**:

1. **Given** an IncubatorAdmin with an active project in session context, **When** they submit the creation form with both toggles OFF, **Then** the user is created with AccountStatus `PendingVerification`, a `ProjectInvitation` with status `Pending` is created, and a verification email is sent.
2. **Given** a user with AccountStatus `PendingVerification` and a `ProjectInvitation` with status `Pending`, **When** they click the verification link and verify their email, **Then** their AccountStatus changes to `Active` and an invitation email is sent.
3. **Given** a user with AccountStatus `Active` and a `ProjectInvitation` with status `Pending`, **When** they click the invitation link and accept, **Then** they set their password, the invitation status becomes `Accepted`, the user is enrolled in the project.

---

### User Story 2 - Individual User Creation with Both Toggles ON (Priority: P1)

An admin creates a user with both "Omitir verificación de correo" and "Omitir aceptación de invitación" toggles ON. The system creates the user as fully active and enrolled in the project immediately. A temporary password is generated, displayed to the admin, and the user's account is flagged with `PasswordResetRequired`.

**Why this priority**: This is the "admin-provisioned" fast path — critical for scenarios where admins need users ready immediately.

**Independent Test**: Create a user with both toggles ON, verify no emails are sent, confirm the temporary password is displayed, and confirm the user can log in and is forced to change password.

**Acceptance Scenarios**:

1. **Given** an admin with an active project, **When** they submit the form with both toggles ON, **Then** the user is created with status `Active`, auto-enrolled in the project, and a temporary password is displayed.
2. **Given** a user created with both toggles ON, **When** they log in with the temporary password, **Then** they are forced to change their password before accessing the system.

---

### User Story 3 - Session Context Enforcement (Priority: P1)

An admin navigates to the user creation page or batch upload page without an active project selected in the session context. The form is blocked with an inline alert: "Seleccione un proyecto desde el selector de contexto para continuar". The admin must select a project via the top-bar context selector before the form becomes usable.

**Why this priority**: Project binding is the foundational constraint — without it, the onboarding toggles and invitation flow have no target project.

**Independent Test**: Navigate to user creation without a project in session, verify the form is blocked. Select a project, verify the form becomes usable.

**Acceptance Scenarios**:

1. **Given** an admin with no active project in session, **When** they navigate to `/Administration/Users/Create`, **Then** the form fields are disabled and an alert prompts project selection.
2. **Given** an admin with no active project in session, **When** they navigate to `/Administration/BatchUpload`, **Then** the upload form is disabled and the same alert is shown.
3. **Given** an admin who then selects a project via the context selector, **When** they return to the creation page, **Then** the form is fully functional and the project name is displayed.

---

### User Story 4 - Batch Upload with Onboarding Toggles (Priority: P2)

An admin navigates to `/Administration/BatchUpload`, sees the two onboarding toggles (global, apply to all rows) and a CSV file upload field. The project is derived from session context. They upload a CSV and submit. The system processes each row applying the toggle state uniformly. The results page shows per-row status: "Creado" (new user), "Inscrito" (existing user enrolled), or "Error" with details. Temporary passwords are only shown when both toggles are ON.

**Why this priority**: Batch is essential for operational efficiency but depends on the individual creation logic being solid first.

**Independent Test**: Upload a CSV with 5 rows (mix of new and existing users) with various toggle states, verify results page accuracy.

**Acceptance Scenarios**:

1. **Given** an admin with an active project and both toggles OFF, **When** they upload a valid CSV, **Then** each new user is created with AccountStatus `PendingVerification`, a pending `ProjectInvitation` per user, and verification emails are sent.
2. **Given** a CSV containing an email that matches an existing user, **When** processed, **Then** the row status shows "Inscrito" and the existing user is enrolled in the project per toggle state.
3. **Given** both toggles ON, **When** the CSV is processed, **Then** temporary passwords are displayed in the results table for newly created users.

---

### User Story 5 - Mixed Toggle Combinations (Priority: P2)

An admin creates users with the two intermediate toggle combinations: (a) Skip verification ON, skip invitation OFF — user is auto-verified but receives an invitation email and must accept. (b) Skip verification OFF, skip invitation ON — user receives a verification email and must verify, but is auto-enrolled in the project upon verification.

**Why this priority**: Covers the full toggle matrix. Less common than the two extremes but necessary for flexibility.

**Independent Test**: Create one user with each intermediate combination, verify the correct emails are sent and the correct onboarding steps are required.

**Acceptance Scenarios**:

1. **Given** skip verification ON and skip invitation OFF, **When** a user is created, **Then** they are auto-verified (AccountStatus `Active`), a `ProjectInvitation` with status `Pending` is created, and an invitation email is sent immediately.
2. **Given** a user with AccountStatus `Active` and a pending `ProjectInvitation`, **When** the user accepts the invitation, **Then** they set their password, the invitation status becomes `Accepted`, and they are enrolled.
3. **Given** skip verification OFF and skip invitation ON, **When** a user is created, **Then** they receive a verification email with AccountStatus `PendingVerification` and are auto-enrolled in the project (no `ProjectInvitation` created).
4. **Given** skip verification OFF and skip invitation ON, **When** the user verifies their email, **Then** they set their password and their AccountStatus becomes `Active`.

---

### User Story 6 - Existing User Auto-Enrollment (Priority: P2)

An admin creates a user (individual or batch) whose email or country+identification matches an existing account. Instead of failing, the system enrolls the existing user in the current project, respecting the toggle state. The admin sees a clear message distinguishing enrollment from creation.

**Why this priority**: Multi-project enrollment is a common real-world scenario. Blocking on duplicates forces admins into workarounds.

**Independent Test**: Create a user, then attempt to create the same user for a different project. Verify enrollment without duplication.

**Acceptance Scenarios**:

1. **Given** a user already exists with email X, **When** an admin creates a user with email X for project Y, **Then** the existing user is enrolled in project Y and the admin sees "Usuario ya existe — inscrito en el proyecto Y".
2. **Given** a user already enrolled in project Y, **When** an admin creates a user with the same email for project Y, **Then** the admin sees "Usuario ya inscrito en este proyecto" — no duplicate enrollment.
3. **Given** a user exists with status Locked, **When** enrolled in a new project, **Then** enrollment succeeds but the admin sees a warning: "Usuario inscrito pero la cuenta está Bloqueada".

---

### User Story 7 - Onboarding Status on Users List (Priority: P3)

The admin users list at `/Administration/Users` displays a combined onboarding status column. Possible values: "Pendiente verificación", "Pendiente invitación", "Pendiente ambos", "Activo". The column is filterable, allowing admins to quickly find users who haven't completed onboarding.

**Why this priority**: Visibility into onboarding state is important for admin follow-up, but the creation and onboarding flows must work first.

**Independent Test**: Create users in different onboarding states, verify the status column displays correctly and filtering works.

**Acceptance Scenarios**:

1. **Given** users in various onboarding states, **When** the admin views the users list, **Then** each user shows the correct combined status.
2. **Given** the users list, **When** the admin filters by "Pendiente invitación", **Then** only users awaiting invitation acceptance are shown.
3. **Given** a user completes all onboarding steps, **When** the admin refreshes the users list, **Then** the user's status shows "Activo".

---

### User Story 8 - Invitation Token Management (Priority: P3)

Invitation tokens expire after 7 days. When a user clicks an expired invitation link, they see an "Invitación expirada" page. Admins can regenerate invitation tokens from the user detail view, which sends a new invitation email with a fresh 7-day window.

**Why this priority**: Token lifecycle management is needed for production use but is not on the critical path.

**Independent Test**: Create a user with invitation required, wait for token expiry (or manually expire), verify expiration page. Regenerate and verify new email is sent.

**Acceptance Scenarios**:

1. **Given** an invitation token older than 7 days, **When** the user clicks the link, **Then** they see "Invitación expirada" with instructions to contact their admin.
2. **Given** an expired invitation, **When** the admin clicks "Reenviar invitación" on the user detail view, **Then** a new token is generated and a new invitation email is sent.
3. **Given** an invalid or tampered token, **When** accessed, **Then** a generic error page is shown and the attempt is logged.

---

### Edge Cases

- **User exists and is already enrolled in the target project:** Show "Usuario ya inscrito en este proyecto" — no duplicate enrollment, no error.
- **User exists but account is Locked/Disabled:** Enroll in project but account remains in its current state. Show warning about account status.
- **Batch with both toggles ON, all users already exist:** All rows show "Inscrito", no temporary passwords generated, summary reflects 0 created.
- **Admin switches project context mid-creation:** Form state is lost (standard browser behavior). The new context applies to the next creation.
- **Invitation accepted after admin revokes enrollment:** Token validation checks enrollment is still expected. If revoked, show "Invitación ya no es válida".
- **User receives invitations for multiple projects:** Each invitation is independent — separate tokens, separate acceptance pages.
- **Verification email link clicked after token expiry (24h):** Show "Enlace de verificación expirado" with option to request a new one (existing behavior).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a single unified user creation form at `/Administration/Users/Create`, replacing the current Enroll and RegisterInternal forms.
- **FR-002**: The unified form MUST include fields: Email, Country (dropdown), Identification, FirstName, LastName. No password field.
- **FR-003**: The form MUST include two toggles: "Omitir verificación de correo" (Skip Email Verification) and "Omitir aceptación de invitación" (Skip Invitation Acceptance), both OFF by default.
- **FR-004**: Both individual and batch creation MUST derive the target project from the active session context. No project selector on either form.
- **FR-005**: If no project is selected in session context, the system MUST block both individual and batch creation with an inline alert: "Seleccione un proyecto desde el selector de contexto para continuar".
- **FR-006**: The system MUST support all 4 toggle combinations as defined in the onboarding matrix (see User Stories 1, 2, 5).
- **FR-007**: When a user must go through verification and/or invitation, they MUST set their own password during the first onboarding step they encounter. The order is: email verification first (if required), then invitation acceptance. Password setup appears on whichever step comes first.
- **FR-008**: When both toggles are ON, the system MUST generate a temporary password (16+ chars, mixed case/digits/special), display it to the admin, and set `PasswordResetRequired` on the account.
- **FR-009**: The system MUST generate cryptographically random invitation tokens with a 7-day expiration per user-project pair.
- **FR-010**: The invitation acceptance page MUST display the project name, an "Aceptar invitación" button, and a password setup form (if the user hasn't set a password yet).
- **FR-011**: When creating a user whose email or country+identification matches an existing account, the system MUST enroll the existing user in the current project instead of creating a duplicate. For existing users, email verification state is inherited from the account — if the user is already verified, no verification email is sent regardless of toggle state. Only the invitation toggle applies: if skip invitation is OFF, a `ProjectInvitation` is created and an invitation email is sent; if skip invitation is ON, the user is auto-enrolled.
- **FR-012**: The batch upload form MUST include the two onboarding toggles applied globally to all rows. CSV format remains unchanged.
- **FR-013**: The batch results page MUST show per-row status: "Creado", "Inscrito", or "Error", with temporary passwords only when both toggles are ON.
- **FR-014**: The admin users list MUST display a combined onboarding status column ("Pendiente verificación", "Pendiente invitación", "Pendiente ambos", "Activo") that is filterable. This status is a computed value derived from AccountStatus and the user's `ProjectInvitation` state relative to the currently active project in session context.
- **FR-015**: Admins MUST be able to regenerate expired invitation tokens from the user detail view.
- **FR-016**: The retired Enroll and RegisterInternal routes MUST redirect to the new unified form to avoid broken bookmarks.

### Key Entities

- **User (existing aggregate, extended)**: Gains invitation-related behavior — generating invitation tokens, managing `ProjectInvitation` child entities. AccountStatus remains account-level only (PendingVerification, Active, Locked, Disabled, PasswordResetRequired) — no new enum values added.
- **ProjectInvitation (new child entity of User aggregate)**: Represents an invitation for a user to join a specific project. Key attributes: Token, ProjectExternalId, ExpiresAtUtc, AcceptedAtUtc, Status (Pending, Accepted, Expired). A user can have multiple ProjectInvitations (one per project). This entity tracks per-project invitation state independently from AccountStatus.
- **InvitationToken (new value object)**: Cryptographically random token with expiration. Similar pattern to existing `EmailVerificationToken`.

**State Model Clarification:** Onboarding state has two independent dimensions:
1. **AccountStatus** (account-level): PendingVerification -> Active (after email verification). Unchanged from current system.
2. **ProjectInvitation.Status** (per-project): Pending -> Accepted (after invitation acceptance). New entity.
The combined onboarding status displayed in FR-014 is computed from both dimensions relative to the active project in session context.

### Non-Functional Requirements

- **NFR-001**: Invitation tokens MUST be cryptographically random, following the same generation approach as existing email verification tokens.
- **NFR-002**: Invitation acceptance endpoints MUST validate token ownership — a user can only accept their own invitation.
- **NFR-003**: Project enrollment MUST be validated server-side; frontend is never authoritative.
- **NFR-004**: Temporary passwords MUST be minimum 16 characters with mixed case, digits, and special characters (matches current batch behavior).
- **NFR-005**: Authorization — Individual creation: IncubatorAdmin, GlobalAdmin. Batch creation: ProjectCoordinator, IncubatorAdmin, GlobalAdmin.
- **NFR-006**: Invitation acceptance endpoints MUST work for users who don't have an active session (token-based access).
- **NFR-007**: Batch upload maximum remains 500 rows per upload.
- **NFR-008**: Email sending MUST NOT block the HTTP response — use background processing for email delivery.
- **NFR-009**: `UserRegisteredEvent` contract MUST remain backward-compatible or be versioned if changed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Admin can create a single user from the unified form with any of the 4 toggle combinations, and the correct onboarding flow is triggered.
- **SC-002**: Admin can batch upload a CSV and the global toggles apply uniformly to all rows with correct per-row results.
- **SC-003**: Creating a user who already exists enrolls them in the current project without duplicating the account.
- **SC-004**: Users who need to verify email receive a verification email and can set their password during verification.
- **SC-005**: Users who need to accept an invitation receive an invitation email within 7-day window and can accept + set password from the acceptance page.
- **SC-006**: Users created with both toggles ON are immediately active with a temporary password displayed to the admin.
- **SC-007**: Admin users list shows the combined onboarding status column and supports filtering by status.
- **SC-008**: Both individual and batch creation are blocked when no project is selected in session context.
- **SC-009**: Expired invitation tokens show an appropriate message and admins can regenerate them.
- **SC-010**: Public self-registration flow remains completely unaffected.

## Assumptions

- Spec 008 (Context Selector UX) is implemented — session context with active project is available via claims.
- MailKit/MimeKit email infrastructure is operational — this spec requires two new email templates but does not redesign email delivery.
- The existing `UserRegisteredEvent` integration event pattern is the mechanism for cross-domain enrollment.
- Invitation token storage follows the same pattern as existing `EmailVerificationToken` (child entity of User aggregate or similar).
- Users are enrolled as project participants — role assignment within the project is a separate workflow outside this spec.

## Dependencies

- **Spec 008 (Context Selector UX)**: Session context with active project selection must be implemented.
- **Existing email infrastructure**: MailKit/MimeKit for sending verification and invitation emails.
- **Existing `UserRegisteredEvent`**: Integration event must be extended or a new event created to carry onboarding configuration.

## Out of Scope

- Public self-registration flow — no changes.
- Navigation/menu redesign — separate spec.
- Role assignment during user creation — users are enrolled as participants; role assignment is a separate workflow.
- Email template visual design/styling — this spec defines when emails are sent and what data they carry.
- Bulk re-invitation (sending invitations to multiple existing users at once) — future enhancement.
- Invitation analytics/reporting (acceptance rates, pending counts) — future enhancement.
- Automatic reminder emails before invitation expiration — future enhancement.
