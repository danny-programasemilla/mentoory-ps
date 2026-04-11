using FluentAssertions;
using Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;
using Mentoory.Tenant.Domain.Enums;
using Xunit;

namespace Mentoory.Tenant.Tests.Domain;

public class ProjectInvitationTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeWithPendingStatus()
    {
        var invitation = ProjectInvitation.Create(
            projectId: 1,
            userId: 2,
            tokenHash: "hash123",
            expiresAtUtc: UtcNow.AddHours(72),
            createdByUserId: 3,
            utcNow: UtcNow);

        invitation.Status.Should().Be(InvitationStatus.Pending);
        invitation.IsActive.Should().BeTrue();
        invitation.ProjectId.Should().Be(1);
        invitation.UserId.Should().Be(2);
        invitation.TokenHash.Should().Be("hash123");
        invitation.ExpiresAtUtc.Should().Be(UtcNow.AddHours(72));
        invitation.CreatedByUserId.Should().Be(3);
        invitation.AcceptedAtUtc.Should().BeNull();
        invitation.ExternalId.Should().NotBeEmpty();
    }

    [Fact]
    public void Accept_WhenPending_ShouldTransitionToAccepted()
    {
        var invitation = ProjectInvitation.Create(1, 2, "hash", UtcNow.AddHours(72), 3, UtcNow);

        invitation.Accept(UtcNow.AddHours(1));

        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.AcceptedAtUtc.Should().Be(UtcNow.AddHours(1));
    }

    [Fact]
    public void Accept_WhenExpired_ShouldThrow()
    {
        var invitation = ProjectInvitation.Create(1, 2, "hash", UtcNow.AddHours(1), 3, UtcNow);

        var act = () => invitation.Accept(UtcNow.AddHours(2));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*pending*");
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_ShouldThrow()
    {
        var invitation = ProjectInvitation.Create(1, 2, "hash", UtcNow.AddHours(72), 3, UtcNow);
        invitation.Accept(UtcNow.AddHours(1));

        var act = () => invitation.Accept(UtcNow.AddHours(2));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CheckExpiration_WhenPastExpiry_ShouldSetExpired()
    {
        var invitation = ProjectInvitation.Create(1, 2, "hash", UtcNow.AddHours(1), 3, UtcNow);

        invitation.CheckExpiration(UtcNow.AddHours(2));

        invitation.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public void CheckExpiration_WhenNotExpired_ShouldRemainPending()
    {
        var invitation = ProjectInvitation.Create(1, 2, "hash", UtcNow.AddHours(72), 3, UtcNow);

        invitation.CheckExpiration(UtcNow.AddHours(1));

        invitation.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void CheckExpiration_WhenAlreadyAccepted_ShouldNotChange()
    {
        var invitation = ProjectInvitation.Create(1, 2, "hash", UtcNow.AddHours(72), 3, UtcNow);
        invitation.Accept(UtcNow.AddHours(1));

        invitation.CheckExpiration(UtcNow.AddHours(100));

        invitation.Status.Should().Be(InvitationStatus.Accepted);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var invitation = ProjectInvitation.Create(1, 2, "hash", UtcNow.AddHours(72), 3, UtcNow);

        invitation.Deactivate();

        invitation.IsActive.Should().BeFalse();
    }
}
