# Tasks: Mentoory Enterprise SaaS Platform

**Input**: Design documents from `/specs/001-mentory-platform-core/`
**Prerequisites**: plan.md, spec.md, data-model.md, research.md, contracts/web-endpoints.md, quickstart.md

**Tests**: Included per constitution mandate (xUnit, Moq, FluentAssertions). Each user story phase includes test tasks for domain and integration coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution scaffolding — all projects, references, and NuGet packages

- [X] T001 Create solution file and all 31 project files (.csproj) with correct project references per implementation plan structure in Mentoory.sln
- [X] T002 [P] Configure NuGet package references per project (MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, MailKit/MimeKit, xUnit, Moq, FluentAssertions) in each .csproj
- [X] T003 [P] Configure Aspire AppHost to orchestrate Mentoory.Web and SQL Server resources in Mentoory.Aspire.AppHost/AppHost.cs and Mentoory.Aspire.AppHost.ServiceDefaults/Extensions.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared kernel, web infrastructure, and cross-cutting concerns that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 [P] Implement Shared.Domain seedwork (Entity, ValueObject, IAggregateRoot, IRepository, IUnitOfWork, SoftDeletableEntity) and Roles constants in Mentoory.Shared.Domain/SeedWork/ and Mentoory.Shared.Domain/Constants/Roles.cs
- [X] T005 [P] Implement Shared.Application core patterns (IBaseRequest, IBaseRequest&lt;T&gt;, BaseCommandHandler, BaseCommandHandler&lt;T&gt;, Result, Result&lt;T&gt;, ResultErrorCodes, ValidatorBehavior) in Mentoory.Shared.Application/MediatR/ and Mentoory.Shared.Application/
- [X] T006 [P] Implement Shared.Application supporting patterns (DataTableRequest, DataTableResponse&lt;T&gt;, ITimeProvider, ITenantContext, IAuditService, AuditEntry, IIntegrationEvent, IntegrationEvent, MediatRIntegrationEventService) in Mentoory.Shared.Application/
- [X] T007 Implement Shared.Infrastructure (SharedAbstractDbContext with domain event dispatch, AbstractRepository, TransactionBehavior, DefaultSystemTimeProvider, TenantContextService, AuditService) in Mentoory.Shared.Infrastructure/
- [X] T008 [P] Create all 8 SSDT schema files (identity, authorization, tenant, diagnostic, knowledge, mentoring, subscription, notification) plus audit schema and AuditLog table in Mentoory.Db/
- [X] T009 Configure Web project Program.cs skeleton with service registration, cookie authentication middleware, rate limiting policies (login: 5/15min, registration: 3/15min, password-reset: 3/60min), honeypot bot detection on public registration/password-reset forms (FR-054), anti-forgery, and middleware pipeline in Mentoory.Web/Program.cs
- [X] T010 [P] Implement Phoenix Admin layout files (_Layout.cshtml, _Navigation.cshtml, _TopBar.cshtml, _Footer.cshtml, _Breadcrumbs.cshtml) with Spanish locale and Phoenix CSS integration in Mentoory.Web/Views/Shared/
- [X] T011 [P] Implement menu infrastructure (MenuItem, MenuGroup models, MenuConfiguration with all role menus, IMenuService interface, MenuService) in Mentoory.Web/Infrastructure/Menu/
- [X] T012 [P] Implement reusable View Components (DataTableComponent, ToastComponent, ConfirmModalComponent, FilterBarComponent) in Mentoory.Web/Views/Shared/Components/
- [X] T013 [P] Create client-side utilities (site.js with global helpers, datatable-helper.js for reusable DataTable initialization, form-helper.js for AJAX form submission) in Mentoory.Web/wwwroot/js/
- [X] T014 [P] Create mentoory.css custom styles using Phoenix CSS variables in Mentoory.Web/wwwroot/css/mentoory.css
- [X] T015 Implement authentication middleware (session cookie validation, session loading, ClaimsPrincipal creation) and tenant context middleware (ITenantContext from active session) in Mentoory.Web/Infrastructure/Authentication/ and Mentoory.Web/Infrastructure/Authorization/

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 — Platform Foundation: Tenant, User & Context Management (Priority: P1) 🎯 MVP

**Goal**: Multi-tenant platform with user registration/login, session management, context selection, role-based access, incubator/project CRUD, and participant enrollment

**Independent Test**: Create an incubator, add users with different roles, verify context selection restricts access appropriately. Run QS-02 through QS-08, QS-09, QS-12, QS-13, QS-14.

### Identity Domain

- [X] T016 [P] [US1] Implement User aggregate root with domain logic for registration, credential management, lockout, email verification, and password reset in Mentoory.Identity.Domain/Aggregates/User/User.cs
- [X] T017 [P] [US1] Implement Credential entity (password hash storage, activation) in Mentoory.Identity.Domain/Aggregates/User/Credential.cs
- [X] T018 [P] [US1] Implement EmailVerificationToken and PasswordResetToken entities with token hashing and expiry validation in Mentoory.Identity.Domain/Aggregates/User/
- [X] T019 [P] [US1] Implement AuthSession aggregate root with session lifecycle (create, validate, expire, deactivate, set context) in Mentoory.Identity.Domain/Aggregates/AuthSession/AuthSession.cs
- [X] T020 [P] [US1] Implement Identity value objects (EmailAddress with normalization, NationalIdentity with country+ID compound, HashedPassword with algorithm metadata) in Mentoory.Identity.Domain/ValueObjects/
- [X] T021 [P] [US1] Implement AccountStatus enum and repository interfaces (IUserRepository, IAuthSessionRepository) and IPasswordHasher domain service interface in Mentoory.Identity.Domain/

### Identity Application

