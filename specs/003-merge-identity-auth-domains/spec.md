# Feature Specification: Merge Identity and Authorization Domains

**Feature Branch**: `003-merge-identity-auth-domains`  
**Created**: 2026-04-03  
**Status**: Draft  
**Input**: User description: "Merge Identity and Authorization domains in a single one. No need to have them separate for effects of this application. Suggests a new domain name. No new logic or features at this point, just the merge. Make sure no orphan code or hanging references are left behind, all tests must pass."

## Clarifications

### Session 2026-04-03

- Q: What should the unified domain name be? → A: "Access" (`Mentoory.Access.*`) — confirmed as the canonical name combining identity and authorization semantics.
- Q: Should the Web Area be renamed from "Identity" to "Access" (changing URLs)? → A: Yes, rename to "Access" for full naming consistency. URLs become `/Access/Login`, `/Access/Register`, etc.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unified Domain Structure with Zero Behavior Change (Priority: P1)

As a developer working on the platform, I need the Identity and Authorization domains consolidated into a single "Access" domain so that the codebase is simpler to navigate and maintain, without any change in runtime behavior.

**Why this priority**: This is the entire scope of the feature — a structural refactor that preserves all existing functionality while eliminating the artificial boundary between two closely coupled domains.

**Independent Test**: Can be fully tested by running the complete test suite (`dotnet test`) before and after the merge, verifying identical pass/fail results and confirming no orphan files, namespaces, or project references remain.

**Acceptance Scenarios**:

1. **Given** the platform has separate Identity and Authorization projects (Domain, Application, Infrastructure), **When** the merge is complete, **Then** a single set of Access projects (Mentoory.Access.Domain, Mentoory.Access.Application, Mentoory.Access.Infrastructure) replaces all six previous projects.
2. **Given** the current solution builds successfully, **When** all Identity and Authorization namespaces are renamed to Access, **Then** the solution builds with zero errors and zero warnings.
3. **Given** all existing E2E and unit tests pass before the merge, **When** the merge is complete, **Then** all tests continue to pass without modification to test logic (only namespace/reference updates).
4. **Given** the web layer references Identity and Authorization areas, **When** the merge is complete, **Then** the web layer references a single Access area and all routes continue to function identically.
5. **Given** the database uses `[identity]` and `[authorization]` schemas, **When** the merge is complete, **Then** both schemas are consolidated into a single `[access]` schema with all tables, indexes, and constraints preserved.
6. **Given** integration events flow between the two domains (e.g., UserRegisteredEvent consumed by Authorization), **When** the merge is complete, **Then** these become internal domain events or direct method calls within the unified Access domain — no cross-domain integration events needed.

---

### User Story 2 - Clean Solution Structure with No Orphan Artifacts (Priority: P1)

As a developer, I need the old Identity and Authorization project files, folders, and references completely removed from the solution so there are no stale artifacts that could cause confusion or build issues.

**Why this priority**: Orphan references are a common source of build failures and developer confusion after refactors; this must be completed as part of the same effort.

**Independent Test**: Can be verified by confirming no files, project references, or using/import statements reference the old `Mentoory.Identity.*` or `Mentoory.Authorization.*` namespaces anywhere in the solution.

**Acceptance Scenarios**:

1. **Given** the merge is complete, **When** searching the solution for `Mentoory.Identity` or `Mentoory.Authorization` namespace references, **Then** zero results are found.
2. **Given** the merge is complete, **When** listing project directories, **Then** no `Mentoory.Identity.*` or `Mentoory.Authorization.*` project folders exist.
3. **Given** the solution file (.sln) previously referenced six Identity/Authorization projects, **When** the merge is complete, **Then** the solution file references exactly three Access projects (Domain, Application, Infrastructure) in their place.
4. **Given** the DependencyInjection registrations previously had separate `AddIdentityApplication`, `AddIdentityInfrastructure`, `AddAuthorizationApplication`, `AddAuthorizationInfrastructure`, **When** the merge is complete, **Then** unified `AddAccessApplication` and `AddAccessInfrastructure` extension methods replace them.

### Edge Cases

