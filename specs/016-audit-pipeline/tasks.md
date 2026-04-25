---

description: "Task list for 016-audit-pipeline"
---

# Tasks: Audit Pipeline Wiring

**Input**: Design documents from `/specs/016-audit-pipeline/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUDED — spec acceptance scenarios and research R-10 explicitly define architecture, unit, and integration coverage. Test tasks are interleaved per user story.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. User story priority order per spec: US1 (P1) → US2 (P2) → US4 (P2) → US3 (P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- All file paths are repository-relative to `/mnt/D/repos/mentoory-ps-audit/`

## Path Conventions

Existing Clean-Architecture solution layout (see plan.md § Project Structure). Source lives under:
- `Mentoory.Shared.Application/`, `Mentoory.Shared.Infrastructure/`, `Mentoory.Web/`
- Per-module: `Mentoory.{Module}.Application/`, `.Infrastructure/`
- Tests: `tests/Mentoory.{Module}.Tests/`, `tests/Mentoory.Tests.Integration/`, new `tests/Mentoory.Tests.Architecture/`, new `tests/Mentoory.Shared.Application.Tests/`
- Schema: `Mentoory.Db/audit/`
- Governance: `.specify/memory/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Register new test projects and configuration scaffolding.

- [X] T001 Create new test project `tests/Mentoory.Tests.Architecture/Mentoory.Tests.Architecture.csproj` (TargetFramework net10.0; references xunit, FluentAssertions, Microsoft.NET.Test.Sdk; ProjectReference to `Mentoory.Shared.Application`, `Mentoory.Access.Application`, `Mentoory.Diagnostic.Application`, `Mentoory.Tenant.Application`, `Mentoory.Knowledge.Application`, `Mentoory.Mentoring.Application`, `Mentoory.Notification.Application`, `Mentoory.Subscription.Application`). Add project to `Mentoory.sln`.
- [X] T002 [P] Create new test project `tests/Mentoory.Shared.Application.Tests/Mentoory.Shared.Application.Tests.csproj` (same stack; ProjectReference to `Mentoory.Shared.Application`, `Mentoory.Shared.Infrastructure`). Add project to `Mentoory.sln`.
- [X] T003 [P] Add an `Audit` section placeholder to `Mentoory.Web/appsettings.json` (empty object `{}`) and `appsettings.Development.json` so `Configure<AuditOptions>` has a section to bind against even with defaults.

**Checkpoint**: Solution builds; new test projects discoverable; `dotnet test` passes (no tests yet).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure every user story depends on — attribute, event-type constants, options, `AuditEntry` extension, schema migration, `AuditService` SQL update, `ITenantContext.UserEmail`, `ICorrelationContext` + fallback impl, `AuditingBehavior`, MediatR pipeline registration.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Attribute, enum, and constants

- [X] T004 [P] Create `Mentoory.Shared.Application/Audit/AuditMode.cs` with `public enum AuditMode { Automatic = 0, Manual = 1 }`.
- [X] T005 [P] Create `Mentoory.Shared.Application/Audit/AuditedAttribute.cs` per contracts/attribute-and-mode.md (sealed class, `[AttributeUsage(AttributeTargets.Class)]`, required `EventType` ctor param, optional `EntityType` and `Mode` init properties, `ArgumentNullException` on empty `EventType`).
- [X] T006 [P] Create `Mentoory.Shared.Application/Audit/AuditEventTypes.cs` with the 5 v1 constants (`ContextActivated`, `RoleAssigned`, `UserRegistered`, `UserLoggedIn`, `AnswerCorrected`) and the 2 reserved-for-future constants (`ProjectStageAdvanced`, `MentoringPlanApproved`).
- [X] T007 [P] Create `Mentoory.Shared.Application/Audit/AuditOptions.cs` per data-model.md § 5 (`RedactedFields`, `DetailsMaxCharacters = 8000`, `RedactionSentinel = "***REDACTED***"`, `TruncationSuffix = "...<truncated>"`, `SectionName = "Audit"`).

### AuditEntry record + IAuditService extension

