using FluentAssertions;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Xunit;

namespace Mentoory.Tenant.Tests.Domain;

public class IncubatorTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var incubator = Incubator.Create("Test Inc", "Description", UtcNow);

        incubator.Name.Should().Be("Test Inc");
        incubator.Description.Should().Be("Description");
        incubator.IsActive.Should().BeTrue();
        incubator.ExternalId.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => Incubator.Create(string.Empty, null, UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var incubator = Incubator.Create("Test", null, UtcNow);
        incubator.Deactivate(UtcNow);
        incubator.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldChangeProperties()
    {
        var incubator = Incubator.Create("Old Name", null, UtcNow);
        incubator.Update("New Name", "New Desc", UtcNow.AddHours(1));

        incubator.Name.Should().Be("New Name");
        incubator.Description.Should().Be("New Desc");
    }
}
