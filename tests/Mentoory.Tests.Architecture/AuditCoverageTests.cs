using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Mentoory.Shared.Application.Audit;
using Mentoory.Tests.Architecture.Internal;
using Xunit;

namespace Mentoory.Tests.Architecture;

/// <summary>
/// Enforces the audit-coverage governance rule defined in
/// <c>.specify/memory/access-security-constitution.md § "Audit Trail Obligations"</c>.
/// </summary>
public sealed class AuditCoverageTests
{
    // See .specify/memory/access-security-constitution.md § "Audit Trail Obligations"
    // for the canonical definition of a "sensitive command".
    private const string SensitiveCommandPattern =
        @"^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$";

    [Fact]
    public void Sensitive_named_commands_must_carry_Audited_attribute()
    {
        var missing = CommandTypeEnumerator
            .GetAllCommandTypes()
            .Where(t => Regex.IsMatch(t.Name, SensitiveCommandPattern))
            .Where(t => t.GetCustomAttribute<AuditedAttribute>() is null)
            .Select(t => t.FullName)
            .ToArray();

        missing.Should().BeEmpty(
            "commands matching the sensitive-action pattern must carry [Audited] "
            + "per access-security-constitution.md § Audit Trail Obligations. "
            + $"Missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Audited_attribute_must_only_be_applied_to_commands()
    {
        var nonCommandTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName is not null && a.FullName.StartsWith("Mentoory.", StringComparison.Ordinal))
            .SelectMany(SafeGetTypes)
            .Where(t => t is not null)
            .Select(t => t!)
            .Where(t => t.GetCustomAttribute<AuditedAttribute>() is not null)
            .Where(t => !CommandTypeEnumerator.IsCommandType(t))
            .Select(t => t.FullName)
            .ToArray();

        nonCommandTypes.Should().BeEmpty(
            "[Audited] is a command-only attribute; queries and other types must not be decorated.");
    }

    [Fact]
    public void Retrofitted_commands_use_known_event_type_constants()
    {
        var expected = new Dictionary<string, string>
        {
            ["SetActiveContextCommand"] = AuditEventTypes.ContextActivated,
            ["AssignRoleCommand"] = AuditEventTypes.RoleAssigned,
            ["RegisterUserCommand"] = AuditEventTypes.UserRegistered,
            ["LoginUserCommand"] = AuditEventTypes.UserLoggedIn,
            ["CorrectAnswerCommand"] = AuditEventTypes.AnswerCorrected,
        };

        foreach (var (commandName, expectedEvent) in expected)
        {
            var type = CommandTypeEnumerator.GetAllCommandTypes()
                .SingleOrDefault(t => t.Name == commandName);
            type.Should().NotBeNull($"{commandName} must be discoverable through CommandTypeEnumerator");

            var attr = type!.GetCustomAttribute<AuditedAttribute>();
            attr.Should().NotBeNull($"{commandName} must carry [Audited]");
            attr!.EventType.Should().Be(expectedEvent,
                $"{commandName} must use the canonical AuditEventTypes constant");
        }
    }

    private static IEnumerable<Type?> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types;
        }
    }
}
