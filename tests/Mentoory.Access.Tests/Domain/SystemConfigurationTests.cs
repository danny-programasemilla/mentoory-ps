using FluentAssertions;
using Mentoory.Access.Domain.Aggregates.SystemConfiguration;
using Xunit;

namespace Mentoory.Access.Tests.Domain;

public class SystemConfigurationTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeWithCorrectValues()
    {
        var config = SystemConfiguration.Create(
            "MaxFailedLoginAttempts",
            "5",
            "Integer",
            "Max failed attempts before lockout",
            UtcNow);

        config.Key.Should().Be("MaxFailedLoginAttempts");
        config.Value.Should().Be("5");
        config.DataType.Should().Be("Integer");
        config.Description.Should().Be("Max failed attempts before lockout");
        config.CreatedAtUtc.Should().Be(UtcNow);
        config.UpdatedAtUtc.Should().Be(UtcNow);
        config.ExternalId.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyKey_ShouldThrow()
    {
        var act = () => SystemConfiguration.Create(string.Empty, "5", "Integer", null, UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*key*");
    }

    [Fact]
    public void Update_ShouldChangeValueAndTimestamp()
    {
        var config = SystemConfiguration.Create("SessionTimeoutHours", "8", "Integer", null, UtcNow);
        var later = UtcNow.AddHours(1);

        config.Update("12", later);

        config.Value.Should().Be("12");
        config.UpdatedAtUtc.Should().Be(later);
    }
}
