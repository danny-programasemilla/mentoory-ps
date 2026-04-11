# E2E Test Tasks: Phase 1–3 Hardening Coverage

**Branch**: `004-phase1-3-hardening` | **Date**: 2026-04-04  
**Input**: All 8 user stories implemented in Phase 1-3 hardening  
**Prerequisites**: All implementation tasks (T001–T116) complete

## Context

The Phase 1–3 hardening implementation delivered 8 user stories covering registration, authentication, password management, batch upload, invitation lifecycle, self-enrollment, configuration management, and admin user management. However, **zero E2E tests** were created for any of these features. The 5 E2E test files specified in tasks.md (T048, T058, T094, T113, T114) do not exist. The existing 30 E2E tests only cover pre-hardening features (routing, authorization, context selection, dashboards, diagnostics). This plan fills that gap with 45 test methods across 12 test files.

## Infrastructure

**Existing (no changes needed)**:
- `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` — WebApplicationFactory + Testcontainers SQL Server 2022 + DACPAC + Chromium headless
- `PlaywrightFixture.ConnectionString` — publicly accessible, used for direct DB manipulation in session/state tests
- `Microsoft.Data.SqlClient` — already imported in PlaywrightFixture.cs
- PostDeployment seeds: Countries (CRI), SystemConfigurations (7 entries), test users (9 accounts, all Active)

**Patterns to follow** (from `tests/Mentoory.Tests.E2E/Tests/LoginRoutingTests.cs`):
- `[Collection(E2ETestCollection.Name)]` on every test class
- Constructor injection of `PlaywrightFixture`
- `try/finally` with `TakeScreenshotOnFailureAsync` + `page.Context.DisposeAsync()`
- Private `LoginAsync(IPage, email, password)` helper per class
- Locators: `input[name='Field']`, `select[name='Field']`, `button[type='submit']`
- Waits: `WaitForLoadStateAsync(LoadState.NetworkIdle)` after navigation
- Unique emails: `$"e2e-{prefix}-{Guid.NewGuid():N}@test.mentoory.com"`

**AccountStatus enum values**: PendingVerification=0, Active=1, Locked=2, Disabled=3, PasswordResetRequired=4

---

## Format: `[ID] [P?] Description`

- **[P]**: Can run in parallel (independent of other tasks)
- Tests requiring direct DB access via `_fixture.ConnectionString` are noted explicitly

---

## Phase 1: Public Registration Flow

**Purpose**: Validate the public registration form renders correctly, country dropdown works, and backend uniqueness/validation errors display per-field.

- [x] E2E-T001 [P] Create `tests/Mentoory.Tests.E2E/Tests/RegistrationTests.cs` — 8 test methods:
  1. `Register_PageLoads_WithCountryDropdownPopulated` — navigate to `/Access/Register`, assert title "Crear Cuenta", assert `select[name='Country']` has Costa Rica option (value "CRI"), assert all 7 form fields present (Email, FirstName, LastName, Country, NationalId, Password, ConfirmPassword)
  2. `Register_CountrySelection_UpdatesNationalIdMaskAndLabel` — select Costa Rica from dropdown, assert `input#nationalIdInput` placeholder becomes "0-0000-0000", assert `label#nationalIdLabel` text becomes "Cédula Nacional", assert maxlength=11
  3. `Register_SuccessfulRegistration_RedirectsToSuccessPage` — fill form with unique email, Country=CRI, NationalId=1-2345-6789, OWASP-compliant password, submit, assert URL contains `/Access/Register/Success`, assert page shows "Registro Exitoso" and "Revise su correo electrónico para verificar su cuenta"
  4. `Register_DuplicateEmail_ShowsFieldError` — register a user first, then re-register with same email + different NationalId, assert page stays on `/Access/Register`, assert "Ya existe una cuenta con este correo electrónico" is visible
  5. `Register_DuplicateNationalId_ShowsFieldError` — register a user first, then re-register with different email + same Country+NationalId, assert "Ya existe una cuenta con este número de identificación" is visible
  6. `Register_EmptyForm_ShowsValidationErrors` — submit empty form, assert client-side validation errors appear on required fields (`.text-danger` spans or `.input-validation-error` classes)
  7. `Register_PasswordMismatch_ShowsValidationError` — fill valid data but ConfirmPassword != Password, submit, assert "Las contraseñas no coinciden" visible
  8. `Register_ShortPassword_ShowsValidationError` — fill password under 12 chars with matching confirm, assert "La contraseña debe tener al menos 12 caracteres" visible

