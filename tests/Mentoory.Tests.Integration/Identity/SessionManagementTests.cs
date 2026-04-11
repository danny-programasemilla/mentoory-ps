using FluentAssertions;
using Mentoory.Access.Application.Commands.LogoutUser;
using Mentoory.Access.Application.Queries.ValidateSession;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Identity;

[Collection(IntegrationTestCollection.Name)]
public class SessionManagementTests : IntegrationTestBase
{
    public SessionManagementTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ValidateSession_WithActiveSession_ReturnsSessionAndUpdatesActivity()
    {
        // Arrange
        var (loginResult, _) = await RegisterActivateAndLoginAsync(
            email: "session@example.com", nationalId: "600600600");
        loginResult.IsSuccess.Should().BeTrue();
        var sessionToken = loginResult.Value!.Session.SessionToken;
        var originalActivity = loginResult.Value.Session.LastActivityUtc;

        // Small delay to ensure time difference
        await Task.Delay(50);

        // Act
        var result = await SendAsync(new ValidateSessionQuery(sessionToken));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SessionToken.Should().Be(sessionToken);
        result.Value.IsActive.Should().BeTrue();
        result.Value.LastActivityUtc.Should().BeOnOrAfter(originalActivity);
    }

    [Fact]
    public async Task ValidateSession_WithNonExistentToken_ReturnsNull()
    {
        // Act
        var result = await SendAsync(new ValidateSessionQuery("nonexistent-token-xyz"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Logout_DeactivatesSession()
    {
        // Arrange
        var (loginResult, _) = await RegisterActivateAndLoginAsync(
            email: "logout@example.com", nationalId: "700700700");
        loginResult.IsSuccess.Should().BeTrue();
        var sessionToken = loginResult.Value!.Session.SessionToken;

        // Act
        var logoutResult = await SendAsync(new LogoutUserCommand(sessionToken));

        // Assert
        logoutResult.IsSuccess.Should().BeTrue();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var session = await dbContext.AuthSessions.FirstAsync(s => s.SessionToken == sessionToken);
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSession_AfterLogout_ReturnsNull()
    {
        // Arrange
        var (loginResult, _) = await RegisterActivateAndLoginAsync(
            email: "validateafterlogout@example.com", nationalId: "800800800");
        loginResult.IsSuccess.Should().BeTrue();
        var sessionToken = loginResult.Value!.Session.SessionToken;

        await SendAsync(new LogoutUserCommand(sessionToken));

        // Act
        var result = await SendAsync(new ValidateSessionQuery(sessionToken));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
