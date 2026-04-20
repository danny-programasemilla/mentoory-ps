---
description: "Dependency-ordered task list for Cross-cutting Hardening — Phase 0"
---

# Tasks: Cross-cutting Hardening — Phase 0 (Quick Wins)

**Input**: Design documents from `/specs/017-crosscut-hardening-phase0/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Organization**: Tasks are grouped by user story. **Execution order is checkpoint-driven** (see Dependencies & Execution Order below) — it diverges from spec priority order because of technical dependencies (an earlier checkpoint never depends on a later one).

**Tests**: Included where an FR requires them (US1 architecture tests, US2 identical-shape test, US5 smoke tests).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps to user story from spec.md (US1..US6)
- **[CP-N]**: Checkpoint marker (commit + push; verification gate in quickstart.md)

---

## Phase 1: Setup

**Status: COMPLETE (commit `053cb77`, CP-0)** — baseline build green, spec/plan/research/data-model/quickstart/contracts committed and pushed. No further setup tasks.

---

## Phase 2: Foundational (QW-1 / CP-1) — blocks US1 and US2

**Purpose**: Land `RoleHierarchies` constants and migrate every hardcoded role literal. Serves FR-001 through FR-003. Blocks US2 (ProjectsController uses `RoleHierarchies.ProjectScope`) and US1 (architecture rule R-4.2.8 asserts no role literals outside `Roles.cs`).

**⚠️ CRITICAL**: No user story below can be merged until CP-1 closes.

- [ ] T001 Add `Mentoory.Shared.Domain/Constants/RoleHierarchies.cs` per data-model.md § C-2 (six `const string` hierarchies; derived from `Roles.*` via compile-time concatenation)
- [ ] T002 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs:17` to `RoleHierarchies.ProjectScope`
- [ ] T003 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Administration/Controllers/DashboardController.cs:10` to `RoleHierarchies.IncubatorScope`
- [ ] T004 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Administration/Controllers/ProjectsController.cs:17` to `RoleHierarchies.IncubatorScope` (intentional — US2/CP-2 elevates it to `ProjectScope` as the security fix)
- [ ] T005 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs:17` to `RoleHierarchies.IncubatorScope`
- [ ] T006 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Coordination/Controllers/AnswerCorrectionController.cs:12` to an appropriate hierarchy (audit: `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin,Mentor"` — this mixes Mentor into an otherwise-ProjectScope; confirm intent, likely `MentoringScope`)
- [ ] T007 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs:17` to `RoleHierarchies.MentoringScope`
- [ ] T008 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Participant/Controllers/DiagnosticController.cs:16` to `RoleHierarchies.ParticipantScope`
- [ ] T009 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Platform/Controllers/ConfigurationController.cs:14` to `RoleHierarchies.PlatformScope`
- [ ] T010 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Platform/Controllers/IncubatorsController.cs:16` to `RoleHierarchies.PlatformScope`
- [ ] T011 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Platform/Controllers/SponsorController.cs:8` to `RoleHierarchies.SponsorScope`
- [ ] T012 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Platform/Controllers/TemplatesController.cs:11` to `RoleHierarchies.PlatformScope`
- [ ] T013 [P] Migrate `[Authorize]` attribute in `Mentoory.Web/Areas/Platform/Controllers/UsersController.cs:10` to `RoleHierarchies.PlatformScope`
- [ ] T014 Migrate role literals in `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` to use `Roles.*` constants; audit every group's `Roles` array
- [ ] T015 Run grep verification: `grep -rn '"GlobalAdmin"\|"IncubatorAdmin"\|"ProjectCoordinator"\|"Mentor"\|"Entrepreneur"\|"Sponsor"' --include='*.cs' | grep -v Roles.cs | grep -v /bin/ | grep -v /obj/` — MUST be empty
- [ ] T016 Run pre-checkpoint gates: `dotnet build Mentoory.sln -warnaserror` (0 warnings), `dotnet test Mentoory.sln --no-build` (green)
- [ ] T017 **[CP-1]** Commit and push: `git add` → `git commit -m "[CP-1] QW-1: RoleHierarchies + migrate role literals"` → `git push`

