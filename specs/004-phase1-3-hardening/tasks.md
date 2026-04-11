# Tasks: Phase 1–3 Hardening

**Input**: Design documents from `/specs/004-phase1-3-hardening/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — the hardening spec explicitly requires E2E validation and acceptance criteria verification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Database Schema & Seed Data)

**Purpose**: Create new database tables and seed reference data required by multiple user stories

- [x] T001 [P] Create SSDT table `Mentoory.Db/access/Tables/SystemConfigurations.sql` — Id, ExternalId, Key (NVARCHAR(100) UNIQUE), Value (NVARCHAR(500)), Description, DataType, CreatedAtUtc, UpdatedAtUtc
- [x] T002 [P] Create SSDT table `Mentoory.Db/access/Tables/Countries.sql` — Id, ExternalId, Name, Code (UNIQUE), IdentificationLabel, IdentificationMask, IdentificationRegex, IdentificationMaxLength, IsActive, CreatedAtUtc
- [x] T003 [P] Create SSDT table `Mentoory.Db/tenant/Tables/ProjectInvitations.sql` — Id, ExternalId, ProjectId (FK), UserId, TokenHash, Status (TINYINT), ExpiresAtUtc, AcceptedAtUtc, CreatedAtUtc, CreatedByUserId, IsActive; filtered unique index on (UserId, ProjectId) WHERE IsActive=1 AND Status=0; index on (ProjectId, Status) WHERE IsActive=1; index on (UserId) WHERE IsActive=1
- [x] T004 Modify SSDT table `Mentoory.Db/tenant/Tables/Projects.sql` — add IsPublic BIT NOT NULL DEFAULT 0 and EnrollmentVariant TINYINT NOT NULL DEFAULT 0
- [x] T005 [P] Create PostDeployment script `Mentoory.Db.PostDeployment/016.SeedCountries.sql` — seed Costa Rica (CRI, "Cedula Nacional", mask "0-0000-0000", regex, maxlen 11)
- [x] T006 [P] Create PostDeployment script `Mentoory.Db.PostDeployment/017.SeedSystemConfiguration.sql` — seed 7 default config entries (EmailVerificationTokenExpiryHours=24, PasswordResetTokenExpiryHours=1, InvitationTokenExpiryHours=72, MaxFailedLoginAttempts=5, LockoutDurationMinutes=15, SessionTimeoutHours=8, PasswordHistoryDepth=5)
- [x] T007 [P] Create PostDeployment script `Mentoory.Db.PostDeployment/018.SeedProjectPublicFlag.sql` — set IsPublic=0 for all existing projects

**Checkpoint**: Database schema and seed data ready for domain entity implementation

---

## Phase 2: Foundational (Domain Entities & Core Infrastructure)

**Purpose**: Create domain aggregates, repository implementations, and core infrastructure fixes that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain Entities

- [x] T008 [P] Create SystemConfiguration aggregate in `Mentoory.Access.Domain/Aggregates/SystemConfiguration/SystemConfiguration.cs` — ExternalId, Key, Value, Description, DataType, CreatedAtUtc, UpdatedAtUtc; factory method Create(); method Update(value, utcNow)
- [x] T009 [P] Create ConfigurationKey enum in `Mentoory.Access.Domain/Enums/ConfigurationKey.cs` — EmailVerificationTokenExpiryHours, PasswordResetTokenExpiryHours, InvitationTokenExpiryHours, MaxFailedLoginAttempts, LockoutDurationMinutes, SessionTimeoutHours, PasswordHistoryDepth
- [x] T010 [P] Create Country entity in `Mentoory.Access.Domain/Aggregates/Country/Country.cs` — ExternalId, Name, Code, IdentificationLabel, IdentificationMask, IdentificationRegex, IdentificationMaxLength, IsActive, CreatedAtUtc
- [x] T011 [P] Create ProjectInvitation aggregate in `Mentoory.Tenant.Domain/Aggregates/ProjectInvitation/ProjectInvitation.cs` — ExternalId, ProjectId, UserId, TokenHash, Status, ExpiresAtUtc, AcceptedAtUtc, CreatedAtUtc, CreatedByUserId, IsActive; factory Create(); methods Accept(utcNow), CheckExpiration(utcNow), Deactivate()
- [x] T012 [P] Create InvitationStatus enum in `Mentoory.Tenant.Domain/Enums/InvitationStatus.cs` — Pending=0, Accepted=1, Expired=2
- [x] T013 [P] Create EnrollmentVariant enum in `Mentoory.Tenant.Domain/Enums/EnrollmentVariant.cs` — FullFlow=0, Bypass=1
- [x] T014 Modify Project entity in `Mentoory.Tenant.Domain/Aggregates/Project/Project.cs` — add IsPublic (bool) and EnrollmentVariant (EnrollmentVariant enum) properties; update Create() factory method to accept these parameters with defaults (false, FullFlow)

### Repository Interfaces

- [x] T015 [P] Create ISystemConfigurationRepository in `Mentoory.Access.Domain/Repositories/ISystemConfigurationRepository.cs` — GetByKeyAsync(string key), GetAllAsync(), UpdateAsync()
- [x] T016 [P] Create ICountryRepository in `Mentoory.Access.Domain/Repositories/ICountryRepository.cs` — GetAllActiveAsync(), GetByCodeAsync(string code)
- [x] T017 [P] Create IProjectInvitationRepository in `Mentoory.Tenant.Domain/Repositories/IProjectInvitationRepository.cs` — GetByExternalIdAsync(), GetActiveByUserAndProjectAsync(), GetByProjectAsync(), GetByUserAsync(), AddAsync(), UpdateAsync()

### Repository Implementations & EF Configuration

- [x] T018 [P] Implement SystemConfigurationRepository in `Mentoory.Access.Infrastructure/Persistence/Repositories/SystemConfigurationRepository.cs` and add EF entity configuration in AccessDbContext
- [x] T019 [P] Implement CountryRepository in `Mentoory.Access.Infrastructure/Persistence/Repositories/CountryRepository.cs` and add EF entity configuration in AccessDbContext
- [x] T020 [P] Implement ProjectInvitationRepository in `Mentoory.Tenant.Infrastructure/Persistence/Repositories/ProjectInvitationRepository.cs` and add EF entity configuration in TenantDbContext
- [x] T021 Update TenantDbContext EF configuration for Project entity — map IsPublic and EnrollmentVariant columns

### Application Layer Infrastructure

- [x] T022 Create ISystemConfigurationReader interface in `Mentoory.Access.Application/Configuration/ISystemConfigurationReader.cs` — GetIntAsync(string key), GetBoolAsync(string key); implementation reads from repository
- [x] T023 Implement SystemConfigurationReader in `Mentoory.Access.Infrastructure/Services/SystemConfigurationReader.cs` — reads from ISystemConfigurationRepository, parses string values to typed results
- [x] T024 Register ISystemConfigurationReader in DI container in `Mentoory.Access.Infrastructure/DependencyInjection.cs`

### Core Domain Fixes

- [x] T025 Modify User.GenerateEmailVerificationToken in `Mentoory.Access.Domain/Aggregates/User/User.cs` — change signature to accept expiryHours parameter instead of hardcoded 24h; invalidate any previously active (unused, unexpired) tokens
- [x] T026 Modify User.GeneratePasswordResetToken in `Mentoory.Access.Domain/Aggregates/User/User.cs` — change signature to accept expiryHours parameter instead of hardcoded 1h
- [x] T027 Add User.AdminVerifyEmail(utcNow) method in `Mentoory.Access.Domain/Aggregates/User/User.cs` — set AccountStatus to Active, set EmailVerifiedAtUtc, bypass token mechanism
- [x] T028 Add User.SetPasswordResetRequired() method in `Mentoory.Access.Domain/Aggregates/User/User.cs` — set AccountStatus to PasswordResetRequired

### Core Web Infrastructure Fixes

- [x] T029 Fix SessionAuthenticationMiddleware in `Mentoory.Web/Infrastructure/Authentication/SessionAuthenticationMiddleware.cs` — query AuthSession table to validate session token is active and not expired on every authenticated request; also load current AccountStatus from User table and refresh the AccountStatus claim if it has changed (handles mid-session admin actions like setting PasswordResetRequired or Disabled); reject with 401/redirect to login if session invalid or user Disabled
- [x] T030 Create PasswordResetRequiredFilter in `Mentoory.Web/Infrastructure/Authentication/PasswordResetRequiredFilter.cs` — global action filter that checks AccountStatus claim (refreshed by T029 middleware on each request); redirects to forced password change page; excludes ChangePassword, Logout, and static resource paths
- [x] T031 Register PasswordResetRequiredFilter as global filter in `Mentoory.Web/Program.cs`

### Unit Tests for Foundational Changes

- [x] T032 [P] Add unit tests for User.GenerateEmailVerificationToken (parameterized expiry) and User.AdminVerifyEmail in `tests/Mentoory.Access.Tests/Domain/UserTests.cs`
- [x] T033 [P] Add unit tests for ProjectInvitation aggregate (Create, Accept, CheckExpiration, Deactivate, invalid transitions) in `tests/Mentoory.Tenant.Tests/Domain/ProjectInvitationTests.cs`
- [x] T034 [P] Add unit tests for SystemConfiguration aggregate in `tests/Mentoory.Access.Tests/Domain/SystemConfigurationTests.cs`

**Checkpoint**: Foundation ready — all domain entities, repositories, infrastructure fixes, and core tests in place. User story implementation can now begin.

---

## Phase 3: User Story 1 — Public Registration with Email Verification (Priority: P1) 🎯 MVP

**Goal**: Public registration produces correct PendingVerification state with verification token; admin can manually verify users; country dropdown with ID mask.

**Independent Test**: Register a user via the public form → verify DB state is PendingVerification with token → login is blocked → admin verifies → login succeeds.

### Implementation for User Story 1

- [x] T035 [P] [US1] Create ListCountriesQuery + handler in `Mentoory.Access.Application/Countries/Queries/ListCountries/ListCountriesQuery.cs` — returns all active countries from repository
- [x] T036 [P] [US1] Create AdminVerifyEmailCommand + handler + validator in `Mentoory.Access.Application/Users/Commands/AdminVerifyEmail/AdminVerifyEmailCommand.cs` — loads user by ExternalId, calls User.AdminVerifyEmail(utcNow), updates UserProfile read model AccountStatus
- [x] T037 [P] [US1] Create RegenerateVerificationTokenCommand + handler + validator in `Mentoory.Access.Application/Users/Commands/RegenerateVerificationToken/RegenerateVerificationTokenCommand.cs` — loads user, reads EmailVerificationTokenExpiryHours from config, calls User.GenerateEmailVerificationToken(utcNow, expiryHours)
- [x] T038 [US1] Modify RegisterUserHandler in `Mentoory.Access.Application/Users/Commands/RegisterUser/RegisterUserHandler.cs` — (1) split uniqueness check into two sequential queries: country+ID first, then email; return specific error messages for each; (2) call User.GenerateEmailVerificationToken(utcNow, expiryHours) with value from SystemConfiguration after creating user; (3) update UserProfile creation to reflect PendingVerification
- [x] T039 [US1] Modify RegisterUserCommandValidator in `Mentoory.Access.Application/Users/Commands/RegisterUser/RegisterUserCommandValidator.cs` — add country code validation against known countries; add country-specific identification format validation using regex from Country entity
- [x] T040 [US1] Modify RegisterController in `Mentoory.Web/Areas/Access/Controllers/RegisterController.cs` — inject country list into view model; display specific uniqueness error messages per field (email vs identification)
- [x] T041 [US1] Modify Register views in `Mentoory.Web/Areas/Access/Views/Register/` — add country dropdown (sourced from ListCountriesQuery), add identification field with data-mask attribute, show field-specific error messages
- [x] T042 [US1] Create registration.js in `Mentoory.Web/wwwroot/js/registration.js` — on country change, fetch mask/regex from data attributes and apply input mask to identification field
- [x] T043 [US1] Add admin verify and regenerate token actions to UsersController in `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — POST endpoints for AdminVerifyEmail and RegenerateVerificationToken commands
- [x] T044 [US1] Update UserProfile sync — modify VerifyEmailHandler in `Mentoory.Access.Application/Users/Commands/VerifyEmail/VerifyEmailHandler.cs` to update UserProfile.AccountStatus after verification