- [X] T022 [P] [US1] Implement RegisterUserCommand, RegisterUserHandler (uniqueness checks, credential creation, token generation, event publishing), and RegisterUserValidator (password policy) in Mentoory.Identity.Application/Commands/RegisterUser/
- [X] T023 [P] [US1] Implement LoginUserCommand, LoginUserHandler (credential validation, lockout check, session creation, single-session enforcement), and LoginUserValidator in Mentoory.Identity.Application/Commands/LoginUser/
- [X] T024 [P] [US1] Implement LogoutUserCommand and handler (session deactivation) in Mentoory.Identity.Application/Commands/LogoutUser/
- [X] T025 [P] [US1] Implement VerifyEmailCommand and handler (token validation, account activation) in Mentoory.Identity.Application/Commands/VerifyEmail/
- [X] T026 [P] [US1] Implement RequestPasswordResetCommand and ResetPasswordCommand with handlers (token generation, generic response, password update) in Mentoory.Identity.Application/Commands/RequestPasswordReset/ and ResetPassword/
- [X] T027 [P] [US1] Implement ChangePasswordCommand and handler (old password verification, history check) in Mentoory.Identity.Application/Commands/ChangePassword/
- [X] T028 [P] [US1] Implement account management commands (LockAccount, UnlockAccount, ActivateAccount, DeactivateAccount) with handlers in Mentoory.Identity.Application/Commands/
- [X] T029 [P] [US1] Implement Identity queries (GetUserByEmail, GetUserByExternalId, ValidateSession, ListUsers with DataTable support) in Mentoory.Identity.Application/Queries/
- [X] T030 [P] [US1] Implement Identity integration events (UserRegisteredEvent, UserEmailVerifiedEvent, LoginAttemptEvent, UserLockedOutEvent) in Mentoory.Identity.Application/IntegrationEvents/
- [X] T031 [US1] Implement Identity Application DependencyInjection.cs with MediatR and FluentValidation registration in Mentoory.Identity.Application/DependencyInjection.cs

### Identity Infrastructure

- [X] T032 [US1] Implement IdentityDbContext with entity configurations for [identity] schema tables in Mentoory.Identity.Infrastructure/Persistence/IdentityDbContext.cs
- [X] T033 [P] [US1] Implement UserRepository in Mentoory.Identity.Infrastructure/Persistence/Repositories/UserRepository.cs
- [X] T034 [P] [US1] Implement AuthSessionRepository in Mentoory.Identity.Infrastructure/Persistence/Repositories/AuthSessionRepository.cs
- [X] T035 [P] [US1] Implement Pbkdf2PasswordHasher (PBKDF2-SHA512, 600K iterations, 128-bit salt, timing-safe comparison) in Mentoory.Identity.Infrastructure/Services/Pbkdf2PasswordHasher.cs
- [X] T036 [US1] Implement Identity Infrastructure DependencyInjection.cs with DbContext and repository registration in Mentoory.Identity.Infrastructure/DependencyInjection.cs

### Identity SSDT

- [X] T037 [P] [US1] Create SSDT table definitions for [identity] schema (Users, Credentials, AuthSessions, EmailVerificationTokens, PasswordResetTokens) with all indexes and constraints in Mentoory.Db/identity/Tables/

### Authorization Domain

- [X] T038 [P] [US1] Implement RoleAssignment aggregate root with role validation logic (entrepreneur limit, GlobalAdmin rules) in Mentoory.Authorization.Domain/Aggregates/RoleAssignment/RoleAssignment.cs
- [X] T039 [P] [US1] Implement PlatformRole enum (GlobalAdmin, IncubatorAdmin, ProjectCoordinator, Mentor, Entrepreneur, Sponsor), Permission enum, and UserContext read model in Mentoory.Authorization.Domain/
- [X] T040 [P] [US1] Implement IRoleAssignmentRepository interface in Mentoory.Authorization.Domain/Repositories/IRoleAssignmentRepository.cs

### Authorization Application

- [X] T041 [P] [US1] Implement AssignRole and RevokeRole commands with handlers and validators in Mentoory.Authorization.Application/Commands/
- [X] T042 [P] [US1] Implement SetActiveContext command with handler (context validation and session update) in Mentoory.Authorization.Application/Commands/SetActiveContext/
- [X] T043 [P] [US1] Implement Authorization queries (GetUserContexts, GetActiveContext, CheckPermission) in Mentoory.Authorization.Application/Queries/
- [X] T044 [US1] Implement UserRegisteredEventHandler (creates default role assignments) in Mentoory.Authorization.Application/IntegrationEvents/Handlers/UserRegisteredEventHandler.cs
- [X] T045 [US1] Implement Authorization Application DependencyInjection.cs in Mentoory.Authorization.Application/DependencyInjection.cs

### Authorization Infrastructure

- [X] T046 [US1] Implement AuthorizationDbContext and RoleAssignmentRepository in Mentoory.Authorization.Infrastructure/Persistence/
- [X] T047 [US1] Implement Authorization Infrastructure DependencyInjection.cs in Mentoory.Authorization.Infrastructure/DependencyInjection.cs

### Authorization SSDT

- [X] T048 [P] [US1] Create SSDT table definition for [authorization] schema (RoleAssignments) with indexes and filtered unique constraint in Mentoory.Db/authorization/Tables/RoleAssignments.sql

### Tenant Domain

- [X] T049 [P] [US1] Implement Incubator aggregate root with create/update/activate/deactivate logic in Mentoory.Tenant.Domain/Aggregates/Incubator/Incubator.cs
- [X] T050 [P] [US1] Implement Project aggregate root with ProjectStage, ProjectParticipant, MentorAssignment child entities and stage initialization logic in Mentoory.Tenant.Domain/Aggregates/Project/
- [X] T051 [P] [US1] Implement Tenant enums (StageType, StageState), LifecyclePosition value object, and repository interfaces (IIncubatorRepository, IProjectRepository) in Mentoory.Tenant.Domain/

### Tenant Application

- [X] T052 [P] [US1] Implement CreateIncubator and UpdateIncubator commands with handlers and validators in Mentoory.Tenant.Application/Commands/
- [X] T053 [P] [US1] Implement CreateProject command with handler (stage initialization, participant enrollment) in Mentoory.Tenant.Application/Commands/CreateProject/
- [X] T054 [P] [US1] Implement EnrollParticipant, AssignMentor, and SetLeadMentor commands with handlers in Mentoory.Tenant.Application/Commands/
- [X] T055 [P] [US1] Implement Tenant queries (ListIncubators, GetIncubatorByExternalId, ListProjects, GetProjectByExternalId, ListProjectParticipants) with DataTable support in Mentoory.Tenant.Application/Queries/
- [X] T056 [P] [US1] Implement Tenant integration events (IncubatorCreatedEvent, ProjectCreatedEvent, ParticipantEnrolledEvent) in Mentoory.Tenant.Application/IntegrationEvents/
- [X] T057 [US1] Implement Tenant Application DependencyInjection.cs in Mentoory.Tenant.Application/DependencyInjection.cs

### Tenant Infrastructure

