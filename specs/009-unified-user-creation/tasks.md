# Tasks: Unified User Creation with Configurable Onboarding

**Input**: Design documents from `/specs/009-unified-user-creation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested. Test tasks omitted — add during implementation if needed.

**Organization**: Tasks grouped by user story. Each story is independently testable after its phase completes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1-US8)
- All paths are relative to repository root

---

## Phase 1: Setup

**Purpose**: Verify prerequisites and branch readiness

- [ ] T001 Verify spec 008 (context selector) is implemented — check `ContextController.Select` exists and `GetActiveProjectId()` returns data
- [ ] T002 Verify clean build on branch 009-unified-user-creation with `dotnet build`

**Checkpoint**: Branch is clean, spec 008 dependency confirmed

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain changes to the invitation infrastructure that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [ ] T003 Add `RequiresAcceptance` (bool, default true) property to `ProjectInvitation.Create()` factory method in `Mentoory.Tenant.Domain/Aggregates/ProjectInvitation/ProjectInvitation.cs`
- [ ] T004 [P] Add `RequiresAcceptance` column (BIT NOT NULL DEFAULT 1) to `Mentoory.Db/tenant/Tables/ProjectInvitations.sql`
- [ ] T005 Add `RequiresAcceptance` parameter to `CreateInvitationCommand` record in `Mentoory.Tenant.Application/Invitations/Commands/CreateInvitation/CreateInvitationCommand.cs`
- [ ] T006 Pass `RequiresAcceptance` through `CreateInvitationHandler` to `ProjectInvitation.Create()` in `Mentoory.Tenant.Application/Invitations/Commands/CreateInvitation/CreateInvitationHandler.cs`
- [ ] T007 Modify `UserRegisteredEventHandler` to compute `RequiresAcceptance` from `EnrollmentVariant` (FullFlow=true, Bypass=false) and pass to `CreateInvitationCommand` in `Mentoory.Tenant.Application/Enrollment/IntegrationEventHandlers/UserRegisteredEventHandler.cs`
- [ ] T008 Modify `UserEmailVerifiedEventHandler` to check `invitation.RequiresAcceptance` instead of `project.EnrollmentVariant` for auto-accept logic in `Mentoory.Tenant.Application/Invitations/IntegrationEventHandlers/UserEmailVerifiedEventHandler.cs`
- [ ] T009 Verify build passes with `dotnet build` — zero warnings

**Checkpoint**: Invitation infrastructure supports RequiresAcceptance flag. Existing tests still pass.

---

## Phase 3: US1 + US2 + US3 — Individual User Creation with Session Context (Priority: P1) MVP

**Goal**: Admin can create a user via a unified form with onboarding toggles, using session context for project binding. Both toggles OFF triggers full onboarding (US1); both ON creates a fully active user with temp password (US2). Form is blocked without active project (US3).

**Independent Test**: Navigate to `/Administration/Users/Create`, verify session context enforcement. Create a user with both toggles OFF — verify user created with PendingVerification. Create a user with both toggles ON — verify temp password displayed and user is Active.

### Application Layer

- [ ] T010 [P] [US1] Create `CreateUserCommand` record and `CreateUserResult` record in `Mentoory.Access.Application/Commands/CreateUser/CreateUserCommand.cs` — fields per contracts/internal-contracts.md
- [ ] T011 [P] [US1] Create `CreateUserCommandValidator` with FluentValidation rules (email format, required fields, country validation) in `Mentoory.Access.Application/Commands/CreateUser/CreateUserCommandValidator.cs`
- [ ] T012 [US1] Create `CreateUserCommandHandler` in `Mentoory.Access.Application/Commands/CreateUser/CreateUserCommandHandler.cs` — check existing user, create or enroll, hash password (temp when both ON), publish `UserRegisteredEvent` with toggle-derived fields (RequiresVerification=!SkipEmailVerification, EnrollmentVariant=SkipInvitationAcceptance?"Bypass":"FullFlow")
- [ ] T013 [P] [US1] Create `SetInitialPasswordCommand` record in `Mentoory.Access.Application/Commands/SetInitialPassword/SetInitialPasswordCommand.cs` — UserExternalId, Token, TokenType (Verification/Invitation), NewPassword, ConfirmPassword
- [ ] T014 [P] [US1] Create `SetInitialPasswordCommandValidator` in `Mentoory.Access.Application/Commands/SetInitialPassword/SetInitialPasswordCommandValidator.cs` — password min 12 chars, match confirmation
- [ ] T015 [US1] Create `SetInitialPasswordCommandHandler` in `Mentoory.Access.Application/Commands/SetInitialPassword/SetInitialPasswordCommandHandler.cs` — validate token, set password hash, activate account (if verification token), mark token used

### Web Layer — Admin Form

- [ ] T016 [P] [US3] Create `CreateUserViewModel` in `Mentoory.Web/Areas/Administration/Models/CreateUserViewModel.cs` — Email, Country, Identification, FirstName, LastName, SkipEmailVerification, SkipInvitationAcceptance, CountryList
- [ ] T017 [US1] Add `Create` GET and POST actions to `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — GET loads countries + checks session context, POST dispatches `CreateUserCommand` with ProjectExternalId/IncubatorExternalId from claims
- [ ] T018 [US3] Add session context enforcement to `Create` GET action — if `GetActiveProjectId()` is null, set ViewBag flag to disable form and show alert "Seleccione un proyecto desde el selector de contexto para continuar"
- [ ] T019 [US1] Create unified form view `Mentoory.Web/Areas/Administration/Views/Users/Create.cshtml` — Email, Country dropdown, Identification, FirstName, LastName, two toggle switches (Bootstrap 5), project name display from session context, submit button
- [ ] T020 [P] [US1] Create `Mentoory.Web/wwwroot/js/user-creation.js` — country-dependent identification mask (reuse existing pattern from Register), toggle switch visual feedback, form validation
- [ ] T021 [US2] Display temp password in success result on `Create` POST response — show alert with generated password when both toggles ON, show onboarding status message when toggles OFF