### Tests for User Story 1

- [x] T045 [P] [US1] Unit tests for modified RegisterUserHandler (specific errors, token generation) in `tests/Mentoory.Access.Tests/Application/RegisterUserHandlerTests.cs`
- [x] T046 [P] [US1] Unit tests for AdminVerifyEmailHandler in `tests/Mentoory.Access.Tests/Application/AdminVerifyEmailHandlerTests.cs`
- [x] T047 [P] [US1] Integration test: register user → verify PendingVerification + token exists → login rejected → admin verify → login succeeds in `tests/Mentoory.Tests.Integration/Identity/RegistrationVerificationTests.cs`
- [x] T048 [US1] E2E test: public registration with country dropdown + ID mask + post-registration state in `tests/Mentoory.Tests.E2E/Registration/PublicRegistrationTests.cs`

**Checkpoint**: Public registration produces correct states, specific errors, and admin can manually verify users.

---

## Phase 4: User Story 2 — Authentication and Session Lifecycle (Priority: P1)

**Goal**: Session tokens validated server-side on every request; PasswordResetRequired enforced; config-driven lockout and session timeout.

**Independent Test**: Log in → deactivate session in DB → next request redirects to login. Set AccountStatus=PasswordResetRequired → login redirects to change password → change password → access restored.