- [X] T058 [US1] Implement TenantDbContext with EF Core global query filters for IncubatorId on all tenant-scoped entities in Mentoory.Tenant.Infrastructure/Persistence/TenantDbContext.cs
- [X] T059 [P] [US1] Implement IncubatorRepository and ProjectRepository in Mentoory.Tenant.Infrastructure/Persistence/Repositories/
- [X] T060 [US1] Implement Tenant Infrastructure DependencyInjection.cs in Mentoory.Tenant.Infrastructure/DependencyInjection.cs

### Tenant SSDT

- [X] T061 [P] [US1] Create SSDT table definitions for [tenant] schema (Incubators, Projects, ProjectStages, ProjectParticipants, MentorAssignments) with all indexes in Mentoory.Db/tenant/Tables/

### PostDeployment Scripts

- [X] T062 [US1] Create PostDeployment seed scripts (001.SeedRoles.sql, 002.SeedGlobalAdmin.sql, 003.SeedDefaultSubscriptionPlan.sql, Script.PostDeployment.sql) in Mentoory.Db.PostDeployment/ — *Note: 003.SeedDefaultSubscriptionPlan.sql seeds a US6 artifact here because a default subscription plan is required for platform bootstrap before US6 implementation*

### Web Layer — Identity Area

- [X] T063 [P] [US1] Implement Login controller (GET form, POST validation) with rate limiting and view models in Mentoory.Web/Areas/Identity/Controllers/LoginController.cs and Models/
- [X] T064 [P] [US1] Implement Login views (form with email/password, error display, Spanish labels) in Mentoory.Web/Areas/Identity/Views/Login/
- [X] T065 [P] [US1] Implement Register controller (GET form, POST with enumeration prevention) with rate limiting in Mentoory.Web/Areas/Identity/Controllers/RegisterController.cs
- [X] T066 [P] [US1] Implement Register views (form with country, national ID, email, password, names, validation) in Mentoory.Web/Areas/Identity/Views/Register/
- [X] T067 [P] [US1] Implement VerifyEmail controller and confirmation view in Mentoory.Web/Areas/Identity/Controllers/VerifyEmailController.cs and Views/
- [X] T068 [P] [US1] Implement ForgotPassword and ResetPassword controllers and views in Mentoory.Web/Areas/Identity/Controllers/ and Views/
- [X] T069 [P] [US1] Implement Logout controller (POST session invalidation, cookie clear) in Mentoory.Web/Areas/Identity/Controllers/LogoutController.cs

### Web Layer — Context Selection

- [X] T070 [US1] Implement Context Selection controller (GET show contexts, POST set context, AJAX switch) in Mentoory.Web/Controllers/ContextController.cs
- [X] T071 [P] [US1] Implement Context Selection views (card-based context list, auto-select logic) in Mentoory.Web/Views/Context/
- [X] T072 [US1] Implement context-switcher.js for top-bar AJAX context switching in Mentoory.Web/wwwroot/js/context-switcher.js

### Web Layer — Platform Area (GlobalAdmin)

- [X] T073 [P] [US1] Implement Platform Incubators controller (Index, Data, Create, Details, Edit) in Mentoory.Web/Areas/Platform/Controllers/IncubatorsController.cs
- [X] T074 [P] [US1] Implement Platform Incubators views (Index with DataTable, Create form, Details, Edit form) in Mentoory.Web/Areas/Platform/Views/Incubators/
- [X] T075 [P] [US1] Implement Platform Users controller (Index, Data) with DataTable in Mentoory.Web/Areas/Platform/Controllers/UsersController.cs
- [X] T076 [P] [US1] Implement Platform Users views (Index with DataTable listing all platform users) in Mentoory.Web/Areas/Platform/Views/Users/

### Web Layer — Administration Area (IncubatorAdmin)

- [X] T077 [P] [US1] Implement Administration Dashboard controller and view (incubator overview) in Mentoory.Web/Areas/Administration/
- [X] T078 [P] [US1] Implement Administration Projects controller (Index, Data, Create, Details) in Mentoory.Web/Areas/Administration/Controllers/ProjectsController.cs
- [X] T079 [P] [US1] Implement Administration Projects views (Index with DataTable, Create form, Details) in Mentoory.Web/Areas/Administration/Views/Projects/
- [X] T080 [P] [US1] Implement Administration Users controller (Index, Data, Enroll) with administrative enrollment in Mentoory.Web/Areas/Administration/Controllers/UsersController.cs
- [X] T081 [P] [US1] Implement Administration Users views (Index with DataTable, Enroll form with specific field conflict feedback) in Mentoory.Web/Areas/Administration/Views/Users/

### Web Layer — Integration

- [X] T082 [US1] Wire up Identity, Authorization, and Tenant services in Program.cs and configure Aspire SQL Server enrichment for all three DbContexts in Mentoory.Web/Program.cs

### Tests — US1

- [X] T082a [P] [US1] Implement Identity domain unit tests (User aggregate, AuthSession lifecycle, value objects, credential validation) in Mentoory.Identity.Tests/
- [X] T082b [P] [US1] Implement Authorization domain unit tests (RoleAssignment rules, entrepreneur one-active-project constraint) in Mentoory.Authorization.Tests/
- [X] T082c [P] [US1] Implement Tenant domain unit tests (Incubator, Project stage initialization, MentorAssignment lead flag) in Mentoory.Tenant.Tests/
- [X] T082d [US1] Implement integration tests for registration, login, session management, context selection, and cross-tenant data isolation using WebApplicationFactory + Testcontainers + Respawn in Mentoory.Tests.Integration/Identity/ and Mentoory.Tests.Integration/Authorization/ and Mentoory.Tests.Integration/Tenant/

**Checkpoint**: At this point, the platform has multi-tenant user management, authentication, session management, context selection, and incubator/project CRUD — fully functional and independently testable with automated test coverage

---

## Phase 4: User Story 2 — Diagnostic Assessment: Form Management & Completion (Priority: P2)

**Goal**: Clone diagnostic form templates, customize questions, entrepreneur fills out diagnostic, score aggregation per topic, answer corrections with audit trail

**Independent Test**: Clone a form template, customize it, have an entrepreneur complete the Initial evaluation, verify responses stored with topic score aggregation. Run through form clone → customize → fill → submit → correct cycle.

### Diagnostic Domain