- What happens when database migration scripts reference old schema names? PostDeployment scripts must be updated to use the new `[access]` schema, and a migration script must rename existing tables.
- How does the UserProfile read model (previously in Authorization, synchronized via integration event from Identity) change? It becomes a regular entity within the Access domain — no integration event synchronization needed since both aggregates now live in the same bounded context.
- What happens to the `[identity]` and `[authorization]` SQL schemas in existing databases? A schema migration must move all tables to the `[access]` schema and drop the empty old schemas.
- What about the Web Areas? The existing `Identity` area (login, register, etc.) will be renamed to `Access`, changing routes from `/Identity/*` to `/Access/*`. All functionality must continue to work under the new URLs.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST consolidate six projects (Mentoory.Identity.Domain, Mentoory.Identity.Application, Mentoory.Identity.Infrastructure, Mentoory.Authorization.Domain, Mentoory.Authorization.Application, Mentoory.Authorization.Infrastructure) into three projects (Mentoory.Access.Domain, Mentoory.Access.Application, Mentoory.Access.Infrastructure).
- **FR-002**: System MUST preserve all existing aggregates (User, AuthSession, RoleAssignment), value objects (EmailAddress, NationalIdentity, HashedPassword), enums (AccountStatus, PlatformRole, Permission), and read models (UserProfile, UserContext) with identical behavior.
- **FR-003**: System MUST update all namespace references from `Mentoory.Identity.*` and `Mentoory.Authorization.*` to `Mentoory.Access.*` throughout the entire solution.
- **FR-004**: System MUST consolidate the `[identity]` and `[authorization]` database schemas into a single `[access]` schema, preserving all tables, indexes, constraints, and data.
- **FR-005**: System MUST remove the cross-domain integration event pattern (UserRegisteredEvent handler in Authorization that creates UserProfile) and replace it with direct domain-internal logic, since both concerns now live in the same bounded context.
- **FR-006**: System MUST update all DI registrations, replacing separate Identity/Authorization registration methods with unified Access registration methods.
- **FR-007**: System MUST rename the Web Area from "Identity" to "Access" (URLs change from `/Identity/*` to `/Access/*`), update all Controllers, Views, and middleware to reference the new Access namespace, and maintain all existing functionality.
- **FR-008**: System MUST remove all old project directories and solution references, leaving zero orphan files or stale references.
- **FR-009**: System MUST ensure all existing tests pass after the merge with only namespace/reference updates — no test logic changes.
- **FR-010**: System MUST update PostDeployment scripts in `/Mentoory.Db.PostDeployment/` to use the new `[access]` schema.

### Key Entities

- **User**: Core identity aggregate — email, credentials, verification tokens, password reset tokens, account status. Moves from Identity to Access domain unchanged.
- **AuthSession**: Session management aggregate — session tokens, IP tracking, active context (incubator/project/role). Moves from Identity to Access domain unchanged.
- **RoleAssignment**: Role-based access control aggregate — user-to-role mappings scoped to incubator/project. Moves from Authorization to Access domain unchanged.
- **UserProfile**: Read model previously synchronized across domains — becomes a regular entity within Access, no longer needing integration event synchronization.
- **UserContext**: Value object/read model for context selection — remains unchanged in Access domain.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The solution compiles with zero errors and zero warnings after the merge.
- **SC-002**: 100% of existing tests pass without changes to test assertions or logic (only namespace/reference updates allowed).
- **SC-003**: All user-facing workflows (login, register, logout, email verification, password reset, context selection, role assignment) function identically before and after the merge.
- **SC-004**: Zero references to `Mentoory.Identity` or `Mentoory.Authorization` namespaces remain anywhere in the codebase after the merge.
- **SC-005**: The solution project count decreases by exactly three (six old projects replaced by three new ones).
- **SC-006**: Database schema consolidation preserves all existing data and relationships.

## Assumptions

- The new unified domain is confirmed as "Access" (`Mentoory.Access.*`), combining identity (who you are) and authorization (what you can do) into a single cohesive bounded context.
- No new features, logic, or behavioral changes are introduced — this is a pure structural refactor.
- The Web Area for login/register routes will be renamed from "Identity" to "Access", changing URLs to `/Access/Login`, `/Access/Register`, etc. for full naming consistency.
- Existing database data must be preserved through schema migration — this is not a destructive rebuild.
- The `UserRegisteredEvent` integration event class itself may be kept if other domains consume it, but the handler within the former Authorization context becomes a direct internal call.
- All SSDT/DACPAC schema definitions in `Mentoory.Db` must be updated to reflect the new `[access]` schema.