### Implementation for User Story 2

- [x] T049 [US2] Modify LoginUserHandler in `Mentoory.Access.Application/Auth/Commands/LoginUser/LoginUserHandler.cs` — read MaxFailedLoginAttempts, LockoutDurationMinutes, SessionTimeoutHours from ISystemConfigurationReader instead of hardcoded values; detect PasswordResetRequired status and include in result
- [x] T050 [US2] Create ForcedPasswordChangeCommand + handler + validator in `Mentoory.Access.Application/Users/Commands/ForcedPasswordChange/ForcedPasswordChangeCommand.cs` — verify current (temporary) password, validate new password against OWASP rules and history (depth from config), change password, clear PasswordResetRequired status, update UserProfile
- [x] T051 [US2] Modify ChangePasswordHandler in `Mentoory.Access.Application/Users/Commands/ChangePassword/ChangePasswordHandler.cs` — read PasswordHistoryDepth from ISystemConfigurationReader instead of hardcoded constant; update UserProfile.AccountStatus after clearing PasswordResetRequired
- [x] T052 [US2] Modify LoginController in `Mentoory.Web/Areas/Access/Controllers/LoginController.cs` — check login result for PasswordResetRequired flag; redirect to forced password change page instead of normal flow
- [x] T053 [US2] Create ChangePasswordController in `Mentoory.Web/Areas/Access/Controllers/ChangePasswordController.cs` — GET renders forced password change form; POST executes ForcedPasswordChangeCommand; [AllowAnonymous] is NOT used, but excluded from PasswordResetRequiredFilter
- [x] T054 [US2] Create ChangePassword views in `Mentoory.Web/Areas/Access/Views/ChangePassword/` — forced password change form with current password, new password, confirm password; Spanish labels and validation messages