- [X] T083 [P] [US2] Implement FormTemplate aggregate root with QuestionTemplate and AnswerOptionTemplate entities in Mentoory.Diagnostic.Domain/Aggregates/FormTemplate/
- [X] T084 [P] [US2] Implement ProjectForm aggregate root with Question, AnswerOption (score/SWOT/ODSR), and FollowUpQuestion entities in Mentoory.Diagnostic.Domain/Aggregates/ProjectForm/
- [X] T085 [P] [US2] Implement DiagnosticResponse aggregate root with QuestionResponse and AnswerCorrection entities in Mentoory.Diagnostic.Domain/Aggregates/DiagnosticResponse/
- [X] T086 [P] [US2] Implement Diagnostic enums (QuestionType, EvaluationStage, StageApplicability, SwotClassification, OdsrOrientation) in Mentoory.Diagnostic.Domain/Enums/
- [X] T087 [P] [US2] Implement Diagnostic value objects (ScoreContribution, TopicScoreAggregate) in Mentoory.Diagnostic.Domain/ValueObjects/
- [X] T088 [P] [US2] Implement Diagnostic repository interfaces (IFormTemplateRepository, IProjectFormRepository, IDiagnosticResponseRepository) in Mentoory.Diagnostic.Domain/Repositories/

### Diagnostic Application

- [X] T089 [P] [US2] Implement CloneFormTemplate command with handler (deep copy template → project form) in Mentoory.Diagnostic.Application/Commands/CloneFormTemplate/
- [X] T090 [P] [US2] Implement CustomizeProjectForm command with handler (add/remove/reorder questions) in Mentoory.Diagnostic.Application/Commands/CustomizeProjectForm/
- [X] T091 [US2] Implement SubmitDiagnosticResponse command with handler (save responses, aggregate scores per topic, mark evaluation complete) in Mentoory.Diagnostic.Application/Commands/SubmitDiagnosticResponse/
- [X] T092 [P] [US2] Implement CorrectAnswer command with handler (audit trail: who, when, previous value) in Mentoory.Diagnostic.Application/Commands/CorrectAnswer/
- [X] T093 [P] [US2] Implement SyncFromTemplate command with handler (partial sync of new questions) in Mentoory.Diagnostic.Application/Commands/SyncFromTemplate/
- [X] T094 [P] [US2] Implement Diagnostic queries (GetProjectForm, GetDiagnosticResponse, ListFormTemplates with subscription filtering, GetTopicScoreAggregation) in Mentoory.Diagnostic.Application/Queries/
- [X] T095 [P] [US2] Implement Diagnostic integration events (DiagnosticCompletedEvent, AnswerCorrectedEvent) in Mentoory.Diagnostic.Application/IntegrationEvents/
- [X] T096 [US2] Implement Diagnostic Application DependencyInjection.cs in Mentoory.Diagnostic.Application/DependencyInjection.cs

### Diagnostic Infrastructure

- [X] T097 [US2] Implement DiagnosticDbContext with entity configurations for [diagnostic] schema in Mentoory.Diagnostic.Infrastructure/Persistence/DiagnosticDbContext.cs
- [X] T098 [P] [US2] Implement FormTemplateRepository, ProjectFormRepository, and DiagnosticResponseRepository in Mentoory.Diagnostic.Infrastructure/Persistence/Repositories/
- [X] T099 [US2] Implement Diagnostic Infrastructure DependencyInjection.cs in Mentoory.Diagnostic.Infrastructure/DependencyInjection.cs

### Diagnostic SSDT

- [X] T100 [P] [US2] Create SSDT table definitions for [diagnostic] schema (FormTemplates, ProjectForms, Questions, AnswerOptions, FollowUpQuestions, DiagnosticResponses, QuestionResponses, QuestionResponseOptions, AnswerCorrections) in Mentoory.Db/diagnostic/Tables/

### Web Layer — Coordination Diagnostics

- [X] T101 [P] [US2] Implement Coordination Diagnostics controller (Index, Clone, Details/Customize) in Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs
- [X] T102 [P] [US2] Implement Coordination Diagnostics views (form list, clone template selection, form customization editor) in Mentoory.Web/Areas/Coordination/Views/Diagnostics/
- [X] T103 [P] [US2] Implement Platform global diagnostic templates controller and views in Mentoory.Web/Areas/Platform/Controllers/TemplatesController.cs (Diagnostics section)

### Web Layer — Participant Diagnostic

- [X] T104 [P] [US2] Implement Participant Diagnostic controller (Index showing stage-filtered questions, Submit) in Mentoory.Web/Areas/Participant/Controllers/DiagnosticController.cs
- [X] T105 [P] [US2] Implement Participant Diagnostic views (form filling with question types, follow-ups, submission confirmation) in Mentoory.Web/Areas/Participant/Views/Diagnostic/

### Web Layer — Answer Correction

- [X] T106 [US2] Implement answer correction UI accessible to authorized users (mentor, coordinator, incubator admin, global admin) with audit trail display
- [X] T107 [US2] Wire up Diagnostic services in Program.cs and configure Aspire enrichment for DiagnosticDbContext

### Tests — US2

- [X] T107a [P] [US2] Implement Diagnostic domain unit tests (FormTemplate cloning, Question/AnswerOption scoring, DiagnosticResponse aggregation, AnswerCorrection audit) in Mentoory.Diagnostic.Tests/
- [X] T107b [US2] Implement Diagnostic integration tests (clone template, submit responses, verify score aggregation, correct answer with audit trail) in Mentoory.Tests.Integration/Diagnostic/

**Checkpoint**: Diagnostic assessment fully functional — templates can be cloned, customized, filled by entrepreneurs, and corrected with audit trail

---

## Phase 5: User Story 3 — Knowledge Structure Management (Priority: P3)

**Goal**: Hierarchical knowledge structures (Structure → Module → Topic → Subject → Resource), template cloning, topic-diagnostic linking

**Independent Test**: Create a knowledge structure with full hierarchy, clone a global template, customize it, verify topics link to diagnostic questions.

### Knowledge Domain

- [ ] T108 [P] [US3] Implement KnowledgeTemplate aggregate root (global template with IsTemplate flag) in Mentoory.Knowledge.Domain/Aggregates/KnowledgeTemplate/
- [ ] T109 [P] [US3] Implement KnowledgeStructure aggregate root with Module, Topic (with priority score ranges), Subject (with duration), Resource entities in Mentoory.Knowledge.Domain/Aggregates/KnowledgeStructure/
- [ ] T110 [P] [US3] Implement Knowledge enums (ResourceType, PriorityLevel), PriorityScoreRange value object, and repository interfaces in Mentoory.Knowledge.Domain/

### Knowledge Application

