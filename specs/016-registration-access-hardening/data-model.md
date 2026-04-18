# Data Model: Registration & Access Hardening

**Feature**: 016-registration-access-hardening
**Date**: 2026-04-18

## Persistent entities

**No changes.** The feature is entirely behavioural — response shaping, validator additions, and logging. The existing `User` aggregate, `HashedPassword` value object, `Credential`, `EmailVerificationToken`, and `PasswordResetToken` entities under `Mentoory.Access.Domain/Aggregates/User/` remain unchanged. No new columns, tables, or indexes. No DACPAC publish is required.

## Application-layer contracts (new or modified)

The feature's "data model" is the shape of the commands, validators, and service types that carry the new behaviour. These live in `Mentoory.Access.Application`.

### `RegisterUserCommand` (modified)

**Path**: `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserCommand.cs`

```csharp
public sealed record RegisterUserCommand(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password,
    string? CorrelationId,
    string? ClientIpAddress) : IBaseRequest;
```

**Changes**:

| Field | Status | Notes |
|-------|--------|-------|
| `CorrelationId` | **new** | HTTP trace identifier; nullable to preserve test-call flexibility. Used only in logging. |
| `ClientIpAddress` | **new** | Remote IP string; nullable. Used only in logging. Never reflected in any response. |
| All other fields | unchanged | |

**Validation rules** (all enforced in `RegisterUserValidator`):

| Rule | Source | Notes |
|------|--------|-------|
| Email — NotEmpty, EmailAddress | existing | Generic Spanish message |
| Country — NotEmpty | existing | |
| NationalId — NotEmpty | existing | |
| FirstName — NotEmpty, MaxLength 100 | existing | |
| LastName — NotEmpty, MaxLength 100 | existing | |
| Password — NotEmpty, MinLength 12, Uppercase, Lowercase, Digit, Special char | existing | |
| Password — **MustNotContainIdentifyingData(Email, NationalId)** | **new** | Applies FR-016-11 and FR-016-12 |

**Handler response contract**:

| Scenario | Result |
|----------|--------|
| Validator fails (any rule) | Short-circuits via MediatR `ValidationBehavior`; handler never executes. Controller logs `ValidatorFailure:<property>` and renders generic failure. |
| All validators pass, fresh combination | `Success()`; user persisted; verification email dispatched via existing token pipeline. Logs `Outcome: Success`. |
| All validators pass, email already exists | `Success()`; NO user created, NO email sent. Logs `Outcome: DuplicateEmail`. |
| All validators pass, national ID already exists | `Success()`; NO user created, NO email sent. Logs `Outcome: DuplicateNationalId`. |

### `AdminEnrollUserCommand` (new)

**Path**: `Mentoory.Access.Application/Commands/AdminEnrollUser/AdminEnrollUserCommand.cs`

```csharp
public sealed record AdminEnrollUserCommand(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password) : IBaseRequest;
```

No correlation / IP fields — admin logging is unchanged (covered by the existing `SendAndLogIfFailureAsync` pipeline), and FR-016-06 scopes the new structured outcome log to the public endpoint.

**Validation rules** (same as public, different validator file):

- Email — NotEmpty, EmailAddress
- Country — NotEmpty
- NationalId — NotEmpty
- FirstName, LastName — NotEmpty, MaxLength 100
- Password — NotEmpty, MinLength 12, Uppercase, Lowercase, Digit, Special char
- Password — `MustNotContainIdentifyingData(Email, NationalId)`

Rule equivalence with the public path is mandated by FR-016-10.

**Handler response contract**:

| Scenario | Result |
|----------|--------|
| Validator fails | Short-circuits via MediatR; ModelState receives FluentValidation errors; admin sees them inline. |
| Fresh combination | `Success()`; admin gets success toast; redirect to member list. |
| Email already exists | `Failure(ResultErrorCodes.GenericError, ("Email", "Ya existe una cuenta con este correo electrónico."))` |
| National ID already exists | `Failure(ResultErrorCodes.GenericError, ("NationalId", "Ya existe una cuenta con este número de identificación."))` |

### `IUserProvisioningService` (new)

**Path**: `Mentoory.Access.Application/Services/IUserProvisioningService.cs`