**Checkpoint CP-1**: RoleHierarchies landed, all role literals replaced, build + tests green, pushed.

---

## Phase 3: User Story 2 — Ship RBAC violations eliminated (Priority: P1, QW-7 / CP-2)

**Goal**: Close the shipped ProjectsController role gap and the RegisterUser enumeration vulnerability.

**Independent Test**: (a) Sign in as a user whose only role is `ProjectCoordinator`; access any `ProjectsController` endpoint within their project — expect success (was denied before). (b) Submit two `RegisterUser` commands, one duplicating email and one duplicating national ID — expect identical `Result.ErrorCode` and `Result.ErrorMessages`.

### Tests for User Story 2

- [ ] T018 [US2] Add unit test `RegisterUserHandler_DuplicateEmail_And_DuplicateNationalId_ReturnIdenticalResult` in `tests/Mentoory.Access.Tests/Commands/RegisterUser/RegisterUserHandlerTests.cs` — construct two commands, assert `IsSuccess==false` and identical `ErrorCode` + `ErrorMessages`

### Implementation for User Story 2

- [ ] T019 [US2] Add `Registration_Conflict` member to the `ErrorCode` enum (locate enum via grep: `grep -rn "enum ErrorCode" --include='*.cs'`); document the semantic meaning in XML doc
- [ ] T020 [US2] Modify `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserHandler.cs` per data-model.md § C-12: collapse the two conflict branches (email, nationalId) into a single branch returning `Failure(ErrorCode.Registration_Conflict, (string.Empty, "No se pudo completar el registro. Verifica tus datos."))`
- [ ] T021 [US2] Update `Mentoory.Web/Areas/Administration/Controllers/ProjectsController.cs` line 17: change `[Authorize(Roles = RoleHierarchies.IncubatorScope)]` (as left by CP-1) to `[Authorize(Roles = RoleHierarchies.ProjectScope)]`
- [ ] T022 [US2] (optional, implementer discretion) Audit `ErrorCode.Email_AlreadyRegistered` and `ErrorCode.NationalId_AlreadyRegistered`: grep for consumers. If none, remove them in this CP; if any remain, leave the enum members for now
- [ ] T023 [US2] Run pre-checkpoint gates: build green, tests green (including new T018)
- [ ] T024 [US2] **[CP-2]** Commit and push: `git commit -m "[CP-2] QW-7: ProjectsController scope + RegisterUser enumeration fix"` → `git push`

**Checkpoint CP-2**: FR-007 (email-vs-nationalid indistinguishability) and FR-022 (ProjectScope on ProjectsController) verifiable; US2 independently demoable.

---

## Phase 4: User Story 4 — Tenant boundary fails loud, not silent (Priority: P2, QW-3 / CP-3)

**Goal**: Remove the silent-failure cast from `TenantContextMiddleware`.

**Independent Test**: Inspect middleware source — no `is TenantContextService` cast. Drive a request end-to-end with an authenticated user — `CurrentIncubatorId` is set on the request-scoped `ITenantContext`.

### Implementation for User Story 4

- [ ] T025 [US4] Add `Mentoory.Shared.Application/Interfaces/ITenantContextWriter.cs` per data-model.md § C-7
- [ ] T026 [US4] Modify `TenantContextService` (locate in `Mentoory.Shared.Infrastructure/Services/`) per § C-8: implement both `ITenantContext` and `ITenantContextWriter`; make `CurrentIncubatorId` private-set; add `SetCurrentIncubatorId(long?)`
- [ ] T027 [US4] Update DI registration (locate in `Mentoory.Web/Program.cs` or module extension): register `TenantContextService` scoped; forward both interface registrations to the same instance via `sp.GetRequiredService<TenantContextService>()`
- [ ] T028 [US4] Modify `Mentoory.Web/Infrastructure/Authorization/TenantContextMiddleware.cs`: change `InvokeAsync` signature to inject `ITenantContextWriter` (per-request via method injection); remove the `is TenantContextService` cast; call `writer.SetCurrentIncubatorId(incubatorId)` directly
- [ ] T029 [US4] Run pre-checkpoint gates: build green, tests green (integration tests that drive the middleware should still pass)
- [ ] T030 [US4] **[CP-3]** Commit and push: `git commit -m "[CP-3] QW-3: ITenantContextWriter + middleware hardening"` → `git push`