- [ ] T111 [P] [US3] Implement CloneKnowledgeTemplate command with handler (deep copy full hierarchy) in Mentoory.Knowledge.Application/Commands/CloneKnowledgeTemplate/
- [ ] T112 [P] [US3] Implement knowledge management commands (AddModule, AddTopic, AddSubject, AddResource, ConfigureTopicPriorityRanges) in Mentoory.Knowledge.Application/Commands/
- [ ] T113 [P] [US3] Implement Knowledge queries (GetKnowledgeStructure with full tree, GetTopicsByIds) in Mentoory.Knowledge.Application/Queries/
- [ ] T114 [US3] Implement Knowledge Application DependencyInjection.cs in Mentoory.Knowledge.Application/DependencyInjection.cs

### Knowledge Infrastructure

- [ ] T115 [US3] Implement KnowledgeDbContext and repositories (KnowledgeTemplateRepository, KnowledgeStructureRepository) in Mentoory.Knowledge.Infrastructure/Persistence/
- [ ] T116 [US3] Implement Knowledge Infrastructure DependencyInjection.cs in Mentoory.Knowledge.Infrastructure/DependencyInjection.cs

### Knowledge SSDT

- [ ] T117 [P] [US3] Create SSDT table definitions for [knowledge] schema (KnowledgeStructures, Modules, Topics with priority score columns, Subjects, Resources) in Mentoory.Db/knowledge/Tables/

### Web Layer

- [ ] T118 [P] [US3] Implement Coordination Knowledge controller (Index, Clone, Details tree view) in Mentoory.Web/Areas/Coordination/Controllers/KnowledgeController.cs
- [ ] T119 [P] [US3] Implement Coordination Knowledge views (structure list, clone template, tree view with modules/topics/subjects/resources) in Mentoory.Web/Areas/Coordination/Views/Knowledge/
- [ ] T120 [P] [US3] Implement Platform global knowledge templates controller and views in Mentoory.Web/Areas/Platform/Controllers/TemplatesController.cs (Knowledge section)
- [ ] T121 [US3] Wire up Knowledge services in Program.cs and configure Aspire enrichment for KnowledgeDbContext
- [ ] T121a [US3] Implement implicit form-to-knowledge-structure association: when a diagnostic form template is cloned into a project, the linked knowledge structure is automatically selected/cloned as well (FR-024) in Mentoory.Diagnostic.Application/Commands/CloneFormTemplate/ and Mentoory.Knowledge.Application/Commands/CloneKnowledgeTemplate/

### Tests — US3

- [ ] T121b [P] [US3] Implement Knowledge domain unit tests (KnowledgeStructure hierarchy, PriorityScoreRange validation, template cloning) in Mentoory.Knowledge.Tests/
- [ ] T121c [US3] Implement Knowledge integration tests (clone template, manage hierarchy, verify implicit form-knowledge association FR-024) in Mentoory.Tests.Integration/Knowledge/

**Checkpoint**: Knowledge structures fully functional — templates can be cloned, hierarchy managed, topics link to diagnostic questions, and diagnostic form selection implicitly selects the associated knowledge structure

---

## Phase 6: User Story 4 — Mentoring Plan Generation (Priority: P4)

**Goal**: Auto-generate mentoring plan from diagnostic scores, mentor-entrepreneur collaborative adjustment, plan approval

**Independent Test**: Complete a diagnostic, verify auto-suggested topics with priority mapping, adjust plan collaboratively, approve, and verify saved plan.

### Mentoring Domain (Plan)

- [ ] T122 [P] [US4] Implement MentoringPlan aggregate root with PlanTopic entity (priority, manual override, SWOT/ODSR summaries) and PlanApproval entity (who approved, when) in Mentoory.Mentoring.Domain/Aggregates/MentoringPlan/
- [ ] T123 [P] [US4] Implement PlanStatus enum (Draft, Approved, InProgress, Completed) and IMentoringPlanRepository interface in Mentoory.Mentoring.Domain/

### Mentoring Application (Plan)

- [ ] T124 [US4] Implement GenerateSuggestedPlan command with handler (aggregate diagnostic scores per topic, map to priority levels via topic score ranges, auto-include High/Medium, flag Low as optional, exclude NotApplicable) in Mentoory.Mentoring.Application/Commands/GenerateSuggestedPlan/
- [ ] T125 [P] [US4] Implement AdjustPlanTopic command (include/exclude topic with manual override flag and justification) in Mentoory.Mentoring.Application/Commands/AdjustPlanTopic/
- [ ] T126 [P] [US4] Implement ApproveMentoringPlan command with handler (save approval record) in Mentoory.Mentoring.Application/Commands/ApproveMentoringPlan/
- [ ] T127 [P] [US4] Implement Mentoring plan queries (GetMentoringPlan, GetSuggestedTopics with SWOT/ODSR context) in Mentoory.Mentoring.Application/Queries/
- [ ] T128 [US4] Implement DiagnosticCompletedEventHandler to auto-trigger plan generation in Mentoory.Mentoring.Application/IntegrationEvents/Handlers/DiagnosticCompletedEventHandler.cs
- [ ] T129 [US4] Implement Mentoring Application DependencyInjection.cs (plan portion) in Mentoory.Mentoring.Application/DependencyInjection.cs

### Mentoring Infrastructure (Plan)

- [ ] T130 [US4] Implement MentoringDbContext with entity configurations for plan tables and MentoringPlanRepository in Mentoory.Mentoring.Infrastructure/Persistence/
- [ ] T131 [US4] Implement Mentoring Infrastructure DependencyInjection.cs in Mentoory.Mentoring.Infrastructure/DependencyInjection.cs

### Mentoring SSDT (Plan)

- [ ] T132 [P] [US4] Create SSDT table definitions for mentoring plan tables (MentoringPlans, PlanTopics) in Mentoory.Db/mentoring/Tables/

### Web Layer

- [ ] T133 [P] [US4] Implement Mentoring Plans controller (Index, Details, AdjustTopic AJAX, Approve) in Mentoory.Web/Areas/Mentoring/Controllers/PlansController.cs
- [ ] T134 [P] [US4] Implement Mentoring Plans views (plan list, detail with topic grid, priority indicators, SWOT/ODSR context, adjust/approve buttons) in Mentoory.Web/Areas/Mentoring/Views/Plans/
- [ ] T135 [P] [US4] Implement Participant Plan controller and view (read-only mentoring plan view) in Mentoory.Web/Areas/Participant/Controllers/PlanController.cs and Views/
- [ ] T136 [US4] Wire up Mentoring plan services in Program.cs and configure Aspire enrichment for MentoringDbContext