**Checkpoint**: Public registration form renders, validates, and creates users correctly.

---

## Phase 2: Login Flow

**Purpose**: Validate login page rendering, credential validation, and navigation after login.

- [x] E2E-T002 [P] Create `tests/Mentoory.Tests.E2E/Tests/LoginFlowTests.cs` — 5 test methods:
  1. `Login_PageLoads_WithCorrectElements` — navigate to `/Access/Login`, assert heading contains "Iniciar Sesión", assert `input[name='Email']`, `input[name='Password']`, `button[type='submit']` present
  2. `Login_ValidCredentials_RedirectsAway` — login with `incadmin1@test.mentoory.com` / `Test123!@#`, assert URL no longer contains `/Access/Login`
  3. `Login_InvalidCredentials_ShowsErrorMessage` — login with `admin@mentoory.com` + wrong password, assert page stays on `/Access/Login`, assert error message "Credenciales inválidas" visible in validation summary
  4. `Login_EmptyFields_ShowsValidationErrors` — submit empty login form, assert validation errors appear for Email and Password fields
  5. `Login_RegisterLink_NavigatesToRegistration` — find and click "Crear una cuenta" or registration link, assert URL contains `/Access/Register`

**Checkpoint**: Login page works correctly for valid/invalid credentials.

---

## Phase 3: Forced Password Change Flow

**Purpose**: Validate that users with PasswordResetRequired status are forced to change their password before accessing any other page.

- [x] E2E-T003 Create `tests/Mentoory.Tests.E2E/Tests/ForcedPasswordChangeTests.cs` — 6 test methods. **DB access required**: register user via `/Access/Register`, then SQL UPDATE to activate and set PasswordResetRequired.
  1. `PasswordResetRequired_Login_RedirectsToChangePassword` — register user with unique email via public registration form, SQL UPDATE `[access].[Users] SET AccountStatus=1, EmailVerifiedAtUtc=GETUTCDATE()` then `SET AccountStatus=4` for that user, login with those credentials, assert URL contains `/Access/ChangePassword`, assert page shows "Debe cambiar su contraseña para continuar"
  2. `ChangePassword_PageLoads_WithCorrectElements` — on the change password page, assert `input[name='CurrentPassword']`, `input[name='NewPassword']`, `input[name='ConfirmNewPassword']` fields present, assert "Cambiar Contraseña" submit button visible, assert "Cerrar sesión" link present
  3. `ChangePassword_ValidChange_RedirectsToContextSelect` — fill CurrentPassword with original password, NewPassword="NewSecurePass12!", ConfirmNewPassword matching, submit, assert URL contains `/Context/Select` or `/AvailableProjects` (no longer on ChangePassword)
  4. `ChangePassword_WrongCurrentPassword_ShowsError` — fill incorrect current password, valid new password, submit, assert page stays on `/Access/ChangePassword`, assert error "La contraseña actual es incorrecta" visible
  5. `ChangePassword_PasswordMismatch_ShowsValidationError` — fill correct current password but mismatched new passwords, submit, assert "Las contraseñas no coinciden" visible
  6. `PasswordResetRequired_CannotNavigateAway_RedirectsBack` — with PasswordResetRequired user logged in, attempt `page.GotoAsync("{BaseUrl}/Administration/Dashboard")`, assert URL redirected back to `/Access/ChangePassword` (PasswordResetRequiredFilter intercepted)

**Checkpoint**: Forced password change enforced for PasswordResetRequired users — cannot bypass.

---

## Phase 4: Session Invalidation

**Purpose**: Validate server-side session validation by the SessionAuthenticationMiddleware — deactivated/expired sessions and disabled users get redirected to login.

- [x] E2E-T004 Create `tests/Mentoory.Tests.E2E/Tests/SessionInvalidationTests.cs` — 2 test methods. **DB access required**.
  1. `SessionDeactivated_NextRequest_RedirectsToLogin` — login as `incadmin1@test.mentoory.com`, navigate to `/Administration/Dashboard` and verify it loads (URL check), SQL UPDATE `[access].[AuthSessions] SET IsActive=0 WHERE UserId = (SELECT Id FROM [access].[Users] WHERE NormalizedEmail = N'INCADMIN1@TEST.MENTOORY.COM') AND IsActive=1`, navigate to `/Administration/Dashboard` again, assert URL redirected to `/Access/Login`
  2. `DisabledUser_NextRequest_RedirectsToLogin` — register+activate user via public registration + DB UPDATE (AccountStatus=1), login as user, verify an authenticated page loads, SQL UPDATE `[access].[Users] SET AccountStatus=3` (Disabled) for that user, navigate to any authenticated page, assert URL redirected to `/Access/Login`

