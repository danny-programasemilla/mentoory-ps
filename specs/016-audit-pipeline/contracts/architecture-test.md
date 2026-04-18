# Contract: Architecture Test — `AuditCoverageTests`

**Layer:** Tests (`Mentoory.Tests.Architecture`)
**Visibility:** Internal / Test
**Files:**
- `tests/Mentoory.Tests.Architecture/Mentoory.Tests.Architecture.csproj` (new project)
- `tests/Mentoory.Tests.Architecture/AuditCoverageTests.cs`
- `tests/Mentoory.Tests.Architecture/Internal/CommandTypeEnumerator.cs`

## Purpose

Enforce FR-015 / FR-016 / SC-004: every command type whose name matches the sensitive-action regex MUST carry `[Audited]`.

## Sensitive-action regex

Hardcoded constant (with a comment citing `access-security-constitution.md`):

```csharp
// See .specify/memory/access-security-constitution.md § "Audit Trail Obligations"
// for the canonical definition of a "sensitive command".
internal const string SensitiveCommandPattern =
    @"^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$";
```

## Test shape

```csharp
namespace Mentoory.Tests.Architecture;

public sealed class AuditCoverageTests
{
    [Fact]
    public void Sensitive_named_commands_must_carry_Audited_attribute()
    {
        var sensitive = CommandTypeEnumerator
            .GetAllCommandTypes()
            .Where(t => Regex.IsMatch(t.Name, SensitiveCommandPattern))
            .ToArray();

        var missing = sensitive
            .Where(t => t.GetCustomAttribute<AuditedAttribute>() is null)
            .Select(t => t.FullName)
            .ToArray();

        missing.Should().BeEmpty(
            because: "commands matching the sensitive-action pattern must carry [Audited] " +
                     "per access-security-constitution.md § Audit Trail Obligations. " +
                     $"Missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Audited_attribute_must_only_be_applied_to_commands()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName!.StartsWith("Mentoory."))
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<AuditedAttribute>() is not null)
            .ToArray();

        var nonCommands = types
            .Where(t => !typeof(IBaseRequest).IsAssignableFrom(t)
                     && !t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseRequest<>)))
            .ToArray();

        nonCommands.Should().BeEmpty(
            because: "[Audited] is a command-only attribute; queries and other types must not be decorated.");
    }

    [Fact]
    public void Retrofitted_commands_use_known_event_type_constants()
    {
        var expected = new Dictionary<string, string>
        {
            ["SetActiveContextCommand"] = AuditEventTypes.ContextActivated,
            ["AssignRoleCommand"]       = AuditEventTypes.RoleAssigned,
            ["RegisterUserCommand"]     = AuditEventTypes.UserRegistered,
            ["LoginUserCommand"]        = AuditEventTypes.UserLoggedIn,
            ["CorrectAnswerCommand"]    = AuditEventTypes.AnswerCorrected,
        };

        foreach (var (commandName, expectedEvent) in expected)
        {
            var type = CommandTypeEnumerator.GetAllCommandTypes()
                .Single(t => t.Name == commandName);
            var attr = type.GetCustomAttribute<AuditedAttribute>();
            attr.Should().NotBeNull($"{commandName} must carry [Audited]");
            attr!.EventType.Should().Be(expectedEvent, $"{commandName} must use the canonical event type constant");
        }
    }
}
```

## Type enumeration helper

```csharp
internal static class CommandTypeEnumerator
{
    public static IEnumerable<Type> GetAllCommandTypes()
    {
        // Force-load every Application assembly so GetAssemblies() finds them:
        var _ = new object[]
        {
            typeof(Mentoory.Access.Application.Commands.AssignRole.AssignRoleCommand),
            typeof(Mentoory.Diagnostic.Application.Commands.CorrectAnswer.CorrectAnswerCommand),
            typeof(Mentoory.Tenant.Application /* any type */),
            // … one touch-reference per Application assembly
        };

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName!.StartsWith("Mentoory.") && a.FullName.Contains(".Application"))
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
            })
            .Where(t => t!.IsClass && !t.IsAbstract)
            .Where(t => typeof(IBaseRequest).IsAssignableFrom(t)
                     || t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseRequest<>)))
            .Cast<Type>();
    }
}
```

## Behavior contract

- `dotnet test tests/Mentoory.Tests.Architecture/` fails with a descriptive FluentAssertions message when any sensitive-named command lacks `[Audited]`.
- Test runs in under 2 seconds (reflection only, no DB/HTTP).
- Failing message includes the command's fully-qualified name AND the constitution pointer.

## CI integration

The existing `dotnet test` invocation in CI picks up the new project via the solution file (add project to `Mentoory.sln`). No additional CI configuration required.