### Tests — US4

- [ ] T136a [P] [US4] Implement Mentoring plan domain unit tests (MentoringPlan aggregate, PlanTopic priority mapping, PlanApproval) in Mentoory.Mentoring.Tests/
- [ ] T136b [US4] Implement Mentoring plan integration tests (generate plan from diagnostic scores, adjust topics, approve plan) in Mentoory.Tests.Integration/Mentoring/

**Checkpoint**: Mentoring plan generation fully functional — diagnostics drive auto-suggestions, mentors adjust collaboratively, plans are approved and persisted

---

## Phase 7: User Story 5 — Mentoring Execution: Scheduling, Sessions & Assignments (Priority: P5)

**Goal**: Session calendar generation, flexible session logging, assignment creation/submission/review cycle

**Independent Test**: Generate a session calendar from an approved plan, conduct a session with topic logging, create an assignment, submit work, and review it.

### Mentoring Domain (Execution)

- [ ] T137 [P] [US5] Implement SessionCalendar aggregate root with MentoringSession entity (scheduling, status transitions, session logging) in Mentoory.Mentoring.Domain/Aggregates/SessionCalendar/
- [ ] T138 [P] [US5] Implement Assignment aggregate root with Submission and ReviewFeedback entities (assignment lifecycle) in Mentoory.Mentoring.Domain/Aggregates/Assignment/
- [ ] T139 [P] [US5] Implement execution enums (SessionStatus, AssignmentStatus) and repository interfaces (ISessionCalendarRepository, IAssignmentRepository) in Mentoory.Mentoring.Domain/

### Mentoring Application (Execution)

- [ ] T140 [US5] Implement GenerateSessionCalendar command with scheduling algorithm (distribute sessions across weeks based on sessions/week, hours/session, subject durations) in Mentoory.Mentoring.Application/Commands/GenerateSessionCalendar/
- [ ] T141 [P] [US5] Implement LogSession command with handler (notes, topics covered, decisions, conducting mentor) in Mentoory.Mentoring.Application/Commands/LogSession/
- [ ] T142 [P] [US5] Implement CreateAssignment command with handler (link to subject, instructions, deadline) in Mentoory.Mentoring.Application/Commands/CreateAssignment/
- [ ] T143 [P] [US5] Implement SubmitAssignment command with handler (content or file, entrepreneur submission) in Mentoory.Mentoring.Application/Commands/SubmitAssignment/
- [ ] T144 [P] [US5] Implement ReviewAssignment command with handler (feedback, approve/revision decision) in Mentoory.Mentoring.Application/Commands/ReviewAssignment/
- [ ] T145 [P] [US5] Implement execution queries (GetSessionCalendar, ListAssignments) in Mentoory.Mentoring.Application/Queries/
- [ ] T146 [US5] Update Mentoring DependencyInjection.cs with execution command/query registrations

### Mentoring Infrastructure (Execution)

- [ ] T147 [US5] Update MentoringDbContext with entity configurations for session and assignment tables in Mentoory.Mentoring.Infrastructure/Persistence/
- [ ] T148 [P] [US5] Implement SessionCalendarRepository and AssignmentRepository in Mentoory.Mentoring.Infrastructure/Persistence/Repositories/

### Mentoring SSDT (Execution)

- [ ] T149 [P] [US5] Create SSDT table definitions for mentoring execution tables (SessionCalendars, MentoringSessions, SessionTopicsCovered, Assignments, Submissions, ReviewFeedback) in Mentoory.Db/mentoring/Tables/

### Web Layer — Mentor Sessions

- [ ] T150 [P] [US5] Implement Mentoring Sessions controller (Index calendar, Details, Log) in Mentoory.Web/Areas/Mentoring/Controllers/SessionsController.cs
- [ ] T151 [P] [US5] Implement Mentoring Sessions views (session calendar, session detail with topic selection, log form with notes/decisions) in Mentoory.Web/Areas/Mentoring/Views/Sessions/

### Web Layer — Mentor Assignments

- [ ] T152 [P] [US5] Implement Mentoring Assignments controller (Index, Create, Details, Review) in Mentoory.Web/Areas/Mentoring/Controllers/AssignmentsController.cs
- [ ] T153 [P] [US5] Implement Mentoring Assignments views (list, create form with subject link, detail with submissions, review form) in Mentoory.Web/Areas/Mentoring/Views/Assignments/

### Web Layer — Participant Sessions & Assignments

- [ ] T154 [P] [US5] Implement Participant Sessions controller and views (upcoming sessions list) in Mentoory.Web/Areas/Participant/Controllers/SessionsController.cs and Views/
- [ ] T155 [P] [US5] Implement Participant Assignments controller (Index, Details, Submit) and views (list, detail, submit work form) in Mentoory.Web/Areas/Participant/Controllers/AssignmentsController.cs and Views/
- [ ] T156 [US5] Wire up Mentoring execution services in Program.cs

### Tests — US5

- [ ] T156a [P] [US5] Implement execution domain unit tests (SessionCalendar scheduling algorithm, Assignment lifecycle, session logging) in Mentoory.Mentoring.Tests/
- [ ] T156b [US5] Implement execution integration tests (generate calendar, log session, create/submit/review assignment) in Mentoory.Tests.Integration/Mentoring/

**Checkpoint**: Full mentoring execution lifecycle — session scheduling, flexible topic coverage, assignment creation/submission/review

---

## Phase 8: User Story 6 — Subscription & Plan Management (Priority: P6)

**Goal**: Subscription plans with boolean/quantitative features, incubator plan assignment, positive-only overrides, limit enforcement

**Independent Test**: Create subscription plans with features, assign to incubators, apply overrides, verify effective feature limits.

### Subscription Domain

- [ ] T157 [P] [US6] Implement SubscriptionPlan aggregate root with PlanFeature and IncubatorOverride entities in Mentoory.Subscription.Domain/Aggregates/SubscriptionPlan/
- [ ] T158 [P] [US6] Implement FeatureType enum (Boolean, Quantitative) and ISubscriptionPlanRepository interface in Mentoory.Subscription.Domain/

### Subscription Application

