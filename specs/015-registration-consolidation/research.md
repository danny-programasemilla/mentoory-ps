# Research: Registration Consolidation

**Feature**: 015-registration-consolidation  
**Date**: 2026-04-14

## R1: Application Service Patterns in the Codebase

**Decision**: Create `IUserRegistrationService` as an Application-layer service interface with its implementation in the Application project.

**Rationale**: The codebase already uses Application Service interfaces for cross-cutting orchestration:
- `IIntegrationEventService` (in `Shared.Application`) with `MediatRIntegrationEventService` implementation
- `IAuditService` (in `Shared.Application`)
- `INotificationQueueService` (in `Notification.Application`)

The registration service orchestrates domain interfaces (`IUserRepository`, `IPasswordHasher`) and application abstractions (`ITimeProvider`, `ISystemConfigurationReader`, `IIntegrationEventService`). Since all dependencies are Domain/Application interfaces — no Infrastructure types — the implementation belongs in the Application layer.

**Alternatives considered**:
- **Domain Service**: Rejected — the service depends on application-layer abstractions (`ISystemConfigurationReader`, `IIntegrationEventService`) which Domain must not reference.
- **Infrastructure Service**: Rejected — no infrastructure-specific types are needed (no DbContext, no external API clients).
- **Inline in handlers (status quo)**: Rejected — 3 handlers duplicate the same 7-step sequence.

## R2: Handler Return Types and Service Design

**Decision**: The shared service returns a `UserRegistrationResult` record containing the created User's essential data (Id, ExternalId, Email, AccountStatus, CreatedAtUtc) plus an optional generated password. Each handler transforms this into its command-specific result.

**Rationale**: The three handlers return different types:
- `RegisterUserHandler` → `Result` (void)
- `RegisterInternalUserHandler` → `Result<RegisterInternalUserResult>` (ExternalId + EnrollmentStatus)
- `BatchRegisterUsersHandler` → `Result<BatchRegistrationResult>` (per-row results with TemporaryPassword)

A unified return type at the service level provides all the data each handler needs, without coupling them.

**Alternatives considered**:
- **Return the full User entity**: Rejected — leaks domain aggregate outside the service boundary. Returning a lightweight result record is cleaner.
- **Different service methods per handler**: Rejected — defeats the purpose of consolidation.

## R3: Batch Handler's Existing-User Logic

**Decision**: The existing-user check and skip/warn logic stays in `BatchRegisterUsersHandler`. The shared service handles only "register one new user."

**Rationale**: The batch handler has unique orchestration for existing users:
1. Look up user by country+ID
2. If found: check email mismatch → warn, publish enrollment event → return "Skipped"
3. If not found: register new user via the shared service

This is batch-specific workflow, not shared registration logic. The `GenerateTemporaryPassword()` helper also stays in the batch handler (or is passed to the service as the password parameter).

**Alternatives considered**:
- **Move existing-user check into shared service**: Rejected — public and admin registration should fail on duplicates (not skip), so the behavior diverges.

## R4: Enroll Action Redundancy Confirmation

**Decision**: Remove `UsersController.Enroll` (GET + POST), `EnrollUserViewModel`, and `Enroll.cshtml`.

**Rationale**: Investigation confirms:
- `Enroll` uses `RegisterUserCommand` (public handler) — no project association, always requires email verification, variant "SelfRegistration"
- `RegisterInternal` uses `RegisterInternalUserCommand` — project association, configurable verification, variant "FullFlow"
- `RegisterInternal` is the correct admin path; `Enroll` is a legacy mistake that creates users without proper project binding
- Both actions live in the same controller (`UsersController`) under the same `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]`

**Alternatives considered**:
- **Keep Enroll but fix it**: Rejected — `RegisterInternal` already does everything correctly. Keeping two admin forms creates UX confusion.

## R5: Test Coverage Strategy

**Decision**: Update existing `RegisterUserHandlerTests` to verify delegation to the shared service. Add new tests for `UserRegistrationService` covering the shared logic. No new tests for `RegisterInternalUserHandler` or `BatchRegisterUsersHandler` in this scope (they have no tests currently).

**Rationale**: The existing tests verify:
- Valid data → success + persistence
- Duplicate email → field-specific error
- Duplicate national ID → field-specific error
- Password hashing called

After consolidation, these tests should target the shared service (where the logic now lives). The handler tests should verify correct delegation (correct parameters passed to service).

**Alternatives considered**:
- **Add full test suites for all 3 handlers**: Out of scope for this refactoring — the spec explicitly focuses on consolidation, not expanding test coverage.

## R6: Naming Consistency — NationalId vs Identification

**Decision**: Leave the field naming discrepancy as-is (out of scope).

**Rationale**: Investigation found an inconsistency:
- `RegisterUserCommand` uses `NationalId`
- `RegisterInternalUserCommand` uses `Identification`
- `BatchUserRow` uses `Identification`
- The domain method `User.Register()` uses `nationalId`

Renaming across commands, validators, view models, and views is a separate concern. The shared service will use `nationalId` (matching the domain) and each handler maps from its own naming.
