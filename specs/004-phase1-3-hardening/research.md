# Research: Phase 1–3 Hardening

**Date**: 2026-04-03  
**Feature**: [spec.md](spec.md)

## R-001: Server-Side Session Validation Strategy

**Decision**: Validate session token against the database on every authenticated request using the existing `SessionAuthenticationMiddleware`.

**Rationale**: The current middleware explicitly documents a "SECURITY RISK" comment — it passes through without checking the session token in the `AuthSessions` table. Cookie possession alone grants access, even after explicit session revocation. The fix is to query the `AuthSessions` table for an active, non-expired session matching the token in the cookie claim. The middleware already has the right hook point; it just needs the actual validation logic.

**Alternatives considered**:
- Redis-cached session validation: Adds infrastructure complexity not justified at current scale. Rejected.
- Short-lived JWTs with refresh tokens: Would require replacing the entire auth model. Out of scope for hardening. Rejected.
- In-memory session cache with invalidation: Race conditions across multiple instances. Rejected for correctness.

**Performance note**: One additional DB query per authenticated request. Mitigated by the existing filtered unique index on `[access].[AuthSessions]` (`WHERE IsActive = 1`). At current scale (dozens of concurrent users), this is acceptable.

---

## R-002: Forced Password Change Enforcement Mechanism

**Decision**: Implement an ASP.NET Core action filter (`PasswordResetRequiredFilter`) that checks the user's `AccountStatus` claim on every request and redirects to the password change page if the status is `PasswordResetRequired`.

**Rationale**: The `AccountStatus.PasswordResetRequired` state exists in the domain but is never enforced at the web layer. The `LoginUserHandler` does not check for it, and no middleware or filter redirects the user. A global action filter is the correct ASP.NET Core mechanism for this — it runs after authentication but before the action, can exclude specific pages (the change-password page itself, logout), and integrates naturally with the MVC pipeline.

**Alternatives considered**:
- Middleware-based approach: Would work but is harder to exclude specific routes. Action filter is more precise. Rejected for ergonomics.
- Login-time redirect only: Insufficient — user could bookmark or navigate directly to other pages. Must be enforced on every request. Rejected for security.

---

## R-003: Database-Driven Configuration Pattern

**Decision**: Create a `SystemConfiguration` aggregate in the Access domain with key-value pairs stored in `[access].[SystemConfigurations]`. Configuration values are read at operation time via a `GetConfigurationQuery` — no startup caching.

**Rationale**: The spec requires that configuration changes take effect without application restart (FR-059). A database table with strongly-typed keys (enum) and string values with type conversion in the query handler is the simplest correct approach. An `ISystemConfigurationReader` interface in the Application layer abstracts the read path for testability.

**Alternatives considered**:
- `IOptions<T>` with hot reload from `appsettings.json`: Requires file changes and restart awareness. Doesn't meet "no restart" requirement. Rejected.
- Distributed cache (Redis): Adds infrastructure. Not needed at current scale. Rejected.
- Configuration cached per-request with `IMemoryCache` + short TTL: Adds complexity without clear benefit at current load. May be added later if DB load becomes a concern. Deferred.

**Default values** (seeded via PostDeployment script):

| Key | Default Value | Unit |
|-----|---------------|------|
| EmailVerificationTokenExpiryHours | 24 | hours |
| PasswordResetTokenExpiryHours | 1 | hours |
| InvitationTokenExpiryHours | 72 | hours |
| MaxFailedLoginAttempts | 5 | count |
| LockoutDurationMinutes | 15 | minutes |
| SessionTimeoutHours | 8 | hours |
| PasswordHistoryDepth | 5 | count |

---

## R-004: Project Invitation Lifecycle Design

**Decision**: Create a `ProjectInvitation` aggregate in the Tenant domain with explicit states: Pending, Accepted, Expired. Invitations are checked lazily (at access time), not eagerly via background jobs.

**Rationale**: No background job infrastructure exists in the current system, and the spec does not require real-time expiration. Lazy expiration (check `ExpiresAtUtc` when the invitation is accessed) is simpler, correct, and sufficient. The invitation aggregate owns its state machine and validates transitions. It lives in the Tenant domain because it models project participation — a Tenant concern.

**Alternatives considered**:
- Background job for eager expiration (Hangfire/Quartz): Adds significant infrastructure. Not justified for this phase. Rejected.
- Invitation as part of the User aggregate in Access domain: Violates bounded context separation. Invitation is about project participation, not identity. Rejected.
- Invitation as part of the Project aggregate: Would bloat the Project aggregate. Invitation has its own lifecycle and identity. Rejected for DDD correctness.

---

## R-005: Batch Registration Processing Strategy

