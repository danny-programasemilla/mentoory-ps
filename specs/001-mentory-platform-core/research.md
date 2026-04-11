# Research: Mentoory Enterprise SaaS Platform

**Branch**: `001-mentory-platform-core` | **Date**: 2026-03-31 | **Phase**: 0

## R-001: In-House Authentication Strategy

**Decision**: Build custom authentication domain using ASP.NET Core's cookie authentication middleware with custom domain entities. Remove `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and `Microsoft.AspNetCore.Identity.UI` dependencies.

**Rationale**: The user explicitly requires in-house authentication with no external auth library as the primary module. ASP.NET Core's cookie middleware (`AddAuthentication().AddCookie()`) is framework-level plumbing, not an identity library — it handles HTTP cookie mechanics only. All domain logic (User entities, credential validation, session lifecycle, lockout, password policy) is built as a first-class Identity bounded context following DDD principles.

**Alternatives considered**:
- **ASP.NET Core Identity**: Feature-rich but opaque; violates the "no external auth library" constraint. Its `IdentityUser` and `SignInManager` couple tightly to specific table structures and don't follow DDD aggregate patterns.
- **IdentityServer/Duende**: Overkill for an MVC-only app with no OAuth2 requirements yet. Can be added later when SSO is needed.
- **Keycloak/Auth0**: External providers — explicitly excluded.

## R-002: Password Hashing Algorithm

**Decision**: PBKDF2 with HMAC-SHA512, 600,000 iterations using .NET's built-in `System.Security.Cryptography.Rfc2898DeriveBytes`. 128-bit random salt per credential.

**Rationale**: OWASP 2023 recommends PBKDF2 with >= 600,000 iterations for SHA-256 (or equivalent for SHA-512). Using .NET built-in classes avoids third-party dependencies for critical security code. The stored format `pbkdf2-sha512$600000${base64-salt}${base64-hash}` includes algorithm metadata enabling future migration to Argon2id without invalidating existing hashes.

**Alternatives considered**:
- **BCrypt.Net-Next**: Well-proven but adds a NuGet dependency for a security-critical path. Comparable security to PBKDF2 at recommended work factors.
- **Argon2id (Konscious.Security.Cryptography)**: Best-in-class memory-hard algorithm, but requires native dependencies that may complicate cross-platform deployment. Can be adopted as a future upgrade with the versioned hash format.

## R-003: Session Management Architecture

**Decision**: Server-side session records in SQL Server with opaque session tokens in HTTP-only, Secure, SameSite=Strict cookies. No client-side session state.

**Rationale**: FR-006 mandates server-side context storage. FR-007 requires single-session enforcement. An `AuthSession` table enables: session invalidation on new login, configurable inactivity timeout via `LastActivityUtc` comparison, audit trail of all sessions, and admin session revocation. The cookie carries only an opaque session ID (cryptographically random, 256-bit), never user data or claims.

**Alternatives considered**:
- **JWT tokens**: Stateless but cannot be revoked server-side without a blocklist (defeating the purpose). Single-session enforcement (FR-007) requires server-side tracking anyway.
- **ASP.NET Core Session middleware**: Designed for shopping-cart-style data, not authentication sessions. Lacks audit, forced expiry, and multi-device management.

## R-004: Admin UI Template

**Decision**: Retain and fully adopt **Phoenix Admin Template** (already in the project) with Bootstrap 5 as the UI foundation.

**Rationale**: Phoenix is already the approved template per constitution (§ Web Layer Patterns). It provides: left-side collapsible navigation, Bootstrap 5 components, dark mode, DataTables integration, responsive design, and a professional admin aesthetic. Replacing it would add cost with no benefit. jQuery is included as part of Phoenix's dependency chain and is acceptable per the user's constraints.

**Alternatives considered**:
- **AdminLTE 4**: Open-source, Bootstrap 5, but would require full UI migration. No advantage over Phoenix which is already integrated.
- **Tabler**: Clean Bootstrap 5 template but less feature-rich than Phoenix for enterprise admin use cases.
- **Custom from scratch**: Maximum control but enormous effort for minimal benefit when Phoenix already provides what's needed.

## R-005: Dynamic Menu Architecture

**Decision**: Code-defined menu structure with role-based filtering at render time. Menu items defined in a `MenuConfiguration` class, filtered by the user's active context role via an `IMenuService`.

**Rationale**: The constitution forbids WebFeatures tables. A code-driven approach avoids database overhead, is version-controlled, and is simple to maintain. Menu items define which roles can see them (many-to-many via string arrays). The menu service intersects the menu definition with the user's active role. No menu duplication per role — shared menu items list multiple roles.