### Tests for User Story 2

- [x] T055 [P] [US2] Unit tests for modified LoginUserHandler (config-driven values, PasswordResetRequired detection) in `tests/Mentoory.Access.Tests/Application/LoginUserHandlerTests.cs`
- [x] T056 [P] [US2] Unit tests for ForcedPasswordChangeHandler in `tests/Mentoory.Access.Tests/Application/ForcedPasswordChangeHandlerTests.cs`
- [x] T057 [P] [US2] Integration test: revoked session → request rejected (server-side validation) in `tests/Mentoory.Tests.Integration/Identity/SessionValidationTests.cs`
- [x] T058 [US2] E2E test: login with PasswordResetRequired → redirected to change password → change → access in `tests/Mentoory.Tests.E2E/Authentication/ForcedPasswordChangeTests.cs`

**Checkpoint**: Authentication is fully secure — server-side session validation, forced password change, config-driven parameters.

---

## Phase 5: User Story 3 — Internal One-by-One Registration (Priority: P2)

**Goal**: Admin can register a user into a specific project with explicit verification toggle; field-level error messages; cross-domain enrollment via integration event.

**Independent Test**: Admin creates user with verification required → user is PendingVerification + project invitation created. Admin creates user without verification → user is Active + enrolled per project variant.

### Implementation for User Story 3

- [x] T059 [P] [US3] Create RegisterInternalUserCommand + handler + validator in `Mentoory.Access.Application/Users/Commands/RegisterInternalUser/RegisterInternalUserCommand.cs` — accepts Country, Identification, Email, Password, RequireEmailVerification, ProjectExternalId; validates uniqueness with field-specific errors; creates user with appropriate status; publishes UserRegisteredEvent
- [x] T060 [P] [US3] Create UserRegisteredEvent in `Mentoory.Access.Application/IntegrationEvents/UserRegisteredEvent.cs` — UserId, UserExternalId, Email, ProjectExternalId, RequiresVerification, EnrollmentVariant, OccurredAtUtc
- [x] T061 [US3] Create UserRegisteredEventHandler in `Mentoory.Tenant.Application/Enrollment/IntegrationEventHandlers/UserRegisteredEventHandler.cs` — receives UserRegisteredEvent (which includes InvitationExpiryHours from the publishing handler); if Bypass variant: enroll directly via EnrollParticipant; if FullFlow variant: create ProjectInvitation in Pending state passing the expiry hours
- [x] T062 [US3] Create CreateInvitationCommand + handler in `Mentoory.Tenant.Application/Invitations/Commands/CreateInvitation/CreateInvitationCommand.cs` — accepts ExpiryHours as a command parameter (caller reads from SystemConfiguration); creates ProjectInvitation with hashed token and expiry based on the provided hours
- [x] T063 [US3] Update UsersController in `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — add internal registration action with project selection and verification toggle; use RegisterInternalUserCommand; display field-level errors
- [x] T064 [US3] Create internal registration views in `Mentoory.Web/Areas/Administration/Views/Users/` — form with country dropdown, ID field, email, password, "Requiere verificacion de correo" checkbox, project selector; Spanish labels

### Tests for User Story 3

- [x] T065 [P] [US3] Unit tests for RegisterInternalUserHandler (both verification paths, field-specific errors) in `tests/Mentoory.Access.Tests/Application/RegisterInternalUserHandlerTests.cs`
- [x] T066 [P] [US3] Unit tests for UserRegisteredEventHandler (FullFlow creates invitation, Bypass enrolls directly) in `tests/Mentoory.Tenant.Tests/Application/UserRegisteredEventHandlerTests.cs`
- [x] T067 [US3] Integration test: internal registration with verification toggle → correct user state + project association in `tests/Mentoory.Tests.Integration/Identity/InternalRegistrationTests.cs`

**Checkpoint**: Internal one-by-one registration works with both verification paths and cross-domain enrollment.

---

## Phase 6: User Story 4 — Batch Registration and Project Enrollment (Priority: P2)

**Goal**: Admin uploads CSV to batch-register users into a project; per-row result report; temporary passwords; idempotent processing.

**Independent Test**: Upload CSV with mix of new users, existing users, invalid rows → verify per-row result report accuracy and DB state.

### Implementation for User Story 4

- [x] T068 [P] [US4] Create BatchRowResult model in `Mentoory.Access.Application/Users/Commands/BatchRegisterUsers/BatchRowResult.cs` — RowNumber, Country, Identification, Email, UserAlreadyExisted, UserCreated, AlreadyInProject, InvitationCreated, EnrolledDirectly, TemporaryPassword, Status, ErrorMessage, Warnings
- [x] T069 [P] [US4] Create BatchRegistrationResult model in `Mentoory.Access.Application/Users/Commands/BatchRegisterUsers/BatchRegistrationResult.cs` — TotalRows, SuccessCount, SkippedCount, ErrorCount, List\<BatchRowResult\> Rows
- [x] T070 [US4] Create BatchRegisterUsersCommand + handler + validator in `Mentoory.Access.Application/Users/Commands/BatchRegisterUsers/BatchRegisterUsersCommand.cs` — accepts CsvStream, ProjectExternalId, IncubatorExternalId; parses CSV with CsvHelper; processes each row: check existence by country+ID, create if new (temp password, PasswordResetRequired status), check project association, enroll/invite per project variant; return per-row results; per-row transaction scope
- [x] T071 [US4] Create CsvUserMap class in `Mentoory.Access.Application/Users/Commands/BatchRegisterUsers/CsvUserMap.cs` — CsvHelper ClassMap for mapping CSV columns (Country, Identification, Email, FirstName, LastName, Province, Canton, District, AddressLine, PostalCode)
- [x] T072 [US4] Create BatchUploadController in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs` — [Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]; GET renders upload form; POST accepts IFormFile, converts to Stream, executes BatchRegisterUsersCommand; returns results view
- [x] T073 [US4] Create batch upload views in `Mentoory.Web/Areas/Administration/Views/BatchUpload/` — upload form with file input and project selector; results view with per-row table showing status, warnings, errors; Spanish labels
- [x] T074 [US4] Create batch-upload.js in `Mentoory.Web/wwwroot/js/batch-upload.js` — file validation (max 500 rows, CSV format), upload progress indicator
- [x] T075 [US4] Add BatchUpload to MenuConfiguration — add menu item under Administration for authorized roles

