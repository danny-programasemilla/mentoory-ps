# Tasks: Notification Generic Delivery

**Input**: Design documents from `/specs/014-notification-generic-delivery/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested. Test tasks omitted.

**Organization**: Tasks are grouped by user story. Since this is a big-bang refactoring, Phase 2 (Foundational) is larger than usual — it creates all new infrastructure before user story phases add domain-specific handlers and templates.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (New Projects & Shared Interfaces)

**Purpose**: Create the new Notification.Contracts project and add shared interfaces to Mentoory.Shared. No existing code is modified — purely additive.

- [X] T001 Create `Mentoory.Notification.Contracts/Mentoory.Notification.Contracts.csproj` with references to `Mentoory.Shared.Application` and `Mentoory.Notification.Domain`
- [X] T002 [P] Create `NotificationRequestedEvent` record in `Mentoory.Notification.Contracts/IntegrationEvents/NotificationRequestedEvent.cs` extending `IntegrationEvent` with fields: NotificationType, Subject, HtmlBody, RecipientUserId, RecipientEmail, SourceEventId
- [X] T003 [P] Create `IEmailLayoutWrapper` interface in `Mentoory.Shared.Application/Notifications/IEmailLayoutWrapper.cs` with method `string WrapInBrandLayout(string innerHtml)`
- [X] T004 [P] Create `ITemplateRenderer` interface in `Mentoory.Shared.Application/Notifications/ITemplateRenderer.cs` with method `Task<string> RenderAsync(string templateName, object model)`
- [X] T005 Create `EmailLayoutWrapper` implementation in `Mentoory.Shared.Infrastructure/Notifications/EmailLayoutWrapper.cs` — extract the HTML structure from `Mentoory.Notification.Infrastructure/Templates/_EmailLayout.cshtml` (header banner, card wrapper, footer with Spanish text) into a string-based wrapper
- [X] T006 Register `IEmailLayoutWrapper` in `Mentoory.Shared.Infrastructure/DependencyInjection.cs`
- [X] T007 Add `Mentoory.Notification.Contracts` project to the solution file

**Checkpoint**: New shared infrastructure is in place. No existing behavior has changed.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create domain-specific template renderers and configure project references. After this phase, originating domains can render templates and publish NotificationRequestedEvent.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T008 Add RazorLight 2.3.1 package reference and `<EmbeddedResource Include="Templates\*.cshtml"/>` to `Mentoory.Access.Infrastructure/Mentoory.Access.Infrastructure.csproj`
- [X] T009 [P] Create `AccessTemplateRenderer` (implements `ITemplateRenderer` via RazorLight with embedded resources from Access.Infrastructure assembly) in `Mentoory.Access.Infrastructure/Services/AccessTemplateRenderer.cs`
- [X] T010 [P] Add RazorLight 2.3.1 package reference and `<EmbeddedResource Include="Templates\*.cshtml"/>` to `Mentoory.Tenant.Infrastructure/Mentoory.Tenant.Infrastructure.csproj`
- [X] T011 [P] Create `TenantTemplateRenderer` (implements `ITemplateRenderer` via RazorLight with embedded resources from Tenant.Infrastructure assembly) in `Mentoory.Tenant.Infrastructure/Services/TenantTemplateRenderer.cs`
- [X] T012 Add project reference to `Mentoory.Notification.Contracts` from `Mentoory.Access.Application/Mentoory.Access.Application.csproj`
- [X] T013 [P] Add project reference to `Mentoory.Notification.Contracts` from `Mentoory.Tenant.Application/Mentoory.Tenant.Application.csproj`
- [X] T014 [P] Add project reference to `Mentoory.Notification.Contracts` from `Mentoory.Notification.Application/Mentoory.Notification.Application.csproj`
- [X] T015 Register `AccessTemplateRenderer` as `ITemplateRenderer` in `Mentoory.Access.Infrastructure/DependencyInjection.cs`
- [X] T016 [P] Register `TenantTemplateRenderer` as `ITemplateRenderer` in `Mentoory.Tenant.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Foundation ready — user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 — Registration Welcome Email (Priority: P1)

**Goal**: The Access domain renders the registration welcome email and publishes `NotificationRequestedEvent`. The Notification domain receives and delivers it.

**Independent Test**: Register a new user and verify the welcome email arrives with correct content and Mentoory branding.

### Implementation for User Story 1

- [X] T017 [US1] Copy `UserRegistration.cshtml` from `Mentoory.Notification.Infrastructure/Templates/` to `Mentoory.Access.Infrastructure/Templates/UserRegistration.cshtml` and remove the `@{ Layout = "_EmailLayout.cshtml"; }` directive (inner content only)
- [X] T018 [US1] Create `UserRegisteredNotificationHandler` in `Mentoory.Access.Application/IntegrationEvents/UserRegisteredNotificationHandler.cs` — handles `UserRegisteredEvent`, renders template via `ITemplateRenderer`, wraps via `IEmailLayoutWrapper`, reads BaseUrl from `INotificationConfigurationReader`, publishes `NotificationRequestedEvent` with NotificationType.UserRegistration