- [X] T008 Modify `Mentoory.Shared.Application/Audit/AuditEntry.cs` to add the 5 new positional fields (`Guid? CorrelationId`, `string Outcome`, `string? ExceptionType`, `string? UserEmail`, `string? RoleContext`) AFTER the existing 10 fields. Update XML doc comments.
- [X] T009 Modify `Mentoory.Shared.Infrastructure/Audit/AuditService.cs` to (a) extend SQL INSERT to 15 columns, (b) add `DBNull.Value` fallback for each new nullable parameter, (c) preserve the existing try/catch-and-log best-effort semantics exactly (no new code paths).

### Database schema

- [X] T010 Modify `Mentoory.Db/audit/Tables/AuditLog.sql` to add the 5 new columns (`CorrelationId UNIQUEIDENTIFIER NULL`, `Outcome NVARCHAR(20) NOT NULL CONSTRAINT DF_AuditLog_Outcome DEFAULT 'Success'`, `ExceptionType NVARCHAR(200) NULL`, `UserEmail NVARCHAR(256) NULL`, `RoleContext NVARCHAR(50) NULL`) and a new filtered nonclustered index `IX_AuditLog_CorrelationId` on `(CorrelationId, OccurredAtUtc DESC)` with `WHERE [CorrelationId] IS NOT NULL`. Verify DACPAC builds via `cd Mentoory.Db && publish-mentoorydb.sh` (or equivalent `dotnet build` of the sqlproj).

### Tenant context extension

- [X] T011 Modify `Mentoory.Shared.Application/Interfaces/ITenantContext.cs` to add `string? UserEmail { get; }` property.
- [X] T012 Modify `Mentoory.Shared.Infrastructure/Services/TenantContextService.cs` to populate `UserEmail` from `HttpContext.User.FindFirstValue(ClaimTypes.Email)` (fallback `null` for anonymous principals). Follow the exact same pattern used for `UserId` and `Role` already in that file.

### Correlation context (abstraction + ambient fallback only — web impl lives in US3)

- [X] T013 [P] Create `Mentoory.Shared.Application/Interfaces/ICorrelationContext.cs` with `Guid CorrelationId` and `string? ClientIpAddress` getters (sealed interface per project convention).
- [X] T014 [P] Create `Mentoory.Shared.Infrastructure/Correlation/AmbientCorrelationContext.cs` implementing `ICorrelationContext`: `CorrelationId` reads `Activity.Current?.RootId` (parsed as GUID) with `Guid.NewGuid()` fallback; `ClientIpAddress` is always `null`. Sealed class.

### Auditing pipeline behavior

- [X] T015 Create `Mentoory.Shared.Application/Behaviors/AuditingBehavior.cs` per contracts/auditing-behavior.md: sealed generic `IPipelineBehavior<TRequest, TResponse>` where `TRequest : IBaseRequest`; ctor injects `IAuditService`, `ITenantContext`, `ICorrelationContext`, `ITimeProvider`, `IOptions<AuditOptions>`, `ILogger<AuditingBehavior<TRequest, TResponse>>`; reads `[Audited]` via reflection once per `TRequest` type (cache statically); pre-call path for `null`/Manual → passthrough; Automatic path → try/catch around `next()`, build `AuditEntry`, call `LogAsync`, rethrow on exception. (Covers FR-001 through FR-005 for the Automatic mode.)
- [X] T016 [P] Create helper `Mentoory.Shared.Application/Audit/AuditPayloadRedactor.cs` (internal sealed static or instance class) implementing the redaction algorithm per research.md R-04: `SerializeRedacted(object command, AuditOptions options, ILogger logger) : string`. Uses `System.Text.Json.Nodes.JsonNode`, iterates top-level properties, substitutes sentinel on case-insensitive name match, truncates to `DetailsMaxCharacters` with suffix, catches `JsonException` and returns sentinel `$"<serialization-failed: {ex.GetType().Name}>"`.
- [X] T017 Modify `Mentoory.Shared.Application/Behaviors/AuditingBehavior.cs` to wire `AuditPayloadRedactor` into the payload construction path (separated into its own task because it depends on T015 being built first — merge after both exist).

### MediatR pipeline registration + options binding

