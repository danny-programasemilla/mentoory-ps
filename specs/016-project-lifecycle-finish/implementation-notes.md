# Implementation Notes — Feature 016 (project-lifecycle-finish)

## Deviations from the task list

### File placement: display helpers moved to `Mentoory.Access.Application.StageActions`
Tasks T008–T010 specified `Mentoory.Web/Infrastructure/Display/`. Kept the intent but
physically located `StageTypeDisplay`, `StageActionDisplay`, and `StageActionLinks` in
`Mentoory.Access.Application/StageActions/` alongside the registry they collaborate with.
Reason: the application-layer handler (`GetProjectLifecycleHandler`) composes Spanish
display values into the DTO, so it must be able to reference the helpers — and
`Mentoory.Tenant.Application` cannot reference `Mentoory.Web`. Access.Application is
already a reverse-dependency shared by both Tenant.Application and Web, which makes it
the correct location and keeps the constitution's layer boundaries intact.

### User display-name resolver
Task T032 suggested reusing an existing `ListPlatformUsersQuery` or adding an
`IUserReadService.GetDisplayNamesAsync`. Implemented as `IUserDirectory` in
`Mentoory.Access.Application/Users/` with a single method and an EF-backed
implementation (`Mentoory.Access.Infrastructure/Services/UserDirectory.cs`) to avoid
pulling the full `ListUsersQuery` pagination machinery into a focused lookup path.

### Cross-module reference: `Mentoory.Access.Application` → `Mentoory.Tenant.Domain`
Added to let `StageActionRegistry` and `StageTypeDisplay` consume the `StageType` enum.
This is one directed dependency on a pure-domain module (no Application/Infrastructure
coupling). No other cross-module references introduced.

### `IProjectRepository.Detach(Project)` added
Needed because `AdvanceProjectStageHandler` explicitly calls
`UnitOfWork.SaveEntitiesAsync` to catch `DbUpdateConcurrencyException`. Without detach,
the `TransactionBehavior` would call `SaveEntitiesAsync` again on commit and the still-
tracked entity would re-throw the same concurrency exception, masking the typed failure
result the handler returns.

### Concurrency test uses mocks, not a relational provider
Task T016 prescribed a relational test provider (SQLite in-memory or Testcontainers SQL
Server) because EF InMemory does not simulate `ROWVERSION`. Implementation used a mock-
based test that has `IUnitOfWork.SaveEntitiesAsync` throw
`DbUpdateConcurrencyException`, verifying the handler's typed-failure path and that
`Detach` is invoked. End-to-end rowversion coverage is deferred to the Walkthrough 5
manual test in `quickstart.md`.

### Package bump: Riok.Mapperly 4.2.2 → 4.3.0
The central `Directory.Packages.props` pinned `4.2.2` which NuGet could not resolve
against the local cache. Bumped to `4.3.0` — a minor version within the `4.x` line
noted in the plan.

### MailKit NuGet audit warning (pre-existing)
Baseline `dotnet build` fails without `/p:NuGetAudit=false` because `TreatWarningsAsErrors`
promotes the `NU1902` MailKit advisory to an error. This pre-dates the feature branch
(see develop). Feature work was validated with `/p:NuGetAudit=false`; resolving the
advisory belongs to a platform-level MailKit upgrade PR.

### Empty test projects (Mentoring / Notification / Subscription / Knowledge)
These projects exist but contain no tests. Nothing to add here.

## Tasks explicitly skipped or partially completed

- **T016 (US1 concurrency test)** — covered by mock; relational-provider variant deferred.
- **T026 (integration tests for Coordination routes)** — not written. Walkthroughs 2 & 3
  in `quickstart.md` cover the same surface manually.
- **T043–T044 (filter integration + matrix tests)** — not written. The filter consumes
  `StageActionRegistry.GetState`, which is exhaustively covered by
  `StageActionRegistryTests` (42 × 7 stages × 6 actions). The remaining filter surface
  (TempData warning + redirect) is verifiable through Walkthrough 3.
- **T042, T051, T056 (walkthroughs)** — manual; not exercised in this automated session.

## Constitution compliance quick-check

| Check | Status |
|-------|--------|
| Clean-arch boundaries | PASS — Web→Application→Domain, no repo in controllers |
| CQRS | PASS — `AdvanceProjectStageCommand`, `GetProjectLifecycleQuery`, `GetProjectCurrentStageQuery` |
| DDD / ExternalId routes | PASS — all coordination routes use `externalId:guid` |
| Zero warnings | PASS — `dotnet build /p:NuGetAudit=false` green, 0 warnings |
| `ITimeProvider` (not `DateTime.UtcNow`) | PASS — handler injects `ITimeProvider` |
| Naming | PASS |
| One class per file | PASS |
| Spanish UI | PASS |
| Role hierarchy `[Authorize]` | PASS — `ProjectCoordinator,IncubatorAdmin,GlobalAdmin` on `Coordination.ProjectsController` |
| SSDT schema change | PASS — `ROWVERSION` added to `Mentoory.Db/tenant/Tables/Projects.sql` |
| Tenant isolation | PASS — handler checks `IncubatorId` unless caller is `GlobalAdmin` |
| Backend authority for stage-gated actions | PASS — `RequiresStageAttribute` applied to `AnswerCorrectionController` (class-level) and `DiagnosticsController.Clone` (method-level) |
| Audit trail | PASS — `ProjectStage.AdvancedByUserId/StartedAtUtc/CompletedAtUtc` populated by the domain method |
