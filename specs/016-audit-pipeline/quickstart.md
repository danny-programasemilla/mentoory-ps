# Quickstart: Adding audit coverage to a new sensitive command

**Feature:** 016-audit-pipeline
**Audience:** Developers adding commands after 016 ships.
**Date:** 2026-04-18

This is the one-page recipe for keeping audit coverage intact as the codebase grows. If you are adding a command whose name matches `^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$`, CI will fail your build until you complete Steps 1 & 2.

---

## TL;DR

```csharp
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

[Audited(AuditEventTypes.YourEvent, EntityType = "YourEntity")]
public sealed record YourSensitiveCommand(/* fields */) : IBaseRequest<YourResult>;
```

That's it for the automatic path. The pipeline behavior handles the rest.

---

## Automatic mode (90% of cases)

**Step 1.** Add a constant to `Mentoory.Shared.Application/Audit/AuditEventTypes.cs`:

```csharp
public const string ProjectStageAdvanced = "Project.StageAdvanced";
```

Naming convention: `"{EntityCategory}.{PastParticipleVerb}"`, PascalCase on each side of the dot.

**Step 2.** Decorate the command:

```csharp
[Audited(AuditEventTypes.ProjectStageAdvanced, EntityType = "Project")]
public sealed record AdvanceProjectStageCommand(
    Guid ProjectExternalId,
    string NewStage) : IBaseRequest;
```

**Step 3.** (No step 3.) The handler body writes NOTHING audit-related. The pipeline behavior captures the redacted payload, user context, correlation id, outcome, and timestamp automatically when the handler returns.

**Verification.** Run `dotnet test tests/Mentoory.Tests.Architecture/`. If the architecture test passes, your coverage is good. Run the feature end-to-end and check `/Administration/AuditLog` — the action appears.

---

## Manual mode (domain-specific detail needed)

Use Manual mode when the audit row must include information the command payload does not carry — typically a "before" value read from the aggregate.

**Step 1.** Same as above — add the event-type constant.

**Step 2.** Decorate the command with `Mode = AuditMode.Manual`:

```csharp
[Audited(AuditEventTypes.AnswerCorrected, EntityType = "AnswerCorrection", Mode = AuditMode.Manual)]
public sealed record CorrectAnswerCommand(/* fields */) : IBaseRequest;
```

**Step 3.** Inject `IAuditService` into the handler and call `LogAsync` with a domain-enriched payload:

```csharp
public partial class CorrectAnswerHandler : BaseCommandHandler<CorrectAnswerCommand>
{
    private readonly IAuditService _audit;
    private readonly ITenantContext _tenant;
    private readonly ICorrelationContext _corr;
    private readonly ITimeProvider _time;
    // …

    public override async Task<Result> Handle(CorrectAnswerCommand req, CancellationToken ct)
    {
        // 1. Read aggregate and capture the "before" value BEFORE mutation.
        var diagnostic = await _repo.GetByExternalIdWithResponsesAsync(req.DiagnosticResponseExternalId, ct);
        var before = diagnostic.GetAnswerText(req.QuestionResponseId);

        // 2. Perform the business operation.
        diagnostic.CorrectAnswer(req.QuestionResponseId, req.NewTextValue, /* ... */);
        await _repo.UnitOfWork.SaveEntitiesAsync(ct);

        // 3. Write the audit entry AFTER commit.
        await _audit.LogAsync(new AuditEntry(
            EventType:       AuditEventTypes.AnswerCorrected,
            UserId:          _tenant.UserId,
            IncubatorId:     _tenant.IncubatorId,
            ProjectId:       _tenant.ProjectId,
            EntityType:      "AnswerCorrection",
            EntityId:        diagnostic.ExternalId.ToString(),
            Action:          nameof(CorrectAnswerCommand),
            Details:         JsonSerializer.Serialize(new { Before = before, After = req.NewTextValue, req.Reason }),
            IpAddress:       _corr.ClientIpAddress,
            OccurredAtUtc:   _time.UtcNow,
            CorrelationId:   _corr.CorrelationId,
            Outcome:         "Success",
            ExceptionType:   null,
            UserEmail:       _tenant.UserEmail,
            RoleContext:     _tenant.Role),
            ct);

        return Success();
    }
}
```

**Important** for Manual mode:

- The pipeline behavior does NOT write an entry; if the handler returns without calling `LogAsync`, no audit row exists.
- Failure handling is the handler's responsibility — wrap the business operation in try/catch and call `LogAsync` with `Outcome = "Failure"` before rethrowing.
- An integration test MUST assert the row is written on the happy path (pattern: `tests/Mentoory.Diagnostic.Tests/Handlers/CorrectAnswerHandlerTests.AuditLogWritten`).

---

## What the behavior captures (Automatic mode)

| Field | Source |
|-------|--------|
| EventType | `[Audited].EventType` |
| EntityType | `[Audited].EntityType` |
| Action | Command type name |
| Details | Redacted JSON payload (top-level properties in `AuditOptions.RedactedFields` replaced with `***REDACTED***`, truncated at 8,000 chars) |
| UserId, UserEmail, IncubatorId, ProjectId, RoleContext | `ITenantContext` |
| CorrelationId, IpAddress | `ICorrelationContext` |
| OccurredAtUtc | `ITimeProvider.UtcNow` (after handler completion) |
| Outcome, ExceptionType | From handler `Result` / exception |

---

## Sensitive field redaction

If your command carries a sensitive top-level property, name it from the standard list (`Password`, `PasswordHash`, `NationalId`, `VerificationToken`, `Token`, `Secret`, `ApiKey`). The behavior redacts these before serialization.

If your command uses a different name (e.g., `Pin`, `Otp`, `RecoveryCode`), add it to `appsettings.json`:

```json
{
  "Audit": {
    "RedactedFields": [ "Password", "PasswordHash", "NationalId", "VerificationToken", "Token", "Secret", "ApiKey", "Pin", "Otp", "RecoveryCode" ]
  }
}
```

Note: v1 redacts only top-level properties (see OQ-1). If you pass a sensitive value via a nested object, redact it yourself by projecting a sanitized payload (Manual mode) OR file an issue referencing OQ-1.

---

## Anti-patterns

- Do NOT call `IAuditService.LogAsync` inside an `Automatic`-mode handler — you'll get duplicate rows.
- Do NOT wrap `LogAsync` in try/catch in the handler — the service handles failures internally, log lines are already emitted.
- Do NOT use `DateTime.UtcNow` when building an `AuditEntry` manually — use `ITimeProvider.UtcNow` (Principle VI).
- Do NOT bypass `[Audited]` by renaming a sensitive command outside the regex — CI will pass, but you've broken the governance contract.

---

## Testing checklist

- [ ] Unit test for the command's handler (existing requirement; unchanged).
- [ ] Integration test that dispatches the command and asserts an `[audit].[AuditLog]` row exists with the expected `EventType` (use `IntegrationTestBase.AssertAuditLogged(AuditEventTypes.YourEvent)`).
- [ ] For Manual mode: explicit test that the `Details` payload contains the domain-specific fields (before/after).
- [ ] Architecture test passes (`dotnet test tests/Mentoory.Tests.Architecture/`).

---

## Where to go next

- Specification: [spec.md](./spec.md)
- Data model: [data-model.md](./data-model.md)
- Pipeline behavior contract: [contracts/auditing-behavior.md](./contracts/auditing-behavior.md)
- Governance obligation: [contracts/governance.md](./contracts/governance.md)
- Task list (generated by `/speckit-tasks`): `tasks.md`