- [X] T018 Modify `Mentoory.Web/Program.cs` to (a) call `builder.Services.Configure<AuditOptions>(builder.Configuration.GetSection(AuditOptions.SectionName))`, (b) register `ICorrelationContext` as `Scoped` → `AmbientCorrelationContext` (foundational default; US3 will replace with `WebCorrelationContext`), (c) insert `cfg.AddOpenBehavior(typeof(AuditingBehavior<,>))` BETWEEN the existing `ValidatorBehavior` and `TransactionBehavior` lines inside `AddMediatR(...)`. Verify registration order `Validator → Auditing → Transaction`.

### Foundational tests

- [X] T019 [P] Create `tests/Mentoory.Shared.Application.Tests/Audit/AuditPayloadRedactorTests.cs` with unit tests: (a) top-level Password field replaced with sentinel; (b) top-level NationalId replaced; (c) non-matching property unchanged; (d) payload > DetailsMaxCharacters truncated with suffix; (e) circular-reference serialization falls through to `<serialization-failed: ...>` sentinel; (f) case-insensitive match (e.g., `password`, `PASSWORD`, `Password` all redacted); (g) custom `RedactedFields` via `AuditOptions` respected.
- [X] T020 [P] Create `tests/Mentoory.Shared.Application.Tests/Behaviors/AuditingBehaviorTests.cs` with unit tests covering the matrix from contracts/auditing-behavior.md § "Testability": no-attribute passthrough, Manual passthrough, Automatic-success write, Automatic-failure-Result write with `Outcome=Failure, ExceptionType=null`, Automatic-throw write with `ExceptionType` populated + rethrow preserving stack, redaction end-to-end via `Password` on a sample command, `ITimeProvider` substitution (verify `OccurredAtUtc` matches the fake time). Use Moq for `IAuditService`, `ITenantContext`, `ICorrelationContext`, `ITimeProvider`.

**Checkpoint**: `dotnet build` passes with zero warnings; `dotnet test tests/Mentoory.Shared.Application.Tests/` passes; DACPAC builds. Audit infrastructure is end-to-end wired but no command is yet decorated with `[Audited]`.

---

## Phase 3: User Story 1 - Platform Admin reviews security-sensitive actions (Priority: P1) 🎯 MVP

**Goal**: Platform Admins can see sensitive commands in a read-only viewer with filtering, within 24h of the action. Delivers capture + persistence + viewing for the four Automatic-mode retrofit commands.

**Independent Test**: Log in as GlobalAdmin, trigger `AssignRole` → the `/Administration/AuditLog` viewer shows a row with `EventType = Role.Assigned`, correct user email, `Outcome = Success`, timestamp. Trigger a failing login → row appears with `Outcome = Failure` and `ExceptionType` populated. Filter by `EventType` or date range → results narrow accordingly. Submit a command carrying a `Password` field → `Details` shows the redaction sentinel, not the original value.

### Retrofit Automatic-mode commands + anonymous resolvers

- [X] T021 [P] [US1] Modify `Mentoory.Access.Application/Commands/SetActiveContext/SetActiveContextCommand.cs` to add `[Audited(AuditEventTypes.ContextActivated, EntityType = "TenantContext")]`. Add `using Mentoory.Shared.Application.Audit;`.
- [X] T022 [P] [US1] Modify `Mentoory.Access.Application/Commands/AssignRole/AssignRoleCommand.cs` to add `[Audited(AuditEventTypes.RoleAssigned, EntityType = "RoleAssignment")]`.
- [X] T023 [P] [US1] Modify `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserCommand.cs` to add `[Audited(AuditEventTypes.UserRegistered, EntityType = "User")]`.
- [X] T024 [P] [US1] Modify `Mentoory.Access.Application/Commands/LoginUser/LoginUserCommand.cs` to add `[Audited(AuditEventTypes.UserLoggedIn, EntityType = "User")]`.
- [X] T025 [P] [US1] Create `Mentoory.Shared.Application/Audit/IAuditAnonymousResolver.cs` — `public interface IAuditAnonymousResolver<in TRequest> { (long? UserId, string? UserEmail) Resolve(TRequest request); }`.
- [X] T026 [P] [US1] Create `Mentoory.Access.Application/Audit/RegisterUserAuditResolver.cs` implementing `IAuditAnonymousResolver<RegisterUserCommand>` returning `(null, command.Email)`.
- [X] T027 [P] [US1] Create `Mentoory.Access.Application/Audit/LoginUserAuditResolver.cs` implementing `IAuditAnonymousResolver<LoginUserCommand>` returning `(null, command.Email)`.
- [X] T028 [US1] Modify `Mentoory.Access.Application/ServiceCollectionExtensions.cs` (`AddAccessApplication`) to register both resolvers as `Scoped`: `services.AddScoped<IAuditAnonymousResolver<RegisterUserCommand>, RegisterUserAuditResolver>();` and similar for login.
- [X] T029 [US1] Modify `Mentoory.Shared.Application/Behaviors/AuditingBehavior.cs` to resolve `IAuditAnonymousResolver<TRequest>?` via `IServiceProvider` (inject `IServiceProvider` OR pattern-match on known command types via an optional resolver interface); if resolver present, override tenant-derived `UserId`/`UserEmail` with resolver output. Document why this is the cleanest seam for FR-014.