**Checkpoint CP-3**: `grep -rn "is TenantContextService" --include='*.cs'` returns empty; middleware tests pass.

---

## Phase 5: User Story 6 — Validator behavior static-typed (Priority: P3, QW-5 / CP-4)

**Goal**: Replace reflection-based `ValidatorBehavior<TRequest, TResponse>` with two concrete-generic pipelines.

**Independent Test**: Submit any existing command that fails FluentValidation; the returned `Result.ErrorCode` and `ErrorMessages` shape are identical to pre-refactor. `ConcurrentDictionary` no longer appears in `Mentoory.Shared.Application/Behaviors/`.

### Implementation for User Story 6

- [ ] T031 [US6] Add `Mentoory.Shared.Application/Behaviors/ResultValidatorBehavior.cs` per data-model.md § C-10 (for `IBaseRequest` commands)
- [ ] T032 [US6] Add `Mentoory.Shared.Application/Behaviors/ResultTValidatorBehavior.cs` per § C-11 (for `IBaseRequest<TResponse>` commands)
- [ ] T033 [US6] Delete `Mentoory.Shared.Application/Behaviors/ValidatorBehavior.cs` (the reflection-based old behavior)
- [ ] T034 [US6] Update DI registration in `Mentoory.Web/Program.cs` (and/or the `AddSharedApplication()` extension method): replace the single open-generic `ValidatorBehavior` registration with two open-generic registrations per data-model.md § C-11 footnote
- [ ] T035 [US6] Run grep verification: `grep -rn "ConcurrentDictionary" Mentoory.Shared.Application/Behaviors/ --include='*.cs'` — MUST be empty
- [ ] T036 [US6] Manual parity smoke: run one existing validator-failing test and confirm `Result` shape unchanged
- [ ] T037 [US6] Run pre-checkpoint gates
- [ ] T038 [US6] **[CP-4]** Commit and push: `git commit -m "[CP-4] QW-5: ValidatorBehavior split into two concrete generics"` → `git push`

**Checkpoint CP-4**: Behavior parity preserved; reflection removed.

---

## Phase 6: User Story 5 — Empty test projects carry real tests (Priority: P2, QW-6 / CP-5)

**Goal**: Establish a non-zero baseline of actual tests in each of Knowledge, Mentoring, Notification, Subscription test projects.

**Independent Test**: `dotnet test tests/Mentoory.{Module}.Tests/` reports ≥ 1 passing `[Fact]` per module with a non-trivial assertion.

### Implementation for User Story 5

- [ ] T039 [P] [US5] Add one real `[Fact]` to `tests/Mentoory.Knowledge.Tests/SmokeTests.cs` exercising real Knowledge module code; fallback per research.md § R-9 is a DI-resolution test calling `AddKnowledgeApplication()`/`AddKnowledgeInfrastructure()`
- [ ] T040 [P] [US5] Add one real `[Fact]` to `tests/Mentoory.Mentoring.Tests/SmokeTests.cs` (same pattern for Mentoring module)
- [ ] T041 [P] [US5] Add one real `[Fact]` to `tests/Mentoory.Notification.Tests/SmokeTests.cs` (same pattern for Notification module)
- [ ] T042 [P] [US5] Add one real `[Fact]` to `tests/Mentoory.Subscription.Tests/SmokeTests.cs` (same pattern for Subscription module)
- [ ] T043 [US5] Run all four test projects individually: each must report ≥ 1 passing test
- [ ] T044 [US5] Run pre-checkpoint gates
- [ ] T045 [US5] **[CP-5]** Commit and push: `git commit -m "[CP-5] QW-6: smoke-test baseline for Knowledge/Mentoring/Notification/Subscription"` → `git push`

