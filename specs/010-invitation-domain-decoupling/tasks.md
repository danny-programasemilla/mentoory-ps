# Tasks: Invitation Domain Decoupling

**Input**: Design documents from `/specs/010-invitation-domain-decoupling/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested. Test tasks omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No project initialization needed — this is a refactoring of an existing codebase.

(No tasks in this phase)

---

## Phase 2: Foundational — Domain & Schema Cleanup

**Purpose**: Remove `TokenHash` from the domain model and delete the cross-domain bridge. These changes MUST complete before any user story work because they alter shared types (`ProjectInvitation.Create()` signature, deleted interfaces).

**CRITICAL**: No user story work can begin until this phase is complete.

- [x] T001 Remove `TokenHash` property and `tokenHash` parameter from `Create()` factory in `Mentoory.Tenant.Domain/Aggregates/ProjectInvitation/ProjectInvitation.cs`
- [x] T002 [P] Remove `[TokenHash] NVARCHAR(128) NOT NULL` column from `Mentoory.Db/tenant/Tables/ProjectInvitations.sql`
- [x] T003 [P] Remove `entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);` from `ConfigureProjectInvitation()` in `Mentoory.Tenant.Infrastructure/Persistence/TenantDbContext.cs`
- [x] T004 Remove token generation (`RandomNumberGenerator`) and update `ProjectInvitation.Create()` call (remove `tokenHash` argument) in `Mentoory.Tenant.Application/Invitations/Commands/CreateInvitation/CreateInvitationHandler.cs`
- [x] T005 [P] Delete file `Mentoory.Shared.Application/Interfaces/IInvitationTokenValidator.cs`
- [x] T006 [P] Delete file `Mentoory.Tenant.Infrastructure/Services/InvitationTokenValidator.cs`
- [x] T007 Remove `builder.Services.AddScoped<IInvitationTokenValidator, InvitationTokenValidator>();` registration from `Mentoory.Tenant.Infrastructure/DependencyInjection.cs`
- [x] T008 [P] Delete file `Mentoory.Access.Application/Commands/SetInitialPassword/TokenType.cs`

**Checkpoint**: Domain model simplified, cross-domain bridge deleted. Build will not pass yet — handler and command still reference removed types.

---

## Phase 3: User Story 1 — Invited User Sets Password via Verification Token (Priority: P1)

**Goal**: Simplify `SetInitialPasswordCommandHandler` to only validate `EmailVerificationToken`s. Remove all invitation concepts from the Access domain.

**Independent Test**: Create a user via admin, click email verification link, set password, confirm user is active and enrolled in project via the existing `UserEmailVerifiedEvent` flow.

### Implementation for User Story 1

- [x] T009 [US1] Remove `TokenType` property from `SetInitialPasswordCommand` record in `Mentoory.Access.Application/Commands/SetInitialPassword/SetInitialPasswordCommand.cs`
- [x] T010 [US1] Remove `IInvitationTokenValidator` dependency, remove `TokenType` switch statement, remove `HandleInvitationTokenAsync()` method — keep only verification token logic in `Mentoory.Access.Application/Commands/SetInitialPassword/SetInitialPasswordCommandHandler.cs`
- [x] T011 [US1] Update `OnboardingController` to remove `TokenType.Invitation` usage from any `SetInitialPasswordCommand` construction in `Mentoory.Web/Areas/Access/Controllers/OnboardingController.cs`
- [x] T012 [US1] Update `AcceptInvitation.cshtml` view — redirect invitation-based password setting to the verification token flow in `Mentoory.Web/Areas/Access/Views/Onboarding/AcceptInvitation.cshtml`

**Checkpoint**: `SetInitialPasswordCommandHandler` has a single validation path. Access domain has zero references to invitation concepts. Build should pass at this point.

---

## Phase 4: User Story 2 — Admin Reissues an Invitation (Priority: P1)

**Goal**: When an admin reissues an invitation, publish an `InvitationReissuedEvent` so Access generates a fresh `EmailVerificationToken` without any synchronous cross-domain call.

**Independent Test**: Let a user's verification token expire, admin triggers reissue, confirm a new `EmailVerificationToken` is generated in Access via the event, and the user can complete onboarding with the new token.

### Implementation for User Story 2

- [x] T013 [P] [US2] Create `InvitationReissuedEvent` record in `Mentoory.Shared.Application/IntegrationEvents/InvitationReissuedEvent.cs` with properties: `UserId` (long), `UserExternalId` (Guid), `InvitationExpiryHours` (int), inheriting from `IntegrationEvent`
- [x] T014 [US2] Update `ReissueInvitationHandler` — remove `RandomNumberGenerator` token generation, remove `tokenHash` from `ProjectInvitation.Create()` call, inject `IIntegrationEventService`, publish `InvitationReissuedEvent` after saving the new invitation in `Mentoory.Tenant.Application/Invitations/Commands/ReissueInvitation/ReissueInvitationHandler.cs`
- [x] T015 [US2] Create `InvitationReissuedEventHandler` in `Mentoory.Access.Application/IntegrationEvents/InvitationReissuedEventHandler.cs` — inject `IUserRepository`, `IPasswordHasher`, `ITimeProvider`; on event: look up user by `UserId`, generate raw token with `RandomNumberGenerator`, hash with `IPasswordHasher`, call `user.GenerateEmailVerificationToken(utcNow, tokenHash, event.InvitationExpiryHours)`, save user

**Checkpoint**: Reissue flow is fully event-driven. No synchronous cross-domain calls remain in the entire codebase.

---

## Phase 5: User Story 3 — User with Multiple Pending Invitations (Priority: P2)

**Goal**: Verify that multi-project enrollment continues to work unchanged when a user verifies their email.

**Independent Test**: Create a user with invitations to 3 projects (2 auto-accept, 1 manual). User sets password via verification token. Confirm 2 invitations are auto-accepted and 1 remains pending.

### Implementation for User Story 3

- [x] T016 [US3] Verify `UserEmailVerifiedEventHandler` in `Mentoory.Tenant.Application/Invitations/IntegrationEventHandlers/UserEmailVerifiedEventHandler.cs` correctly handles multiple pending invitations — no code changes expected, confirm behavior with manual testing or code review

**Checkpoint**: All user stories are independently functional. Multi-project enrollment works via the existing event-driven flow.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Build verification, cleanup, and final validation

- [x] T017 Run `dotnet build` and confirm zero warnings across all projects
- [x] T018 Run `dotnet test` and confirm all existing tests pass
- [x] T019 Remove any orphaned `using` statements referencing deleted types (`TokenType`, `IInvitationTokenValidator`) across all projects
- [x] T020 Run quickstart.md validation — manually verify full onboarding flow: admin creates user → email → set password → enrolled in project
- [x] T021 Run quickstart.md validation — manually verify reissue flow: token expires → admin reissues → new email → set password → enrolled

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A — no setup tasks
- **Foundational (Phase 2)**: No dependencies — start immediately. BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) completion
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) completion
- **User Story 3 (Phase 5)**: Depends on US1 and US2 completion (verification of combined behavior)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2). Independent of US2.
- **User Story 2 (P1)**: Can start after Foundational (Phase 2). Independent of US1.
- **User Story 3 (P2)**: Verification only — depends on US1 + US2 being complete to test the combined flow.

### Within Each Phase

- T001 must complete before T004 (Create() signature change)
- T005, T006, T008 can run in parallel (independent file deletions)
- T002, T003 can run in parallel (independent schema/config changes)
- T009 should precede T010 (command change before handler change)
- T013 must precede T014 and T015 (event definition before publisher/consumer)

### Parallel Opportunities

- In Phase 2: T002, T003, T005, T006, T008 are all parallelizable (different files)
- Between Phase 3 and Phase 4: US1 and US2 can run in parallel after foundational phase
- T013 (event creation) can start in parallel with Phase 3 tasks

---

## Parallel Example: Foundational Phase

```text
# These can all run in parallel (different files, no dependencies):
T002: Remove TokenHash column from Mentoory.Db/tenant/Tables/ProjectInvitations.sql
T003: Remove TokenHash config from TenantDbContext.cs
T005: Delete IInvitationTokenValidator.cs
T006: Delete InvitationTokenValidator.cs
T008: Delete TokenType.cs