### Web Layer — Public Onboarding Pages

- [ ] T022 [US1] Create `OnboardingController` in `Mentoory.Web/Areas/Access/Controllers/OnboardingController.cs` — `[AllowAnonymous]`, actions: VerifyEmail (GET/POST), AcceptInvitation (GET/POST), Expired (GET)
- [ ] T023 [US1] Implement `VerifyEmail` GET action — validate token, show password setup form with project name. If token expired, redirect to Expired page.
- [ ] T024 [US1] Implement `VerifyEmail` POST action — dispatch `SetInitialPasswordCommand` (TokenType=Verification), on success redirect to login or next onboarding step
- [ ] T025 [US1] Create `Mentoory.Web/Areas/Access/Views/Onboarding/VerifyEmail.cshtml` — password field, confirm password field, project info, submit button. Spanish labels.
- [ ] T026 [US1] Implement `AcceptInvitation` GET action — validate invitation token via `GetInvitationDetailsQuery`, show project name + accept button + password form (if user has no password yet)
- [ ] T027 [US1] Implement `AcceptInvitation` POST action — dispatch `SetInitialPasswordCommand` (if needed) + `AcceptInvitationCommand`, on success redirect to login
- [ ] T028 [US1] Create `Mentoory.Web/Areas/Access/Views/Onboarding/AcceptInvitation.cshtml` — project name, "Aceptar invitacion" button, conditional password form, Spanish labels
- [ ] T029 [P] [US1] Create `Mentoory.Web/Areas/Access/Views/Onboarding/Expired.cshtml` — "Invitacion expirada" message with instructions to contact admin

- [ ] T030 Verify build passes with `dotnet build` — zero warnings

**Checkpoint**: US1 (full onboarding), US2 (bypass), and US3 (context enforcement) are all functional. Admin can create users via the unified form.

---

## Phase 4: US4 — Batch Upload with Onboarding Toggles (Priority: P2)

**Goal**: Admin can batch upload a CSV with global onboarding toggles. Project from session context. Results show per-row Creado/Inscrito/Error.

**Independent Test**: Upload a 5-row CSV with both toggles OFF — verify all users created with PendingVerification. Upload again with both ON — verify temp passwords in results.

### Application Layer

- [ ] T031 [P] [US4] Create `BatchCreateUsersCommand` record and `BatchCreateUsersResult` record in `Mentoory.Access.Application/Commands/BatchCreateUsers/BatchCreateUsersCommand.cs` — fields per contracts/internal-contracts.md
- [ ] T032 [P] [US4] Create `BatchCreateUsersCommandValidator` in `Mentoory.Access.Application/Commands/BatchCreateUsers/BatchCreateUsersCommandValidator.cs` — max 500 rows, required ProjectExternalId
- [ ] T033 [US4] Create `BatchCreateUsersCommandHandler` in `Mentoory.Access.Application/Commands/BatchCreateUsers/BatchCreateUsersCommandHandler.cs` — iterate rows, reuse CreateUser logic (check existing, create or enroll), collect per-row results with Creado/Inscrito/Error outcomes

### Web Layer

