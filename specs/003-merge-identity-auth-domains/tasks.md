# Tasks: Merge Identity and Authorization Domains

**Input**: Design documents from `/specs/003-merge-identity-auth-domains/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: No new test logic — only namespace/reference updates to existing tests. Tests are verification-only (existing tests must pass after merge).

**Organization**: Tasks grouped by user story. US1 is the merge itself; US2 is orphan cleanup/verification.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)
- Exact file paths included in descriptions

---

## Phase 1: Setup

**Purpose**: Create the new Access project structure that will receive merged code

- [x] T001 Create `Mentoory.Access.Domain/Mentoory.Access.Domain.csproj` with project reference to `Mentoory.Shared.Domain` — copy package/property settings from `Mentoory.Identity.Domain.csproj`
- [x] T002 [P] Create `Mentoory.Access.Application/Mentoory.Access.Application.csproj` with project references to `Mentoory.Access.Domain` and `Mentoory.Shared.Application` — copy package/property settings from `Mentoory.Identity.Application.csproj`
- [x] T003 [P] Create `Mentoory.Access.Infrastructure/Mentoory.Access.Infrastructure.csproj` with project references to `Mentoory.Access.Domain` and `Mentoory.Shared.Infrastructure` — copy package/property settings from `Mentoory.Identity.Infrastructure.csproj`
- [x] T004 *(depends on T001-T003)* Create `tests/Mentoory.Access.Tests/Mentoory.Access.Tests.csproj` with project references to `Mentoory.Access.Domain`, `Mentoory.Access.Application`, and `Mentoory.Access.Infrastructure` — copy test framework references from `tests/Mentoory.Identity.Tests/Mentoory.Identity.Tests.csproj`
- [x] T005 Add all four new projects to `Mentoory.sln` under a new "Access" solution folder, keeping old projects in place temporarily for compilation

**Checkpoint**: New empty projects exist and solution builds (old projects still present)

---

## Phase 2: Foundational (Domain Layer Merge)

**Purpose**: Move all Domain types to Access.Domain — this MUST complete before Application/Infrastructure layers can reference them

**CRITICAL**: No US1 Application/Infrastructure tasks can begin until Domain layer is fully migrated

- [x] T006 [P] Move User aggregate files from `Mentoory.Identity.Domain/Aggregates/User/` to `Mentoory.Access.Domain/Aggregates/User/` — update namespace from `Mentoory.Identity.Domain.Aggregates.User` to `Mentoory.Access.Domain.Aggregates.User` in User.cs, Credential.cs, EmailVerificationToken.cs, PasswordResetToken.cs
- [x] T007 [P] Move AuthSession aggregate from `Mentoory.Identity.Domain/Aggregates/AuthSession/` to `Mentoory.Access.Domain/Aggregates/AuthSession/` — update namespace to `Mentoory.Access.Domain.Aggregates.AuthSession`
- [x] T008 [P] Move RoleAssignment aggregate from `Mentoory.Authorization.Domain/Aggregates/RoleAssignment/` to `Mentoory.Access.Domain/Aggregates/RoleAssignment/` — update namespace to `Mentoory.Access.Domain.Aggregates.RoleAssignment`
- [x] T009 [P] Move value objects from `Mentoory.Identity.Domain/ValueObjects/` to `Mentoory.Access.Domain/ValueObjects/` — update namespace to `Mentoory.Access.Domain.ValueObjects` in EmailAddress.cs, NationalIdentity.cs, HashedPassword.cs
- [x] T010 [P] Move read models from `Mentoory.Authorization.Domain/ReadModels/` to `Mentoory.Access.Domain/ReadModels/` — update namespace to `Mentoory.Access.Domain.ReadModels` in UserProfile.cs, UserContext.cs
- [x] T011 [P] Move enums from `Mentoory.Identity.Domain/Enums/AccountStatus.cs` and `Mentoory.Authorization.Domain/Enums/PlatformRole.cs`, `Permission.cs` to `Mentoory.Access.Domain/Enums/` — update all namespaces to `Mentoory.Access.Domain.Enums`
- [x] T012 [P] Move repository interfaces from `Mentoory.Identity.Domain/Repositories/` (IUserRepository.cs, IAuthSessionRepository.cs) and `Mentoory.Authorization.Domain/Repositories/` (IRoleAssignmentRepository.cs, IUserProfileRepository.cs) to `Mentoory.Access.Domain/Repositories/` — update all namespaces to `Mentoory.Access.Domain.Repositories`
- [x] T013 [P] Move service interfaces from `Mentoory.Identity.Domain/Services/IPasswordHasher.cs` to `Mentoory.Access.Domain/Services/` — update namespace to `Mentoory.Access.Domain.Services`

**Checkpoint**: All Domain types are in Access.Domain with correct namespaces. Access.Domain project compiles standalone.

---

## Phase 3: User Story 1 — Unified Domain Structure with Zero Behavior Change (Priority: P1)

**Goal**: Complete the merge across Application, Infrastructure, Web, and Database layers so all code compiles and runs under the Access namespace.

**Independent Test**: `dotnet build` succeeds with zero warnings; `dotnet test` passes all tests.

### Application Layer

- [x] T014 [P] [US1] Move Identity commands from `Mentoory.Identity.Application/Commands/` to `Mentoory.Access.Application/Commands/` — update namespaces in all subfolders: RegisterUser/, LoginUser/, LogoutUser/, VerifyEmail/, ActivateAccount/, DeactivateAccount/, LockAccount/, UnlockAccount/, ChangePassword/, RequestPasswordReset/, ResetPassword/ (Command, Handler, Validator files each)
- [x] T015 [P] [US1] Move Authorization commands from `Mentoory.Authorization.Application/Commands/` to `Mentoory.Access.Application/Commands/` — update namespaces in AssignRole/, RevokeRole/, SetActiveContext/ (Command, Handler, Validator files each)
- [x] T016 [P] [US1] Move Identity queries from `Mentoory.Identity.Application/Queries/` to `Mentoory.Access.Application/Queries/` — update namespaces in GetUserByEmail/, GetUserByExternalId/, ListUsers/, ValidateSession/
- [x] T017 [P] [US1] Move Authorization queries from `Mentoory.Authorization.Application/Queries/` to `Mentoory.Access.Application/Queries/` — update namespaces in GetActiveContext/, GetUserContexts/, CheckPermission/, ListIncubatorMembers/
- [x] T018 [US1] Move integration events from `Mentoory.Identity.Application/IntegrationEvents/` to `Mentoory.Access.Application/IntegrationEvents/` — update namespaces in UserRegisteredEvent.cs, UserEmailVerifiedEvent.cs, UserLockedOutEvent.cs, LoginAttemptEvent.cs. **Before moving**: verify no handlers outside the former Authorization domain subscribe to `UserRegisteredEvent` (grep for `INotificationHandler<UserRegisteredEvent>` across the entire solution). If none exist beyond the handler removed in T019, retain the event class for potential future cross-domain consumers but remove the handler.
- [x] T019 [US1] Remove `UserRegisteredEventHandler` from `Mentoory.Authorization.Application/IntegrationEvents/Handlers/` — inline UserProfile creation directly into `RegisterUserHandler` in `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserHandler.cs`. **Transaction semantics**: UserProfile creation MUST occur within the same `SaveChangesAsync` call as User creation (single unit of work) — add `IUserProfileRepository` dependency, create UserProfile immediately after User aggregate is added to the repository but before `SaveChangesAsync`, so both persist or both fail atomically. Remove the `UserRegisteredEvent` publish call from the handler since it was only consumed by the now-deleted handler.
- [x] T020 [US1] Create unified `Mentoory.Access.Application/DependencyInjection.cs` with `AddAccessApplication()` extension method — register MediatR handlers and FluentValidation validators from the Access.Application assembly

### Infrastructure Layer

- [x] T021 [US1] Create `Mentoory.Access.Infrastructure/Persistence/AccessDbContext.cs` — merge all DbSets and OnModelCreating configuration from `IdentityDbContext` and `AuthorizationDbContext` into a single context using `[access]` schema for all table mappings. **Merge verification**: after combining both contexts, verify (a) no duplicate entity type configurations exist, (b) all `HasSchema` calls use `"access"`, (c) all `HasIndex`/`HasKey`/property configurations are preserved without conflicts, (d) run `dotnet build` immediately after creating AccessDbContext before proceeding to repository tasks
- [x] T022 [P] [US1] Move `UserRepository.cs` from `Mentoory.Identity.Infrastructure/Persistence/Repositories/` to `Mentoory.Access.Infrastructure/Persistence/Repositories/` — update namespace and DbContext type from IdentityDbContext to AccessDbContext
- [x] T023 [P] [US1] Move `AuthSessionRepository.cs` from `Mentoory.Identity.Infrastructure/Persistence/Repositories/` to `Mentoory.Access.Infrastructure/Persistence/Repositories/` — update namespace and DbContext type
- [x] T024 [P] [US1] Move `RoleAssignmentRepository.cs` from `Mentoory.Authorization.Infrastructure/Persistence/Repositories/` to `Mentoory.Access.Infrastructure/Persistence/Repositories/` — update namespace and DbContext type from AuthorizationDbContext to AccessDbContext
- [x] T025 [P] [US1] Move `UserProfileRepository.cs` from `Mentoory.Authorization.Infrastructure/Persistence/Repositories/` to `Mentoory.Access.Infrastructure/Persistence/Repositories/` — update namespace and DbContext type
- [x] T026 [P] [US1] Move `Pbkdf2PasswordHasher.cs` from `Mentoory.Identity.Infrastructure/Services/` to `Mentoory.Access.Infrastructure/Services/` — update namespace to `Mentoory.Access.Infrastructure.Services`
- [x] T027 [US1] Create unified `Mentoory.Access.Infrastructure/DependencyInjection.cs` with `AddAccessInfrastructure()` extension method — register AccessDbContext, all four repositories, and IPasswordHasher

### Web Layer

- [x] T028 [US1] Rename `Mentoory.Web/Areas/Identity/` directory to `Mentoory.Web/Areas/Access/` — update `[Area("Identity")]` to `[Area("Access")]` on all controllers: LoginController.cs, RegisterController.cs, LogoutController.cs, VerifyEmailController.cs, ForgotPasswordController.cs, ResetPasswordController.cs — update controller namespaces to `Mentoory.Web.Areas.Access.Controllers`
- [x] T029 [P] [US1] Update view model namespaces in `Mentoory.Web/Areas/Access/Models/` — rename from `Mentoory.Web.Areas.Identity.Models` to `Mentoory.Web.Areas.Access.Models` in LoginViewModel.cs, RegisterViewModel.cs, ForgotPasswordViewModel.cs, ResetPasswordViewModel.cs
- [x] T030 [US1] Move/rename Views from `Areas/Identity/Views/` to `Areas/Access/Views/` — update `@model` directives and `_ViewImports.cshtml` if present
- [x] T031 [US1] Update `Mentoory.Web/Program.cs` — replace `AddIdentityApplication()` + `AddAuthorizationApplication()` with `AddAccessApplication()`, replace `AddIdentityInfrastructure()` + `AddAuthorizationInfrastructure()` with `AddAccessInfrastructure()`, update cookie paths from `/Identity/Login` to `/Access/Login`, `/Identity/Logout` to `/Access/Logout`
- [x] T032 [US1] Update `Mentoory.Web/Infrastructure/Persistence/DbContextFactory.cs` — replace "Identity" → IdentityDbContext and "Authorization" → AuthorizationDbContext entries with single "Access" → AccessDbContext entry, update using statements
- [x] T033 [US1] Update Web middleware namespace references — update using statements in `SessionAuthenticationMiddleware.cs` and `TenantContextMiddleware.cs` in `Mentoory.Web/Infrastructure/` from Identity/Authorization namespaces to Access namespace
- [x] T034 [US1] Update `Mentoory.Web/Mentoory.Web.csproj` — replace project references to Identity.Application, Identity.Infrastructure, Authorization.Application, Authorization.Infrastructure with Access.Application and Access.Infrastructure

### Database Layer

- [x] T035 [US1] Create `Mentoory.Db/access/Schema.sql` with `CREATE SCHEMA [access]` — move all table SQL files from `Mentoory.Db/identity/Tables/` and `Mentoory.Db/authorization/Tables/` to `Mentoory.Db/access/Tables/` — update all table definitions to use `[access]` schema prefix instead of `[identity]` or `[authorization]`
- [x] T036 [US1] Remove `Mentoory.Db/identity/` and `Mentoory.Db/authorization/` directories (Schema.sql and Tables/ in each)
- [x] T037 [US1] Update PostDeployment seed scripts — replace `[identity]` and `[authorization]` schema references with `[access]` in `Mentoory.Db.PostDeployment/002.SeedGlobalAdmin.sql`, `004.SeedTestData.sql`, and `005.SeedUserProfiles.sql`
- [x] T038 [US1] Add PostDeployment migration script `Mentoory.Db.PostDeployment/015.MigrateToAccessSchema.sql` — transfer data from old `[identity].*` and `[authorization].*` tables to new `[access].*` tables (idempotent, checks if old schemas exist before running)

### Test Projects

- [x] T039 [P] [US1] Move Identity test files from `tests/Mentoory.Identity.Tests/` to `tests/Mentoory.Access.Tests/` preserving folder structure (Domain/, Application/, Handlers/, Validators/) — update all namespaces from `Mentoory.Identity.Tests` to `Mentoory.Access.Tests` and using statements from `Mentoory.Identity.*` to `Mentoory.Access.*`
- [x] T040 [P] [US1] Move Authorization test files from `tests/Mentoory.Authorization.Tests/` to `tests/Mentoory.Access.Tests/` preserving folder structure (Domain/, Handlers/, Validators/, Infrastructure/) — update all namespaces from `Mentoory.Authorization.Tests` to `Mentoory.Access.Tests` and using statements from `Mentoory.Authorization.*` to `Mentoory.Access.*`
- [x] T041 [US1] Update Integration test files — update `Mentoory.Identity` and `Mentoory.Authorization` namespace references to `Mentoory.Access` in `tests/Mentoory.Tests.Integration/Fixtures/MentooryWebApplicationFactory.cs`, `Fixtures/IntegrationTestBase.cs`, `DependencyInjection/ServiceResolutionTests.cs`, `Schema/SchemaDriftTests.cs`, `Identity/RegistrationTests.cs`, `Identity/LoginTests.cs`, `Identity/SessionManagementTests.cs`, `Authorization/ContextSelectionTests.cs`
- [x] T042 [US1] Update E2E test URL references — replace `/Identity/` URL paths with `/Access/` in `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` and any other E2E files referencing identity routes
- [x] T043 [US1] Update `tests/Mentoory.Tests.Integration/Mentoory.Tests.Integration.csproj` — replace project references from Identity/Authorization projects to Access projects

**Checkpoint**: Solution compiles. All commands, queries, handlers, repositories, DbContext, and web layer use Access namespace. Tests updated.

---

## Phase 4: User Story 2 — Clean Solution Structure with No Orphan Artifacts (Priority: P1)

**Goal**: Remove all old Identity and Authorization project directories and solution references, leaving zero stale artifacts.

**Independent Test**: Grep for `Mentoory.Identity` and `Mentoory.Authorization` across the entire solution returns zero matches.

- [x] T044 [P] [US2] Delete old source project directories: `Mentoory.Identity.Domain/`, `Mentoory.Identity.Application/`, `Mentoory.Identity.Infrastructure/`, `Mentoory.Authorization.Domain/`, `Mentoory.Authorization.Application/`, `Mentoory.Authorization.Infrastructure/`
- [x] T045 [P] [US2] Delete old test project directories: `tests/Mentoory.Identity.Tests/`, `tests/Mentoory.Authorization.Tests/`
- [x] T046 [US2] Update `Mentoory.sln` — remove all 8 old project entries (6 source + 2 test) and 2 old solution folders (Identity, Authorization), verify 4 new Access project entries and 1 Access solution folder are present. **Count assertion**: record the total project count in the `.sln` before the merge began and verify the final count is exactly 3 fewer (8 removed − 4 added = net −4 project entries, but −3 logical projects since test projects go from 2→1 while source goes from 6→3)
- [x] T047 [US2] Scan and update all remaining `.csproj` files across the solution — replace any lingering project references to Identity/Authorization projects with Access equivalents (check `Mentoory.Web.csproj`, `Mentoory.Tests.E2E.csproj`, `Mentoory.Tests.Integration.csproj`, `Mentoory.Aspire.AppHost.csproj`)
- [x] T048 [US2] Full orphan reference scan — grep the entire repository for `Mentoory\.Identity` and `Mentoory\.Authorization` in all `.cs`, `.csproj`, `.sln`, `.sql`, `.cshtml`, `.json` files — fix any remaining references found

**Checkpoint**: No references to old namespaces exist anywhere in the codebase.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, documentation updates, and build/test validation

- [x] T049 Build verification — run `dotnet build` and confirm zero errors and zero warnings across the entire solution
- [x] T050 Test verification — run `dotnet test` and confirm 100% of tests pass with no test logic changes (only namespace updates)
- [x] T050b Workflow verification — after T050 passes, explicitly confirm E2E tests cover login, register, logout, email verification, password reset, and context selection flows under the new `/Access/` URLs. If any workflow is not covered by existing E2E tests, manually verify it by running the application and navigating through the flow. This validates SC-003 (all user-facing workflows function identically).
- [x] T051 [P] Update `.claude/domain-reference.md` if it references Identity or Authorization domains — replace with Access domain documentation
- [x] T052 [P] Update `.claude/architecture.md` and `.claude/web-patterns.md` if they reference Identity or Authorization namespaces — replace with Access
- [x] T053 Run quickstart.md verification commands — grep for orphan references as documented in `specs/003-merge-identity-auth-domains/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — **BLOCKS all US1 Application/Infrastructure tasks**
- **US1 (Phase 3)**: Depends on Foundational (Domain layer) completion. Application layer depends on Domain; Infrastructure depends on Domain + Application; Web depends on Application + Infrastructure; DB is independent of C# layers
- **US2 (Phase 4)**: Depends on US1 completion — cannot delete old projects until all code has been moved
- **Polish (Phase 5)**: Depends on US2 completion

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational phase (Phase 2). This is the main work.
- **User Story 2 (P1)**: **Depends on US1** — cleanup can only happen after all code is migrated.