### Tests for User Story 4

- [x] T076 [P] [US4] Unit tests for BatchRegisterUsersHandler (new users, existing users, duplicates, invalid rows, email mismatch warning, idempotency) in `tests/Mentoory.Access.Tests/Application/BatchRegisterUsersHandlerTests.cs`
- [x] T077 [US4] Integration test: upload CSV with mixed scenarios → verify per-row results + DB state in `tests/Mentoory.Tests.Integration/Identity/BatchRegistrationTests.cs`

**Checkpoint**: Batch registration works end-to-end with idempotent processing and per-row reporting.

---

## Phase 7: User Story 5 — Project Invitation and Enrollment Lifecycle (Priority: P2)

**Goal**: Invitations have explicit Pending/Accepted/Expired states; full flow enforces verification before acceptance; admins can reissue and manually accept.

**Independent Test**: Create invitation → verify Pending → accept → verify Accepted + participant created. Create invitation → let expire → verify Expired → reissue → new Pending.

### Implementation for User Story 5

- [x] T078 [P] [US5] Create AcceptInvitationCommand + handler + validator in `Mentoory.Tenant.Application/Invitations/Commands/AcceptInvitation/AcceptInvitationCommand.cs` — loads invitation, calls CheckExpiration(utcNow), validates user email is verified (for FullFlow), calls Accept(utcNow), enrolls participant via EnrollParticipant AND creates a RoleAssignment (e.g., Entrepreneur role) so the user can select that project as their active context
- [x] T079 [P] [US5] Create AdminAcceptInvitationCommand + handler in `Mentoory.Tenant.Application/Invitations/Commands/AdminAcceptInvitation/AdminAcceptInvitationCommand.cs` — admin accepts on behalf of user, bypasses user email verification check, calls Accept(utcNow), enrolls participant AND creates a RoleAssignment (same as T078)
- [x] T080 [P] [US5] Create ReissueInvitationCommand + handler in `Mentoory.Tenant.Application/Invitations/Commands/ReissueInvitation/ReissueInvitationCommand.cs` — accepts ExpiryHours as a command parameter (caller reads from SystemConfiguration); deactivates existing invitation, creates new one with fresh token and expiry based on the provided hours
- [x] T081 [P] [US5] Create ListPendingInvitationsQuery + handler in `Mentoory.Tenant.Application/Invitations/Queries/ListPendingInvitations/ListPendingInvitationsQuery.cs` — filter by project and/or user; include expired invitations in results
- [x] T082 [P] [US5] Create GetInvitationDetailsQuery + handler in `Mentoory.Tenant.Application/Invitations/Queries/GetInvitationDetails/GetInvitationDetailsQuery.cs` — return invitation with status, expiry, user info, project info
- [x] T083 [US5] Create UserEmailVerifiedEventHandler in `Mentoory.Tenant.Application/Invitations/IntegrationEventHandlers/UserEmailVerifiedEventHandler.cs` — when user verifies email, check for pending invitations; if project uses Bypass variant, auto-accept
- [x] T084 [US5] Add invitation management actions to UsersController or create InvitationsController in `Mentoory.Web/Areas/Administration/Controllers/` — list pending invitations, accept on behalf, reissue; views for invitation state display