### Admin viewer — query, repository, controller, view, JS, menu

- [X] T030 [P] [US1] Create `Mentoory.Shared.Application/Queries/Audit/AuditLogDto.cs` per data-model.md § 8 (16-field positional record — matches data-model.md, not the 17 stated here).
- [X] T031 [P] [US1] Create `Mentoory.Shared.Application/Queries/Audit/GetAuditLogPagedQuery.cs`. **Deviation**: Wraps `DataTableRequest` (existing convention) → returns `DataTableResponse<AuditLogDto>`. `PagedResult<T>` does not exist in this codebase; `DataTableRequest`/`DataTableResponse` is used by every other paginated query (see `ListIncubatorMembersQuery`).
- [X] T032 [US1] Create `Mentoory.Shared.Application/Queries/Audit/GetAuditLogPagedQueryValidator.cs`. Enforces Length ∈ [1, 100], Start ≥ 0, Outcome ∈ {"Success", "Failure"}, FromUtc ≤ ToUtc, and well-formed date strings in filter values.
- [X] T033 [US1] Create `Mentoory.Shared.Infrastructure/Persistence/Audit/AuditReadDbContext.cs` with `QueryTrackingBehavior.NoTracking` default. Registered in `Program.cs` via `AddDbContext` pointing at `DefaultConnection`.
- [X] T034 [US1] Create `AuditLogReadEntity.cs`. **Deviation**: lives in `Mentoory.Shared.Application/Queries/Audit/` (not Infrastructure) so `IAuditLogReadRepository` can expose `IQueryable<AuditLogReadEntity>`. EF Core can't translate `OrderBy` over positional-record projections, so sorting/filtering must run on the entity surface before the final `.Select(...)` to DTO.
- [X] T035 [US1] Create `GetAuditLogPagedQueryHandler.cs` — `BaseCommandHandler<TQuery, DataTableResponse<AuditLogDto>>` that applies filters (EventType equality, UserEmail contains-ignore-case, Outcome equality, OccurredAtUtc range), whitelist-sorted, paged, then projected to DTO via inline `.Select()`.
- [X] T036 [P] [US1] ~~AuditLogMapper (Mapperly)~~ **Skipped** — Mapperly is not used anywhere else in the codebase. Followed the existing inline `.Select()` projection convention (see `ListIncubatorMembersHandler`) to avoid introducing a new package for a one-off mapping.
- [X] T037 [US1] Create `Mentoory.Web/Areas/Administration/Controllers/AuditLogController.cs` — `[Area("Administration")] [Authorize(Roles = "GlobalAdmin")]`, inherits `Controller`, injects `MediatRExecutor`. **Deviation**: `Data(...)` is `[HttpPost]` with `[FromForm] DataTableServerRequest` to match existing convention (all other DataTable endpoints use POST + antiforgery).
- [X] T038 [P] [US1] Create `Mentoory.Web/Areas/Administration/Views/AuditLog/Index.cshtml` — uses the existing `DataTableViewComponent`. Filter bar auto-generated by `datatable-helper.js`; custom dropdowns supplied via `filters` array in audit-log.js.
- [X] T039 [P] [US1] Create `Mentoory.Web/wwwroot/js/audit-log.js` — wraps `initDataTable` with Outcome / EventType select filters (Spanish labels), expandable child row rendering `Details` as pretty-printed JSON plus secondary fields (EntityType, EntityId, CorrelationId, ExceptionType, IpAddress, IncubatorId, ProjectId, UserId).
- [X] T040 [US1] Modify `MenuConfiguration.cs`. **Deviation**: placed under the `"Plataforma"` group (already GlobalAdmin-only) rather than `"Administración"` (IncubatorAdmin+GlobalAdmin). The current `MenuService` filters at group level only — placing under Administración would leak the link to IncubatorAdmin. The contract intent (GlobalAdmin-only visibility) is preserved.

