# Feature Specification: Cross-cutting Hardening — Phase 0 (Quick Wins)

**Feature Branch**: `017-crosscut-hardening-phase0`
**Created**: 2026-04-20
**Status**: Draft
**Input**: User description: Close high-leverage, low-risk cross-cutting code-quality gaps identified in the April 2026 infrastructure audit. Remove shipped constitution violations, establish mechanical enforcement of constitution rules via architecture tests, and prepare the surface for the strategic cross-cutting specs (Outbox, Audit, Permissions) queued in brainstorm #10.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Platform engineer: constitution violations fail the build (Priority: P1)

A platform engineer refactors a command handler and accidentally references `DateTime.UtcNow` in the Domain layer, or introduces a `"GlobalAdmin"` literal in a new controller attribute. Today these mistakes ship; they are caught — if at all — during manual code review.

**Why this priority**: Prevents silent constitutional drift. Without mechanical enforcement, every new feature is one distracted reviewer away from introducing debt. This user story alone delivers the P1 value: a green CI with this bundle means the constitution's mechanical rules are honored; a red CI means the engineer sees the problem before the PR is even opened for review.

**Independent Test**: Introduce a deliberate violation (add `"Sponsor"` literal to a controller, or `DateTime.UtcNow` to a Domain file); run the architecture test suite; expect failure with a pointer to the offending file. Remove the violation; expect green.

**Acceptance Scenarios**:

1. **Given** a fresh checkout of the hardened branch, **When** the engineer runs `dotnet test` against `Mentoory.Tests.Architecture`, **Then** all eight rules (R-4.2.1 through R-4.2.8) pass and the run reports zero failures.
2. **Given** an engineer adds a hardcoded role string `"IncubatorAdmin"` to a new controller, **When** CI runs, **Then** the architecture test fails with a message identifying the file and the forbidden literal.
3. **Given** an engineer adds `using EntityFrameworkCore;` to a `Domain` project file, **When** CI runs, **Then** rule R-4.2.1 fails and blocks the merge.

---

### User Story 2 — Security engineer: shipped RBAC violations are eliminated (Priority: P1)

A security-conscious reviewer auditing production code finds that (a) `ProjectsController` protects operations with `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]` — missing `ProjectCoordinator` and contradicting the role hierarchy — and (b) `RegisterUserHandler` returns field-keyed errors that allow an anonymous attacker to enumerate which emails and national IDs are already registered.

**Why this priority**: Both are shipped violations. The RBAC gap breaks constitution §X (role hierarchy) in production; the enumeration issue is a security finding. Both are fixed as part of this bundle because the authorization fix depends on the centralised `RoleHierarchies` constants introduced here.

**Independent Test**: Attempt to access a `ProjectsController` endpoint as a user whose only role is `ProjectCoordinator`. Before the fix: denied (wrong). After the fix: allowed. Separately, submit registration requests with an existing email and an existing national ID. Before the fix: different error messages. After the fix: identical response shape.

**Acceptance Scenarios**:

1. **Given** a user assigned only the `ProjectCoordinator` role, **When** the user invokes any `ProjectsController` endpoint within their assigned project scope, **Then** access is granted (was denied before the fix).
2. **Given** an anonymous client, **When** the client submits registration requests with (a) an already-registered email or (b) an already-registered national ID, **Then** both requests return identical response bodies and status codes, with no field-keyed differentiation.
3. **Given** the `RoleHierarchies.ProjectScope` constant, **When** grepped from the solution, **Then** the single expansion is `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"` and it is referenced by `ProjectsController`.

---

### User Story 3 — Framework engineer: Domain layer is framework-pure (Priority: P2)

A framework engineer working on the strategic outbox spec needs to dispatch domain events without coupling the Domain layer to MediatR. Today, `Mentoory.Shared.Domain/SeedWork/Entity.cs` directly references `MediatR.INotification`, violating constitution §I.

**Why this priority**: P2 because it unblocks the strategic outbox/audit specs. A framework-pure Domain means future dispatch strategies (outbox, inbox, in-proc, distributed) are plug-in decisions in the Application/Infrastructure layer, not refactors that ripple through every aggregate.

**Independent Test**: Grep `Mentoory.Shared.Domain` for `using MediatR`. Expect zero hits. Grep `Mentoory.Shared.Domain.csproj` for MediatR package reference. Expect zero hits. Build the solution. Expect green. Run an existing test that emits a domain event end-to-end. Expect the event to be delivered to its `INotificationHandler` via the adapter.

