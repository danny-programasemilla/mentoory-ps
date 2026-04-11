# Quickstart Validation: Mentoory Platform

**Branch**: `001-mentory-platform-core` | **Date**: 2026-03-31 | **Phase**: 1

These scenarios validate the foundation before building higher-level features. Each scenario is self-contained and testable independently.

---

## QS-01: Solution Builds with Zero Warnings

**Validates**: Constitution Principle V, project scaffolding

**Steps**:
1. `dotnet restore`
2. `dotnet build --configuration Release`
3. Verify: Exit code 0, zero warnings, zero errors
4. `dotnet build Mentoory.Db/MentooryDb.sqlproj`
5. Verify: DACPAC produced in output directory

**Pass criteria**: Clean build across all projects including SSDT.

---

## QS-02: User Registration End-to-End

**Validates**: Identity domain, PBKDF2 hashing, email verification, registration workflow

**Steps**:
1. Navigate to `/Identity/Register`
2. Fill form: Country="Costa Rica", NationalId="123456789", Email="test@example.com", Password="SecurePass123!", FirstName="Juan", LastName="Pérez"
3. Submit form
4. Verify: User created in `[identity].Users` with AccountStatus=PendingVerification
5. Verify: Credential created in `[identity].Credentials` with hashed password (not plaintext)
6. Verify: EmailVerificationToken created with hashed token
7. Verify: Redirect to "Revise su correo electrónico para verificar su cuenta" page
8. Simulate email verification: GET `/Identity/VerifyEmail?token={raw-token}`
9. Verify: AccountStatus changed to Active, EmailVerifiedAtUtc set

**Pass criteria**: Full registration flow from form to verified account.

---

## QS-03: Login and Session Management

**Validates**: Cookie authentication, session tracking, single-session enforcement

**Steps**:
1. With verified user from QS-02, navigate to `/Identity/Login`
2. Enter email and password
3. Submit form
4. Verify: AuthSession created in `[identity].AuthSessions` with IsActive=true
5. Verify: Cookie set with HttpOnly, Secure, SameSite=Strict attributes
6. Verify: Redirect to context selection page
7. Open a second browser/incognito window and log in as the same user
8. Verify: First session's IsActive set to false (single session enforcement, FR-007)
9. Return to first browser, attempt navigation
10. Verify: Redirect to login page (session invalidated)

**Pass criteria**: Single-session enforcement works correctly.

---

## QS-04: Account Lockout on Failed Logins

**Validates**: Security controls, lockout threshold, progressive lockout

**Steps**:
1. Attempt login with valid email and wrong password — 5 times
2. Verify: After 5th failure, AccountStatus changed to Locked, LockoutEndUtc set to +15 minutes
3. Attempt login with correct password
4. Verify: Login denied with "Cuenta bloqueada temporalmente"
5. Wait for lockout to expire (or manually advance time in test)
6. Attempt login with correct password
7. Verify: Login succeeds, AccountStatus = Active, FailedLoginAttempts reset to 0

**Pass criteria**: Lockout activates after threshold and auto-expires.

---

## QS-05: Multi-Tenant Data Isolation

**Validates**: EF Core global query filters, tenant scoping (FR-001, SC-006)

**Steps**:
1. Create Incubator A and Incubator B (via Global Admin)
2. Create Project A1 under Incubator A
3. Create Project B1 under Incubator B
4. Log in as an IncubatorAdmin of Incubator A
5. Set active context to Incubator A
6. List projects via `/Administration/Projects`
7. Verify: Only Project A1 visible, Project B1 not visible
8. Attempt to access Project B1 by URL manipulation: `/Administration/Projects/{B1-externalId}`
9. Verify: 403 Forbidden or redirect (not a data leak)
10. Verify in SQL: Both projects exist in `[tenant].Projects` table but query filter restricts visibility

**Pass criteria**: Complete data isolation between incubators at all access paths.

---

## QS-06: Context Selection and Auto-Select

**Validates**: Authorization domain, context management (FR-005 through FR-009)

**Steps**:
1. Create a user with roles in two incubators (Incubator A as Mentor, Incubator B as Coordinator)
2. Log in as this user
3. Verify: Redirect to context selection page showing two options
4. Select Incubator A / Mentor role
5. Verify: Session updated with ActiveIncubatorId, ActiveProjectId, ActiveRole
6. Verify: Sidebar shows Mentor-appropriate menu items
7. Switch context to Incubator B / Coordinator via context switcher
8. Verify: Sidebar updates to Coordinator menu items
9. Create a new user with only one role in one incubator/project
10. Log in as this user
11. Verify: Auto-select (no context selection page shown), direct to dashboard

**Pass criteria**: Multi-context and auto-select both work correctly.

---

## QS-07: Dynamic Menu Rendering by Role

**Validates**: Menu service, role-based filtering, Phoenix sidebar

