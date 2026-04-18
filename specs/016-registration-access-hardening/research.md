# Research: Registration & Access Hardening

**Feature**: 016-registration-access-hardening
**Date**: 2026-04-18

All spec-level clarifications were resolved in the `## Clarifications` section of the spec (Session 2026-04-18). This document captures the implementation-level design decisions required to realise the feature, the alternatives considered, and the rationale.

---

## Decision 1 — Command structure: split vs shared with mode flag

**Decision**: Split into two distinct Application commands.

- `RegisterUserCommand` (public path) — keeps its existing name and file location. Gains new metadata fields (`CorrelationId`, `ClientIpAddress`) used for logging. Handler masks uniqueness conflicts into a `Success` result.
- `AdminEnrollUserCommand` (admin path) — new command at `Mentoory.Access.Application/Commands/AdminEnrollUser/`. Handler returns `Failure` with a field-attributed error for uniqueness conflicts.

**Rationale**: FR-016-07 explicitly mandates "a distinct code path from the public self-registration endpoint". A shared command with a `CallerContext` mode flag couples the two contracts and creates a single failure mode (flag-value confusion) that could re-introduce the oracle — e.g., an admin flow routed through the public endpoint would surface specific errors. Splitting at the command boundary makes the two contracts independently typed, independently validated, and independently testable, with zero runtime branching between them.

**Alternatives considered**:

- **Single command with `CallerContext` enum (rejected)**: Minimises duplication but couples response shape to the flag. A misrouted command (admin flag on public endpoint, or the reverse) silently leaks. The spec's intent is binary: public = masked, admin = specific. Encoding that binary at the type level is the safer design.
- **Keep shared command, diverge at controller only (rejected)**: Would require controllers to re-interpret the handler's error list, reconstructing field-attributed errors only on admin and collapsing them on public. This moves response-shape logic into the Web layer and means the handler still distinguishes the two internally — the oracle surface migrates rather than closes. Also violates "handler returns intent-shaped Results".

---

## Decision 2 — Shared provisioning logic: internal service

**Decision**: Introduce `IUserProvisioningService` in `Mentoory.Access.Application/Services/` with a single method:

```csharp
Task<UserProvisioningOutcome> ProvisionAsync(
    UserProvisioningRequest request,
    CancellationToken cancellationToken);
```

`UserProvisioningOutcome` is an `enum` with values `Success`, `DuplicateEmail`, `DuplicateNationalId`.
`UserProvisioningRequest` is a DTO carrying `Email, Country, NationalId, FirstName, LastName, Password, UtcNow`.

Implementation lives at `Mentoory.Access.Application/Infrastructure/UserProvisioningService.cs` and holds the uniqueness-check + hash + `User.Register` + token-generation + persist logic that previously lived inline in `RegisterUserHandler`.

Both handlers call `ProvisionAsync` and map the outcome to their own response shape — this is the only difference between them.

**Rationale**: Extracts the ~30 lines of duplicated logic between the two handlers while keeping each handler focused solely on its response contract. The service is Application-layer (not Domain) because it orchestrates repository + hasher + time + config — classic Application-service responsibilities. It returns a bare enum rather than a `Result`, because only the handlers need to shape the public-facing Result; intermediate layers should not encode response-shape concerns.

**Alternatives considered**:

- **Static helper class (rejected)**: Would hold DI-required state (repository, hasher, time provider, config reader) and re-introduce service-locator anti-pattern (forbidden by Constitution § Dependency Governance).
- **Inherit shared base handler (rejected)**: `BaseCommandHandler<T>` is reserved for the CQRS plumbing; a second inheritance layer would make response-shape overrides non-obvious and test doubles more painful. Composition (injected service) beats inheritance here.
- **Duplicate the logic verbatim in both handlers (rejected)**: Violates the code-review standard "Logic duplicated more than twice — extract".

---

## Decision 3 — Password identifying-data rule placement

**Decision**: Implement as a reusable `RuleBuilderOptions<T, string>` extension method on `FluentValidation.IRuleBuilder`:

```csharp
public static IRuleBuilderOptions<T, string> MustNotContainIdentifyingData<T>(
    this IRuleBuilder<T, string> builder,
    Func<T, string> emailSelector,
    Func<T, string> nationalIdSelector);
```

File: `Mentoory.Access.Application/Validation/PasswordIdentifyingDataRule.cs`. Threshold constant `MinimumSubstringLength = 4` is a `const int` on the class.

The rule performs the following checks (all case-insensitive; returns `false` → validation fails if ANY match):

1. Full email string (always applied — FR-016-11)
2. Email local part (portion before `@`) — only if ≥ 4 chars (FR-016-11)
3. National ID verbatim — only if ≥ 4 chars (FR-016-12)
4. National ID with non-alphanumerics stripped — only if stripped form ≥ 4 chars (FR-016-12)

Message: `"La contraseña no puede contener su correo electrónico ni su número de identificación."` (FR-016-13; does not identify which check matched).

**Rationale**: FluentValidation extension methods are the idiomatic way to share cross-command rules. Both validators add a single line:
```csharp
RuleFor(x => x.Password)
    .MustNotContainIdentifyingData(x => x.Email, x => x.NationalId);
```