**Acceptance Scenarios**:

1. **Given** the hardened codebase, **When** the engineer inspects `Mentoory.Shared.Domain/SeedWork/Entity.cs`, **Then** the file neither references nor imports any MediatR type.
2. **Given** an aggregate raises a domain event implementing `IDomainEvent`, **When** the transaction commits, **Then** the registered `INotificationHandler<DomainEventNotification<T>>` receives the event unchanged.
3. **Given** a domain event has zero registered handlers, **When** the transaction commits, **Then** the event is published without error.

---

### User Story 4 — Platform engineer: tenant boundary fails loud, not silent (Priority: P2)

A platform engineer adds a test double for `ITenantContext` in an integration test. Today, `TenantContextMiddleware` casts the resolved `ITenantContext` to the concrete `TenantContextService` type; if the cast fails, the middleware silently no-ops and the tenant boundary quietly stops working.

**Why this priority**: P2 because it hardens the single most security-critical piece of middleware. The abstraction leakage is subtle and its failure mode is invisible, which is exactly the kind of issue that ships undetected.

**Independent Test**: Register an alternate `ITenantContext` implementation in DI; drive a request through the middleware; assert that `CurrentIncubatorId` is set after the middleware runs. Before the fix: silently unset. After the fix: set or explicit DI registration error at startup.

**Acceptance Scenarios**:

1. **Given** the hardened `TenantContextMiddleware`, **When** the engineer inspects its source, **Then** no `is TenantContextService` or concrete-type cast appears.
2. **Given** `ITenantContextWriter` is resolved via DI, **When** an authenticated request carrying an `ActiveIncubatorId` claim flows through the middleware, **Then** `CurrentIncubatorId` on the request-scoped `ITenantContext` equals the claim value.
3. **Given** DI is misconfigured (writer missing), **When** the app starts, **Then** startup fails with a clear missing-service error (not a silent no-op at request time).

---

### User Story 5 — Test culture: empty modules carry at least one real test (Priority: P2)

The Knowledge, Mentoring, Notification, and Subscription test projects exist but contain zero tests. The first feature shipped into any of those modules sets the culture: either tests are expected, or they are optional. Today the default is "optional".

**Why this priority**: P2. Each of the four modules is on the near-term roadmap. A non-zero baseline makes "every new PR maintains non-zero tests" a trivial CI rule (tracked separately as OQ-1).

**Independent Test**: For each of the four modules, run `dotnet test Mentoory.{Module}.Tests`. Expect at least one passing test with a meaningful assertion.

**Acceptance Scenarios**:

1. **Given** `Mentoory.Knowledge.Tests`, **When** `dotnet test` runs, **Then** at least one `[Fact]` executes and passes with a non-trivial assertion.
2. **Given** `Mentoory.Mentoring.Tests`, **When** `dotnet test` runs, **Then** at least one `[Fact]` executes and passes.
3. **Given** `Mentoory.Notification.Tests`, **When** `dotnet test` runs, **Then** at least one `[Fact]` executes and passes.
4. **Given** `Mentoory.Subscription.Tests`, **When** `dotnet test` runs, **Then** at least one `[Fact]` executes and passes.

---

### User Story 6 — Framework engineer: validator behavior is static-typed, not reflective (Priority: P3)

A framework engineer debugging a validation failure today traces through a reflection-based factory that builds `Result<T>.Failure(...)` via cached `MethodInfo.Invoke`. The logic works but hides coupling to the shape of `Result<T>` behind a `ConcurrentDictionary`.

**Why this priority**: P3. The current implementation is functional. Splitting into two concrete generics is a clarity and maintainability win, not a correctness fix.

**Independent Test**: Run every existing test; all pass. Submit a command with validation failures; the returned `Result` shape matches what it did before the refactor.

**Acceptance Scenarios**:

1. **Given** the refactored pipeline, **When** an `IBaseRequest` (non-generic) command fails validation, **Then** `ResultValidatorBehavior<TRequest>` constructs the failure `Result` directly, without reflection.
2. **Given** an `IBaseRequest<T>` command fails validation, **When** the pipeline runs, **Then** `ResultTValidatorBehavior<TRequest, TResponse>` constructs `Result<TResponse>.Failure(...)` directly.
3. **Given** the refactored code, **When** grepped, **Then** `ConcurrentDictionary` no longer appears in `Mentoory.Shared.Application/Behaviors/`.

---

### Edge Cases