### Within User Story 1

```
Domain (Phase 2) ──→ Application (T014-T020) ──→ Infrastructure (T021-T027) ──→ Web (T028-T034)
                                                                                      │
                                                  Database (T035-T038) ───────────────┘ (parallel with C# layers)
                                                  Tests (T039-T043) ──────────────────┘ (after App + Infra)
```

### Parallel Opportunities

**Phase 2 (Domain)**: T006-T013 are ALL parallel (different folders, no file conflicts)

**Phase 3 Application**: T014-T017 are parallel (different command/query folders)

**Phase 3 Infrastructure**: T022-T026 are parallel (different repository files)

**Phase 3 Web**: T028-T029 are parallel (controllers vs view models)

**Phase 3 Tests**: T039-T040 are parallel (different source test projects)

**Phase 3 Database**: T035-T038 can run in parallel with C# Application/Infrastructure tasks (different file types)

**Phase 4 Cleanup**: T044-T045 are parallel (source dirs vs test dirs)

---

## Parallel Example: Phase 2 (Domain Layer)

```text
# All domain tasks can run simultaneously:
T006: Move User aggregate (Identity.Domain/Aggregates/User/ → Access.Domain/Aggregates/User/)
T007: Move AuthSession aggregate (Identity.Domain/Aggregates/AuthSession/ → Access.Domain/Aggregates/AuthSession/)
T008: Move RoleAssignment aggregate (Authorization.Domain/Aggregates/RoleAssignment/ → Access.Domain/Aggregates/RoleAssignment/)
T009: Move value objects (Identity.Domain/ValueObjects/ → Access.Domain/ValueObjects/)
T010: Move read models (Authorization.Domain/ReadModels/ → Access.Domain/ReadModels/)
T011: Move enums (both domains → Access.Domain/Enums/)
T012: Move repository interfaces (both domains → Access.Domain/Repositories/)
T013: Move service interfaces (Identity.Domain/Services/ → Access.Domain/Services/)
```