**Checkpoint**: Server-side session validation works — revoked sessions and disabled accounts are immediately rejected.

---

## Phase 5: Admin User Management

**Purpose**: Validate internal registration form with country dropdown, project assignment, and verification toggle.

- [x] E2E-T005 [P] Create `tests/Mentoory.Tests.E2E/Tests/AdminUserManagementTests.cs` — 4 test methods:
  1. `RegisterInternal_PageLoads_WithCountryDropdownAndFields` — login as `incadmin1@test.mentoory.com` (with context selection), navigate to `/Administration/Users/RegisterInternal`, assert heading "Registrar Usuario Interno", assert fields: `input[name='Email']`, `input[name='Password']`, `select[name='Country']` (with Costa Rica option), `input[name='Identification']`, `input[name='ProjectExternalId']`, `input[name='RequireEmailVerification']` (checkbox)
  2. `RegisterInternal_SuccessfulRegistration_RedirectsToUsersList` — lookup ProjectExternalId from DB via `SELECT TOP 1 CAST(ExternalId AS NVARCHAR(50)) FROM [tenant].[Projects]`, fill form with unique email + CRI + identification + password + ProjectExternalId + RequireEmailVerification unchecked, submit, assert URL contains `/Administration/Users`, assert TempData success message "Usuario registrado exitosamente" visible
  3. `RegisterInternal_EmptyForm_ShowsValidationErrors` — submit empty RegisterInternal form, assert validation errors on required fields
  4. `AdminUsersIndex_PageLoads_WithDataTable` — navigate to `/Administration/Users`, intercept AJAX POST to `/Administration/Users/Data`, assert HTTP 200 response, parse JSON and assert `recordsTotal > 0`

**Checkpoint**: Admin can register internal users with project assignment and verification control.

---

## Phase 6: Batch Upload

**Purpose**: Validate CSV upload, per-row result reporting, and temporary password generation.

- [x] E2E-T006 [P] Create `tests/Mentoory.Tests.E2E/Tests/BatchUploadTests.cs` �� 4 test methods:
  1. `BatchUpload_PageLoads_WithCorrectElements` — login as `incadmin1@test.mentoory.com`, navigate to `/Administration/BatchUpload`, assert heading "Carga Masiva de Usuarios", assert file input with `accept=".csv"`, assert `input[name='ProjectExternalId']` field, assert "Procesar Archivo" submit button
  2. `BatchUpload_ValidCsv_ShowsResultsPage` — lookup ProjectExternalId from DB, create CSV buffer: `"Country,Identification,Email,FirstName,LastName\nCRI,3-4567-8901,e2e-batch-{guid}@test.mentoory.com,Batch,UserOne"`, upload using `SetInputFilesAsync(new FilePayload { Name="test.csv", MimeType="text/csv", Buffer=csvBytes })`, fill ProjectExternalId, submit, assert page shows "Resultados de Carga Masiva", assert Total card shows "1", assert Exitosos card shows "1", assert table row with `table-success` class exists
  3. `BatchUpload_ResultsPage_ShowsTemporaryPassword` — on results page from test 2, assert the "Contraseña temporal" column cell in the success row contains a non-empty non-"—" value
  4. `BatchUpload_EmptyForm_ShowsValidationError` — submit batch upload form without selecting a file, assert validation error appears

**Checkpoint**: Batch upload processes CSV, shows per-row results with status colors, and generates temporary passwords.

---

## Phase 7: Available Projects / Self-Enrollment

**Purpose**: Validate that users without project context see public projects and can request enrollment.