- **GlobalAdmin with null incubator**: `TenantContextWriter.SetCurrentIncubatorId(null)` must be a valid call and must not throw; middleware must preserve existing behavior for users operating in global scope without a selected incubator.
- **Domain event with no subscribers**: an event published through the `DomainEventNotification<T>` adapter when no handler is registered must complete without error (MediatR's default no-op behavior must remain observable end-to-end).
- **Architecture test false positives on generated code**: Mapperly-generated mappers and EF-generated code must not trigger naming or dependency rules. The test project must exclude generated types via type-name or namespace filters.
- **RegisterUser timing-based enumeration**: response-time differences between duplicate-email and duplicate-nationalid paths may still leak information. This is not closed by this feature; it is tracked as `OQ-2` for a follow-up spec.
- **Role strings persisted in the database**: if `RoleAssignments.Role` (or equivalent) stores role names as strings, those values MUST match the new `Roles.*` constants exactly. Implementation MUST verify during the replacement pass; any mismatch is a data-migration concern that MUST be handled in-PR, not silently ignored.

## Requirements *(mandatory)*

### Functional Requirements

**Authorization constants (QW-1)**

- **FR-001**: The system MUST expose a `Roles` static class (in `Mentoory.Shared.Application/Authorization/`) containing one public `const string` per role currently used in the platform: `GlobalAdmin`, `IncubatorAdmin`, `ProjectCoordinator`, `Mentor`, `Entrepreneur`, `Sponsor`.
- **FR-002**: The system MUST expose a `RoleHierarchies` static class containing comma-joined role strings for use with ASP.NET `[Authorize(Roles = ...)]`, covering at minimum: `PlatformScope`, `IncubatorScope`, `ProjectScope`, `MentoringScope`, `ParticipantScope`, `SponsorScope`. Each hierarchy MUST include every strictly higher role per constitution §X (higher-privilege roles inherit lower-scope access).
- **FR-003**: Every existing reference to a role name as a string literal (in `MenuConfiguration.cs`, controller `[Authorize]` attributes, or any other platform code) MUST be replaced with a reference to `Roles.*` or `RoleHierarchies.*`.

**Domain framework purity (QW-2)**

- **FR-004**: `Mentoory.Shared.Domain/SeedWork/` MUST define an `IDomainEvent` marker interface with no members.
- **FR-005**: `Mentoory.Shared.Domain/SeedWork/Entity.cs` MUST store domain events as `IReadOnlyCollection<IDomainEvent>` (or equivalent) and MUST NOT reference any MediatR type. The file MUST NOT contain `using MediatR;`.
- **FR-006**: `Mentoory.Shared.Domain.csproj` MUST NOT reference the MediatR NuGet package.
- **FR-007**: The system MUST provide an adapter (e.g., `DomainEventNotification<TEvent>` where `TEvent : IDomainEvent`) that wraps an `IDomainEvent` as a MediatR `INotification` at the dispatch boundary (`SaveEntitiesAsync` or equivalent). Domain event handlers MUST subscribe via `INotificationHandler<DomainEventNotification<TEvent>>`.
- **FR-008**: Every concrete domain event class that previously implemented `INotification` MUST be updated to implement `IDomainEvent` instead. Consumers (handlers) MUST be updated to the adapter-wrapped type.

**Tenant middleware hardening (QW-3)**

- **FR-009**: The system MUST define `ITenantContextWriter` in `Mentoory.Shared.Application/Interfaces/` with a single member: `void SetCurrentIncubatorId(long? incubatorId)`.
- **FR-010**: `TenantContextService` MUST implement both `ITenantContext` (read side) and `ITenantContextWriter` (write side). DI MUST register the same scoped instance for both interfaces.
- **FR-011**: `TenantContextMiddleware` MUST inject `ITenantContextWriter` directly. The middleware MUST NOT perform any `is`-check or cast to a concrete type.

**Mechanical constitution enforcement (QW-4)**

- **FR-012**: The solution MUST contain a test project `tests/Mentoory.Tests.Architecture/` using `NetArchTest.Rules` (or equivalent static-analysis test framework) with at minimum the following passing rules:
  - **R-4.2.1** `Mentoory.*.Domain` assemblies MUST NOT depend on MediatR, Entity Framework Core, ASP.NET Core, or FluentValidation.
  - **R-4.2.2** `Mentoory.*.Domain` source MUST NOT contain `DateTime.UtcNow` or `DateTime.Now`. (Source-level text scan is acceptable if IL-level inspection is infeasible.)
  - **R-4.2.3** `Mentoory.*.Application` assemblies MUST NOT depend on Entity Framework Core or ASP.NET Core.
  - **R-4.2.4** `Mentoory.*.Infrastructure` assemblies MUST NOT depend on `Mentoory.Web`.
  - **R-4.2.5** No class name MUST contain `AutoMapper`, and no class MUST reference the `AutoMapper.*` namespace.
  - **R-4.2.6** No class MUST reference the `Dapper.*` namespace.
  - **R-4.2.7** Classes ending with `Command` MUST implement `IBaseRequest` or `IBaseRequest<T>`; classes ending with `Query` MUST implement `IBaseRequest<T>`; classes ending with `Handler` MUST inherit `BaseCommandHandler<*>` or implement `IRequestHandler<*>`.
  - **R-4.2.8** Role name string literals (`"GlobalAdmin"`, `"IncubatorAdmin"`, `"ProjectCoordinator"`, `"Mentor"`, `"Entrepreneur"`, `"Sponsor"`) MUST NOT appear in any `.cs` file outside `Mentoory.Shared.Application/Authorization/Roles.cs` or generated code.
- **FR-013**: The architecture test project MUST be registered with CI and MUST fail the build on any rule violation.

**Validator behavior refactor (QW-5)**

- **FR-014**: The existing reflection-based `ValidatorBehavior<TRequest, TResponse>` MUST be removed.
- **FR-015**: The system MUST provide two replacement pipeline behaviors:
  - `ResultValidatorBehavior<TRequest>` implementing `IPipelineBehavior<TRequest, Result>` for `IBaseRequest` commands.
  - `ResultTValidatorBehavior<TRequest, TResponse>` implementing `IPipelineBehavior<TRequest, Result<TResponse>>` for `IBaseRequest<TResponse>` commands.
- **FR-016**: Each behavior MUST construct its specific `Result` or `Result<T>` failure directly from collected `ValidationFailure` entries, without reflection, without caching factories, and without `ConcurrentDictionary`.
- **FR-017**: DI MUST register both behaviors as open generics so MediatR dispatches each to the correct pipeline.
- **FR-018**: The observable behavior (error-code, message structure, validation ordering) MUST remain identical to the prior implementation.

**Test baseline (QW-6)**

- **FR-019**: Each of `Mentoory.Knowledge.Tests`, `Mentoory.Mentoring.Tests`, `Mentoory.Notification.Tests`, `Mentoory.Subscription.Tests` MUST contain at least one xUnit `[Fact]` with a non-trivial assertion (an assertion on real module code — not "assembly loads" no-op).

**Ship-violation fixes (QW-7)**

- **FR-020**: `RegisterUserHandler` MUST NOT return a field-keyed error that differentiates between "email already registered" and "national ID already registered". Both conflict cases MUST return an identical `Result.Failure` payload with a single field-agnostic error entry and the Spanish message `"No se pudo completar el registro. Verifica tus datos."`.
- **FR-021**: A unit test MUST assert that two distinct `RegisterUserCommand` instances — one conflicting only on email, one conflicting only on national ID — produce identical `Result.ErrorCode` and `Result.ErrorMessages` values.
- **FR-022**: `ProjectsController` MUST use `[Authorize(Roles = RoleHierarchies.ProjectScope)]` (which expands to `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"`) in place of the current `"IncubatorAdmin,GlobalAdmin"` attribute.

### Key Entities

- **`Roles` / `RoleHierarchies` constants**: source-of-truth authorization identifiers. Referenced everywhere role names appear in attributes, menu configuration, or lookup code.
- **`IDomainEvent`**: framework-free marker for domain-emitted events. Implemented in the Domain layer; adapted to `INotification` at the Application/Infrastructure boundary.
- **`DomainEventNotification<TEvent>`**: MediatR-side wrapper around `IDomainEvent`. Bridges Domain events into the MediatR pipeline without leaking MediatR into Domain.
- **`ITenantContextWriter`**: write-only interface for setting the per-request tenant context. Used exclusively by middleware.
- **`Mentoory.Tests.Architecture`**: test project hosting the NetArchTest rules that mechanically enforce constitution principles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of constitution-mandated layer-dependency, naming, and forbidden-pattern rules (R-4.2.1 through R-4.2.8) execute as automated tests, and all pass in CI.
- **SC-002**: `using MediatR;` appears zero times inside `Mentoory.Shared.Domain/`, verified by automated scan.
- **SC-003**: Zero role-name string literals appear outside the `Roles` constants file, verified by R-4.2.8.
- **SC-004**: `TenantContextMiddleware` contains zero concrete-type casts against `ITenantContext`, verified by source review and architecture test.
- **SC-005**: Each of four previously empty test projects (Knowledge, Mentoring, Notification, Subscription) executes at least one passing test with a real assertion.
- **SC-006**: Users whose only role is `ProjectCoordinator` can access `ProjectsController` endpoints within their project scope — a capability that is currently denied in production.
- **SC-007**: Anonymous callers submitting `RegisterUser` with either a duplicate email or a duplicate national ID receive identical response bodies and status codes; no field-keyed differentiation remains.
- **SC-008**: `dotnet build` produces zero warnings across the solution (constitution §V preserved).
- **SC-009**: All pre-existing tests continue to pass; no regressions introduced by the refactor.
- **SC-010**: Reflection-based factory dispatch is removed from the validation pipeline; zero occurrences of `ConcurrentDictionary` remain in `Mentoory.Shared.Application/Behaviors/`.

## Assumptions

- **Reasonable defaults**: where the audit did not prescribe exact naming, defaults follow existing project conventions (e.g., `Roles` and `RoleHierarchies` live under `Authorization/` in Application per OQ-3).
- **Scope bounded to the seven quick wins**: the Outbox, Audit pipeline, and Permission-layer specs are deliberately deferred and tracked as follow-ups in brainstorm #10 and the roadmap.
- **Architecture test framework**: `NetArchTest.Rules` is the default choice; a switch to `ArchUnitNET` or equivalent is acceptable if NetArchTest cannot express a rule (in particular R-4.2.2) — in which case a source-grep-based `[Fact]` or Roslyn analyzer is an acceptable fallback.
- **Role strings in persistent storage**: if any database-stored role name diverges from the new `Roles.*` constants, the implementation MUST raise a data-migration issue in the same PR rather than silently adjusting the constants to match the data.
- **Existing handler test coverage**: `RegisterUserHandler` already has a test class. Adding the identical-response-shape assertion is additive, not a new test infrastructure requirement.
- **Bundle-PR review ergonomics**: reviewers will see the change as a single PR with per-QW commits. Splitting the PR is left at implementer discretion if review burden becomes untenable.

## Dependencies

- **Governing documents**: `.specify/memory/constitution.md` (§I Clean Architecture, §V Zero-Warnings, §X Role Hierarchy), `.specify/memory/access-security-constitution.md`, and `CLAUDE.md` coding standards apply.
- **Blocked-by**: none.
- **Blocks / enables**: the strategic cross-cutting specs queued in `brainstorm/10-cross-cutting-hardening.md` — Subscription enforcement, Audit pipeline behavior + `[Audited]` attribute, and the `CheckPermission` layer — land against the cleaner surface introduced here.
- **Package dependencies**: `NetArchTest.Rules` (test-only). No new runtime packages.

## Out of Scope

- Outbox pattern via `SaveChangesInterceptor` and durable dispatch — strategic, separate spec per brainstorm #10.
- `AuditingBehavior<TRequest, TResponse>` pipeline behavior and `[Audited]` attribute — strategic, separate spec.
- `CheckPermissionQuery` layering on top of `[Authorize]` and `[RequirePermission]` attribute — strategic, separate spec.
- Introduction of a `BaseController` for MVC controllers.
- Migration from custom `ITimeProvider` to `System.TimeProvider`.
- Replacement of namespace-parsing `DbContextFactory` with explicit registration.
- Extraction of cross-module integration-event contracts into a neutral `Mentoory.Contracts` assembly.
- CI rule failing the build when any `Mentoory.*.Tests` project contains zero tests (tracked as `OQ-1`, likely separate PR).
- `RegisterUser` response-time-based enumeration hardening (tracked as `OQ-2`).

## Open Questions

- **OQ-1** — Should CI fail the build when a `Mentoory.*.Tests` project contains zero `[Fact]`/`[Theory]` attributes? Deferred to follow-up.
- **OQ-2** — `RegisterUser` response-time equalization to close timing-based enumeration. Deferred to follow-up spec.
- **OQ-3** — Resolved during spec drafting: `Roles.cs` lives in `Shared.Application/Authorization/` (not Domain), because roles are tied to ASP.NET `[Authorize]` semantics and are not first-class Domain concepts.