### Integration tests for US1

- [X] T041 [P] [US1] Create `tests/Mentoory.Tests.Integration/Audit/AuditPipelineTests.cs` — 8 tests covering happy + failure paths for all four retrofitted commands. All pass against a real SQL Server container with the DACPAC deployed.
- [X] T042 [P] [US1] Create `tests/Mentoory.Tests.Integration/Audit/AdminViewerTests.cs` — 5 tests exercising the query handler end-to-end: happy-path return, EventType filter, Outcome+UserEmail filter, Length>100 rejected, unknown Outcome rejected. **Deviation**: HTTP-level 403 authorization check deferred — no authenticated-client fixture exists in this test suite yet; relying on `[Authorize(Roles = "GlobalAdmin")]` attribute coverage.
- [X] T043 [US1] Modify `IntegrationTestBase.cs` to add `AssertAuditLoggedAsync(expectedEventType, userEmail = null)` helper returning the `AuditLogReadEntity` row. Also added `audit` to Respawn's `SchemasToInclude`.

**Checkpoint**: User Story 1 fully testable independently. MVP complete: capture + retrofit + viewer work end-to-end.

---

## Phase 4: User Story 2 - Developer applies audit coverage to a new sensitive command (Priority: P2)

**Goal**: CI enforcement of `[Audited]` on sensitive-named commands via an architecture test; governance section added to `access-security-constitution.md` so the rule is discoverable.

**Independent Test**: Add a new command `ApproveSomethingCommand : IBaseRequest` with no `[Audited]` attribute in a scratch location. Run `dotnet test tests/Mentoory.Tests.Architecture/`. Test fails with a message naming the type and citing `access-security-constitution.md § Audit Trail Obligations`. Add `[Audited(AuditEventTypes.SomethingApproved)]` — test passes.

### Architecture test project

- [X] T044 [US2] Create `tests/Mentoory.Tests.Architecture/Internal/CommandTypeEnumerator.cs` — static `GetAllCommandTypes()` with touch-type assembly loader + `ReflectionTypeLoadException` fallback. Shared `IsCommandType(Type)` helper exposed for reuse by `AuditCoverageTests`.
- [X] T045 [US2] Create `AuditCoverageTests.cs` with the three tests. Regex constant carries the constitution-pointer comment. **Scope note**: the regex is strict — `AssignMentorCommand` (Tenant) and `RegisterInternalUserCommand` (Access) were discovered by the test and decorated with `[Audited]` (`MentorAssigned` / `UserRegistered`) in the same change.
- [X] T046 [P] [US2] Create `PipelineOrderingTests.cs`. **Deviation**: implemented as a source-text scan over `Program.cs` rather than booting `WebApplicationFactory<Program>`. The factory approach requires a live SQL connection string at startup (Aspire `EnrichSqlServerDbContext` prerequisite); a text scan is faster (<20 ms), deterministic, and equally protective — it fails if the three `AddOpenBehavior(...)` calls lose their order.

### Governance updates

- [X] T047 [US2] `.specify/memory/access-security-constitution.md` — appended "Audit Trail Obligations" section (verbatim from contracts/governance.md). Sync Impact Report + version footer updated to **1.1.0**. `Last Amended` → `2026-04-18`.
- [X] T048 [P] [US2] `.specify/memory/constitution.md` — added item 10 to "Specification Validation & Enforcement" citing `AuditCoverageTests`. Sync Impact Report updated with a `1.1.1 → 1.1.2` PATCH entry; version footer bumped to **1.1.2**; `Last Amended` → `2026-04-18`.

