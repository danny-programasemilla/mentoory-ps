# Tasks: Registration Consolidation

**Input**: Design documents from `/specs/015-registration-consolidation/`  
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: Test tasks included — existing tests must be updated and new service tests added per plan Phase 4.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No setup needed — project structure already exists. All changes are within existing projects.

*(No tasks — skip to Foundational)*

---

## Phase 2: Foundational (Shared Service Creation)

**Purpose**: Create the `IUserRegistrationService` and all supporting types. This MUST complete before any handler refactoring (US2) can begin. US1 (Remove Enroll) does NOT depend on this phase.

**CRITICAL**: No handler refactoring (Phase 4) can begin until this phase is complete.

- [x] T001 [P] Create `EmailVerificationMode` enum in `Mentoory.Access.Application/Services/EmailVerificationMode.cs` — values: `Required`, `Skipped`
- [x] T002 [P] Create `UserRegistrationRequest` record in `Mentoory.Access.Application/Services/UserRegistrationRequest.cs` — fields: Email, Country, NationalId, FirstName, LastName, Password, ProjectExternalId (Guid?), EmailVerificationMode, EnrollmentVariant (string), RequirePasswordReset (bool)
- [x] T003 [P] Create `UserRegistrationResult` record in `Mentoory.Access.Application/Services/UserRegistrationResult.cs` — fields: UserId (int), UserExternalId (Guid), Email (string), AccountStatus (string), CreatedAtUtc (DateTime)
- [x] T004 Create `IUserRegistrationService` interface in `Mentoory.Access.Application/Services/IUserRegistrationService.cs` — method: `RegisterAsync(UserRegistrationRequest, CancellationToken)` returning `Task<Result<UserRegistrationResult>>` (depends on T001-T003)
- [x] T005 Create `UserRegistrationService` implementation in `Mentoory.Access.Application/Services/UserRegistrationService.cs` — extract the shared 10-step registration sequence from existing handlers: normalize email, check uniqueness (country+ID then email), hash password, `User.Register()`, verification mode handling (Required → generate token, Skipped → AdminVerifyEmail), optional `SetPasswordResetRequired`, persist, read invitation expiry config, publish `UserRegisteredEvent`, return result. Dependencies: `IUserRepository`, `IPasswordHasher`, `ITimeProvider`, `ISystemConfigurationReader`, `IIntegrationEventService`, `ILogger<UserRegistrationService>` (depends on T004)
- [x] T006 Register `IUserRegistrationService` → `UserRegistrationService` as scoped in `Mentoory.Access.Application/DependencyInjection.cs` or the appropriate `AddAccessApplication` method (depends on T005)

**Checkpoint**: Shared service exists and compiles. Handlers not yet refactored — existing behavior unchanged.

---

## Phase 3: User Story 1 — Remove Redundant Admin Enrollment Path (Priority: P1)

**Goal**: Eliminate the legacy `Enroll` admin action that incorrectly uses the public registration handler, leaving `RegisterInternal` as the sole admin registration flow.

**Independent Test**: After completion, `/Administration/Users/Enroll` returns 404, the admin panel has one registration flow, and no references to `EnrollUserViewModel` or `Enroll.cshtml` exist.

**Note**: This phase has NO dependency on Phase 2 (Foundational). It can be executed in parallel with Phase 2 if desired.

### Implementation for User Story 1