- [x] E2E-T007 Create `tests/Mentoory.Tests.E2E/Tests/AvailableProjectsTests.cs` — 3 test methods. **Multi-step setup with DB access**.
  1. `AvailableProjects_UserWithNoContext_SeesProjectList` — (a) login as `incadmin1@test.mentoory.com`, navigate to `/Administration/Projects/Create`, fill Name="E2E Public Project", check IsPublic, select EnrollmentVariant=Directo, submit; (b) register new user via `/Access/Register` with unique email; (c) SQL UPDATE `[access].[Users] SET AccountStatus=1, EmailVerifiedAtUtc=GETUTCDATE()` for that user; (d) login as new user; (e) assert page navigates to available projects (URL contains `/AvailableProjects`), assert at least one project card visible, assert "E2E Public Project" text appears on page
  2. `AvailableProjects_NoPublicProjects_ShowsEmptyState` — register+activate user via registration + DB (no public projects exist in seed data), login, assert page shows "No hay proyectos disponibles"
  3. `AvailableProjects_SelfEnrollment_ShowsSuccessMessage` — with public project existing from test 1, login as no-context user, click "Solicitar inscripción" button on project card, assert success message "Solicitud de inscripción enviada exitosamente"

**Checkpoint**: Users without project context land on available projects page and can self-enroll.

---

## Phase 8: Configuration Management

**Purpose**: Validate GlobalAdmin can view and update system configuration values via the UI.

- [x] E2E-T008 [P] Create `tests/Mentoory.Tests.E2E/Tests/ConfigurationManagementTests.cs` — 3 test methods:
  1. `Configuration_PageLoads_WithAllSeededKeys` — login as `admin@mentoory.com` (GlobalAdmin + select first context), navigate to `/Platform/Configuration`, assert table visible, assert all 7 config keys present as `code` elements: EmailVerificationTokenExpiryHours, PasswordResetTokenExpiryHours, InvitationTokenExpiryHours, MaxFailedLoginAttempts, LockoutDurationMinutes, SessionTimeoutHours, PasswordHistoryDepth
  2. `Configuration_UpdateValue_ShowsSuccessMessage` — find the row for MaxFailedLoginAttempts, change its value input from "5" to "10", click that row's save button, assert success message "Configuración actualizada exitosamente" visible; then reset back to "5" and save again (cleanup)
  3. `Configuration_NonGlobalAdmin_CannotAccess` — login as `incadmin1@test.mentoory.com`, navigate to `/Platform/Configuration`, assert access denied (URL redirects to `/Access/Login` or page shows 403)

**Checkpoint**: Configuration management accessible to GlobalAdmin only, values can be updated inline.

---

## Phase 9: Project Creation with New Fields

**Purpose**: Validate that the project creation form includes IsPublic checkbox and EnrollmentVariant dropdown.

- [x] E2E-T009 [P] Create `tests/Mentoory.Tests.E2E/Tests/ProjectCreationTests.cs` — 4 test methods:
  1. `CreateProject_PageLoads_WithNewFields` — login as `incadmin1@test.mentoory.com`, navigate to `/Administration/Projects/Create`, assert `input[name='IsPublic']` checkbox present, assert `select[name='EnrollmentVariant']` dropdown present with options "Flujo completo" (value 0) and "Directo" (value 1)
  2. `CreateProject_ValidSubmission_RedirectsToProjectsList` — fill Name="E2E Project {guid}", check IsPublic, select EnrollmentVariant=Directo, submit, assert URL contains `/Administration/Projects`, assert success message "Proyecto creado exitosamente"
  3. `CreateProject_EmptyName_ShowsValidationError` — submit form with empty Name field, assert validation error visible
  4. `CreateProject_IsPublicCheckbox_DefaultsToUnchecked` — navigate to create page, assert `input[name='IsPublic']` is NOT checked by default

**Checkpoint**: Project creation includes visibility and enrollment variant controls.

---

## Phase 10: Email Verification UI

**Purpose**: Validate email verification page behavior for invalid tokens and unverified login attempts.

- [x] E2E-T010 [P] Create `tests/Mentoory.Tests.E2E/Tests/EmailVerificationTests.cs` — 2 test methods:
  1. `VerifyEmail_InvalidToken_ShowsErrorMessage` — navigate to `/Access/VerifyEmail?token=invalid-token-abc123`, assert error message visible in `alert-danger` element
  2. `VerifyEmail_UnverifiedUser_CannotLogin` — register new user via `/Access/Register` (gets PendingVerification status), attempt to login with those credentials, assert login fails with error "Verifique su correo electrónico" visible

**Checkpoint**: Email verification properly blocks unverified users from logging in.

---

## Phase 11: Logout

**Purpose**: Validate that logout terminates the session and subsequent requests require re-authentication.