**Checkpoint**: `dotnet test tests/Mentoory.Tests.Architecture/` green. Governance discoverable. Future sensitive commands are enforceably covered.

---

## Phase 5: User Story 4 - Correction handler captures domain-specific detail (Priority: P2)

**Goal**: `CorrectAnswerHandler` captures both the previous and new answer text in the audit row via Manual mode — proving the escape-hatch pattern works end-to-end on a real handler.

**Independent Test**: Correct a diagnostic answer from text "original" to "fixed". Query `[audit].[AuditLog]` → exactly one row with `EventType = Answer.Corrected`, `Action = CorrectAnswerCommand`, `Details` containing BOTH "original" AND "fixed" (as JSON fields `Before` and `After`). The `AnswerCorrection` aggregate still reflects the change (no domain-side regression).

### Manual-mode retrofit

- [X] T049 [US4] `CorrectAnswerCommand.cs` — added `[Audited(AuditEventTypes.AnswerCorrected, EntityType = "AnswerCorrection", Mode = AuditMode.Manual)]`.
- [X] T050 [US4] `CorrectAnswerHandler.cs` — injects `IAuditService`, `ITenantContext`, `ICorrelationContext`; captures the before-snapshot (`TextValue`, `NumericValue`, `SelectedOptionIds`) from the aggregate before the domain call; writes the audit row AFTER `SaveEntitiesAsync` succeeds. Details JSON carries `{ Before, After, Reason }`. On handler failure (not-found), no audit row is written — verified by the unit test.

### Manual-mode tests

- [X] T051 [P] [US4] `CorrectAnswerHandlerTests.cs` — added `AuditLog_entry_is_written_with_before_and_after`. Uses Moq callback to capture the `AuditEntry`; asserts `Details` JSON parses and carries both `Before.TextValue="Original"` and `After.NewTextValue="Corrected"`. Also updated the existing `Handle_WithNonExistentResponse_ShouldReturnFailure` to assert `LogAsync` is NEVER called on failure.
- [X] T052 [P] [US4] `CorrectAnswerAuditTests.cs` — integration test seeds a template/form/response, dispatches the command via MediatR, asserts exactly ONE row in `[audit].[AuditLog]` with `Action=CorrectAnswerCommand` (proving the Automatic-mode passthrough does NOT also write a row) and both before/after values in `Details`.

**Checkpoint**: Manual-mode pattern validated on a real handler. Future Manual-mode handlers have a reference implementation to copy.

---

## Phase 6: User Story 3 - Auditor reconstructs an incident across multiple commands (Priority: P3)

**Goal**: Replace the `AmbientCorrelationContext` registration with a `WebCorrelationContext` + `CorrelationMiddleware` so that multiple commands dispatched within the same HTTP request share a correlation id, and that id is echoed in the response header.

**Independent Test**: Send an HTTP POST to any endpoint whose controller dispatches two commands (e.g., login followed by SetActiveContext after a successful login flow). Query `[audit].[AuditLog]` → both entries share the same `CorrelationId`. Send another request with a custom `X-Correlation-Id` header set to a known GUID → entries use THAT GUID. Response headers include `X-Correlation-Id` on both requests.

### Web correlation implementation