**Checkpoint CP-5**: four previously-empty test projects have non-zero tests; FR-019 verifiable.

---

## Phase 7: User Story 3 — Domain layer framework-pure (Priority: P2, QW-2 / CP-6)

**Goal**: Remove MediatR from `Mentoory.Shared.Domain`. All domain events implement `IDomainEvent`; dispatch goes through a `DomainEventNotification<T>` adapter.

**Independent Test**: `grep -rn "using MediatR" Mentoory.Shared.Domain/` returns empty. `Mentoory.Shared.Domain.csproj` has no MediatR `PackageReference`. Existing event-emitting end-to-end flows still trigger their handlers.

**⚠️ CRITICAL ORDER**: Sub-steps below MUST land together in a single CP-6 commit. Do NOT commit partial migrations.

### Implementation for User Story 3

- [ ] T046 [US3] Add `Mentoory.Shared.Domain/SeedWork/IDomainEvent.cs` per data-model.md § C-3 (marker interface, no members, no `using MediatR`)
- [ ] T047 [US3] Add `Mentoory.Shared.Application/DomainEvents/DomainEventNotification.cs` per § C-4 (generic adapter wrapping `IDomainEvent` as `INotification`)
- [ ] T048 [US3] Modify `Mentoory.Shared.Domain/SeedWork/Entity.cs` per § C-5: change `List<INotification>` → `List<IDomainEvent>`; change method signatures; remove `using MediatR;`. Solution will FAIL to compile at this point — expected.
- [ ] T049 [US3] Modify `Mentoory.Shared.Infrastructure/Persistence/SharedAbstractDbContext.cs` `DispatchDomainEventsAsync` (or equivalent) per § C-6: wrap each `IDomainEvent` in `DomainEventNotification<>` via `MakeGenericType` + `Activator.CreateInstance` before `IPublisher.Publish`
- [ ] T050 [US3] Enumerate every concrete domain event class currently inheriting `INotification`. Command: `grep -rn ": INotification\|,\s*INotification" --include='*.cs' Mentoory.*.Domain/` (excluding integration events). Record the list.
- [ ] T051 [US3] For each class listed in T050, change `: INotification` (or `,  INotification`) to `: IDomainEvent`. Remove unused `using MediatR;` imports.
- [ ] T052 [US3] Enumerate every `INotificationHandler<TEvent>` where `TEvent : IDomainEvent` (post-migration). Command: `grep -rn "INotificationHandler<" --include='*.cs' Mentoory.*.Application/`
- [ ] T053 [US3] For each handler from T052, change the generic parameter from `INotificationHandler<TEvent>` to `INotificationHandler<DomainEventNotification<TEvent>>`; update the `Handle` method parameter and unwrap via `notification.DomainEvent`. NOTE: do NOT touch handlers whose `TEvent : IIntegrationEvent` — integration events stay as-is.
- [ ] T054 [US3] Run `dotnet build Mentoory.sln`. Compiler will be the checklist for any missed handler. Fix systematically until green.
- [ ] T055 [US3] Remove `<PackageReference Include="MediatR" />` from `Mentoory.Shared.Domain/Mentoory.Shared.Domain.csproj`. Rebuild. If build fails, a Domain type still references MediatR — fix and retry.
- [ ] T056 [US3] Verification greps:
  - `grep -rn "using MediatR\|MediatR\." Mentoory.Shared.Domain/ --include='*.cs'` — MUST be empty
  - `grep -rn "<PackageReference Include=\"MediatR" Mentoory.Shared.Domain/*.csproj` — MUST be empty
- [ ] T057 [US3] Manual smoke: trigger a domain-event-emitting flow (e.g., user registration). Confirm the corresponding handler's side effect fires (check logs, outbox table if any, email queue, etc.). This is a no-regression check for FR-007 + edge case E-2.
- [ ] T058 [US3] Run pre-checkpoint gates: full solution build (warnaserror), full test suite
- [ ] T059 [US3] **[CP-6]** Commit and push: `git commit -m "[CP-6] QW-2: IDomainEvent migration; MediatR removed from Shared.Domain"` → `git push`