A future password-change/reset command reuses the same helper by applying the same one-liner — satisfying FR-016-14 ("shared password validation surface").

**Alternatives considered**:

- **Value object `Password` with factory validation in Domain (rejected)**: Moves rule into Domain, which the Constitution allows, but couples Password construction to caller-supplied identifying data (email + national ID). Passwords are created at many surfaces and the identifying data is contextual (admin-supplied vs user-supplied); keeping the check at the Application command layer — where caller context exists — is cleaner.
- **`PropertyValidator<T, string>` subclass (rejected)**: Equivalent functionality but more boilerplate than the extension-method pattern already used elsewhere in this codebase (per spec survey of existing FluentValidation usage).
- **Inline `Must(...)` predicate on each validator (rejected)**: Duplicates the substring-and-threshold logic at every call site. Violates DRY and the code-review standard.

---

## Decision 4 — Correlation ID and client IP plumbing to handler

**Decision**: Pass `CorrelationId` (string) and `ClientIpAddress` (string) as fields on `RegisterUserCommand`. The public controller populates them at construction time using `HttpContext.TraceIdentifier` and `HttpContext.Connection.RemoteIpAddress?.ToString()`.

`AdminEnrollUserCommand` does NOT carry these fields — admin enrollment logging already happens through existing `SendAndLogIfFailureAsync` pipeline and no new surface area is required for admin (FR-016-06 is explicitly about the public endpoint).

**Rationale**: Keeps the Application layer free of `HttpContext` / `IHttpContextAccessor`. Controller is the natural boundary where HTTP-world fields are known; passing them in as command data is the same pattern used elsewhere when tenant and user context are promoted into command metadata. Tests construct commands with synthetic values, which is the same DI-free testability the codebase favours.

**Alternatives considered**:

- **Inject `IHttpContextAccessor` into the handler (rejected)**: Leaks HTTP-layer concerns into Application; `IHttpContextAccessor` is on the Constitution's informal list of anti-patterns to minimise.
- **Introduce `IRequestContext` abstraction in `Mentoory.Shared.Application` (rejected for this feature, acceptable later)**: Would be the cleaner long-term solution but expands scope beyond this narrow hardening feature. The spec's out-of-scope list already explicitly bounds this work; we will use the minimal path now and revisit when a second handler needs the same metadata.

---

## Decision 5 — Response indistinguishability mechanics at the HTTP layer

**Decision**: Public controller uses a single `RedirectToAction("Success")` at the end of both the genuinely-new path AND the uniqueness-conflict path. The handler returns `Success()` in both cases; the controller cannot distinguish them and does not need to. The `Success` GET action renders the unchanged `Success.cshtml` view.

Byte-for-byte response parity follows from: same status code (302 on POST; 200 on the rendered GET), same `Location` header, same view, no per-request variable rendering.

The `Success.cshtml` view is augmented with a secondary line guiding users who believe they already have an account toward password recovery — this line is constant (not parameterised by outcome) so its presence does not reveal the outcome.

**Rationale**: The simplest design that meets the visible-output indistinguishability bar. The controller is oblivious to the outcome distinction; only the handler (and the log) knows the real outcome.

**Alternatives considered**:

- **Render the confirmation inline rather than redirect (rejected)**: Would couple the POST's response body to the outcome. Redirect-then-GET keeps the POST response body constant (just headers + short 302 body).
- **Artificial timing padding on the conflict path (scoped out)**: Would mitigate the timing oracle identified in the spec's Edge Cases. Rejected as out of scope because (a) the spec explicitly scopes indistinguishability to "visible HTTP layer" and (b) adding artificial delay increases cost against an attacker ultimately bounded by the `registration` rate-limit (3 requests / 15 min in production). Noted for future consideration if rate-limit assumptions weaken.

---

## Decision 6 — Structured outcome logging for the public endpoint

**Decision**: Extend the existing `[LoggerMessage]`-generated logging in `RegisterUserHandler` with a single structured log line per request:

```csharp
[LoggerMessage(
    Level = LogLevel.Information,
    Message = "Public registration outcome. Email: {Email}, Outcome: {Outcome}, CorrelationId: {CorrelationId}, ClientIp: {ClientIp}")]
partial void LogPublicRegistrationOutcome(
    string email,
    string outcome,
    string correlationId,
    string clientIp);
```

`outcome` is the enum name or a stringified `ValidatorFailure:<rulename>` when the MediatR validation behavior short-circuits (see Decision 7).

**National ID is NOT logged** — FR-016-06 mandates redaction. The canonical form stays in the database via the `User` aggregate only.

**Rationale**: The existing codebase uses source-generated `[LoggerMessage]` partial methods exclusively in the Access handlers. Staying in the same pattern means no new logger infrastructure and compile-time verification of message templates.

**Alternatives considered**:

- **ILogger direct calls with Scope (rejected)**: Equivalent functionality but inconsistent with the module's existing style.

---

## Decision 7 — Logging validator-rule failures