- [ ] T159 [P] [US6] Implement CreateSubscriptionPlan command with handler (versioned plan creation with features) in Mentoory.Subscription.Application/Commands/CreateSubscriptionPlan/
- [ ] T160 [P] [US6] Implement AssignPlanToIncubator command with handler in Mentoory.Subscription.Application/Commands/AssignPlanToIncubator/
- [ ] T161 [P] [US6] Implement ApplyOverride command with handler (positive-only accumulative, no expiration) in Mentoory.Subscription.Application/Commands/ApplyOverride/
- [ ] T162 [P] [US6] Implement Subscription queries (GetEffectiveFeatures with plan + overrides, CheckFeatureLimit, ListSubscriptionPlans) in Mentoory.Subscription.Application/Queries/
- [ ] T163 [US6] Implement Subscription Application DependencyInjection.cs in Mentoory.Subscription.Application/DependencyInjection.cs

### Subscription Infrastructure

- [ ] T164 [US6] Implement SubscriptionDbContext and SubscriptionPlanRepository in Mentoory.Subscription.Infrastructure/Persistence/
- [ ] T165 [US6] Implement Subscription Infrastructure DependencyInjection.cs in Mentoory.Subscription.Infrastructure/DependencyInjection.cs

### Subscription SSDT

- [ ] T166 [P] [US6] Create SSDT table definitions for [subscription] schema (SubscriptionPlans, PlanFeatures, IncubatorOverrides) in Mentoory.Db/subscription/Tables/

### Web Layer

- [ ] T167 [P] [US6] Implement Platform Subscriptions controller (Index, Data, Create, Details, AddFeature) in Mentoory.Web/Areas/Platform/Controllers/SubscriptionsController.cs
- [ ] T168 [P] [US6] Implement Platform Subscriptions views (Index with DataTable, Create form, Details with feature grid) in Mentoory.Web/Areas/Platform/Views/Subscriptions/
- [ ] T169 [US6] Implement Incubator plan assignment (AssignPlan) and override (ApplyOverride) endpoints in Platform IncubatorsController and corresponding view sections
- [ ] T170 [US6] Integrate subscription limit checks into project creation flow (CheckFeatureLimit before CreateProject) and wire up services in Program.cs

### Tests — US6

- [ ] T170a [P] [US6] Implement Subscription domain unit tests (SubscriptionPlan, PlanFeature, positive-only IncubatorOverride, effective limit calculation) in Mentoory.Subscription.Tests/

**Checkpoint**: Subscription management fully functional — plans with features, assignments, overrides, and limit enforcement

---

## Phase 9: User Story 7 — Project Lifecycle Management (Priority: P7)

**Goal**: Fixed 7-stage project lifecycle with manual advancement, stage-driven UI visibility

**Independent Test**: Create a project, advance through stages sequentially, verify stage-driven UI changes, confirm no stage skipping.