```csharp
public interface IUserProvisioningService
{
    Task<UserProvisioningOutcome> ProvisionAsync(
        UserProvisioningRequest request,
        CancellationToken cancellationToken);
}

public sealed record UserProvisioningRequest(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password);

public enum UserProvisioningOutcome
{
    Success = 0,
    DuplicateEmail = 1,
    DuplicateNationalId = 2
}
```

**Invariant ordering**: the service checks uniqueness in the order `NationalId` then `Email` (matching the existing handler's order; keeps the existing pattern for FR-006/FR-007 from platform core). On first conflict, returns immediately. On full success, creates the `User` aggregate, hashes the password, generates the verification token, persists, and returns `Success`.

**Side effects**:

- On `Success`: inserts `User`, inserts `Credential`, inserts `EmailVerificationToken`, commits unit of work. Email dispatch is triggered via the existing token pipeline (unchanged).
- On `DuplicateEmail` / `DuplicateNationalId`: no DB writes, no email dispatch.

### `PasswordIdentifyingDataRule` (new)

**Path**: `Mentoory.Access.Application/Validation/PasswordIdentifyingDataRule.cs`

```csharp
public static class PasswordIdentifyingDataRule
{
    public const int MinimumSubstringLength = 4;

    public static IRuleBuilderOptions<T, string> MustNotContainIdentifyingData<T>(
        this IRuleBuilder<T, string> builder,
        Func<T, string> emailSelector,
        Func<T, string> nationalIdSelector);

    internal static bool Contains(string password, string email, string nationalId);
}
```

**Behaviour of `Contains` (the check predicate; `MustNotContainIdentifyingData` wraps it and inverts)**:

```
Let pw = password (unchanged for case-insensitive comparison)
Let pwLower = password.ToLowerInvariant()

Check 1 (always applies):
  If email.Length >= 1 AND pwLower.Contains(email.ToLowerInvariant()) → return true
  (The spec notes emails always contain '@' and are always >= 4 characters in practice.)

Check 2:
  Let local = email.SubstringBefore('@')
  If local.Length >= MinimumSubstringLength
     AND pwLower.Contains(local.ToLowerInvariant()) → return true

Check 3:
  If nationalId.Length >= MinimumSubstringLength
     AND pw.Contains(nationalId, StringComparison.Ordinal) → return true

Check 4:
  Let stripped = nationalId stripped of all non-alphanumeric characters
  If stripped.Length >= MinimumSubstringLength
     AND pw.Contains(stripped, StringComparison.Ordinal) → return true

Return false.
```

National-ID checks are case-sensitive (exact match to stored form per spec Edge Cases). Email checks are case-insensitive because emails are normalised to uppercase in storage.

**Rejection message**: `"La contraseña no puede contener su correo electrónico ni su número de identificación."` — single message, never distinguishes which check matched (FR-016-13).

## Web-layer models (view models)

### `RegisterViewModel`

**Path**: `Mentoory.Web/Areas/Access/Models/RegisterViewModel.cs` — **unchanged**.

The view itself changes (single generic banner replaces field-attributed errors); the model does not.

### `EnrollUserViewModel`

**Path**: `Mentoory.Web/Areas/Administration/Models/EnrollUserViewModel.cs` — **unchanged** (the admin path keeps its existing field-attributed error rendering).

## Logging schema (new)

Structured log entry emitted once per public-registration POST:

| Field | Type | Source | Purpose |
|-------|------|--------|---------|
| `Email` | string | submitted | Identifying the user |
| `Outcome` | string | handler (or controller on validator failure) | One of: `Success`, `DuplicateEmail`, `DuplicateNationalId`, `ValidatorFailure:<rule>` |
| `CorrelationId` | string | `HttpContext.TraceIdentifier` | Ties log to HTTP request |
| `ClientIp` | string | `HttpContext.Connection.RemoteIpAddress` | Security analysis |

`NationalId` is explicitly **not logged** (FR-016-06).

No changes to the existing admin-path logging behaviour.

## State transitions

No new persisted state transitions — the feature only gates the `Registered` transition of the `User` aggregate behind the shared provisioning service. Existing state transitions (`Registered → EmailVerified → Active`) are unchanged.
