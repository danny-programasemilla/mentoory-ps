using FluentAssertions;
using Mentoory.Identity.Domain.Aggregates.AuthSession;
using Xunit;

namespace Mentoory.Identity.Tests.Domain;

public class AuthSessionTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var session = AuthSession.Create("token123", 1, "127.0.0.1", "Chrome", UtcNow, TimeSpan.FromHours(8));

        session.SessionToken.Should().Be("token123");
        session.UserId.Should().Be(1);
        session.IsActive.Should().BeTrue();
        session.ExpiresAtUtc.Should().Be(UtcNow.AddHours(8));
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var session = AuthSession.Create("token", 1, "127.0.0.1", null, UtcNow, TimeSpan.FromHours(8));
        session.IsValid(UtcNow.AddHours(4)).Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var session = AuthSession.Create("token", 1, "127.0.0.1", null, UtcNow, TimeSpan.FromHours(8));
        session.IsValid(UtcNow.AddHours(9)).Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var session = AuthSession.Create("token", 1, "127.0.0.1", null, UtcNow, TimeSpan.FromHours(8));
        session.Deactivate();
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetContext_ShouldSetActiveContextProperties()
    {
        var session = AuthSession.Create("token", 1, "127.0.0.1", null, UtcNow, TimeSpan.FromHours(8));
        session.SetContext(10, 20, "Mentor");

        session.ActiveIncubatorId.Should().Be(10);
        session.ActiveProjectId.Should().Be(20);
        session.ActiveRole.Should().Be("Mentor");
    }
}