### Tests for User Story 5

- [x] T085 [P] [US5] Unit tests for AcceptInvitationHandler (happy path, expired, unverified email blocked) in `tests/Mentoory.Tenant.Tests/Application/AcceptInvitationHandlerTests.cs`
- [x] T086 [P] [US5] Unit tests for ReissueInvitationHandler (deactivates old, creates new) in `tests/Mentoory.Tenant.Tests/Application/ReissueInvitationHandlerTests.cs`
- [x] T087 [US5] Integration test: full invitation lifecycle (create → accept → participant enrolled; create → expire → reissue → accept) in `tests/Mentoory.Tests.Integration/Tenant/InvitationLifecycleTests.cs`

**Checkpoint**: Invitation lifecycle is deterministic with explicit states and admin controls.

---

## Phase 8: User Story 6 — First Login Experience Without Project Context (Priority: P3)

**Goal**: Verified users without project associations see public projects in Registration stage and can self-enroll.

**Independent Test**: Log in as verified user with no project associations → see public projects list → request enrollment → verify enrollment follows project variant.

### Implementation for User Story 6

- [x] T088 [P] [US6] Create ListPublicProjectsQuery + handler in `Mentoory.Tenant.Application/Projects/Queries/ListPublicProjects/ListPublicProjectsQuery.cs` — returns projects where IsPublic=true AND CurrentStageType=Registration AND CurrentStageState=InProgress; includes incubator name
- [x] T089 [P] [US6] Create RequestSelfEnrollmentCommand + handler + validator in `Mentoory.Tenant.Application/Invitations/Commands/RequestSelfEnrollment/RequestSelfEnrollmentCommand.cs` — creates invitation or auto-enrolls per project's EnrollmentVariant; validates user is verified and not already in project
- [x] T090 [US6] Modify HomeController in `Mentoory.Web/Controllers/HomeController.cs` — after login, if user has no active RoleAssignments (no project context), redirect to available projects page instead of role-based dashboard
- [x] T091 [US6] Create AvailableProjectsController in `Mentoory.Web/Controllers/AvailableProjectsController.cs` — [Authorize]; GET lists public projects via ListPublicProjectsQuery; POST handles self-enrollment via RequestSelfEnrollmentCommand
- [x] T092 [US6] Create available projects views in `Mentoory.Web/Views/AvailableProjects/` — project cards/list with name, description, incubator; "Solicitar inscripcion" button; empty state message when no projects available; Spanish labels

### Tests for User Story 6

- [x] T093 [P] [US6] Unit tests for ListPublicProjectsHandler (filters correctly by IsPublic + Registration stage) in `tests/Mentoory.Tenant.Tests/Application/ListPublicProjectsHandlerTests.cs`
- [x] T094 [US6] E2E test: verified user with no projects → sees public project list → requests enrollment in `tests/Mentoory.Tests.E2E/Registration/SelfEnrollmentTests.cs`

- [x] T094b [US6] Add IsPublic and EnrollmentVariant fields to project create/edit UI in `Mentoory.Web/Areas/Administration/Views/Projects/` — "Visible publicamente" checkbox and "Variante de inscripcion" dropdown (Flujo completo / Directo); update CreateProjectCommand and EditProjectCommand to accept these fields

**Checkpoint**: Users without project context have a meaningful landing experience with self-enrollment capability.

---

## Phase 9: User Story 7 — Configurable System Settings in Database (Priority: P3)

**Goal**: All hardcoded operational parameters read from database; changes take effect without restart.

**Independent Test**: Change MaxFailedLoginAttempts from 5 to 3 in DB → verify lockout triggers after 3 failures.

### Implementation for User Story 7