**Checkpoint**: Access domain can render and publish registration notifications. Notification domain doesn't handle them yet (handled in Phase 6).

---

## Phase 4: User Story 2 — Project Invitation Email (Priority: P1)

**Goal**: The Tenant domain renders the project invitation email and publishes `NotificationRequestedEvent`.

**Independent Test**: Reissue a project invitation and verify the email arrives with correct project/incubator details and Mentoory branding.

### Implementation for User Story 2

- [X] T019 [P] [US2] Copy `ProjectInvitation.cshtml` from `Mentoory.Notification.Infrastructure/Templates/` to `Mentoory.Tenant.Infrastructure/Templates/ProjectInvitation.cshtml` and remove the layout directive
- [X] T020 [US2] Create `InvitationReissuedNotificationHandler` in `Mentoory.Tenant.Application/IntegrationEvents/InvitationReissuedNotificationHandler.cs` — handles `InvitationReissuedEvent`, renders template via `ITemplateRenderer`, wraps via `IEmailLayoutWrapper`, reads BaseUrl from `INotificationConfigurationReader`, publishes `NotificationRequestedEvent` with NotificationType.ProjectInvitation

**Checkpoint**: Tenant domain can render and publish invitation notifications.

---

## Phase 5: User Story 3 — Login Security Alert (Priority: P1)

**Goal**: The Access domain parses login context (IP, browser, OS), determines suspicion level, renders the appropriate alert template, and publishes `NotificationRequestedEvent`. LoginContext metadata is NOT stored — it's only used during rendering.

**Independent Test**: Trigger a login attempt and verify the correct alert email (normal or suspicious) arrives with IP/browser/OS details in the rendered body.

### Implementation for User Story 3

- [X] T021 [P] [US3] Copy `LoginAlert.cshtml` and `SuspiciousLoginAlert.cshtml` from `Mentoory.Notification.Infrastructure/Templates/` to `Mentoory.Access.Infrastructure/Templates/` and remove layout directives
- [X] T022 [US3] Create `LoginAttemptNotificationHandler` in `Mentoory.Access.Application/IntegrationEvents/LoginAttemptNotificationHandler.cs` — handles `LoginAttemptEvent`, parses IP/UserAgent into browser name and OS, determines suspicious flag (3+ failed attempts or post-lockout), selects template (LoginAlert vs SuspiciousLoginAlert), renders via `ITemplateRenderer`, wraps via `IEmailLayoutWrapper`, publishes `NotificationRequestedEvent` with NotificationType.LoginAlert

**Checkpoint**: All three notification types now have rendering handlers in their originating domains.

---

## Phase 6: Cutover — Notification Domain Simplification

**Goal**: Replace old type-specific infrastructure with the single generic handler and delivery path. Remove LoginContext from domain, database, and EF mappings. This is the breaking-change phase — all modifications must be applied together for the build to pass.

### Notification Domain Changes

- [X] T023 Remove `LoginContext` property from `Notification` aggregate and simplify `Create()` factory method in `Mentoory.Notification.Domain/Aggregates/Notification/Notification.cs`
- [X] T024 [P] Delete `LoginContext` value object file at `Mentoory.Notification.Domain/ValueObjects/LoginContext.cs` (or its actual path under Aggregates/)
- [X] T025 Remove `OwnsOne(e => e.LoginContext, ...)` mapping from `Mentoory.Notification.Infrastructure/Persistence/NotificationDbContext.cs`
- [X] T026 Remove `LoginContext_*` columns from `Mentoory.Db/notification/Tables/Notifications.sql`

### Notification Application Changes

- [X] T027 Create `NotificationRequestedHandler` in `Mentoory.Notification.Application/IntegrationEvents/NotificationRequestedHandler.cs` — handles `NotificationRequestedEvent`, checks user notification preferences (no preference = send, opt-out model), calls `NotificationQueueService.QueueAsync()`
- [X] T028 Replace type-specific methods in `Mentoory.Notification.Infrastructure/Services/NotificationQueueService.cs` — remove `QueueLoginAlertAsync()`, `QueueRegistrationEmailAsync()`, `QueueInvitationEmailAsync()`, add single `QueueAsync(NotificationType, string subject, string htmlBody, long recipientUserId, string recipientEmail, Guid? sourceEventId, DateTime scheduledForUtc, DateTime createdAtUtc)`

### Old Code Removal

