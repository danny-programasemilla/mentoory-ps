# Tasks: Notification Domain - Email Delivery System

**Input**: Design documents from `/specs/011-notification-email-delivery/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/integration-events.md, quickstart.md

**Tests**: Not explicitly requested. Test tasks omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add new NuGet dependencies and prepare empty module projects for implementation.

- [ ] T001 Add RazorLight package to Directory.Packages.props
- [ ] T002 Add UAParser package to Directory.Packages.props
- [ ] T003 Add RazorLight PackageReference to Mentoory.Notification.Infrastructure/Mentoory.Notification.Infrastructure.csproj
- [ ] T004 Add UAParser PackageReference to Mentoory.Notification.Infrastructure/Mentoory.Notification.Infrastructure.csproj
- [ ] T005 Add Mentoory.Notification.Application PackageReference to Mentoory.Notification.Infrastructure/Mentoory.Notification.Infrastructure.csproj (if not present)
- [ ] T006 Verify solution builds with zero warnings after dependency additions

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain model, database schema, DbContext, and email infrastructure that ALL user stories depend on.

**CRITICAL**: No user story work can begin until this phase is complete.

### Domain Entities & Enums

- [ ] T007 [P] Create NotificationType enum in Mentoory.Notification.Domain/Enums/NotificationType.cs (LoginAlert=0, UserRegistration=1, ProjectInvitation=2)
- [ ] T008 [P] Create DeliveryChannel enum in Mentoory.Notification.Domain/Enums/DeliveryChannel.cs (Email=0, Sms=1, InApp=2)
- [ ] T009 [P] Create DeliveryStatus enum in Mentoory.Notification.Domain/Enums/DeliveryStatus.cs (Pending=0, Sent=1, Failed=2, Suppressed=3)
- [ ] T010 [P] Create LoginContext value object in Mentoory.Notification.Domain/Aggregates/Notification/LoginContext.cs (IpAddress, BrowserName, OperatingSystem, IsSuspicious)
- [ ] T011 [P] Create DeliveryAttempt value object in Mentoory.Notification.Domain/Aggregates/Notification/DeliveryAttempt.cs (AttemptNumber, AttemptedAtUtc, Success, FailureReason)
- [ ] T012 Create NotificationRecipient child entity in Mentoory.Notification.Domain/Aggregates/Notification/NotificationRecipient.cs (UserId, Email, DeliveryChannel, DeliveryStatus, SentAtUtc, FailureReason, _deliveryAttempts collection)
- [ ] T013 Create Notification aggregate root in Mentoory.Notification.Domain/Aggregates/Notification/Notification.cs (ExternalId, NotificationType, Subject, HtmlBody, SourceEventId, ScheduledForUtc, CreatedAtUtc, LoginContext, _recipients collection; factory method with guards)
- [ ] T014 [P] Create NotificationPreference aggregate root in Mentoory.Notification.Domain/Aggregates/NotificationPreference/NotificationPreference.cs (ExternalId, UserId, NotificationType, IsEnabled, UpdatedAtUtc; factory method with guard for system-mandatory types)

### Repository Interfaces

- [ ] T015 [P] Create INotificationRepository in Mentoory.Notification.Domain/Repositories/INotificationRepository.cs (Add, GetPendingAsync, GetBySourceEventIdAndTypeAsync, UnitOfWork)
- [ ] T016 [P] Create INotificationPreferenceRepository in Mentoory.Notification.Domain/Repositories/INotificationPreferenceRepository.cs (Add, Update, GetByUserAndTypeAsync, GetByUserAsync, UnitOfWork)

### Database Schema (SSDT)

- [ ] T017 [P] Create Notifications table in Mentoory.Db/notification/Tables/Notifications.sql (per data-model.md, includes LoginContext owned columns, deduplication index, schedule index)
- [ ] T018 [P] Create NotificationRecipients table in Mentoory.Db/notification/Tables/NotificationRecipients.sql (per data-model.md, includes filtered index on Pending status)
- [ ] T019 [P] Create DeliveryAttempts table in Mentoory.Db/notification/Tables/DeliveryAttempts.sql (per data-model.md)
- [ ] T020 [P] Create NotificationPreferences table in Mentoory.Db/notification/Tables/NotificationPreferences.sql (per data-model.md, unique index on UserId+NotificationType)

### DbContext & Repositories

- [ ] T021 Create NotificationDbContext in Mentoory.Notification.Infrastructure/Persistence/NotificationDbContext.cs (inherit SharedAbstractDbContext, DbSets for Notification and NotificationPreference, fluent configuration for all entities including owned LoginContext and DeliveryAttempt)
- [ ] T022 [P] Create NotificationRepository in Mentoory.Notification.Infrastructure/Persistence/NotificationRepository.cs (implement INotificationRepository, GetPendingAsync with Include for Recipients and DeliveryAttempts, GetBySourceEventIdAndTypeAsync, AsNoTracking on reads)
- [ ] T023 [P] Create NotificationPreferenceRepository in Mentoory.Notification.Infrastructure/Persistence/NotificationPreferenceRepository.cs (implement INotificationPreferenceRepository, AsNoTracking on reads)

### Email Infrastructure

- [ ] T024 [P] Create SmtpSettings options class in Mentoory.Notification.Infrastructure/Configuration/SmtpSettings.cs (Host, Port, Username, Password, FromAddress, FromName, UseSsl)
- [ ] T025 [P] Create NotificationSettings options class in Mentoory.Notification.Infrastructure/Configuration/NotificationSettings.cs (PollingIntervalSeconds, MaxRetryAttempts, BaseUrl)
- [ ] T026 [P] Create IEmailService interface in Mentoory.Notification.Infrastructure/Services/IEmailService.cs (SendAsync with to, subject, htmlBody, cancellationToken)
- [ ] T027 [P] Create ITemplateRenderer interface in Mentoory.Notification.Infrastructure/Services/ITemplateRenderer.cs (RenderAsync with templateName and model object)
- [ ] T028 [P] Create ILoginContextParser interface in Mentoory.Notification.Application/Services/ILoginContextParser.cs (Parse with userAgentString, ipAddress returning LoginContext)
- [ ] T029 Create MailtrapEmailService in Mentoory.Notification.Infrastructure/Services/MailtrapEmailService.cs (implement IEmailService using MailKit SmtpClient, connect to sandbox.smtp.mailtrap.io:2525)
- [ ] T030 Create MailgunEmailService in Mentoory.Notification.Infrastructure/Services/MailgunEmailService.cs (implement IEmailService using MailKit SmtpClient, connect to smtp.mailgun.org:587 with STARTTLS)
- [ ] T031 Create RazorLightTemplateRenderer in Mentoory.Notification.Infrastructure/Services/RazorLightTemplateRenderer.cs (implement ITemplateRenderer using RazorLight embedded resources project with memory caching)
- [ ] T032 Create UaParserLoginContextParser in Mentoory.Notification.Infrastructure/Services/UaParserLoginContextParser.cs (implement ILoginContextParser using UAParser, graceful degradation to "Navegador desconocido"/"Sistema operativo desconocido")

### Razor Email Templates

- [ ] T033 Create shared email layout in Mentoory.Notification.Infrastructure/Templates/_EmailLayout.cshtml (branded header with logo and color banner, card-based content area with @RenderBody(), footer with platform links, all Spanish text)
- [ ] T034 [P] Create login alert template in Mentoory.Notification.Infrastructure/Templates/LoginAlert.cshtml (IP, browser, OS, timestamp, standard tone, uses _EmailLayout)
- [ ] T035 [P] Create suspicious login alert template in Mentoory.Notification.Infrastructure/Templates/SuspiciousLoginAlert.cshtml (same data as LoginAlert but urgency styling, warning banner, uses _EmailLayout)
- [ ] T036 [P] Create user registration template in Mentoory.Notification.Infrastructure/Templates/UserRegistration.cshtml (user name, project name, verification URL, expiration, uses _EmailLayout)
- [ ] T037 [P] Create project invitation template in Mentoory.Notification.Infrastructure/Templates/ProjectInvitation.cshtml (inviter context, project name, incubator name, action URL, expiration, uses _EmailLayout)
- [ ] T038 Mark all .cshtml template files as EmbeddedResource in Mentoory.Notification.Infrastructure/Mentoory.Notification.Infrastructure.csproj

### SMTP Configuration

- [ ] T039 [P] Add Smtp and Notification sections to Mentoory.Web/appsettings.Development.json (Mailtrap credentials placeholder per quickstart.md)
- [ ] T040 [P] Add Smtp and Notification sections to Mentoory.Web/appsettings.json (Mailgun credentials placeholder per quickstart.md)

### DI Registration

- [ ] T041 Create DependencyInjection.cs in Mentoory.Notification.Application/DependencyInjection.cs (AddNotificationApplication extension: register MediatR from assembly, FluentValidation validators)
- [ ] T042 Create DependencyInjection.cs in Mentoory.Notification.Infrastructure/DependencyInjection.cs (AddNotificationInfrastructure extension: DbContext, repositories, IEmailService based on environment, ITemplateRenderer, ILoginContextParser, IHostedService, SmtpSettings/NotificationSettings binding)
- [ ] T043 Register notification module in Mentoory.Web/Program.cs (add builder.Services.AddNotificationApplication() and builder.AddNotificationInfrastructure() calls)

### Build Verification

- [ ] T044 Verify solution builds with zero warnings after foundational phase

**Checkpoint**: Foundation ready -- domain model, database schema, email infrastructure, and DI all in place. User story implementation can begin.

---

## Phase 3: User Story 1 - User Registration Email (Priority: P1) MVP

**Goal**: When a user is registered, the system sends a branded welcome/verification email in Spanish with the user's name, project name, verification link, and expiration.

**Independent Test**: Create a user via admin interface, confirm email arrives in Mailtrap with correct content and working verification link.

### Implementation for User Story 1

- [ ] T045 [US1] Create INotificationQueueService interface in Mentoory.Notification.Application/Services/INotificationQueueService.cs (QueueRegistrationEmailAsync, QueueInvitationEmailAsync, QueueLoginAlertAsync, ProcessPendingAsync)
- [ ] T046 [US1] Create NotificationQueueService in Mentoory.Notification.Infrastructure/Services/NotificationQueueService.cs (implement INotificationQueueService: create Notification aggregate with rendered template, add recipient, check deduplication via SourceEventId, persist via repository)
- [ ] T047 [US1] Create UserRegisteredNotificationHandler in Mentoory.Notification.Application/IntegrationEvents/UserRegisteredNotificationHandler.cs (implement INotificationHandler<UserRegisteredEvent>, check RequiresVerification, call INotificationQueueService.QueueRegistrationEmailAsync)
- [ ] T048 [US1] Create NotificationProcessorService in Mentoory.Notification.Infrastructure/Services/NotificationProcessorService.cs (BackgroundService with PeriodicTimer 15s, IServiceScopeFactory for scoped access, call ProcessPendingAsync, try-catch per notification, exponential backoff retry logic: 1m/5m/15m/1h/4h, max 5 attempts, record DeliveryAttempt, handle template rendering errors as immediate failure)
- [ ] T049 [US1] Verify registration email end-to-end: create user, confirm Notification record created in DB with Pending status, confirm email arrives in Mailtrap within 2 minutes, verify template content (Spanish, branded layout, verification link, expiration)

**Checkpoint**: User Story 1 fully functional. Registration emails are queued, processed by background service, and delivered via Mailtrap.

---

## Phase 4: User Story 2 - Project Invitation Email (Priority: P1)

**Goal**: When a user is invited to a project (initial or reissue), the system sends a branded invitation email with project/incubator context and action link.

**Independent Test**: Create an invitation and reissue it. Confirm both emails arrive in Mailtrap with correct project/incubator names and action URLs.

### Implementation for User Story 2

- [ ] T050 [US2] Create InvitationReissuedEvent in Mentoory.Tenant.Application/IntegrationEvents/InvitationReissuedEvent.cs (UserId, UserExternalId, Email, FirstName, LastName, ProjectExternalId, ProjectName, IncubatorName, InvitationExpiryHours, OccurredOnUtc per contracts/integration-events.md)
- [ ] T051 [US2] Publish InvitationReissuedEvent from ReissueInvitationHandler in Mentoory.Tenant.Application/Invitations/Commands/ReissueInvitation/ReissueInvitationHandler.cs (enrich with user/project/incubator names, publish via IIntegrationEventService after SaveEntitiesAsync)
- [ ] T052 [US2] Extend UserRegisteredNotificationHandler to also queue ProjectInvitation email when EnrollmentVariant is "Invitation" and ProjectExternalId is not null in Mentoory.Notification.Application/IntegrationEvents/UserRegisteredNotificationHandler.cs
- [ ] T053 [US2] Create InvitationReissuedNotificationHandler in Mentoory.Notification.Application/IntegrationEvents/InvitationReissuedNotificationHandler.cs (implement INotificationHandler<InvitationReissuedEvent>, call INotificationQueueService.QueueInvitationEmailAsync)
- [ ] T054 [US2] Verify invitation email end-to-end: create user with Invitation enrollment, confirm invitation email arrives. Reissue invitation, confirm fresh email arrives with new token. Verify deduplication (replay same event, no duplicate)

**Checkpoint**: User Stories 1 and 2 both functional. Registration and invitation emails work independently.

---

## Phase 5: User Story 3 - Login Security Alert (Priority: P2)

**Goal**: On every successful login, send a security alert with IP, browser, OS, and timestamp. Suspicious logins (3+ failed attempts or post-lockout) get elevated urgency.

**Independent Test**: Log in normally, confirm standard alert arrives. Trigger 3+ failed attempts then succeed, confirm suspicious alert with distinct subject.

### Implementation for User Story 3

- [ ] T055 [US3] Enrich LoginAttemptEvent with UserAgentString and FailedAttemptCount in Mentoory.Access.Application/IntegrationEvents/LoginAttemptEvent.cs (add string? UserAgentString and int FailedAttemptCount parameters)
- [ ] T056 [US3] Publish LoginAttemptEvent from LoginController on successful login in Mentoory.Web/Areas/Access/Controllers/LoginController.cs (populate IpAddress from HttpContext.Connection.RemoteIpAddress, UserAgentString from Request.Headers["User-Agent"], FailedAttemptCount from User aggregate)
- [ ] T057 [US3] Create LoginAttemptNotificationHandler in Mentoory.Notification.Application/IntegrationEvents/LoginAttemptNotificationHandler.cs (implement INotificationHandler<LoginAttemptEvent>, filter Success==true only, check preferences before queuing, determine suspicious flag: FailedAttemptCount >= 3, parse UA via ILoginContextParser, call INotificationQueueService.QueueLoginAlertAsync)
- [ ] T058 [US3] Verify login alert end-to-end: log in, confirm standard alert arrives with IP/browser/OS. Fail 3+ times then succeed, confirm suspicious alert with "Actividad sospechosa detectada" subject. Test unknown User-Agent, confirm "Navegador desconocido"/"Sistema operativo desconocido" in email

**Checkpoint**: All three notification types functional. Login alerts work with standard and suspicious variants.

---

## Phase 6: User Story 4 - Notification Preferences Management (Priority: P2)

**Goal**: Users can disable login alerts via a global toggle. Registration and invitation emails are system-mandatory and cannot be disabled.

**Independent Test**: Disable login alerts for a user, log in, confirm no alert email. Verify registration/invitation emails still send regardless.

### Implementation for User Story 4

- [ ] T059 [US4] Create UpdateNotificationPreferenceCommand in Mentoory.Notification.Application/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommand.cs (UserId, NotificationType, IsEnabled)
- [ ] T060 [US4] Create UpdateNotificationPreferenceValidator in Mentoory.Notification.Application/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceValidator.cs (reject disabling system-mandatory types: UserRegistration, ProjectInvitation; Spanish validation messages)
- [ ] T061 [US4] Create UpdateNotificationPreferenceHandler in Mentoory.Notification.Application/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceHandler.cs (create or update NotificationPreference aggregate, persist via repository)
- [ ] T062 [P] [US4] Create GetNotificationPreferencesQuery in Mentoory.Notification.Application/Queries/GetNotificationPreferences/GetNotificationPreferencesQuery.cs (UserId)
- [ ] T063 [P] [US4] Create NotificationPreferenceDto in Mentoory.Notification.Application/Queries/GetNotificationPreferences/NotificationPreferenceDto.cs
- [ ] T064 [US4] Create GetNotificationPreferencesHandler in Mentoory.Notification.Application/Queries/GetNotificationPreferences/GetNotificationPreferencesHandler.cs (query preferences via repository, AsNoTracking, return list of DTOs)
- [ ] T065 [US4] Add preference check to LoginAttemptNotificationHandler before queuing in Mentoory.Notification.Application/IntegrationEvents/LoginAttemptNotificationHandler.cs (query INotificationPreferenceRepository, if disabled skip silently, if lookup fails default to sending with error log)
- [ ] T066 [US4] Verify preferences end-to-end: disable login alerts for user, log in, confirm no alert email. Attempt to disable registration type, confirm rejection. Verify registration email still sends. Simulate preference lookup failure, confirm fail-open behavior

**Checkpoint**: Full notification preferences working. Login alerts respect user toggle. System emails always send.

---

## Phase 7: User Story 5 - Email Provider Switching (Priority: P3)

**Goal**: Development uses Mailtrap, production uses Mailgun. Switching is configuration-only.

**Independent Test**: Run with dev config, emails arrive in Mailtrap. Switch to prod config, emails route through Mailgun.

### Implementation for User Story 5

- [ ] T067 [US5] Verify MailtrapEmailService connects and sends via Mailtrap in development environment (run app with appsettings.Development.json, send test email, confirm arrival in Mailtrap inbox)
- [ ] T068 [US5] Verify MailgunEmailService configuration is correct for production (validate SMTP host smtp.mailgun.org:587, STARTTLS, mail.mentoory.com credentials in appsettings.json)
- [ ] T069 [US5] Verify DI registration selects correct provider based on IHostEnvironment.IsDevelopment() in Mentoory.Notification.Infrastructure/DependencyInjection.cs
- [ ] T070 [US5] Verify missing/invalid SMTP credentials produce clear error in DeliveryAttempt.FailureReason and notification is marked for retry

**Checkpoint**: Both email providers verified. Environment-based switching confirmed.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup across all user stories.

- [ ] T071 Verify solution builds with zero warnings (TreatWarningsAsErrors)
- [ ] T072 Verify all email templates render correctly in Spanish with consistent branded layout
- [ ] T073 Verify deduplication works across all notification types (replay events, confirm no duplicates)
- [ ] T074 Verify exponential backoff retry: simulate SMTP failure, confirm retry schedule 1m/5m/15m/1h/4h, confirm Failed after 5 attempts
- [ ] T075 Verify background service recovers after crash: stop and restart app, confirm pending notifications are picked up
- [ ] T076 Run quickstart.md validation end-to-end with fresh Mailtrap inbox
- [ ] T077 Publish DACPAC and verify all notification schema tables are created correctly

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies -- can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion -- BLOCKS all user stories
- **US1 Registration Email (Phase 3)**: Depends on Foundational -- MVP target
- **US2 Invitation Email (Phase 4)**: Depends on Foundational + T045-T046 (queue service from US1)
- **US3 Login Alert (Phase 5)**: Depends on Foundational + T045-T046 (queue service from US1)
- **US4 Preferences (Phase 6)**: Depends on US3 (adds preference check to login handler)
- **US5 Provider Switching (Phase 7)**: Depends on Foundational (email services exist)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US1: Registration) ─┬→ Phase 4 (US2: Invitation)
                                                                          ├→ Phase 5 (US3: Login Alert) → Phase 6 (US4: Preferences)
                                                                          └→ Phase 7 (US5: Provider Switching)
                                                                          
All → Phase 8 (Polish)
```