- [x] T095 [US7] Create GetConfigurationQuery + handler in `Mentoory.Access.Application/Configuration/Queries/GetConfiguration/GetConfigurationQuery.cs` — returns typed configuration value by key
- [x] T096 [US7] Create UpdateConfigurationCommand + handler + validator in `Mentoory.Access.Application/Configuration/Commands/UpdateConfiguration/UpdateConfigurationCommand.cs` — validates key exists, value is parseable to declared DataType, value is positive for numeric types; updates value and UpdatedAtUtc
- [x] T097 [US7] Sweep all remaining hardcoded values — verify LoginUserHandler (T049), ChangePasswordHandler (T051), User token generation methods (T025, T026) all read from SystemConfiguration. Note: cookie ExpireTimeSpan in Program.cs is a maximum transport-level bound and cannot be made dynamic without app restart; the operative session timeout is enforced server-side by the SessionAuthenticationMiddleware (T029) using the DB-configured SessionTimeoutHours value. Document this in a code comment in Program.cs.
- [x] T098 [US7] Create configuration management UI in `Mentoory.Web/Areas/Platform/Controllers/ConfigurationController.cs` — [Authorize(Roles = "GlobalAdmin")]; list all config entries; edit value inline; Spanish labels
- [x] T099 [US7] Create configuration views in `Mentoory.Web/Areas/Platform/Views/Configuration/` — table with Key, Value, Description, DataType, LastModified; edit modal/inline; validation feedback

### Tests for User Story 7

- [ ] T100 [P] [US7] Unit tests for UpdateConfigurationHandler (valid update, invalid type, non-existent key) in `tests/Mentoory.Access.Tests/Application/UpdateConfigurationHandlerTests.cs`
- [ ] T101 [US7] Integration test: change config value in DB → verify behavior changes on next operation in `tests/Mentoory.Tests.Integration/Configuration/SystemConfigurationTests.cs`

**Checkpoint**: All operational parameters are database-driven and configurable without restart.

---

## Phase 10: User Story 8 — Manual Verification and Administrative User Management (Priority: P3)

**Goal**: Admins can search users by state, inspect detailed user info, and manually progress users through verification/invitation/unlock workflows.

**Independent Test**: Create users in various states → admin searches by status → views details → manually verifies email, accepts invitation, regenerates token, unlocks account.

### Implementation for User Story 8

- [x] T102 [P] [US8] Create ListUsersByStatusQuery + handler in `Mentoory.Access.Application/Users/Queries/ListUsersByStatus/ListUsersByStatusQuery.cs` — filter by AccountStatus, search by email/name/identification; paged results
- [x] T103 [P] [US8] Create GetUserDetailsQuery + handler in `Mentoory.Access.Application/Users/Queries/GetUserDetails/GetUserDetailsQuery.cs` — return detailed user state from Access domain only: account status, email verification state, active credentials count, email verified date. Does NOT query Tenant domain (Clean Architecture boundary).
- [x] T103b [P] [US8] Create GetUserProjectAssociationsQuery + handler in `Mentoory.Tenant.Application/Invitations/Queries/GetUserProjectAssociations/GetUserProjectAssociationsQuery.cs` — accepts UserId; returns pending/accepted/expired invitations and active project participations for that user. Controller composes both queries to build the full user detail view.
- [x] T104 [US8] Update UsersController in `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` — add search/filter by status action; add user detail view action that composes GetUserDetailsQuery (Access) + GetUserProjectAssociationsQuery (Tenant) into a single view model; add manual action endpoints (verify, regenerate token, unlock, reset password); integrate with AdminVerifyEmail, RegenerateVerificationToken, AdminAcceptInvitation, ReissueInvitation, AdminResetPassword commands; pass InvitationExpiryHours from SystemConfiguration when calling ReissueInvitation
- [x] T105 [US8] Create user management views in `Mentoory.Web/Areas/Administration/Views/Users/` — (1) search/filter page with status dropdown, search input, DataTable results; (2) user detail page showing all state info; (3) action buttons per user state (verify, regenerate, unlock, accept invitation, reissue invitation); Spanish labels
- [x] T106 [US8] Add admin unlock action — create UnlockAccountCommand + handler in `Mentoory.Access.Application/Users/Commands/UnlockAccount/UnlockAccountCommand.cs` — loads user, calls User.Unlock(), updates UserProfile
- [x] T106b [US8] Add admin password reset action — create AdminResetPasswordCommand + handler in `Mentoory.Access.Application/Users/Commands/AdminResetPassword/AdminResetPasswordCommand.cs` — loads user by ExternalId, generates new temporary password, hashes and sets as active credential, calls User.SetPasswordResetRequired(), updates UserProfile; wire into UsersController (T104) admin actions

### Tests for User Story 8

- [x] T107 [P] [US8] Unit tests for ListUsersByStatusHandler and GetUserDetailsHandler in `tests/Mentoory.Access.Tests/Application/UserManagementQueryTests.cs`
- [x] T108 [US8] Integration test: create users in all states → filter → verify results in `tests/Mentoory.Tests.Integration/Identity/UserManagementTests.cs`

**Checkpoint**: Administrators have full visibility and control over user lifecycle states.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, cross-story integration, and comprehensive E2E coverage