- [ ] T034 [US4] Modify `BatchUploadController.Index` GET action in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs` — remove project dropdown fetch, add session context enforcement, pass toggle defaults to view
- [ ] T035 [US4] Modify `BatchUploadController.Index` POST action — read toggles from form, read ProjectExternalId from session context claims, dispatch `BatchCreateUsersCommand`
- [ ] T036 [US4] Modify `Mentoory.Web/Areas/Administration/Views/BatchUpload/Index.cshtml` — remove project dropdown, add two toggle switches, add session context enforcement alert (same pattern as Create form)
- [ ] T037 [US4] Update `Mentoory.Web/Areas/Administration/Views/BatchUpload/Results.cshtml` — replace "Exitoso"/"Omitido" with "Creado"/"Inscrito"/"Error", show temp passwords only when both toggles ON, update summary cards

- [ ] T038 Verify build passes with `dotnet build` — zero warnings

**Checkpoint**: US4 is functional. Admin can batch upload with toggles and see correct per-row results.

---

## Phase 5: US5 + US6 — Mixed Toggles & Existing User Handling (Priority: P2)

**Goal**: All 4 toggle combinations work correctly (US5). Existing users are auto-enrolled without duplication (US6).

**Independent Test**: US5 — create user with skip verification ON + skip invitation OFF, verify immediate invitation email trigger. US6 — create a user twice for different projects, verify enrollment without duplication.

### Existing User Detection (US6)

- [ ] T039 [US6] Enhance `CreateUserCommandHandler` — when user already exists, check if already enrolled in target project. If not enrolled, publish `UserRegisteredEvent` for enrollment. Return `Enrolled` or `AlreadyEnrolled` outcome. Handle Locked/Disabled accounts with warning message.
- [ ] T040 [US6] Enhance `BatchCreateUsersCommandHandler` — same existing user detection per row. Map to "Inscrito" status in results. Skip verification toggle for already-verified users (only invitation toggle applies).
- [ ] T041 [US6] Update Create.cshtml success display — show different messages for Created vs Enrolled vs AlreadyEnrolled outcomes, include account status warnings

### Mixed Toggle Verification (US5)

- [ ] T042 [US5] Verify Case 3 (skip verification ON, skip invitation OFF) works end-to-end — user auto-verified, invitation created with RequiresAcceptance=true, invitation acceptance page works
- [ ] T043 [US5] Verify Case 4 (skip verification OFF, skip invitation ON) works end-to-end — user PendingVerification, invitation created with RequiresAcceptance=false, after verification auto-accepts and enrolls

- [ ] T044 Verify build passes with `dotnet build` — zero warnings

**Checkpoint**: All 4 toggle combinations verified. Existing user handling works for both individual and batch flows.

---

## Phase 6: US7 — Onboarding Status on Users List (Priority: P3)

**Goal**: Admin users list shows a combined onboarding status column computed from AccountStatus + ProjectInvitation state relative to active project. Filterable.

**Independent Test**: Create users in different onboarding states, verify status column accuracy and filtering.

- [ ] T045 [US7] Extend `IncubatorMemberListItemDto` with `OnboardingStatus` (string) field in `Mentoory.Access.Application/Queries/ListIncubatorMembers/`
- [ ] T046 [US7] Modify `ListIncubatorMembersHandler` — after fetching users, query ProjectInvitation status for each user in the active project (via cross-domain query or Tenant application query). Compute combined status: "Pendiente verificacion" / "Pendiente invitacion" / "Pendiente ambos" / "Activo"
- [ ] T047 [US7] Add `OnboardingStatus` column to DataTable in `Mentoory.Web/Areas/Administration/Views/Users/Index.cshtml` — display with appropriate badge colors
- [ ] T048 [US7] Add sort expression for OnboardingStatus in `ListIncubatorMembersHandler.SortColumns` dictionary
- [ ] T049 [US7] Add filter support for OnboardingStatus — dropdown filter in DataTable header or server-side filter parameter

- [ ] T050 Verify build passes with `dotnet build` — zero warnings

**Checkpoint**: US7 complete. Admin can see and filter onboarding status on the users list.

---

## Phase 7: US8 — Invitation Token Management (Priority: P3)

**Goal**: Expired tokens show appropriate message. Admins can regenerate invitation tokens from user detail.

**Independent Test**: Access an expired invitation link — verify "Invitacion expirada" page. Click "Reenviar invitacion" — verify new token created.

- [ ] T051 [US8] Add "Reenviar invitacion" button to user detail view — dispatches `ReissueInvitationCommand` (existing handler) via POST to `/Administration/Users/ReissueInvitation`
- [ ] T052 [US8] Add `ReissueInvitation` POST action to `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — fetch pending invitation for user+project, dispatch `ReissueInvitationCommand`
- [ ] T053 [US8] Verify `OnboardingController.AcceptInvitation` GET correctly detects expired tokens and redirects to Expired page
- [ ] T054 [US8] Verify `OnboardingController.VerifyEmail` GET correctly detects expired tokens and redirects to Expired page

- [ ] T055 Verify build passes with `dotnet build` — zero warnings

**Checkpoint**: US8 complete. Token lifecycle management works.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Backward compatibility, cleanup, and final verification