**Checkpoint CP-6**: largest refactor; end-to-end dispatch preserved; Domain now MediatR-free.

---

## Phase 8: User Story 1 — Constitution violations fail the build (Priority: P1, QW-4 / CP-7)

**Goal**: Add `Mentoory.Tests.Architecture` with eight rules that mechanically enforce constitution principles.

**⚠️ MUST RUN LAST**: Rules R-4.2.1 (no MediatR in Domain) requires CP-6 done. Rule R-4.2.8 (no role literals) requires CP-1 done. Any prior-CP regression surfaces here.

**Independent Test**: `dotnet test tests/Mentoory.Tests.Architecture/` — 8 rules, all green. Introduce a deliberate violation → rule fails with a specific offender.

### Tests for User Story 1 (the rules ARE the tests)

- [ ] T060 [US1] Add `NetArchTest.Rules` package version to `Directory.Packages.props` (latest stable)
- [ ] T061 [US1] Create `tests/Mentoory.Tests.Architecture/Mentoory.Tests.Architecture.csproj` per data-model.md § C-13 (xUnit + NetArchTest.Rules + FluentAssertions; references every `Mentoory.*.{Domain,Application,Infrastructure}` csproj + `Mentoory.Web` csproj)
- [ ] T062 [US1] Add `tests/Mentoory.Tests.Architecture/` to `Mentoory.sln`: `dotnet sln add tests/Mentoory.Tests.Architecture/Mentoory.Tests.Architecture.csproj`
- [ ] T063 [US1] Implement rule **R-4.2.1** in `LayerBoundaryTests.cs`: `Mentoory.*.Domain` must not depend on MediatR, EntityFrameworkCore, AspNetCore, FluentValidation (NetArchTest: `Types.InAssembliesMatching("Mentoory.*.Domain").ShouldNot().HaveDependencyOnAny(...)`)
- [ ] T064 [US1] Implement rule **R-4.2.2** in `NoDateTimeUtcNowTests.cs` (source-grep fallback per research.md § R-3): walk `.cs` files under `Mentoory.*.Domain` and `Mentoory.*.Application`; strip comments+string-literals; assert `DateTime.UtcNow` and `DateTime.Now` absent
- [ ] T065 [US1] Implement rule **R-4.2.3** in `LayerBoundaryTests.cs`: `Mentoory.*.Application` must not depend on EntityFrameworkCore or AspNetCore
- [ ] T066 [US1] Implement rule **R-4.2.4** in `LayerBoundaryTests.cs`: `Mentoory.*.Infrastructure` must not depend on `Mentoory.Web`
- [ ] T067 [US1] Implement rule **R-4.2.5** in `ForbiddenPatternTests.cs`: no class name contains `AutoMapper`; no class references `AutoMapper` namespace
- [ ] T068 [US1] Implement rule **R-4.2.6** in `ForbiddenPatternTests.cs`: no class references `Dapper` namespace
- [ ] T069 [US1] Implement rule **R-4.2.7** in `NamingConventionTests.cs`: classes ending with `Command` implement `IBaseRequest` or `IBaseRequest<T>`; classes ending with `Query` implement `IBaseRequest<T>`; classes ending with `Handler` inherit `BaseCommandHandler` or implement `IRequestHandler<,>`
- [ ] T070 [US1] Implement rule **R-4.2.8** in `NoRoleLiteralsTests.cs` (source-grep): enumerate `.cs` files excluding `Roles.cs`, generated files, bin/obj; assert no `"GlobalAdmin"`, `"IncubatorAdmin"`, `"ProjectCoordinator"`, `"Mentor"`, `"Entrepreneur"`, `"Sponsor"` literals
- [ ] T071 [US1] Configure exclusions for generated code (Mapperly mappers, EF migrations) in all rules per edge case E-3. Typical exclusion: class-name contains `Mapper` generated by Mapperly (suffix `Mapper` + `[Generated]` attribute) or namespace matches `*.Generated.*`
- [ ] T072 [US1] Run `dotnet test tests/Mentoory.Tests.Architecture/` — all 8 rules MUST pass. If any rule fails, the surfaced offender is a genuine violation from a prior CP — fix it in this CP (do NOT suppress the rule) or escalate.
- [ ] T073 [US1] Run pre-checkpoint gates: full solution build (warnaserror) + full test suite (includes the new architecture tests)
- [ ] T074 [US1] **[CP-7]** Commit and push: `git commit -m "[CP-7] QW-4: Mentoory.Tests.Architecture with 8 constitution rules"` → `git push`