**Decision**: Process CSV rows synchronously within a single HTTP request. Each row is processed independently in a loop. Transaction scope: one transaction per row (not per file) to allow partial success.

**Rationale**: With a 500-row maximum (clarified in spec), synchronous processing within a single request is feasible. Per-row transactions ensure that a failure on row 50 doesn't roll back the 49 successfully processed rows. CsvHelper (approved in constitution) handles CSV parsing. Temporary passwords are generated per user, hashed, and the plaintext is included only in the result report response (never persisted).

**Alternatives considered**:
- Background job with progress polling: Over-engineered for 500 rows. Adds complexity. Rejected.
- Single transaction for entire file: Fragile — one bad row kills all. Violates FR-043 (row-level failures must not prevent processing of remaining rows). Rejected.
- Async message queue per row: Adds infrastructure (message broker) not present in the system. Rejected.

---

## R-006: Country-Based Identification Mask

**Decision**: Store country data (name, code, identification mask pattern, validation regex) in a `[shared].[Countries]` table. The registration page loads countries from the database and applies client-side masking via JavaScript based on the selected country's mask pattern.

**Rationale**: The spec requires country-driven identification validation (FR-004). Storing mask/validation data in the database allows adding new countries without code changes. Initial seed includes Costa Rica national ID format. Client-side masking improves UX; server-side validation (regex) ensures correctness.

**Alternatives considered**:
- Hardcoded country rules in JavaScript: Inflexible, requires deployments for changes. Rejected.
- Third-party library for international ID validation: No library covers Costa Rica-specific formats. Custom validation needed. Rejected.

---

## R-007: Registration Error Message Specificity

**Decision**: Split the existing uniqueness check into two sequential queries: (1) check country+identification, (2) check email. Return specific error messages for each violation. For public registration, display user-friendly messages. For internal registration, display field-level error messages.

**Rationale**: The current `RegisterUserHandler` combines both checks into a single query and returns a generic error message. The spec explicitly requires separate, specific errors (FR-006, FR-007). The validation order is mandated: country+identification first, then email.

**Alternatives considered**:
- Single query with result analysis: Could work but makes the error specificity harder to achieve cleanly. Rejected for clarity.
- Return all violations at once (not ordered): Violates the spec's mandated validation order. Rejected.

---

## R-008: UserProfile Read Model Synchronization

**Decision**: Update the `UserProfile` read model synchronously within the same transaction whenever the User aggregate's status changes. This applies to all state transitions: verification, lockout, unlock, deactivation, password reset required.

**Rationale**: The current implementation creates the `UserProfile` during registration but never updates it — if a user's status changes, the read model stays at "PendingVerification" forever. The fix is to add `UserProfile` update logic to every handler that modifies `User.AccountStatus`. This is acceptable at current scale; event-driven sync can be added later.

**Alternatives considered**:
- Domain events with eventual consistency: Adds complexity. Read model staleness tolerance is zero (admin screens must show current state). Rejected for this phase.
- Denormalize via database trigger: Violates Clean Architecture (business logic in DB). Rejected.

---

## R-009: Cross-Domain Enrollment Orchestration

**Decision**: When a user is created via internal or batch registration and needs project enrollment, the Access domain publishes a `UserRegisteredEvent` integration event. The Tenant domain's `UserRegisteredEventHandler` creates the `ProjectInvitation` (or auto-enrolls in bypass mode). This keeps domain boundaries clean.

**Rationale**: The current system has `EnrollParticipantCommand` and `AssignRoleCommand` as separate, unorchestrated commands. The spec requires that internal registration atomically associates the user with a project. Using an integration event (approved in constitution, Principle IV) allows the Access domain to signal registration completion without knowing about project enrollment details.

**Alternatives considered**:
- Orchestration in the controller (call both commands sequentially): Puts business logic in the web layer. Violates Principle I. Rejected.
- Saga pattern: Over-engineered for in-process modular monolith. Rejected.

---

## R-010: Public Project Self-Enrollment Landing Page

**Decision**: Modify the `HomeController` redirect logic to detect users with no active project associations and redirect them to a new "Available Projects" page that lists public projects in Registration stage. The `ListPublicProjectsQuery` in Tenant Application handles the data retrieval.

**Rationale**: Currently, users without project context are redirected based on their role, which can lead to broken or empty pages. The spec requires a meaningful landing experience (FR-047). The query filters projects by `IsPublic = true` AND `CurrentStageType = Registration` AND `CurrentStageState = InProgress`.

**Alternatives considered**:
- Embed project listing in the existing dashboard: Dashboards are role-specific and assume project context. Adding a context-free listing would complicate them. Rejected.
- Separate public-facing page (unauthenticated): The spec says "verified users" — must be authenticated. Rejected.