- [x] E2E-T011 [P] Create `tests/Mentoory.Tests.E2E/Tests/LogoutTests.cs` — 2 test methods:
  1. `Logout_AuthenticatedUser_RedirectsToLogin` — login as `entrepreneur1@test.mentoory.com`, find and submit the logout form/button in navigation, assert URL contains `/Access/Login`
  2. `Logout_ThenAccessProtectedPage_RedirectsToLogin` — after logout, navigate to `/Administration/Dashboard`, assert URL redirected to `/Access/Login`

**Checkpoint**: Logout invalidates session; protected pages require re-authentication.

---

## Phase 12: Batch-Created User Full Journey

**Purpose**: End-to-end composite test validating the complete batch user onboarding: CSV upload → temp password → forced password change → application access.

- [x] E2E-T012 Create `tests/Mentoory.Tests.E2E/Tests/BatchUserJourneyTests.cs` — 2 test methods. **Composite E2E combining batch upload + forced password change**.
  1. `BatchCreatedUser_Login_ForcedPasswordChange_ThenAccess` — (a) admin login, upload CSV with 1 user (unique email), (b) extract temporary password from results page table cell, (c) logout admin, (d) login as batch-created user with temp password, (e) assert redirect to `/Access/ChangePassword`, (f) fill current=tempPassword, new="NewSecurePass12!", confirm matching, submit, (g) assert redirect to `/Context/Select` or `/AvailableProjects` (no longer on ChangePassword)
  2. `BatchCreatedUser_CannotSkipPasswordChange` — after batch user login (with temp password), try navigating to `/Administration/Dashboard`, assert URL redirected back to `/Access/ChangePassword` (PasswordResetRequiredFilter)

**Checkpoint**: Complete batch user journey works end-to-end — from admin upload to user's first authenticated session.

---

## Dependencies & Execution Order

### Parallel Opportunities

- **Phase 1-2, 5-6, 8-11**: All marked [P] — can run in parallel (different test files, no shared state dependencies)
- **Phase 3-4**: Use DB access via `_fixture.ConnectionString` — independent of each other, can run in parallel
- **Phase 7**: Multi-step composite — depends on project creation pattern but self-contained within the test
- **Phase 12**: Composite — depends on batch upload + forced password change patterns, self-contained

```
[P] E2E-T001 (Registration)
[P] E2E-T002 (Login)
[P] E2E-T005 (Admin User Management)
[P] E2E-T006 (Batch Upload)
[P] E2E-T008 (Configuration Management)
[P] E2E-T009 (Project Creation)
[P] E2E-T010 (Email Verification)
[P] E2E-T011 (Logout)

E2E-T003 (Forced Password Change) — uses DB access
E2E-T004 (Session Invalidation) — uses DB access
E2E-T007 (Available Projects) — multi-step composite with DB access
E2E-T012 (Batch User Journey) — end-to-end composite
```

### Recommended Implementation Order

1. E2E-T001 + E2E-T002 + E2E-T009 + E2E-T011 (simple form tests — validate infrastructure works)
2. E2E-T005 + E2E-T006 + E2E-T008 + E2E-T010 (admin and config tests)
3. E2E-T003 + E2E-T004 (DB access tests)
4. E2E-T007 + E2E-T012 (composite multi-step tests)

---

## Summary

| Task | File | Methods | Priority |
|------|------|---------|----------|
| E2E-T001 | RegistrationTests.cs | 8 | P1 |
| E2E-T002 | LoginFlowTests.cs | 5 | P1 |
| E2E-T003 | ForcedPasswordChangeTests.cs | 6 | P1 |
| E2E-T004 | SessionInvalidationTests.cs | 2 | P1 |
| E2E-T005 | AdminUserManagementTests.cs | 4 | P2 |
| E2E-T006 | BatchUploadTests.cs | 4 | P2 |
| E2E-T007 | AvailableProjectsTests.cs | 3 | P3 |
| E2E-T008 | ConfigurationManagementTests.cs | 3 | P3 |
| E2E-T009 | ProjectCreationTests.cs | 4 | P2 |
| E2E-T010 | EmailVerificationTests.cs | 2 | P2 |
| E2E-T011 | LogoutTests.cs | 2 | P2 |
| E2E-T012 | BatchUserJourneyTests.cs | 2 | P1 |
| **Total** | **12 files** | **45** | |

---

## Verification

After all tasks complete:
1. `dotnet build` — zero warnings
2. `dotnet test tests/Mentoory.Tests.E2E` — all tests pass (existing 30 + new 45 = 75 total)
3. `dotnet test` — full suite green (unit + integration + E2E)