**Checkpoint CP-7**: constitution rules mechanically enforced. US1's value delivered.

---

## Phase 9: Polish & PR (CP-8)

- [ ] T075 Verify the full branch: `dotnet build Mentoory.sln -warnaserror` (0 warnings), `dotnet test Mentoory.sln` (all green including architecture tests)
- [ ] T076 Review commit log: `git log develop..HEAD --oneline` — expect ~8 commits (CP-0..CP-7), each tagged `[CP-N]`
- [ ] T077 Manual end-to-end smoke per quickstart.md: sign in as each role; confirm menu + authorization; register a new user; trigger a domain-event flow; confirm handlers fire
- [ ] T078 **[CP-8]** Open PR to `develop` using the body from quickstart.md Phase CP-8: `gh pr create --base develop ...`
- [ ] T079 Confirm CI green on the PR (architecture tests run in the standard `dotnet test` invocation per research.md § R-10)
- [ ] T080 Request review; address feedback iteratively; **do not** squash until review is done (per-CP commits preserve review granularity)

---

## Dependencies & Execution Order

### Phase dependency graph

```text
Phase 1 Setup (CP-0, DONE)
     │
     ▼
Phase 2 Foundational (CP-1 / QW-1 RoleHierarchies)  ◄── blocks all below
     │
     ├──► Phase 3 US2 (CP-2 / QW-7 ship-violations)
     │
     ├──► Phase 4 US4 (CP-3 / QW-3 TenantWriter)              [independent]
     │
     ├──► Phase 5 US6 (CP-4 / QW-5 Validator split)           [independent]
     │
     ├──► Phase 6 US5 (CP-5 / QW-6 smoke tests)               [independent]
     │
     └──► Phase 7 US3 (CP-6 / QW-2 IDomainEvent)              [independent from 4-6]
                  │
                  ▼
             Phase 8 US1 (CP-7 / QW-4 architecture tests)  ◄── blocks on ALL prior CPs
                  │
                  ▼
             Phase 9 Polish + PR (CP-8)
```

### Recommended serial execution order (deterministic; minimises rebase pain)

**CP-0** (DONE) → **CP-1** (Phase 2) → **CP-2** (US2) → **CP-3** (US4) → **CP-4** (US6) → **CP-5** (US5) → **CP-6** (US3) → **CP-7** (US1) → **CP-8** (PR).

This order diverges from spec priority order. Spec priority is value-weighted (US1 is P1 because it prevents future drift). Checkpoint order is dependency-weighted (US1 must be LAST because all its rules depend on prior CPs being clean).

### User-story priority vs. checkpoint order

| Spec priority | User Story | Checkpoint | Rationale |
|---|---|---|---|
| P1 | US1 Arch tests | **CP-7 (last)** | All rules require prior CPs green; CP-7 is the acceptance gate |
| P1 | US2 Ship violations | CP-2 | Depends on CP-1's `RoleHierarchies` |
| P2 | US3 Domain purity | CP-6 | Large refactor; lands after small CPs so branch is mostly-green |
| P2 | US4 TenantWriter | CP-3 | Independent; small; ships early |
| P2 | US5 Smoke tests | CP-5 | Independent; mechanical |
| P3 | US6 Validator split | CP-4 | Independent; mechanical |

### Parallel opportunities

Within each CP, tasks marked `[P]` can run in parallel (different files, no shared state):

