# Feature Specification: Registration Consolidation

**Feature Branch**: `015-registration-consolidation`  
**Created**: 2026-04-14  
**Status**: Draft  
**Input**: User description: "Consolidate the three user registration paths (public, admin, batch) to eliminate code duplication, remove dead or redundant registration flows, and ensure a clean, maintainable design."

## Context & Problem Statement

The platform currently has **four registration entry points** served by **three separate handlers**, with significant code duplication and one semantically incorrect reuse:

| Entry Point | Controller Action | Handler Used | Project? | Email Verification | Variant |
| ----------- | ----------------- | ------------ | -------- | ------------------ | ------- |
| Public self-registration | `RegisterController.Index` | `RegisterUserHandler` | No | Always required | SelfRegistration |
| Admin enrollment (legacy) | `UsersController.Enroll` | `RegisterUserHandler` | No | Always required | SelfRegistration |
| Admin internal registration | `UsersController.RegisterInternal` | `RegisterInternalUserHandler` | Yes | Configurable | FullFlow |
| Batch CSV upload | `BatchUploadController.Index` | `BatchRegisterUsersHandler` | Yes | Never (auto-verified) | FullFlow |

**Problems identified:**

1. **Redundant admin paths**: `UsersController.Enroll` and `UsersController.RegisterInternal` both allow admins to create individual users, but `Enroll` incorrectly reuses the public registration handler — meaning admin-enrolled users get no project association, always require email verification, and are tagged as "SelfRegistration" instead of "FullFlow."
2. **Massive code duplication**: All three handlers independently implement the same sequence — uniqueness checks (country+ID, then email), password hashing, `User.Register()` call, optional email-verification token generation, persistence, configuration reads, and `UserRegisteredEvent` publishing.
3. **Dead admin path**: `UsersController.Enroll` is functionally superseded by `UsersController.RegisterInternal`, which correctly supports project association and configurable verification. The `Enroll` action, its view model, and its view are dead code.

The three **actual business cases** that must remain are:

1. **Public self-registration** — anonymous user creates their own account, always verified.
2. **Admin single-user registration** — admin creates a user within a project context, verification is configurable.
3. **Batch registration** — admin uploads a CSV of users for a project, passwords auto-generated, verification skipped.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Remove redundant admin enrollment path (Priority: P1)

An administrator currently sees two separate "create user" options in the admin panel. Only one (RegisterInternal) works correctly with project association. The legacy Enroll path must be removed so that there is a single, clear admin registration flow.

**Why this priority**: The duplicate path is a source of confusion and bugs — admin-created users via Enroll lack project association and are incorrectly classified as self-registrations. Removing it is a prerequisite for safely consolidating the remaining handlers.

**Independent Test**: After removal, verify that the admin panel has exactly one "register user" action, that it associates users with a project, and that no references to the removed Enroll action, view, or view model remain in the codebase.

**Acceptance Scenarios**:

1. **Given** the admin panel Users section, **When** an admin navigates to user management, **Then** there is a single "register user" action (formerly RegisterInternal) and no Enroll option.
2. **Given** the legacy Enroll route `/Administration/Users/Enroll`, **When** any user requests it, **Then** the server returns 404 (route no longer exists).
3. **Given** the codebase after cleanup, **When** searching for `EnrollUserViewModel`, `Enroll.cshtml`, or the `Enroll` action, **Then** no references are found.

---

### User Story 2 - Consolidate shared registration logic (Priority: P1)

All three remaining registration flows (public, admin, batch) share the same core sequence: validate uniqueness, hash password, create user via domain factory, optionally generate verification token, persist, read configuration, and publish an integration event. This shared logic must be extracted so that each handler delegates to a common service rather than reimplementing the sequence.

**Why this priority**: The duplication is the root cause of the problem — changes to registration logic (e.g., a new uniqueness rule, a new event field) must currently be applied in three places, risking drift and bugs.

**Independent Test**: After consolidation, verify that each handler delegates to the shared service. Introduce a hypothetical change (e.g., adding a new field to the event) and confirm it only needs to change in one place.

**Acceptance Scenarios**:

1. **Given** the three remaining handlers (public, admin, batch), **When** reviewing the user-creation and event-publishing logic, **Then** the core registration sequence exists in exactly one place (a shared application service).
2. **Given** a modification to the registration flow (e.g., adding a new uniqueness check), **When** the change is made in the shared service, **Then** all three registration paths reflect the change without individual handler modifications.
3. **Given** each registration path, **When** a user is successfully registered, **Then** the same domain factory method, the same event structure, and the same persistence pattern are used (via the shared service).

---

### User Story 3 - Preserve distinct registration behaviors (Priority: P1)

While the core registration logic is shared, each path has legitimate behavioral differences that must be preserved:

- **Public**: Email verification always required; no project association; variant is "SelfRegistration"; rate-limited.
- **Admin**: Email verification configurable; project association required; variant is "FullFlow"; FirstName/LastName optional.
- **Batch**: Email verification skipped (auto-verified); project association required; variant is "FullFlow"; password auto-generated with reset required; handles existing users gracefully (skip + warning).

**Why this priority**: The consolidation must not flatten these differences. Each path exists for a distinct business reason and must retain its specific behavior.