- [X] T029 Delete `LoginAttemptNotificationHandler.cs` from `Mentoory.Notification.Application/IntegrationEvents/`
- [X] T030 [P] Delete `UserRegisteredNotificationHandler.cs` from `Mentoory.Notification.Application/IntegrationEvents/`
- [X] T031 [P] Delete `InvitationReissuedNotificationHandler.cs` from `Mentoory.Notification.Application/IntegrationEvents/`
- [X] T032 Delete all template files from `Mentoory.Notification.Infrastructure/Templates/` (UserRegistration.cshtml, ProjectInvitation.cshtml, LoginAlert.cshtml, SuspiciousLoginAlert.cshtml, _EmailLayout.cshtml)
- [X] T033 [P] Delete `RazorLightTemplateRenderer.cs` and `ITemplateRenderer.cs` from `Mentoory.Notification.Infrastructure/Services/`
- [X] T034 Update `Mentoory.Notification.Infrastructure/DependencyInjection.cs` — remove `RazorLightTemplateRenderer` registration, remove RazorLight package reference from csproj, remove old handler-related DI registrations
- [X] T035 Remove project references to `Mentoory.Access.Contracts` and `Mentoory.Tenant.Contracts` from `Mentoory.Notification.Application/Mentoory.Notification.Application.csproj` (Notification no longer consumes their events directly)

**Checkpoint**: Notification domain is now a pure delivery service. The Notification aggregate, handler, queue service, and database schema are fully generic.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verify build, clean up references, validate delivery.

- [X] T036 Verify `dotnet build` succeeds with zero warnings across the entire solution
- [X] T037 [P] Verify all project reference chains are clean — no circular references, no unnecessary references
- [X] T038 [P] Verify `NotificationType` enum is accessible from `Notification.Contracts` to all publishing domains
- [X] T039 Run quickstart.md validation — confirm the "Adding a New Notification Type" flow requires zero Notification domain changes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational
- **US2 (Phase 4)**: Depends on Foundational — can run in parallel with US1
- **US3 (Phase 5)**: Depends on Foundational — can run in parallel with US1 and US2
- **Cutover (Phase 6)**: Depends on ALL user story phases (US1 + US2 + US3) — new handlers must exist before old ones are deleted
- **Polish (Phase 7)**: Depends on Cutover completion

### User Story Dependencies

- **US1 (P1)**: Independent — can start after Foundational
- **US2 (P1)**: Independent — can start after Foundational, parallel with US1
- **US3 (P1)**: Independent — can start after Foundational, parallel with US1 and US2
- **Cutover**: Depends on US1 + US2 + US3 all complete

### Within Each User Story

- Copy template before creating handler (handler references template name)
- Handler creation is the main deliverable per story

### Parallel Opportunities

- T002, T003, T004 (Setup): All create independent files, can run in parallel
- T009, T010, T011 (Foundational): Template renderers for different domains, can run in parallel
- T012, T013, T014 (Foundational): Project reference updates, can run in parallel
- US1, US2, US3 phases: Fully independent, can run in parallel after Foundational
- T029, T030, T031 (Cutover): Old handler deletions, can run in parallel
- T036, T037, T038 (Polish): Independent verifications, can run in parallel

---

## Parallel Example: User Stories 1-3 (after Foundational)

```text
# All three user stories can launch in parallel:

# US1 - Registration Email:
Task: "Copy UserRegistration.cshtml to Access.Infrastructure/Templates/"
Task: "Create UserRegisteredNotificationHandler in Access.Application"

# US2 - Invitation Email (parallel with US1):
Task: "Copy ProjectInvitation.cshtml to Tenant.Infrastructure/Templates/"
Task: "Create InvitationReissuedNotificationHandler in Tenant.Application"

# US3 - Login Alert (parallel with US1 and US2):
Task: "Copy LoginAlert.cshtml + SuspiciousLoginAlert.cshtml to Access.Infrastructure/Templates/"
Task: "Create LoginAttemptNotificationHandler in Access.Application"
```

---

## Implementation Strategy

### MVP First (All P1 Stories — This Is a Refactoring)

Since all three notification types are P1 and the refactoring is big-bang:

1. Complete Phase 1: Setup (new project + shared interfaces)
2. Complete Phase 2: Foundational (template renderers + project references)
3. Complete Phases 3-5: All user stories (new handlers + templates in originating domains)
4. Complete Phase 6: Cutover (remove old code, simplify Notification domain)
5. **STOP and VALIDATE**: Build passes, all notification types deliver correctly
6. Complete Phase 7: Polish

### Key Risk Mitigation

- Phases 1-5 are **additive only** — no existing behavior changes until Phase 6
- Phase 6 is the **single breaking-change phase** — all removals and simplifications happen together
- If any user story handler has issues, the old handlers still exist until Phase 6

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Phase 6 (Cutover) is the critical phase — all changes must be applied together
- Commit after each phase for clean git history
- The INotificationConfigurationReader (from feature 013) is used by originating domain handlers for BaseUrl — ensure 013 is merged first