- **CP-1**: T002–T013 (12 controller edits in parallel — same edit pattern, different files). T014 (MenuConfiguration.cs) serial.
- **CP-5**: T039–T042 (4 smoke-test additions in parallel — different test projects).
- **CP-7**: T063–T070 (rule-implementation tasks writable in parallel across files `LayerBoundaryTests.cs`, `NoDateTimeUtcNowTests.cs`, `ForbiddenPatternTests.cs`, `NamingConventionTests.cs`, `NoRoleLiteralsTests.cs`).

Between CPs: NO parallelism. Each CP is a gate; the next CP starts only after the previous has green build + tests + push.

### Rebase policy between CPs

If `develop` advances while the branch is in flight, rebase BETWEEN checkpoints (never mid-CP). Preferred sequence:

```bash
git fetch origin
git rebase origin/develop
# resolve conflicts if any
dotnet build -warnaserror && dotnet test
git push --force-with-lease origin 017-crosscut-hardening-phase0
```

`--force-with-lease` is required after rebase; `--force` without `-with-lease` is forbidden (loses work if someone else pushed).

---

## Parallel Example: Phase 2 CP-1

```bash
# After T001 (RoleHierarchies.cs added), dispatch 12 controller edits in parallel:
Task T002: migrate BatchUploadController.cs
Task T003: migrate DashboardController.cs
Task T004: migrate ProjectsController.cs (to IncubatorScope — not yet ProjectScope)
Task T005: migrate UsersController.cs (Administration)
Task T006: migrate AnswerCorrectionController.cs
Task T007: migrate DiagnosticsController.cs
Task T008: migrate DiagnosticController.cs (Participant)
Task T009: migrate ConfigurationController.cs
Task T010: migrate IncubatorsController.cs
Task T011: migrate SponsorController.cs
Task T012: migrate TemplatesController.cs (Platform)
Task T013: migrate UsersController.cs (Platform)

# Then serial:
T014 MenuConfiguration.cs → T015 grep verify → T016 build+test gates → T017 commit+push
```

---

## Implementation Strategy

### Serial (recommended — single implementer)

1. Phase 1 Setup: DONE.
2. Phase 2 Foundational: T001..T017 → **CP-1 push**.
3. Phase 3 US2: T018..T024 → **CP-2 push**.
4. Phase 4 US4: T025..T030 → **CP-3 push**.
5. Phase 5 US6: T031..T038 → **CP-4 push**.
6. Phase 6 US5: T039..T045 → **CP-5 push**.
7. Phase 7 US3: T046..T059 → **CP-6 push**.
8. Phase 8 US1: T060..T074 → **CP-7 push**.
9. Phase 9 Polish + PR: T075..T080 → **CP-8 PR**.

### Parallel (multi-implementer after CP-1)

Not recommended for this bundle. Reasons:
- CP-6 (Domain refactor) touches widely; parallel work on the same branch risks conflicts.
- CP-7 (architecture tests) must land last; splitting work across implementers complicates the "all prior CPs done" gate.
- The bundle is small enough that serial execution is faster than coordination overhead.

Accept parallel only if two independent implementers claim disjoint CPs (e.g., CP-3 and CP-4) and agree who rebases first.

### Checkpoint validation (at every CP)

Per quickstart.md:
1. `dotnet build Mentoory.sln -warnaserror` → 0 warnings
2. `dotnet test Mentoory.sln --no-build` → all green
3. `git status --short` → clean
4. `git commit -m "[CP-N] ..."` (use template in plan.md)
5. `git push origin 017-crosscut-hardening-phase0`

If any gate fails: STOP. Fix root cause. `--no-verify`, warning suppression, and test skipping are FORBIDDEN.

---

## Notes

- **[P]** tasks = different files, no dependencies on other incomplete tasks
- **[US#]** labels map each task to its user story for traceability
- **[CP-N]** markers are commit+push gates — every CP ends with a push to `origin`
- Each user story MUST be independently verifiable at its checkpoint boundary
- Commit granularity = one commit per checkpoint (NOT one commit per task). Tasks within a CP are logical groupings; the CP is the reviewable unit.
- Verify gates (build/tests) BEFORE committing, not after. A red commit pushed is a red branch.
- Avoid: vague tasks, same-file conflicts between [P] tasks, cross-CP dependencies that break checkpoint independence.
