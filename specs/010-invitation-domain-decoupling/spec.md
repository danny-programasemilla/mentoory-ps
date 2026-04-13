# Feature Specification: Invitation Domain Decoupling

**Feature Branch**: `010-invitation-domain-decoupling`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: Remove cross-domain coupling between Access and Tenant modules by eliminating the invitation token validation path from Access. Unify all onboarding authentication on Access-domain EmailVerificationTokens.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Invited User Sets Password via Verification Token (Priority: P1)

An admin creates a user and assigns them to a project. The system generates an EmailVerificationToken (Access domain) and creates a ProjectInvitation (Tenant domain) independently. The user receives an email containing the Access-domain verification token. They click the link, set their password, and are automatically enrolled in their project(s).

**Why this priority**: This is the primary onboarding flow. Every invited user goes through it. It must work with zero cross-domain coupling.

**Independent Test**: Can be tested by creating a user via admin, clicking the email link, setting a password, and confirming the user lands in the correct project. Validates that invitation acceptance is driven entirely by the existing `UserEmailVerifiedEvent` flow.

**Acceptance Scenarios**:

1. **Given** an admin has created a user assigned to a project, **When** the user clicks the verification link and sets their password, **Then** their account becomes active and they are enrolled in the project
2. **Given** an admin has created a user assigned to a project with `RequiresAcceptance = false`, **When** the user verifies their email, **Then** the invitation is auto-accepted and the user is enrolled without additional action
3. **Given** an admin has created a user assigned to a project with `RequiresAcceptance = true`, **When** the user verifies their email, **Then** the invitation remains pending until the user manually accepts

---

### User Story 2 - Admin Reissues an Invitation (Priority: P1)

An admin reissues an invitation for a user who has not yet completed onboarding (e.g., their token expired). The system deactivates the old invitation, creates a new one, and triggers generation of a fresh EmailVerificationToken in the Access domain via an integration event. A new email is sent with the fresh token.

**Why this priority**: Token expiry is common (24h default vs 7-day invitation). Reissue is the recovery mechanism and must work without cross-domain synchronous calls.

**Independent Test**: Can be tested by letting an invitation's verification token expire, triggering a reissue from the admin panel, and confirming the user receives a new email with a working token that allows password setup and project enrollment.

**Acceptance Scenarios**:

1. **Given** a user's verification token has expired, **When** an admin reissues the invitation, **Then** a new EmailVerificationToken is generated and a new email is sent
2. **Given** a user has a valid unused token, **When** an admin reissues the invitation, **Then** the old token is invalidated and replaced with a new one
3. **Given** a user has already verified their email and is active, **When** an admin reissues the invitation, **Then** a new token is generated but the user can also log in directly to accept

---

### User Story 3 - User with Multiple Pending Invitations (Priority: P2)

A user is invited to multiple projects before completing onboarding. When they verify their email by setting their password, all pending invitations that do not require manual acceptance are auto-accepted in a single operation.

**Why this priority**: Multi-project enrollment is a real scenario but less frequent than single-project onboarding.

**Independent Test**: Can be tested by creating a user with invitations to three projects (two auto-accept, one manual), verifying the email, and confirming the user is enrolled in exactly two projects with one invitation pending.

**Acceptance Scenarios**:

1. **Given** a user has 3 pending invitations (2 auto-accept, 1 manual), **When** the user verifies their email, **Then** 2 invitations are accepted and 1 remains pending
2. **Given** a user has 2 pending invitations where 1 has expired, **When** the user verifies their email, **Then** only the non-expired auto-accept invitation is accepted

---

### Edge Cases

- What happens when a verification token expires but the invitation is still active? The user cannot set their password. Admin must reissue, which generates both a new invitation and a new token.
- What happens when the invitation expires but the token is still valid? The user sets their password successfully. `UserEmailVerifiedEventHandler` checks expiration on each invitation and skips expired ones. User is verified but not enrolled. Admin can reissue.
- What happens when a reissue event handler fails in Access? The invitation is reissued in Tenant, but no new token is created in Access. Standard error handling applies. Admin can reissue again.
- What happens when the user is already active and receives a reissue? A new token is generated (harmless). The user can also log in and accept the invitation manually.

## Requirements *(mandatory)*

### Functional Requirements

#### Removals