- [x] T007 [US1] Remove `Enroll()` GET action (lines 55-59) and `Enroll()` POST action (lines 61-99) from `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — also remove the `using Mentoory.Access.Application.Commands.RegisterUser;` import if no longer referenced
- [x] T008 [US1] Remove `EnrollUserViewModel` class from `Mentoory.Web/Areas/Administration/Models/UserViewModels.cs` (lines 5-43) — if the file becomes empty after removal, delete the entire file
- [x] T009 [US1] Delete view file `Mentoory.Web/Areas/Administration/Views/Users/Enroll.cshtml`
- [x] T010 [US1] Search for and remove any navigation links or menu items referencing the Enroll action — search patterns: `asp-action="Enroll"`, `Url.Action("Enroll"`, `"Enroll"` in `MenuConfiguration.cs` or `_Layout.cshtml` files within the Administration area

**Checkpoint**: Enroll path fully removed. `RegisterInternal` is the only admin registration action. Build should compile (the removed code has no dependents).

---

## Phase 4: User Story 2 — Consolidate Shared Registration Logic (Priority: P1)

**Goal**: Refactor all three remaining handlers to delegate to `IUserRegistrationService`, eliminating duplicated registration logic.

**Independent Test**: Each handler's constructor takes `IUserRegistrationService` instead of 5-6 individual dependencies. The core registration sequence (uniqueness, hashing, creation, verification, persistence, event) exists only in `UserRegistrationService`.

**Depends on**: Phase 2 (Foundational) must be complete.

### Implementation for User Story 2

- [x] T011 [US2] Refactor `RegisterUserHandler` in `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserHandler.cs` — replace constructor dependencies with `IUserRegistrationService` + `ILogger`. Replace handler body: construct `UserRegistrationRequest` with (verification = `Required`, project = `null`, variant = `"SelfRegistration"`, requirePasswordReset = `false`), call `_registrationService.RegisterAsync()`, propagate failures, return `Success()` on success
- [x] T012 [US2] Refactor `RegisterInternalUserHandler` in `Mentoory.Access.Application/Commands/RegisterInternalUser/RegisterInternalUserHandler.cs` — replace constructor dependencies with `IUserRegistrationService` + `ILogger`. Replace handler body: construct `UserRegistrationRequest` with (verification = `Required` or `Skipped` based on `request.RequireEmailVerification`, project = `request.ProjectExternalId`, variant = `"FullFlow"`, requirePasswordReset = `false`), call service, transform result to `RegisterInternalUserResult(result.UserExternalId, enrollmentStatus)` where enrollmentStatus = "PendingVerification" if verification required, "Enrolled" otherwise
- [x] T013 [US2] Refactor `BatchRegisterUsersHandler` in `Mentoory.Access.Application/Commands/BatchRegisterUsers/BatchRegisterUsersHandler.cs` — keep: row iteration loop, existing-user check/skip/warn logic, `GenerateTemporaryPassword()`, `BatchRowResult` construction, `IUserRepository` (for existing-user lookups), `IIntegrationEventService` (for existing-user event publishing). Replace the "register new user" branch: generate temp password, construct `UserRegistrationRequest` with (verification = `Skipped`, project = `request.ProjectExternalId`, variant = `"FullFlow"`, requirePasswordReset = `true`), call service. Remove dependencies no longer needed: `IPasswordHasher`, `ITimeProvider`, `ISystemConfigurationReader`

**Checkpoint**: All three handlers delegate to the shared service. Core registration logic exists in exactly one place. Build compiles with zero warnings.

---

## Phase 5: User Story 3 — Verify Distinct Registration Behaviors (Priority: P1)

**Goal**: Confirm that each registration path correctly constructs its `UserRegistrationRequest` with the right parameters, and the shared service correctly handles each mode.

**Independent Test**: Updated handler tests verify correct delegation. New service tests cover all verification modes, password reset, and event publishing.

**Depends on**: Phase 4 (US2) must be complete.

### Implementation for User Story 3

- [x] T014 [US3] Update `RegisterUserHandlerTests` in `tests/Mentoory.Access.Tests/Handlers/RegisterUserHandlerTests.cs` — mock `IUserRegistrationService` instead of individual dependencies. Test: handler constructs request with verification = `Required`, project = `null`, variant = `"SelfRegistration"`, requirePasswordReset = `false`. Test: service failure propagates correctly. Test: service success → handler returns `Success()`
- [x] T015 [US3] Create `UserRegistrationServiceTests` in `tests/Mentoory.Access.Tests/Services/UserRegistrationServiceTests.cs` — test core registration logic: (1) valid data → user created + event published + result returned, (2) duplicate email → failure with field-specific error on "Email", (3) duplicate national ID → failure with field-specific error on "NationalId", (4) password hashed before `User.Register`, (5) EmailVerificationMode.Required → token generated via `GenerateEmailVerificationToken`, (6) EmailVerificationMode.Skipped → `AdminVerifyEmail` called, (7) RequirePasswordReset=true → `SetPasswordResetRequired` called, (8) event includes correct ProjectExternalId and EnrollmentVariant

**Checkpoint**: All tests pass. Each registration path's distinct behavior is verified through tests.

---

## Phase 6: User Story 4 — Clean Up Dead Code and Verify (Priority: P2)

**Goal**: Remove all orphaned artifacts, verify zero warnings, run full test suite, confirm no dead code remains.

**Independent Test**: `dotnet build` produces zero warnings, `dotnet test` passes all tests, grep for removed types finds no references.

**Depends on**: Phases 3-5 (US1, US2, US3) must be complete.

### Implementation for User Story 4

- [x] T016 [US4] Run `dotnet build` and fix any compiler warnings or errors — ensure zero warnings with `TreatWarningsAsErrors`
- [x] T017 [US4] Run `dotnet test` and fix any failing tests — update integration tests in `tests/Mentoory.Tests.Integration/` if they reference removed types (e.g., `EnrollUserViewModel`, old handler constructor signatures)
- [x] T018 [US4] Dead code scan — search the entire codebase for: `EnrollUserViewModel`, `Enroll.cshtml`, orphaned `using` statements referencing removed types, unused constructor parameters in refactored handlers. Remove any findings.
- [x] T019 [US4] Verify DI resolution — confirm `IUserRegistrationService` resolves correctly by checking `tests/Mentoory.Tests.Integration/DependencyInjection/ServiceResolutionTests.cs` includes the new service (add `[InlineData(typeof(IUserRegistrationService))]` if the test uses this pattern)

**Checkpoint**: Solution builds clean, all tests pass, no dead code remains.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — can start immediately
- **US1 (Phase 3)**: No dependencies on Phase 2 — can start in parallel with Foundational
- **US2 (Phase 4)**: Depends on Phase 2 (Foundational) completion — BLOCKED until service exists
- **US3 (Phase 5)**: Depends on Phase 4 (US2) — needs refactored handlers to test
- **US4 (Phase 6)**: Depends on Phases 3, 4, 5 — final verification after all changes

### User Story Dependencies

- **US1 (P1)**: Independent — can start immediately, touches only Web layer files
- **US2 (P1)**: Depends on Foundational — touches Application layer handler files
- **US3 (P1)**: Depends on US2 — tests verify the refactored handlers
- **US4 (P2)**: Depends on US1 + US2 + US3 — final sweep

### Within Each Phase

- T001, T002, T003 can run in parallel (independent files)
- T004 depends on T001-T003 (interface references the types)
- T005 depends on T004 (implements the interface)
- T006 depends on T005 (registers the implementation)
- T007, T008, T009, T010 can run in parallel (different files, same goal)
- T011, T012, T013 touch different files but all depend on T006
- T014, T015 touch different test files and can run in parallel

### Parallel Opportunities

```
                    ┌─ T001 (enum)
Phase 2 start ──────┼─ T002 (request)     ──► T004 ──► T005 ──► T006
                    └─ T003 (result)                              │
                                                                  ▼
Phase 3 start ──► T007, T008, T009, T010 (all parallel)     Phase 4: T011, T012, T013
                              │                                   │
                              ▼                                   ▼
                         (US1 done)                    Phase 5: T014, T015 (parallel)
                              │                                   │
                              └──────────────┬────────────────────┘
                                             ▼
                                   Phase 6: T016 → T017 → T018 → T019
```

---

## Parallel Example: Foundational Phase

```text
# Launch all type definitions together:
Task: "Create EmailVerificationMode enum in Mentoory.Access.Application/Services/EmailVerificationMode.cs"
Task: "Create UserRegistrationRequest record in Mentoory.Access.Application/Services/UserRegistrationRequest.cs"
Task: "Create UserRegistrationResult record in Mentoory.Access.Application/Services/UserRegistrationResult.cs"

# Then sequentially:
Task: "Create IUserRegistrationService interface" (depends on above)
Task: "Create UserRegistrationService implementation" (depends on interface)
Task: "Register in DI" (depends on implementation)
```

## Parallel Example: US1 + US2 Overlap

```text
# US1 can run in parallel with Phase 2 (Foundational):
Phase 2: Creating shared service files (Application layer)
Phase 3: Removing Enroll path (Web layer — different files)

# Once Phase 2 completes, US2 begins while US1 may already be done
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 2: Foundational (shared service) + Phase 3: US1 (remove Enroll) — in parallel
2. Complete Phase 4: US2 (refactor handlers)
3. **STOP and VALIDATE**: Build compiles, handlers delegate correctly, Enroll is gone
4. Proceed to US3 (tests) and US4 (cleanup)

### Sequential Delivery

1. Phase 2 (Foundational) → Service exists, compiles
2. Phase 3 (US1) → Enroll removed, single admin path
3. Phase 4 (US2) → Handlers consolidated
4. Phase 5 (US3) → Tests verify behaviors
5. Phase 6 (US4) → Clean build, no dead code

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 is intentionally independent of US2 — they touch different layers (Web vs Application)
- US3 is a test/verification story, not an implementation story — its tasks are test-focused
- Commit after each phase checkpoint
- The `GenerateTemporaryPassword()` method stays in `BatchRegisterUsersHandler` — it's batch-specific, not shared
- The `NationalId` vs `Identification` naming inconsistency is out of scope (documented in research.md R6)