- [ ] T056 [P] Add redirect from `/Administration/Users/Enroll` to `/Administration/Users/Create` in `UsersController`
- [ ] T057 [P] Add redirect from `/Administration/Users/RegisterInternal` to `/Administration/Users/Create` in `UsersController`
- [ ] T058 Remove or mark as obsolete the old `RegisterInternalUserCommand`, `RegisterInternalUserHandler`, `RegisterInternalUserCommandValidator` (keep `RegisterUserCommand` for public registration)
- [ ] T059 [P] Update `MenuConfiguration.cs` — ensure "Usuarios" link under Administration points to updated controller actions
- [ ] T060 Verify public self-registration flow (`/Access/Register`) is completely unaffected by all changes
- [ ] T061 Run full test suite with `dotnet test` — all existing tests pass
- [ ] T062 Run `dotnet build` final verification — zero warnings across all projects

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — verify prerequisites
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1+US2+US3 (Phase 3)**: Depends on Phase 2 — core MVP
- **US4 (Phase 4)**: Depends on Phase 2 — can run in parallel with Phase 3
- **US5+US6 (Phase 5)**: Depends on Phase 3 (needs CreateUserCommandHandler)
- **US7 (Phase 6)**: Depends on Phase 2 — can run in parallel with Phase 3
- **US8 (Phase 7)**: Depends on Phase 3 (needs OnboardingController)
- **Polish (Phase 8)**: Depends on all user story phases complete

### User Story Dependencies

- **US1+US2+US3 (P1)**: After Foundational — no dependencies on other stories
- **US4 (P2)**: After Foundational — largely independent, shares command patterns with US1
- **US5+US6 (P2)**: After US1+US2 — extends CreateUserCommandHandler logic
- **US7 (P3)**: After Foundational — independent query/view work, can parallel with US1
- **US8 (P3)**: After US1 — needs OnboardingController to exist

### Within Each User Story

- Application layer (commands/handlers) before web layer (controllers/views)
- Domain changes before application changes
- Core implementation before edge case handling

### Parallel Opportunities

- T003 and T004 (entity + SQL) can run in parallel
- T010, T011, T013, T014, T016 (command records + validators + view models) can all run in parallel
- T031, T032 (batch command + validator) can run in parallel
- Phase 4 (US4) and Phase 6 (US7) can run in parallel with Phase 3
- T056, T057, T059 (redirects + menu) can all run in parallel

---

## Parallel Example: Phase 3 (US1+US2+US3)

```bash
# Launch all command/validator records in parallel:
Task T010: "Create CreateUserCommand + CreateUserResult records"
Task T011: "Create CreateUserCommandValidator"
Task T013: "Create SetInitialPasswordCommand record"
Task T014: "Create SetInitialPasswordCommandValidator"
Task T016: "Create CreateUserViewModel"

# Then sequentially (depend on above):
Task T012: "Create CreateUserCommandHandler" (needs T010)
Task T015: "Create SetInitialPasswordCommandHandler" (needs T013)
Task T017: "Add Create actions to UsersController" (needs T016, T012)

# Then UI in parallel:
Task T019: "Create unified form view" (needs T017)
Task T020: "Create user-creation.js"
Task T029: "Create Expired.cshtml"

# Then onboarding pages:
Task T022-T028: "OnboardingController + views" (needs T015)
```

---

## Implementation Strategy

### MVP First (Phase 1 + 2 + 3)

1. Complete Phase 1: Setup verification
2. Complete Phase 2: Foundational domain changes (RequiresAcceptance)
3. Complete Phase 3: US1+US2+US3 (individual creation + toggles + context enforcement)
4. **STOP and VALIDATE**: Test all 4 toggle combinations via unified form
5. This delivers the core value — unified admin user creation with configurable onboarding

### Incremental Delivery

1. Setup + Foundational → Infrastructure ready
2. US1+US2+US3 → Individual creation works → **MVP**
3. US4 → Batch upload works → Major milestone
4. US5+US6 → Edge cases handled → Feature-complete for creation
5. US7 → Admin visibility → Operational readiness
6. US8 → Token management → Production-ready
7. Polish → Backward compatibility → Ship

### Parallel Team Strategy

With multiple developers after Phase 2 completes:
- Developer A: Phase 3 (US1+US2+US3) — application + admin forms
- Developer B: Phase 4 (US4) — batch upload
- Developer C: Phase 6 (US7) — onboarding status query + DataTable

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- Email delivery infrastructure does not exist yet — token generation and events work but emails are not physically sent. This is a known limitation, not a blocker.
- All Spanish text must use proper accent marks (verificacion → verificación, invitación, etc.)
- Constitution: zero warnings, ITimeProvider for dates, ExternalId on routes, Spanish UI
- Commit after each task or logical group