- **FR-001**: System MUST remove the `TokenType` enum and the `TokenType` property from `SetInitialPasswordCommand`. The handler MUST always validate an EmailVerificationToken.
- **FR-002**: System MUST delete `IInvitationTokenValidator` interface from Shared.Application and its `InvitationTokenValidator` implementation from Tenant.Infrastructure.
- **FR-003**: System MUST remove `TokenHash` from the `ProjectInvitation` aggregate, including the parameter from `Create()`, token generation from `CreateInvitationHandler`, and token generation from `ReissueInvitationHandler`.
- **FR-004**: System MUST remove the DI registration of `InvitationTokenValidator` from Tenant.Infrastructure.DependencyInjection.

#### Additions

- **FR-005**: System MUST introduce an `InvitationReissuedEvent` integration event carrying `UserId`, `UserExternalId`, and `InvitationExpiryHours`. Published by `ReissueInvitationHandler` after deactivating the old invitation and creating a new one.
- **FR-006**: System MUST introduce an `InvitationReissuedEventHandler` in Access.Application that generates a fresh `EmailVerificationToken` for the user when an `InvitationReissuedEvent` is received.

#### Modifications

- **FR-007**: `SetInitialPasswordCommandHandler` MUST be simplified to only validate EmailVerificationTokens. The `TokenType.Invitation` branch MUST be removed entirely.
- **FR-008**: `ReissueInvitationHandler` MUST publish `InvitationReissuedEvent` instead of generating tokens directly. Token generation crypto logic (`RandomNumberGenerator`) MUST be removed from this handler.
- **FR-009**: `CreateInvitationHandler` MUST remove its token generation logic. `ProjectInvitation.Create()` MUST be updated to not require a `tokenHash` parameter.
- **FR-010**: Any controller or frontend endpoint that sends `SetInitialPasswordCommand` with `TokenType.Invitation` MUST be updated to use the verification token flow.

#### Unchanged (guardrails)

- **FR-011**: The existing `UserEmailVerifiedEvent` to `UserEmailVerifiedEventHandler` flow MUST remain unchanged. This is the mechanism that auto-accepts pending invitations.
- **FR-012**: The `UserRegisteredEvent` flow MUST remain unchanged. Registration already generates an EmailVerificationToken.
- **FR-013**: `ValidateVerificationTokenHandler` MUST remain unchanged. It is purely within the Access domain.

### Key Entities

- **ProjectInvitation** (Tenant domain): Represents an invitation to join a project. After this change, no longer carries a `TokenHash`. Lifecycle: Pending -> Accepted/Expired. Owns `ExternalId`, `ProjectId`, `UserId`, `Status`, `ExpiresAtUtc`, `RequiresAcceptance`.
- **EmailVerificationToken** (Access domain): PBKDF2-hashed, single-use, expiry-aware token owned by the User aggregate. Serves as the sole authentication mechanism for onboarding.
- **InvitationReissuedEvent** (new): Integration event bridging Tenant's invitation reissue to Access's token generation without synchronous coupling.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Access.Application has zero references to invitation concepts — no `IInvitationTokenValidator`, no `TokenType.Invitation`, no invitation ExternalId parsing
- **SC-002**: `SetInitialPasswordCommandHandler` has exactly one token validation path (EmailVerificationToken)
- **SC-003**: Reissuing an invitation produces a fresh EmailVerificationToken via event-driven communication, with no synchronous cross-domain call
- **SC-004**: Users can complete the full onboarding flow (click link, set password, land in project) with identical UX to the current system
- **SC-005**: Solution builds with zero warnings (`TreatWarningsAsErrors`)
- **SC-006**: All existing tests continue to pass

## Assumptions

- The notification module (email sending) will accept the EmailVerificationToken from Access for all onboarding emails. Notification module changes are out of scope but must be coordinated.
- The `User.GenerateEmailVerificationToken()` method in the Access domain is sufficient for generating tokens during both initial registration and reissue scenarios.
- The existing `UserEmailVerifiedEventHandler` in Tenant correctly auto-accepts all non-`RequiresAcceptance` pending invitations — no modifications needed.
- Database migration for dropping the `TokenHash` column from ProjectInvitations can be handled as a follow-up or included in this work at the implementer's discretion.
- Token expiry for reissued invitations will use the `InvitationExpiryHours` value passed via the integration event, aligning token lifetime with invitation lifetime.
