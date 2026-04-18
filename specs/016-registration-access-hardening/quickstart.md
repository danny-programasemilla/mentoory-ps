# Quickstart: Registration & Access Hardening

**Feature**: 016-registration-access-hardening
**Date**: 2026-04-18

This quickstart walks a developer through verifying the feature end-to-end against a locally running Mentoory instance. It covers the three user stories in the spec.

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` returns 10.x)
- SQL Server reachable per `aspire.config.json`
- Repository checked out; working directory `/mnt/D/repos/mentoory-ps-access-polish`
- Build clean: `dotnet build` succeeds with zero warnings
- An existing admin user with role `IncubatorAdmin` or `GlobalAdmin` is available (seeded by the `001-mentory-platform-core` PostDeployment scripts)

## Start the app

```bash
dotnet run --project Mentoory.Aspire.AppHost
```

Open the Aspire dashboard and navigate to the Mentoory.Web URL.

---

## US1 — Close the public enumeration oracle

### Step 1 — Fresh submission (Class A)

1. Open the public registration page: `/Access/Register/Index`.
2. Fill in a combination with a fresh email and fresh national ID, passing all other rules.
3. Submit.

**Expected**: Browser lands on `/Access/Register/Success` (status 302 → 200). Visible page: `"Revise su correo electrónico para confirmar su cuenta."` + secondary line directing to password recovery.
**Log**: `Public registration outcome. Email: <email>, Outcome: Success, CorrelationId: <traceid>, ClientIp: <ip>`
**DB**: new `User` row exists; `EmailVerificationToken` generated.

### Step 2 — Duplicate national ID submission (Class B, conflict)

1. Return to `/Access/Register/Index`.
2. Use a DIFFERENT email from Step 1, but the SAME national ID + country.
3. Submit.

**Expected**: Browser lands on the **same** confirmation page. The visible HTML is byte-for-byte identical to Step 1.
**Log**: `Outcome: DuplicateNationalId`.
**DB**: NO new `User` row. NO verification email queued.

### Step 3 — Duplicate email submission (Class B, conflict)

1. Return to `/Access/Register/Index`.
2. Use a DIFFERENT national ID from Step 1, but the SAME email.
3. Submit.

**Expected**: Same confirmation page as Steps 1 and 2.
**Log**: `Outcome: DuplicateEmail`.
**DB**: NO new `User` row.

### Step 4 — Validator-rule failure (Class C)

1. Return to `/Access/Register/Index`.
2. Submit with a malformed email (`notanemail`), short password (`a`), or empty field.
3. Submit.

**Expected**: Form re-renders with the single banner `"No fue posible completar el registro. Revise los datos e intente nuevamente."` Field-level error messages are **NOT** displayed.
**Log**: `Outcome: ValidatorFailure:<property>`.

### Step 5 — Response parity verification

Using your browser's network inspector or `curl`, capture the responses from Steps 1–3. Confirm:

- Same HTTP status on POST (`302 Found`)
- Same `Location` header value
- Same body text length
- Following the redirect, same `200` GET response, same rendered HTML length

Two Class-B responses must be byte-identical to the Class-A response.

---

## US2 — Admin enrollment returns specific feedback

### Step 6 — Admin fresh enrollment (Class A)

1. Sign in as `IncubatorAdmin` or `GlobalAdmin`.
2. Navigate to `/Administration/Users/Enroll`.
3. Fill in a fresh combination and submit.

**Expected**: Redirect to `/Administration/Users`; success toast `"Usuario inscrito exitosamente."` User is created.

### Step 7 — Admin duplicate national ID (Class B)

1. Return to `/Administration/Users/Enroll`.
2. Submit with a DUPLICATE national ID (one already in the DB).

**Expected**: Form re-renders with an inline field error attached to the `NationalId` field: `"Ya existe una cuenta con este número de identificación."` The `Email` field has NO error.

### Step 8 — Admin duplicate email (Class B)

1. Return to `/Administration/Users/Enroll`.
2. Submit with a DUPLICATE email.

**Expected**: Form re-renders with an inline field error attached to `Email`: `"Ya existe una cuenta con este correo electrónico."`

### Step 9 — Unauthenticated access to admin endpoint

1. Sign out.
2. Directly `POST` to `/Administration/Users/Enroll` (or open the `GET`).

**Expected**: 302 redirect to the login page. No information about the form's validation or conflict behaviour is revealed.

---

## US3 — Password-contains-identifying-data

### Step 10 — Full email in password (rejected)

1. At the public form, try:
   - Email: `jane.doe@example.com`
   - Password: `Jane.Doe@Example.com1!`
2. Submit.

**Expected**: Class C (generic failure banner). Log outcome: `ValidatorFailure:Password`.

### Step 11 — Email local part in password (rejected)

1. Email: `jane.doe@example.com` (local part `jane.doe`, 8 chars, ≥ 4 threshold)
2. Password: `My-jane.doe-passw0rd!`
3. Submit.

**Expected**: Class C (generic failure).

### Step 12 — Below-threshold local part (accepted)

1. Email: `a@b.co` (local part `a`, 1 char)
2. Password: `CorrectHorseBattery9!` (does not contain the literal `a@b.co`)
3. Submit.

**Expected**: Class A (success redirect). The below-threshold local part is skipped by the rule.

### Step 13 — National ID verbatim in password (rejected)

1. National ID: `9-123-4567`
2. Password: `Secure9-123-4567!`
3. Submit.

**Expected**: Class C (generic failure).

### Step 14 — National ID separator-stripped in password (rejected)

1. National ID: `9-123-4567` (stripped form: `91234567`, 8 chars)
2. Password: `Secure91234567!`
3. Submit.

**Expected**: Class C (generic failure).

### Step 15 — Below-threshold national ID (accepted)

1. National ID: `91` (2 chars)
2. Password: `Bicycle91TestingA!` — contains `91` substring
3. Submit.

**Expected**: Class A (success). The sub-threshold national ID is skipped.

### Step 16 — Admin path applies the same rule

1. Signed-in admin, at `/Administration/Users/Enroll`.
2. Submit with email `admin@test.com` and password `My-admin@test.com-P4ss!`.

**Expected**: Form re-renders with a FluentValidation error on the `Password` field carrying the Spanish identifying-data message. (Admin path surfaces the field-level error per Class C admin behaviour.)

---

## Automated test coverage

Run the unit and integration tests:

```bash
dotnet test tests/Mentoory.Access.Application.Tests
dotnet test tests/Mentoory.Web.Tests
```

Expected new tests:

- `PasswordIdentifyingDataRuleTests` — all thresholds, both forms, case sensitivity
- `RegisterUserHandlerTests` — the three outcome branches, logging assertions
- `AdminEnrollUserHandlerTests` — three outcome branches with field attribution
- `RegisterControllerTests` — byte-equality harness for Classes A vs B
- `UsersControllerEnrollTests` — admin-specific feedback harness

All should pass.

---

## Rollback

If the feature needs to be reverted:

- Revert the branch `016-registration-access-hardening`.
- No DB migration is required — all changes are code-only.
- No configuration changes are required — `registration` rate-limit and anti-forgery stay as they were.
