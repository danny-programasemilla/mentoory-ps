using FluentAssertions;
using Mentoory.Access.Domain.Aggregates.RoleAssignment;
using Xunit;

namespace Mentoory.Access.Tests.Domain;

public class RoleAssignmentTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidRole_ShouldSucceed()
    {
        var assignment = RoleAssignment.Create(1, 10, 20, "Mentor", UtcNow);

        assignment.UserId.Should().Be(1);
        assignment.IncubatorId.Should().Be(10);
        assignment.ProjectId.Should().Be(20);
        assignment.Role.Should().Be("Mentor");
        assignment.IsActive.Should().BeTrue();
        assignment.ExternalId.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithInvalidRole_ShouldThrow()
    {
        var act = () => RoleAssignment.Create(1, 10, null, "InvalidRole", UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Revoke_WhenActive_ShouldDeactivate()
    {
        var assignment = RoleAssignment.Create(1, 10, null, "IncubatorAdmin", UtcNow);
        assignment.Revoke(UtcNow.AddHours(1));

        assignment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldThrow()
    {
        var assignment = RoleAssignment.Create(1, 10, null, "IncubatorAdmin", UtcNow);
        assignment.Revoke(UtcNow);

        var act = () => assignment.Revoke(UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithNullProjectId_ShouldBeValid()
    {
        var assignment = RoleAssignment.Create(1, 10, null, "GlobalAdmin", UtcNow);
        assignment.ProjectId.Should().BeNull();
    }
}