**Alternatives considered**:
- **Database-driven menus**: More flexible but adds a CRUD UI for menu management that isn't needed. The menu changes infrequently and should be part of code deployments.
- **Reflection-based from controller attributes**: Fragile, hard to control ordering and grouping, and ties menu structure to code structure.

## R-006: Web Layer Organization

**Decision**: MVC Areas organized by **user role/perspective**, not by bounded context.

**Rationale**: Users navigate the platform through their role. A Project Coordinator accesses diagnostic forms, knowledge structures, and project management in one unified area. Organizing Areas by role groups related features for the user while the backend remains organized by bounded context. This matches the existing project convention in CLAUDE.md.

**Areas**:
- `Identity` — Login, registration, password reset, email verification (public/unauthenticated)
- `Platform` — Global Admin features (incubators, subscriptions, global templates)
- `Administration` — Incubator Admin features (projects, users, settings)
- `Coordination` — Project Coordinator features (diagnostics, knowledge, lifecycle, mentor assignment)
- `Mentoring` — Mentor features (plans, sessions, assignments)
- `Participant` — Entrepreneur features (diagnostics, learning, submissions)
- `Sponsor` — Sponsor read-only dashboards

**Alternatives considered**:
- **Areas per bounded context**: Would scatter related screens across multiple Areas from the user's perspective. A coordinator would need to navigate between "Diagnostic", "Knowledge", and "Tenant" areas.
- **Feature folders (no Areas)**: Scales poorly with 50+ screens; Areas provide natural URL segmentation and route isolation.

## R-007: DataTables Server-Side Processing

**Decision**: Use DataTables with server-side processing for all listing pages. Implement a reusable `DataTableRequest`/`DataTableResponse` pattern in the Shared.Application layer.

**Rationale**: The spec requires 50 concurrent incubators with 100 projects each (SC-009). Client-side processing would load entire datasets. Server-side processing sends only the visible page, with sorting and filtering executed in SQL. A shared base pattern avoids repetitive code per listing.

**Pattern**:
- `DataTableRequest` record: page, pageSize, sortColumn, sortDirection, searchTerm, filters dictionary
- `DataTableResponse<T>` record: data, draw, recordsTotal, recordsFiltered
- Controller receives DataTables AJAX parameters, maps to `DataTableRequest`, sends as MediatR query
- Query handler applies filters/sorting/paging via EF Core `IQueryable` extension methods

**Alternatives considered**:
- **Client-side DataTables**: Simpler but won't scale to thousands of records.
- **Custom table component**: More control but DataTables is already approved and battle-tested.

## R-008: Multi-Tenancy Data Isolation

**Decision**: Global query filters in EF Core with mandatory `IncubatorId` on all tenant-scoped entities. A `TenantContext` service provides the current incubator ID from the authenticated session.

**Rationale**: FR-001 requires logical isolation with shared tables. EF Core global query filters automatically apply `WHERE IncubatorId = @currentIncubatorId` to every query, making it impossible to accidentally leak data. The filter is registered in `OnModelCreating` and reads from a scoped `ITenantContext` service.

**Safety layers**:
1. EF Core global query filter (automatic)
2. Repository methods validate tenant context before writes
3. Integration tests verify isolation (SC-006)
4. `IgnoreQueryFilters()` is forbidden outside of Global Admin operations (enforced by code review)

**Alternatives considered**:
- **Schema-per-tenant**: Strong isolation but prevents cross-tenant admin queries, complicates migrations, and doesn't scale to 50+ tenants.
- **Database-per-tenant**: Strongest isolation but enormous operational complexity for this scale.

## R-009: Testing Strategy Technology Choices

**Decision**:
- **Unit tests**: xUnit + Moq + FluentAssertions (per constitution)
- **Integration tests**: xUnit + `WebApplicationFactory<Program>` + Respawn for database cleanup + Testcontainers for SQL Server
- **E2E tests**: Playwright for .NET (Microsoft.Playwright)
- **Test structure**: One test project per bounded context for unit tests; shared integration and E2E test projects

**Rationale**: Constitution mandates xUnit, Moq, FluentAssertions. Testcontainers provides disposable SQL Server instances for integration tests without requiring a persistent database. Respawn efficiently resets database state between tests. Playwright is the modern standard for browser automation and has first-class .NET support.

**Alternatives considered**:
- **NUnit**: Viable but constitution specifies xUnit.
- **NSubstitute**: Good alternative to Moq but not the established standard in this project.
- **Selenium**: Older, more brittle than Playwright, slower execution.
- **LocalDb for integration tests**: Requires SQL Server installed locally; Testcontainers is more portable.