- [ ] T171 [US7] Implement AdvanceProjectStage command with handler (sequential validation, no skipping, no backward movement) in Mentoory.Tenant.Application/Commands/AdvanceProjectStage/
- [ ] T172 [P] [US7] Implement ProjectStageAdvancedEvent integration event in Mentoory.Tenant.Application/IntegrationEvents/ProjectStageAdvancedEvent.cs
- [ ] T173 [P] [US7] Implement Coordination Dashboard controller and overview view in Mentoory.Web/Areas/Coordination/Controllers/DashboardController.cs and Views/
- [ ] T174 [P] [US7] Implement Coordination Lifecycle controller (Index with stage visualization, Advance action) in Mentoory.Web/Areas/Coordination/Controllers/LifecycleController.cs
- [ ] T175 [P] [US7] Implement Coordination Lifecycle views (stage pipeline visualization, advance controls, current stage indicator) in Mentoory.Web/Areas/Coordination/Views/Lifecycle/
- [ ] T176 [P] [US7] Implement Coordination Participants controller (Index, Enroll, AssignMentor) in Mentoory.Web/Areas/Coordination/Controllers/ParticipantsController.cs and Views/
- [ ] T177 [US7] Implement stage-driven UI visibility logic (conditionally show/hide actions based on project's current stage) across Coordination, Participant, and Mentoring areas

### Tests — US7

- [ ] T177a [P] [US7] Implement lifecycle unit tests (sequential stage advancement, skip prevention, backward movement prevention) in Mentoory.Tenant.Tests/

**Checkpoint**: Project lifecycle management fully functional — stages advance sequentially, UI adapts to current stage

---

## Phase 10: User Story 8 — Notification System (Priority: P8)

**Goal**: Centralized notification engine with email delivery, deduplication, scheduling, and user preferences

**Independent Test**: Trigger notification events (session reminder, assignment created), verify email delivery, confirm deduplication, test preference overrides.

### Notification Domain

- [ ] T178 [P] [US8] Implement Notification aggregate root with NotificationRecipient entity (delivery tracking per recipient) in Mentoory.Notification.Domain/Aggregates/Notification/
- [ ] T179 [P] [US8] Implement NotificationPreference entity in Mentoory.Notification.Domain/Aggregates/Notification/NotificationPreference.cs
- [ ] T180 [P] [US8] Implement Notification enums (DeliveryChannel, DeliveryStatus, NotificationType) and repository interfaces in Mentoory.Notification.Domain/

### Notification Application

- [ ] T181 [P] [US8] Implement SendNotification command with handler (compose, check preferences, check deduplication via SourceEventId, queue for delivery) in Mentoory.Notification.Application/Commands/SendNotification/
- [ ] T182 [P] [US8] Implement ScheduleNotification command with handler (set ScheduledForUtc, validate event still valid before send) in Mentoory.Notification.Application/Commands/ScheduleNotification/
- [ ] T183 [P] [US8] Implement UpdatePreferences command with handler (per role, per context, per notification type) in Mentoory.Notification.Application/Commands/UpdatePreferences/
- [ ] T184 [P] [US8] Implement Notification queries (GetNotificationLog, GetUserPreferences) in Mentoory.Notification.Application/Queries/
- [ ] T185 [US8] Implement notification event handlers (SessionScheduledEventHandler, AssignmentCreatedEventHandler, StageAdvancedEventHandler) in Mentoory.Notification.Application/IntegrationEvents/Handlers/
- [ ] T186 [US8] Implement Notification Application DependencyInjection.cs in Mentoory.Notification.Application/DependencyInjection.cs

### Notification Infrastructure

- [ ] T187 [US8] Implement NotificationDbContext and repositories (NotificationRepository, NotificationPreferenceRepository) in Mentoory.Notification.Infrastructure/Persistence/
- [ ] T188 [US8] Implement EmailService with MailKit SMTP delivery (configurable SMTP settings, retry policy) in Mentoory.Notification.Infrastructure/Services/EmailService.cs
- [ ] T189 [US8] Implement Notification Infrastructure DependencyInjection.cs in Mentoory.Notification.Infrastructure/DependencyInjection.cs

### Notification SSDT

- [ ] T190 [P] [US8] Create SSDT table definitions for [notification] schema (Notifications, NotificationRecipients, NotificationPreferences) in Mentoory.Db/notification/Tables/

### Web Layer

- [ ] T191 [US8] Wire up Notification services in Program.cs and configure Aspire enrichment for NotificationDbContext

### Tests — US8

- [ ] T191a [P] [US8] Implement Notification domain unit tests (deduplication via SourceEventId, preference filtering, delivery status transitions) in Mentoory.Notification.Tests/

**Checkpoint**: Notification system fully functional — events trigger notifications, preferences respected, deduplication works, email delivery via MailKit

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Remaining dashboards, health checks, final validation

- [ ] T192 [P] Implement Mentoring Dashboard controller and view (mentor overview with upcoming sessions, pending assignments) in Mentoory.Web/Areas/Mentoring/Controllers/DashboardController.cs and Views/
- [ ] T193 [P] Implement Participant Dashboard controller and view (entrepreneur overview with current stage, upcoming sessions, pending assignments) in Mentoory.Web/Areas/Participant/Controllers/DashboardController.cs and Views/
- [ ] T194 [P] Implement Sponsor Dashboard and Progress controllers and views (read-only project overview, progress metrics) in Mentoory.Web/Areas/Sponsor/
- [ ] T195 [P] Implement HomeController with role-based redirect to appropriate dashboard in Mentoory.Web/Controllers/HomeController.cs
- [ ] T196 Configure Aspire health checks for all 8 registered DbContexts (/health and /alive endpoints) in Mentoory.Web/Program.cs
- [ ] T197 Verify zero-warnings build across all projects (`dotnet build --configuration Release`)
- [ ] T198 Run quickstart.md validation scenarios (QS-01 through QS-14) to verify end-to-end platform functionality

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — **BLOCKS all user stories**
- **User Stories (Phase 3–10)**: All depend on Foundational phase completion
  - US1 (P1) must complete before US2–US8 can begin (provides auth, tenancy, context)
  - US2 (P2) can start after US1 — provides diagnostic data for US4
  - US3 (P3) can start after US1 — provides knowledge structure for US4
  - US4 (P4) depends on US2 and US3 — needs diagnostic scores and topic/subject data
  - US5 (P5) depends on US4 — needs an approved mentoring plan
  - US6 (P6) can start after US1 — independent subscription management
  - US7 (P7) can start after US1 — extends tenant domain with lifecycle logic
  - US8 (P8) can start after US1 — listens to events from other domains
- **Polish (Phase 11)**: Depends on all user stories being complete

### User Story Dependencies

```
US1 (Foundation) ──→ US2 (Diagnostics) ──→ US4 (Mentoring Plans) ──→ US5 (Execution)
                 ──→ US3 (Knowledge)  ──↗
                 ──→ US6 (Subscriptions) [independent]
                 ──→ US7 (Lifecycle) [independent]
                 ──→ US8 (Notifications) [independent]
```

### Within Each User Story

- Domain layer before Application layer
- Application layer before Infrastructure layer
- Infrastructure layer before Web layer
- SSDT tables can be built in parallel with domain code
- All [P] tasks within a phase can run in parallel

### Parallel Opportunities

- **Phase 1**: T002 and T003 can run in parallel after T001
- **Phase 2**: T004–T006 (Shared.Domain/Application) can run in parallel; T008–T014 (Web infrastructure) can run in parallel after T007
- **Phase 3 (US1)**: All domain BCs (Identity T016–T021, Authorization T038–T040, Tenant T049–T051) can run in parallel; SSDT tables (T037, T048, T061) can run in parallel with domain code; Web controllers/views within the same area can run in parallel
- **After US1**: US6, US7, and US8 can run in parallel (independent of each other); US2 and US3 can run in parallel
- **Phase 11**: T192–T195 dashboards can all run in parallel

---

## Parallel Example: User Story 1 Domain Layer

```bash
# Launch all three BC domain layers together:
Task: "Implement User aggregate root in Mentoory.Identity.Domain/"
Task: "Implement RoleAssignment aggregate in Mentoory.Authorization.Domain/"
Task: "Implement Incubator aggregate in Mentoory.Tenant.Domain/"

# Launch all SSDT schemas together:
Task: "Create [identity] schema tables in Mentoory.Db/identity/Tables/"
Task: "Create [authorization] schema tables in Mentoory.Db/authorization/Tables/"
Task: "Create [tenant] schema tables in Mentoory.Db/tenant/Tables/"

# Launch all Identity Area controllers together:
Task: "Implement Login controller in Mentoory.Web/Areas/Identity/"
Task: "Implement Register controller in Mentoory.Web/Areas/Identity/"
Task: "Implement VerifyEmail controller in Mentoory.Web/Areas/Identity/"
Task: "Implement ForgotPassword/ResetPassword controllers in Mentoory.Web/Areas/Identity/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Run QS-01 through QS-09, QS-12, QS-13, QS-14
5. Deploy/demo if ready — multi-tenant platform with auth and user management

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Test independently → Deploy/Demo (**MVP!**)
3. US2 + US3 (parallel) → Diagnostic assessment + Knowledge structures
4. US4 → Mentoring plan generation (needs US2 + US3)
5. US5 → Mentoring execution (needs US4)
6. US6 → Subscription management (can deploy anytime after US1)
7. US7 → Lifecycle management (can deploy anytime after US1)
8. US8 → Notifications (can deploy anytime after US1)

### Parallel Team Strategy

With multiple developers after Foundational phase:

- **Developer A**: US1 (Identity + Authorization domains)
- **Developer B**: US1 (Tenant domain + Web layer)
- After US1 complete:
  - **Developer A**: US2 (Diagnostics) → US4 (Plans) → US5 (Execution)
  - **Developer B**: US3 (Knowledge) + US6 (Subscriptions)
  - **Developer C**: US7 (Lifecycle) + US8 (Notifications)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All UI text must be in Spanish; code/docs in English
- Zero warnings required before committing (TreatWarningsAsErrors)
- External entities use ExternalId (Guid) in routes, never internal IDs
- No EF migrations — all schema via SSDT/DACPAC
- No AutoMapper — use Mapperly for object mapping
