# Research: Merge Identity and Authorization Domains

**Branch**: `003-merge-identity-auth-domains` | **Date**: 2026-04-03

## R1: DbContext Consolidation Strategy

**Decision**: Merge IdentityDbContext and AuthorizationDbContext into a single AccessDbContext.

**Rationale**: Both contexts already share the same physical SQL Server database and connection string (`DefaultConnection`). The separation existed only because they were in different bounded contexts. With the merge, a single DbContext is simpler, enables cross-entity queries without cross-context joins, and aligns with the single `[access]` schema.

**Alternatives considered**:
- Keep two DbContexts under Access namespace — rejected because it would add unnecessary complexity for no benefit when both domains are merged.
- Use a single shared DbContext across all domains — rejected because other domains (Tenant, Diagnostic, etc.) remain separate bounded contexts.

## R2: Integration Event Disposition

**Decision**: Keep `UserRegisteredEvent` class in `Mentoory.Access.Application/IntegrationEvents/` but remove the `UserRegisteredEventHandler` from the former Authorization context, replacing it with direct `UserProfile` creation in the `RegisterUserHandler`.

**Rationale**: The event may be consumed by other domains (e.g., Notification domain for welcome emails). Removing the class entirely would break those consumers. However, within the now-unified Access domain, direct creation of UserProfile during registration is simpler and transactionally consistent (same DbContext, same unit of work).

**Alternatives considered**:
- Remove event entirely — rejected because other domains may depend on it.
- Keep the handler as-is (notification-based) — rejected because intra-domain event handling adds unnecessary indirection.

## R3: Web Area Rename Impact

**Decision**: Rename `Areas/Identity/` to `Areas/Access/`. Update cookie authentication paths in Program.cs from `/Identity/Login` to `/Access/Login`.

**Rationale**: Clarification session confirmed full naming consistency. The application is in development (not production with bookmarked URLs), so URL stability is not a concern.

**Impact points**:
- `Program.cs` lines 70-72: LoginPath, LogoutPath, AccessDeniedPath
- All controllers in `Areas/Identity/Controllers/` → `Areas/Access/Controllers/`
- All view models in `Areas/Identity/Models/` → `Areas/Access/Models/`
- All views in `Areas/Identity/Views/` → `Areas/Access/Views/`
- E2E tests referencing `/Identity/` URLs (PlaywrightFixture.cs)
- Integration tests referencing `/Identity/` paths

## R4: Database Schema Migration Strategy

**Decision**: Create new `[access]` schema in SSDT, move all table definitions under it, and add a PostDeployment migration script to transfer existing data.

**Rationale**: SSDT/DACPAC handles schema changes declaratively. The DACPAC publish will create the new schema and tables. A PostDeployment script handles data migration from old schemas.

**Migration approach**:
1. Create `Mentoory.Db/access/Schema.sql` defining `CREATE SCHEMA [access]`
2. Move all table SQL files to `Mentoory.Db/access/Tables/`
3. Update all table definitions to use `[access]` schema
4. Remove old `identity/` and `authorization/` schema folders
5. Add PostDeployment migration script to:
   - Transfer data from `[identity].*` and `[authorization].*` to `[access].*` tables
   - Handle the rename atomically
6. Update existing PostDeployment seed scripts (002, 004, 005) to reference `[access]` schema

## R5: DbContextFactory Update

**Decision**: Replace the two mapping entries ("Identity" → IdentityDbContext, "Authorization" → AuthorizationDbContext) with a single entry ("Access" → AccessDbContext).

**Rationale**: The factory uses `requestNs.Split('.')[1]` to extract the module name from MediatR request namespaces. After the merge, all commands/queries will be in `Mentoory.Access.Application.*`, so the module name resolves to "Access".

**Impact**: No changes to the routing logic itself, only the dictionary entries.

## R6: Test Project Consolidation

**Decision**: Merge `tests/Mentoory.Identity.Tests/` and `tests/Mentoory.Authorization.Tests/` into `tests/Mentoory.Access.Tests/`.

**Rationale**: Test projects follow domain naming convention (`{Domain}.Tests`). Both test projects reference their respective Domain and Application projects, which are now unified.

**Approach**:
- Create `Mentoory.Access.Tests` project
- Move all test files from both projects, updating namespaces
- Merge project references to the three new Access projects
- Remove old test project directories and solution references

## R7: Cross-Domain Consumers of Identity Events

**Decision**: Verify which domains consume UserRegisteredEvent before removing it.

**Finding**: The UserRegisteredEventHandler in Authorization.Application is the only handler. Other domains (Notification, Tenant, etc.) do not currently consume identity events. The event class should still be retained for future use but the handler is safely removable.

**Verification method**: Grep for `UserRegisteredEvent` across all Application projects outside Identity/Authorization.