**Independent Test**: Register a user through each of the three paths and verify that each one applies its specific rules (verification, project association, password strategy, enrollment variant).

**Acceptance Scenarios**:

1. **Given** a public self-registration, **When** the user submits the form, **Then** email verification is always required, no project is associated, and the enrollment variant is "SelfRegistration."
2. **Given** an admin registration with verification disabled, **When** the admin submits the form, **Then** the user is auto-verified, associated with the selected project, and the enrollment variant is "FullFlow."
3. **Given** a batch CSV upload containing a user who already exists, **When** the batch is processed, **Then** the existing user is skipped with a warning, an enrollment event is published for them, and no duplicate account is created.

---

### User Story 4 - Clean up dead code and unused artifacts (Priority: P2)

After removing the Enroll path and consolidating handlers, all orphaned code must be deleted: unused view models, views, validator classes, command records, and any other artifacts that no longer have references.

**Why this priority**: Dead code increases cognitive load, confuses new developers, and can mask real issues during searches and refactoring.

**Independent Test**: After cleanup, the solution builds with zero warnings, all tests pass, and no unreferenced registration-related types remain.

**Acceptance Scenarios**:

1. **Given** the solution after consolidation, **When** building with `TreatWarningsAsErrors`, **Then** zero warnings are produced.
2. **Given** the codebase after cleanup, **When** searching for removed types (e.g., `EnrollUserViewModel`, unused command records), **Then** no references exist.
3. **Given** the existing test suite, **When** all tests are run, **Then** they pass (updating any tests that referenced removed types).

---

### Edge Cases

- What happens when the shared service is called without a project ID for admin/batch paths? Validation must reject it before reaching the service.
- How does the batch handler handle a row where the email belongs to a different person (different country+ID)? This should remain an error, not a skip.
- What happens if the same email appears twice within a single batch CSV? The second row should fail uniqueness within the batch processing loop.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST remove the `UsersController.Enroll` action, its GET counterpart, the `EnrollUserViewModel`, and the `Enroll.cshtml` view.
- **FR-002**: System MUST extract the shared registration sequence (uniqueness checks, password hashing, user creation via `User.Register`, optional verification token generation, persistence, configuration reads, and `UserRegisteredEvent` publishing) into a single shared application service.
- **FR-003**: The shared service MUST accept parameters that distinguish between registration modes: whether email verification is required/skipped/configurable, whether a project is associated, the enrollment variant string, and whether a password is provided or must be auto-generated.
- **FR-004**: `RegisterUserHandler` (public registration) MUST delegate to the shared service with: verification = always required, project = none, variant = "SelfRegistration", password = user-provided.
- **FR-005**: `RegisterInternalUserHandler` (admin registration) MUST delegate to the shared service with: verification = configurable, project = required, variant = "FullFlow", password = user-provided, FirstName/LastName = optional.
- **FR-006**: `BatchRegisterUsersHandler` (batch registration) MUST delegate to the shared service for each new user with: verification = skipped (auto-verified), project = required, variant = "FullFlow", password = auto-generated with reset required.
- **FR-007**: The batch handler MUST retain its existing behavior for rows where the user already exists: publish the enrollment event and return "Skipped" status with appropriate warnings.
- **FR-008**: All existing validators MUST continue to enforce their current rules (the `RegisterUserValidator` requires FirstName/LastName; the `RegisterInternalUserValidator` does not; the `BatchRegisterUsersValidator` enforces per-row and max-row rules).
- **FR-009**: All orphaned code (unused commands, validators, view models, views, result records) MUST be deleted after consolidation.
- **FR-010**: The solution MUST build with zero warnings after all changes.

### Key Entities

- **User**: The domain aggregate created via `User.Register()` factory method. Central to all registration flows.
- **UserRegisteredEvent**: The integration event published after successful registration, consumed by the Tenant module for enrollment and by the notification system for welcome emails.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The registration-related code (handlers + shared service) has zero duplicated business logic sequences — uniqueness checks, user creation, and event publishing each appear in exactly one location.
- **SC-002**: Adding a new field to the registration event or a new uniqueness check requires changing exactly one file (the shared service), not three.
- **SC-003**: The admin panel presents exactly one "register user" flow, not two.
- **SC-004**: All three registration paths (public, admin, batch) produce correct results: the right verification state, the right project association, and the right enrollment variant.
- **SC-005**: The solution builds with zero warnings and all existing tests pass after consolidation.
- **SC-006**: No dead registration-related code (unreferenced commands, view models, views, validators) remains in the codebase.

## Assumptions

- The `RegisterInternal` action and its view will be renamed or repurposed as the sole admin registration entry point. The UI label may change (e.g., from "Registrar Interno" to "Registrar Usuario") but the functional behavior remains.
- The existing `User.Register()` domain factory method signature is stable and does not need modification.
- The batch handler's `GenerateTemporaryPassword()` method and existing-user-skip logic are correct and do not need redesign — only relocation into or alongside the shared service.
- The `UserRegisteredEvent` record structure is stable; only the construction site changes (from three handlers to one service).
- Existing tests for `RegisterUserHandler` and validators will be updated to reflect the new delegation pattern, not deleted.