**Decision**: Hook the log from the MediatR `ValidationBehavior` short-circuit (or the controller's `ModelState` invalid branch) by invoking a lightweight log helper in the controller before returning the generic failure view. The command-level logger does not run on validator failures because the handler never executes.

Controller code (conceptual):

```csharp
if (!ModelState.IsValid)
{
    _logger.LogInformation(
        "Public registration outcome. Email: {Email}, Outcome: ValidatorFailure:{Rule}, CorrelationId: {CorrelationId}, ClientIp: {ClientIp}",
        model.Email, FirstFailingRule(ModelState), HttpContext.TraceIdentifier, HttpContext.Connection.RemoteIpAddress?.ToString());
    ...
}
```

`FirstFailingRule(ModelState)` returns the property name or the first ModelState key/value, without including user-submitted values beyond what's already acceptable (email).

**Rationale**: Validator failures are a valid outcome surface under FR-016-06 and need to be logged. The controller is the only point that has both the ModelState diagnostic and the HTTP context. A small private logger message keeps the surface minimal.

**Alternatives considered**:

- **Custom `IValidationPipelineBehavior` that logs before rejecting (rejected)**: Heavier; would need to discriminate public vs admin at the pipeline layer (which would need knowledge the pipeline does not have). Keeping the log at the controller is narrower.

---

## Decision 8 — FluentValidation validator registration

**Decision**: No registration changes required. `AdminEnrollUserValidator` will be discovered by the existing assembly-scanning registration in `Mentoory.Access.Application/DependencyInjection.cs` (confirmed by inspection of existing validator wiring during research).

**Rationale**: The codebase already uses `services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly)` (or equivalent) in the Access DI module. New validators in the same assembly are auto-registered.

---

## Decision 9 — Timing-channel mitigation (explicit non-goal)

**Decision**: Out of scope for this feature. The conflict path performs ~0 work (one DB read, then return); the fresh path performs ~5 DB writes, a password hash, and an outbound email send. Wall-clock differences are measurable by a dedicated attacker.

The defences that remain in force:

- `registration` rate limit (3 req / 15 min in production) bounds iteration cost.
- The visible response is still a masked success, so a timing-observing attacker learns less than from a response-body oracle.

**Rationale**: Explicit scope bound in the spec's Edge Cases and Assumptions. Recorded here so the decision is traceable: the team chose the rate-limit + visible-parity defence and did not add constant-time padding.

---

## Summary of file additions / modifications

| Kind | Path | Purpose |
|------|------|---------|
| MODIFY | `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserCommand.cs` | Add `CorrelationId`, `ClientIpAddress` fields |
| MODIFY | `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserHandler.cs` | Delegate to `IUserProvisioningService`; mask conflict; structured outcome log |
| MODIFY | `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserValidator.cs` | Add `MustNotContainIdentifyingData(...)` |
| NEW | `Mentoory.Access.Application/Commands/AdminEnrollUser/AdminEnrollUserCommand.cs` | Admin command |
| NEW | `Mentoory.Access.Application/Commands/AdminEnrollUser/AdminEnrollUserHandler.cs` | Admin handler — specific failure |
| NEW | `Mentoory.Access.Application/Commands/AdminEnrollUser/AdminEnrollUserValidator.cs` | Admin validator; same password rule |
| NEW | `Mentoory.Access.Application/Services/IUserProvisioningService.cs` | Interface + outcome enum + request DTO |
| NEW | `Mentoory.Access.Application/Infrastructure/UserProvisioningService.cs` | Implementation with uniqueness + create + token logic |
| NEW | `Mentoory.Access.Application/Validation/PasswordIdentifyingDataRule.cs` | Reusable FluentValidation rule |
| MODIFY | `Mentoory.Web/Areas/Access/Controllers/RegisterController.cs` | Populate correlation-id / IP on command; log validator failures; generic failure copy |
| MODIFY | `Mentoory.Web/Areas/Access/Views/Register/Index.cshtml` | Replace field-attributed errors with single generic banner |
| MODIFY | `Mentoory.Web/Areas/Access/Views/Register/Success.cshtml` | Add constant "already registered?" hint toward password recovery |
| MODIFY | `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` | `Enroll` dispatches `AdminEnrollUserCommand` |
| NEW | `tests/Mentoory.Access.Application.Tests/Commands/AdminEnrollUser/AdminEnrollUserHandlerTests.cs` | Specific-feedback cases |
| MODIFY | `tests/Mentoory.Access.Application.Tests/Commands/RegisterUser/RegisterUserHandlerTests.cs` | Conflict-masking, outcome-log cases |
| NEW | `tests/Mentoory.Access.Application.Tests/Validation/PasswordIdentifyingDataRuleTests.cs` | Threshold + normalisation cases |
| NEW | `tests/Mentoory.Web.Tests/Areas/Access/RegisterControllerTests.cs` | Visible-output parity harness (fresh vs DuplicateEmail vs DuplicateNationalId) |
| NEW | `tests/Mentoory.Web.Tests/Areas/Administration/UsersControllerEnrollTests.cs` | Specific-feedback harness |