# Then sequentially:
T001: Remove TokenHash from ProjectInvitation.cs (domain change)
T004: Update CreateInvitationHandler.cs (depends on T001)
T007: Remove DI registration (depends on T006)
```

## Parallel Example: User Stories

```text
# After Phase 2 completes, US1 and US2 can proceed in parallel:

# Developer A: User Story 1
T009: Simplify SetInitialPasswordCommand.cs
T010: Simplify SetInitialPasswordCommandHandler.cs
T011: Update OnboardingController.cs
T012: Update AcceptInvitation.cshtml

# Developer B: User Story 2
T013: Create InvitationReissuedEvent.cs
T014: Update ReissueInvitationHandler.cs
T015: Create InvitationReissuedEventHandler.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (domain cleanup + deletions)
2. Complete Phase 3: User Story 1 (simplify password setting)
3. **STOP and VALIDATE**: Build passes, Access domain has zero invitation references
4. Onboarding flow works end-to-end with verification tokens only

### Incremental Delivery

1. Complete Foundational → Domain model clean
2. Add User Story 1 → Password setting simplified → Build passes
3. Add User Story 2 → Reissue is event-driven → Full decoupling achieved
4. Verify User Story 3 → Multi-project enrollment confirmed
5. Polish → Zero warnings, all tests pass, manual validation complete

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- This is a refactoring — no new user-facing features, so no UI tests needed
- US3 has no code changes; it's a verification of existing behavior post-refactoring
- Build will break during Phase 2 and not recover until Phase 3 tasks complete — plan accordingly
- Commit after each phase checkpoint for clean rollback points