**Steps**:
1. Log in as GlobalAdmin → verify sidebar shows Platform menu items (Incubadoras, Suscripciones, Plantillas)
2. Log in as IncubatorAdmin → verify sidebar shows Administration menu items (Proyectos, Usuarios)
3. Log in as ProjectCoordinator → verify sidebar shows Coordination menu items (Diagnósticos, Conocimiento, Ciclo de Vida)
4. Log in as Mentor → verify sidebar shows Mentoring menu items (Planes, Sesiones, Tareas)
5. Log in as Entrepreneur → verify sidebar shows Participant menu items (Diagnóstico, Plan, Sesiones)
6. Log in as Sponsor → verify sidebar shows only read-only Dashboard
7. Verify: No role can see menu items belonging to other roles

**Pass criteria**: Each role sees exactly its permitted menu items.

---

## QS-08: DataTable Server-Side Processing

**Validates**: Reusable DataTable pattern, server-side paging/sorting/filtering

**Steps**:
1. Seed 50 incubators via PostDeployment script
2. Navigate to `/Platform/Incubators` as GlobalAdmin
3. Verify: DataTable renders first page (25 rows)
4. Verify: "Mostrando 1 a 25 de 50 registros" (Spanish locale)
5. Click page 2 → verify next 25 rows loaded via AJAX (no full page reload)
6. Type search term in search box → verify filtered results
7. Click column header → verify sorted results
8. Verify: Network tab shows POST requests to `/Platform/Incubators/Data`

**Pass criteria**: Full DataTable server-side lifecycle works with reusable pattern.

---

## QS-09: Rate Limiting on Public Endpoints

**Validates**: Rate limiting middleware, abuse prevention (FR-054)

**Steps**:
1. Send 5 POST requests to `/Identity/Login` within 15 minutes from same IP
2. Verify: All 5 processed normally
3. Send 6th request
4. Verify: HTTP 429 Too Many Requests returned
5. Send 3 POST requests to `/Identity/Register` within 15 minutes from same IP
6. Verify: All 3 processed normally
7. Send 4th request
8. Verify: HTTP 429 returned

**Pass criteria**: Rate limits enforced per endpoint policy.

---

## QS-10: DACPAC Deployment Cycle

**Validates**: SSDT pipeline, database deployment, seed data

**Steps**:
1. Build DACPAC: `dotnet build Mentoory.Db/MentooryDb.sqlproj`
2. Deploy to local SQL Server: `sqlpackage /Action:Publish /SourceFile:... /TargetConnectionString:...`
3. Verify: All schemas created (`[identity]`, `[authorization]`, `[tenant]`, `[audit]`, etc.)
4. Verify: All tables created with correct columns, indexes, constraints
5. Verify: PostDeployment scripts executed (roles seeded, default admin created)
6. Add a new column to an existing table in SSDT
7. Rebuild and re-deploy
8. Verify: Differential deployment — only the new column added, existing data preserved

**Pass criteria**: Full DACPAC build-deploy-incremental cycle works.

---

## QS-11: Aspire Health Checks

**Validates**: Aspire integration, service health monitoring

**Steps**:
1. Start application via Aspire: `dotnet run --project Mentoory.Aspire.AppHost`
2. Navigate to Aspire dashboard
3. Verify: `mentoory-web` resource shows as healthy
4. Hit `/health` endpoint
5. Verify: JSON response with status "Healthy" and individual check results (identity-db, tenant-db, etc.)
6. Hit `/alive` endpoint
7. Verify: HTTP 200

**Pass criteria**: Health checks report correct status for all registered DbContexts.

---

## QS-12: Audit Trail for Security Events

**Validates**: Cross-cutting audit service, security event logging (FR-045)

**Steps**:
1. Perform login → check `[audit].AuditLog` for "Login" event with userId, IP, timestamp
2. Select context → check for "ContextChange" event
3. Failed login attempt → check for "LoginFailed" event
4. Account lockout → check for "AccountLocked" event
5. Verify: Each event has EventType, UserId, Action, IpAddress, OccurredAtUtc
6. Verify: AuditLog is append-only (no UPDATE or DELETE operations)

**Pass criteria**: All security-relevant events are captured in the audit log.

---

## QS-13: Registration Enumeration Prevention

**Validates**: Information disclosure prevention (FR-052, SC-011)

**Steps**:
1. Register with unique email and national ID → success message
2. Attempt to register with the same email → generic error "No fue posible completar el registro"
3. Attempt to register with the same national ID → same generic error
4. Attempt to register with both duplicated → same generic error
5. Verify: Response time is similar for all cases (timing-safe)
6. Verify: HTTP response code is the same for all failure cases

**Pass criteria**: No information leakage about which field caused the conflict on public endpoints.

---

## QS-14: Inactivity Timeout

**Validates**: Configurable session inactivity timeout

**Steps**:
1. Set inactivity timeout to 2 minutes (via appsettings for test)
2. Login and navigate to a page
3. Wait 2 minutes without any activity
4. Attempt navigation
5. Verify: Redirect to login page with "Su sesión ha expirado por inactividad"
6. Verify: AuthSession.IsActive set to false

**Pass criteria**: Sessions expire after configurable inactivity period.
