using FluentAssertions;
using Xunit;

namespace Mentoory.Tests.Architecture;

/// <summary>
/// Asserts the MediatR pipeline-behavior ordering configured in
/// <c>Mentoory.Web/Program.cs</c>. Audit must sit BETWEEN validation and the transaction
/// so that (a) invalid requests never produce audit rows and (b) transactional rollback
/// does not swallow the audit write (which uses its own connection).
///
/// Source-text scan over Program.cs is deliberately preferred to a <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{T}"/>
/// boot because booting the host requires a live SQL connection string — out of scope for
/// a fast architecture test.
/// </summary>
public sealed class PipelineOrderingTests
{
    [Fact]
    public void MediatR_pipeline_order_is_Validator_then_Auditing_then_Transaction()
    {
        var programSource = LoadProgramSource();

        var validatorIx = programSource.IndexOf("AddOpenBehavior(typeof(ValidatorBehavior<,>))", StringComparison.Ordinal);
        var auditingIx = programSource.IndexOf("AddOpenBehavior(typeof(AuditingBehavior<,>))", StringComparison.Ordinal);
        var transactionIx = programSource.IndexOf("AddOpenBehavior(typeof(TransactionBehavior<,>))", StringComparison.Ordinal);

        validatorIx.Should().BeGreaterThan(-1, "ValidatorBehavior must be registered in Program.cs");
        auditingIx.Should().BeGreaterThan(-1, "AuditingBehavior must be registered in Program.cs");
        transactionIx.Should().BeGreaterThan(-1, "TransactionBehavior must be registered in Program.cs");

        validatorIx.Should().BeLessThan(auditingIx,
            "ValidatorBehavior must run before AuditingBehavior (invalid requests must not produce audit rows)");
        auditingIx.Should().BeLessThan(transactionIx,
            "AuditingBehavior must run before TransactionBehavior (so audit captures the transaction result)");
    }

    private static string LoadProgramSource()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Mentoory.Web", "Program.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate Mentoory.Web/Program.cs while walking up from " + baseDir);
    }
}