- [X] T053 [P] [US3] Create `WebCorrelationContext.cs` — reads `HttpContext.Items[CorrelationItemKey]`, falls back to `Guid.NewGuid()` when no request context exists (so DI-in-tests scenarios don't crash).
- [X] T054 [P] [US3] Create `CorrelationMiddleware.cs` — accepts `X-Correlation-Id` header as GUID or generates fresh; stamps `HttpContext.Items` + response header.
- [X] T055 [P] [US3] Create `CorrelationMiddlewareExtensions.cs` — `UseCorrelation()` extension method.
- [X] T056 [US3] `Program.cs` — swapped `AmbientCorrelationContext` → `WebCorrelationContext` scoped registration; inserted `app.UseCorrelation()` BEFORE `app.UseHttpsRedirection()`. Removed the now-unused `using Mentoory.Shared.Infrastructure.Correlation;` import.

### Correlation tests

- [X] T057 [P] [US3] Create `CorrelationPropagationTests.cs` — 3 tests: (a) fresh GUID stamped when no header, (b) valid incoming header echoed verbatim, (c) invalid header replaced with fresh GUID. **Deviation**: the "two commands within one authenticated HTTP request share a correlation id" sub-test is deferred; requires an authenticated-client fixture that does not yet exist.
- [X] T058 [P] [US3] Create `AmbientCorrelationContextTests.cs` — 3 tests: `ClientIpAddress` always null; fresh GUID when no `Activity.Current`; RootId parses when a valid W3C traceparent was seeded.

**Checkpoint**: All four user stories independently functional. Incident reconstruction works.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Cross-cutting validation, negative-path coverage, documentation, and simplification pass.

- [X] T059 [P] **Deviation**: moved from a new `BestEffortWriteTests.cs` (integration) to a unit-level test inside `AuditingBehaviorTests`: `Audit_service_failure_is_swallowed_and_command_still_succeeds`. Also hardened the behavior itself with a defense-in-depth `try/catch` around `_auditService.LogAsync(...)` so a future service implementation cannot accidentally propagate into the command pipeline. The unit test is faster, doesn't require a SQL container, and proves the same guarantee.
- [X] T060 [P] `specs/016-audit-pipeline/checklists/ui-spanish.md` — Spanish QA checklist for the admin viewer (page, table, filter dropdowns, pagination, status badges, menu). Not yet executed manually — must run before release.
- [X] T061 Self-review pass over the new files in this branch (handler/middleware/view JS) — methods stay under 30 lines, EF queries use `AsNoTracking`, magic strings replaced with `AuditEventTypes` constants and `CorrelationMiddleware.HeaderName`. No `/simplify` skill run — feedback from the author is deferred until PR review.
- [ ] T062 **Blocked**: full-solution `dotnet build` fails pre-existing on `NU1902` (MailKit 4.15.1 CVE). All feature code compiles clean; all test projects pass with the `-p:WarningsNotAsErrors=NU1902` suppression. Track resolution separately (MailKit upgrade).
- [ ] T063 **Manual task**: load `/Administration/AuditLog` with 1,000 seeded rows and time to interactive. Not executed in this session — production-environment sign-off only.
- [X] T064 Full test suite green: **406+ tests passing** across 11 projects (Access 92, Diagnostic 61, Tenant 37, Shared.Application 18, Architecture 4, Integration 97, E2E 97 + Notification/Knowledge/Mentoring/Subscription).
- [ ] T065 **Manual task**: quickstart.md Automatic+Manual-mode scratch-command verification. Covered in spirit by the end-to-end integration tests (`AuditPipelineTests` proves Automatic; `CorrectAnswerAuditTests` proves Manual). A developer walkthrough against `quickstart.md` is still a useful onboarding artifact.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies — start immediately.
- **Phase 2 Foundational**: Depends on Phase 1. BLOCKS every user story.
- **Phase 3 US1 (P1) MVP**: Depends on Phase 2 only. The main MVP increment.
- **Phase 4 US2 (P2)**: Depends on Phase 2 only. Can run in parallel with US1 / US4 / US3 if staffed, but logically ships alongside or shortly after US1.
- **Phase 5 US4 (P2)**: Depends on Phase 2 only. Can run in parallel with US1 / US2 / US3.
- **Phase 6 US3 (P3)**: Depends on Phase 2 and Phase 3 (admin viewer surfaces the `CorrelationId` column for inspection). Can run in parallel with US2 / US4.
- **Phase 7 Polish**: Depends on all four stories being feature-complete.

### Within-Story Ordering

- **US1**: Retrofit command attributes (T021–T024) and anonymous resolvers (T025–T028) can run in parallel with viewer tasks (T030–T040). T029 depends on T015. T043 depends on T041 writing first to establish the helper signature.
- **US2**: T044 before T045, T046. T047, T048 can run in parallel with each other and with the test tasks.
- **US4**: T049 before T050. T051, T052 after T050.
- **US3**: T053, T054, T055 in parallel; T056 depends on all three. T057, T058 after T056.

### Parallel Opportunities (at-a-glance)

- Phase 1: T002, T003 parallel to T001 (different csproj / config files).
- Phase 2: T004–T007 parallel (all new files). T013, T014 parallel. T019, T020 parallel. Schema (T010), `ITenantContext` (T011→T012), behavior (T015→T016→T017) are sequential chains.
- Phase 3: All four retrofit tasks (T021–T024) parallel. Anonymous resolver files (T025–T027) parallel. Viewer query/DTO/mapper (T030, T031, T036) parallel. Viewer shell (T038, T039) parallel.
- Phase 4: T046 parallel to T045 (different test files); T048 parallel to T047.
- Phase 5: T051, T052 parallel (different test files).
- Phase 6: T053, T054, T055 parallel (three new files).
- Phase 7: T059, T060 parallel.

---

## Parallel Example: User Story 1 retrofit fan-out

```bash
# Kick off all four Automatic-mode command attribute applications at once:
Task: "T021 Apply [Audited(AuditEventTypes.ContextActivated, ...)] to SetActiveContextCommand.cs"
Task: "T022 Apply [Audited(AuditEventTypes.RoleAssigned, ...)] to AssignRoleCommand.cs"
Task: "T023 Apply [Audited(AuditEventTypes.UserRegistered, ...)] to RegisterUserCommand.cs"
Task: "T024 Apply [Audited(AuditEventTypes.UserLoggedIn, ...)] to LoginUserCommand.cs"

# In parallel: create anonymous resolvers (three separate files):
Task: "T025 Create IAuditAnonymousResolver.cs"
Task: "T026 Create RegisterUserAuditResolver.cs"
Task: "T027 Create LoginUserAuditResolver.cs"

# Also in parallel: viewer query/DTO/mapper triad:
Task: "T030 Create AuditLogDto.cs"
Task: "T031 Create GetAuditLogPagedQuery.cs"
Task: "T036 Create AuditLogMapper.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 Setup (3 tasks).
2. Complete Phase 2 Foundational (17 tasks) — this is the heaviest phase; most of the feature's core code lives here.
3. Complete Phase 3 US1 (23 tasks) — retrofit + admin viewer + integration tests.
4. **STOP and VALIDATE**: All four Automatic-mode commands produce AuditLog rows; viewer renders and filters; redaction verified; integration tests green.
5. Deploy/demo MVP.

### Incremental Delivery

1. Setup + Foundational → infrastructure ready, but no visible audit output.
2. MVP = Foundational + US1 → Platform Admin can see sensitive actions, redaction works, anonymous flows covered.
3. Add US2 → CI-enforced; future sensitive commands automatically audited.
4. Add US4 → Manual-mode proven on a real handler; `CorrectAnswer` captures before/after.
5. Add US3 → correlation id shared across commands in one request; incident reconstruction works.
6. Polish → best-effort DB-failure test, Spanish QA, simplify pass.

### Parallel Team Strategy

With three developers after Phase 2 completes:

- **Developer A**: US1 (retrofit + viewer) — the largest story, usually the senior on the team.
- **Developer B**: US2 (architecture test + governance) + US4 (Manual mode on CorrectAnswer).
- **Developer C**: US3 (correlation middleware + tests) — can start after US1 commits the admin viewer so the test can verify correlated rows surface in it.

---

## Notes

- Tests are interleaved per story because the spec explicitly defines acceptance scenarios and the research pass planned a specific test split (R-10).
- Task IDs are sequential in execution order across the whole document (T001 → T065). `[P]` marks parallelizability within a phase, not across phases.
- File paths are absolute relative to the repo root.
- `[Story]` labels are strict on Phase 3+ tasks; Setup/Foundational/Polish tasks carry no story label.
- Stop at each "Checkpoint" to validate independently.
- Do NOT skip the Foundational phase — every user story has a hard dependency on it.
- Avoid: (a) applying `[Audited]` before Foundational lands (the pipeline behavior isn't there yet — no capture happens); (b) swapping `WebCorrelationContext` in before US1 is green (lose the AmbientContext fallback that foundational tests rely on); (c) changing the sensitive-action regex without updating both the architecture test AND the constitution section in one PR.