## R-010: CI/CD Pipeline Architecture

**Decision**: GitHub Actions with three workflow files:
1. `ci.yml` — Triggered on PR and push to develop: build, lint, unit tests, integration tests
2. `cd-staging.yml` — Triggered on push to develop: deploy to staging
3. `cd-production.yml` — Triggered on release tag: deploy to production

**Rationale**: Separate workflows for CI and CD keep concerns clear. PR-triggered CI catches issues before merge. Staging auto-deploys from develop for continuous validation. Production deploys only on explicit release tags for safety.

**Alternatives considered**:
- **Single monolithic workflow**: Simpler but harder to maintain and slower (runs deployment steps on every PR).
- **Azure DevOps Pipelines**: Viable but the user specified GitHub Actions.

## R-011: Database Migration/Deployment Strategy

**Decision**: SSDT/DACPAC with DacFx for deployment. Schema changes authored in the `Mentoory.Db` SQL project, deployed via `SqlPackage.exe` in CI/CD. Post-deployment scripts in `Mentoory.Db.PostDeployment/` for seed data and data migrations.

**Rationale**: Constitution mandates SSDT/DACPAC (Principle X). EF Core migrations are forbidden. DacFx compares the DACPAC against the target database and generates a differential deployment script. Post-deployment scripts handle data seeding and are idempotent.

**Rollback strategy**: Before each deployment, generate a reverse-DACPAC from the current production schema. If deployment fails or causes issues, apply the reverse-DACPAC. Additionally, keep the previous deployment artifact (DACPAC + scripts) as a tagged release.

**Alternatives considered**:
- **EF Core Migrations**: Explicitly forbidden by constitution.
- **DbUp**: Script-based migration tool; viable but SSDT provides full schema comparison and drift detection which is superior for long-term maintenance.

## R-012: Rate Limiting Implementation

**Decision**: Use ASP.NET Core's built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`) with fixed window policies per endpoint group.

**Rationale**: Built into ASP.NET Core 8+, no external dependency. Configurable per-endpoint via policy names. Policies:
- `login`: 5 requests per 15 minutes per IP
- `registration`: 3 requests per 15 minutes per IP
- `password-reset`: 3 requests per 60 minutes per email/IP combination

**Alternatives considered**:
- **AspNetCoreRateLimit NuGet**: Third-party; unnecessary since the framework provides native support.
- **Redis-backed distributed rate limiting**: Only needed when running multiple instances behind a load balancer. Can be added later; start with in-memory.

## R-013: Email Delivery Architecture

**Decision**: MailKit/MimeKit for SMTP delivery (constitution-approved). Email service in Shared.Infrastructure with queue-based async delivery via MediatR notification handlers.

**Rationale**: Email sending should never block the request pipeline. When an event triggers an email (e.g., registration verification), an integration event is published. The notification handler in the Notification domain composes the email and sends via MailKit. Failed deliveries are logged and retried (configurable retry policy).

**Initial implementation**: Direct SMTP via MailKit. Future: Replace with SendGrid/SES via the same interface.

## R-014: Aspire Integration Strategy

**Decision**: Maintain Aspire AppHost for local development orchestration and production deployment. Each bounded context's DbContext uses Aspire's SQL Server enrichment for connection resilience, health checks, and OpenTelemetry.

**Rationale**: Aspire is already integrated and provides: service discovery, health checks (/health, /alive), OpenTelemetry tracing/metrics/logging, connection string management, and Azure deployment support. Each new DbContext registers through Aspire's `EnrichSqlServerDbContext<T>()` method.

**Key consideration**: All bounded contexts share a single SQL Server database with schema-based isolation (e.g., `[identity].*`, `[tenant].*`, `[diagnostic].*`). Aspire manages the single connection string; each DbContext configures its own schema.

## R-015: Context Selection UX Strategy

**Decision**: After login, redirect to a context selection page unless the user has exactly one valid context (auto-select per FR-009). Context selection shows a card-based UI listing available incubator/project/role combinations. Selected context is stored server-side in the AuthSession.

**Rationale**: FR-005 through FR-009 require server-side context management. The context selection page is a simple query that lists the user's role assignments. For entrepreneurs with one incubator, the project is deterministic (one active project per incubator), enabling full auto-selection.

**UX flow**:
1. Login success → check role assignments count
2. If exactly one valid combination → auto-select, redirect to dashboard
3. If multiple → show context selection page
4. After selection → store in AuthSession, redirect to role-appropriate dashboard
5. Context switch → available via top-bar dropdown, re-selects and reloads