## Parallel Example: Phase 3 Application Layer

```text
# All command/query move tasks can run simultaneously:
T014: Move Identity commands (11 command sets)
T015: Move Authorization commands (3 command sets)
T016: Move Identity queries (4 query sets)
T017: Move Authorization queries (4 query sets)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Domain Layer Merge (T006-T013)
3. Complete Phase 3: US1 full merge (T014-T043)
4. **STOP and VALIDATE**: `dotnet build` + `dotnet test`
5. Proceed to cleanup only after validation passes

### Incremental Delivery

1. Setup + Domain Layer → Domain compiles standalone
2. Application Layer → Commands/queries compile
3. Infrastructure Layer → Repositories + DbContext compile
4. Web Layer + DB → Full application runs
5. Tests → All tests pass
6. Cleanup (US2) → No orphan references
7. Polish → Documentation updated

### Single Developer Strategy

Work sequentially through phases. Maximize parallel tasks within each phase:
1. Phase 1 + 2 together (small, fast)
2. Phase 3: Application → Infrastructure → Web → DB → Tests (layer by layer)
3. Phase 4: Delete old projects
4. Phase 5: Verify everything

---

## Notes

- [P] tasks = different files, no dependencies within the phase
- [US1]/[US2] labels map tasks to user stories from spec.md
- This is a pure structural refactor — do NOT change any business logic, validation rules, or test assertions
- When moving files: copy content, update namespace declaration and all using statements, then delete original after verification
- The only "logic change" is T019: replacing the cross-domain UserRegisteredEventHandler with direct UserProfile creation in RegisterUserHandler
- Commit after each completed layer (Domain, Application, Infrastructure, Web, DB, Tests) for safe rollback points
- If `dotnet build` fails at any checkpoint, fix before proceeding — do not accumulate errors across layers