- [x] T109 Validate UserProfile read model sync across ALL state transitions — verify every handler that changes AccountStatus also updates UserProfile; write integration test covering full lifecycle (register → verify → lock → unlock → disable) in `tests/Mentoory.Tests.Integration/Identity/UserProfileSyncTests.cs`
- [x] T110 [P] Update existing unit tests that break due to modified method signatures (User.GenerateEmailVerificationToken, LoginUserHandler hardcoded values, ChangePasswordHandler) in `tests/Mentoory.Access.Tests/`
- [x] T111 [P] Update existing integration tests in `tests/Mentoory.Tests.Integration/Identity/RegistrationTests.cs` and `LoginTests.cs` — update assertions for new behavior (specific errors, token generation, config-driven values)
- [x] T112 [P] Update existing E2E tests in `tests/Mentoory.Tests.E2E/` — update login routing tests, authorization tests, and any tests affected by session validation and PasswordResetRequired filter
- [x] T113 E2E test: batch upload → batch-created user login with temp password → forced password change → access granted in `tests/Mentoory.Tests.E2E/Registration/BatchRegistrationE2ETests.cs`
- [x] T114 E2E test: full invitation lifecycle — internal registration with FullFlow → pending verification → admin verify → pending invitation → accept → active participant in `tests/Mentoory.Tests.E2E/Enrollment/InvitationLifecycleE2ETests.cs`
- [x] T115 Run quickstart.md manual validation scenarios — verify all 5 manual verification steps pass
- [x] T116 Verify zero compiler warnings — run `dotnet build` and confirm TreatWarningsAsErrors passes with all changes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) completion — BLOCKS all user stories
- **US1 (Phase 3)** and **US2 (Phase 4)**: Both P1, can start after Foundational; independent of each other
- **US3 (Phase 5)**: P2, can start after Foundational; uses integration event pattern from US1
- **US4 (Phase 6)**: P2, depends on US3 (reuses UserRegisteredEvent and enrollment flow)
- **US5 (Phase 7)**: P2, depends on US3 (uses CreateInvitationCommand from T062); provides invitation management commands (Accept, Reissue) consumed by US6 and US8
- **US6 (Phase 8)**: P3, depends on Project.IsPublic (Phase 2) and invitation flow (US5)
- **US7 (Phase 9)**: P3, can start after Foundational; finalizes config sweep
- **US8 (Phase 10)**: P3, depends on all admin commands from US1/US5 being available
- **Polish (Phase 11)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 1 (Setup)
    └─► Phase 2 (Foundational)
            ├─► US1 (P1) ─────────────┐
            ├─► US2 (P1) ─────────────┤
            ├─► US3 (P2) ─────────────┤
            ├─► US4 (P2) ◄── US3 ─────┤
            ├─► US5 (P2) ◄── US3 ─────┤
            ├─► US6 (P3) ◄── US5 ─────┤
            ├─► US7 (P3) ─────────────┤
            └─► US8 (P3) ◄── US1,US5 ─┤
                                       └─► Phase 11 (Polish)
```

### Within Each User Story

- Domain entities/commands before controllers/views
- Commands before queries (queries may depend on command-created data)
- Implementation before tests
- Story complete before moving to next priority

### Parallel Opportunities

- **Phase 1**: T001–T007 all parallelizable (different SQL files)
- **Phase 2**: T008–T013 (domain entities), T015–T017 (repo interfaces), T018–T020 (repo impls), T032–T034 (unit tests) — all marked [P] within their groups
- **US1 + US2**: Can proceed in parallel (both P1, different concerns)
- **US3 + US5**: Can proceed in parallel (US3 publishes events, US5 provides commands — integration tested later)
- **US7**: Largely independent — config sweep can happen in parallel with other P3 stories

---

## Parallel Example: Phase 2 Foundation

```
# Launch all domain entities in parallel:
T008: SystemConfiguration aggregate
T009: ConfigurationKey enum
T010: Country entity
T011: ProjectInvitation aggregate
T012: InvitationStatus enum
T013: EnrollmentVariant enum

# Then launch all repository interfaces in parallel:
T015: ISystemConfigurationRepository
T016: ICountryRepository
T017: IProjectInvitationRepository

# Then launch all repo implementations in parallel:
T018: SystemConfigurationRepository + EF config
T019: CountryRepository + EF config
T020: ProjectInvitationRepository + EF config
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (database schema + seeds)
2. Complete Phase 2: Foundational (domain entities, repos, core fixes)
3. Complete Phase 3: US1 — Public Registration
4. Complete Phase 4: US2 — Authentication & Session
5. **STOP and VALIDATE**: Registration + login + verification + forced password change all work end-to-end
6. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 + US2 → Core registration and auth hardened (MVP!)
3. US3 + US5 → Internal enrollment with invitation lifecycle
4. US4 → Batch registration (builds on US3 enrollment flow)
5. US6 + US7 + US8 → Polish: public projects, config, admin tools
6. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- This is a hardening effort — many tasks modify existing files rather than creating new ones
- All user-facing text must be in Spanish (Principle IX)
- All controllers must include higher-privilege roles in [Authorize] (Principle X)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