- **US1** is the MVP -- delivers the queue service, processor, and first email type
- **US2, US3, US5** can proceed in parallel after US1 (they reuse the queue service)
- **US4** depends on US3 (it adds preference checking to the login handler)

### Within Each User Story

- Domain entities before repositories
- Repositories before services
- Services before event handlers
- Event handlers before end-to-end verification

### Parallel Opportunities

**Phase 2 parallelism** (T007-T040):
- All enums (T007-T009) in parallel
- All value objects (T010-T011) in parallel
- All SSDT tables (T017-T020) in parallel
- All interfaces (T015-T016, T026-T028) in parallel
- Email services (T029-T030) in parallel with template renderer (T031)
- All Razor templates (T034-T037) in parallel after layout (T033)
- Config files (T039-T040) in parallel

**Post-US1 parallelism** (Phases 4, 5, 7):
- US2 (Invitation), US3 (Login Alert), and US5 (Provider Switching) can run in parallel

---

## Parallel Example: Phase 2 Foundational

```
# Batch 1: All enums and value objects (T007-T011) in parallel
# Batch 2: All entities (T012-T014) -- T012 depends on T010-T011, T013 depends on T012
# Batch 3: Repository interfaces (T015-T016) and SSDT tables (T017-T020) in parallel
# Batch 4: DbContext (T021) then repositories (T022-T023) in parallel
# Batch 5: All service interfaces (T024-T028) in parallel
# Batch 6: Service implementations (T029-T032) and layout template (T033) in parallel
# Batch 7: Content templates (T034-T037) in parallel, then embed (T038)
# Batch 8: Config (T039-T040), DI (T041-T043) in parallel
# Batch 9: Build verification (T044)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (dependency additions)
2. Complete Phase 2: Foundational (domain, DB, email infra, DI)
3. Complete Phase 3: User Story 1 (registration email)
4. **STOP and VALIDATE**: Create a user, confirm email arrives in Mailtrap
5. Deploy/demo if ready -- the notification pipeline is proven

### Incremental Delivery

1. Setup + Foundational -> Foundation ready
2. Add US1 (Registration) -> Test -> **MVP!**
3. Add US2 (Invitation) -> Test -> Two email types working
4. Add US3 (Login Alert) -> Test -> Security alerts active
5. Add US4 (Preferences) -> Test -> User control over notifications
6. Add US5 (Provider Switching) -> Test -> Production-ready
7. Polish -> Full verification -> Ship

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All email template text must be in Spanish
- All code must compile with zero warnings
- Use ITimeProvider for all DateTime access in Application layer
- Use ExternalId for any routes (never internal IDs)
