using FluentAssertions;
using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Identity;

[Collection(IntegrationTestCollection.Name)]
public class LoginTests : IntegrationTestBase
{
    public LoginTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_CreatesActiveSession()
    {
        // Arrange
        await RegisterAndActivateUserAsync(email: "login@example.com", nationalId: "100100100");

        // Act
        var result = await SendAsync(new LoginUserCommand(
            "login@example.com", "SecureP@ss123!", "192.168.1.1", "Test/1.0"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Session.IsActive.Should().BeTrue();
        result.Value.Session.SessionToken.Should().NotBeNullOrEmpty();
        result.Value.Session.IpAddress.Should().Be("192.168.1.1");
        result.Value.Session.UserAgent.Should().Be("Test/1.0");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_IncrementsFailedAttempts()
    {
        // Arrange
        await RegisterAndActivateUserAsync(email: "failedlogin@example.com", nationalId: "200200200");

        // Act
        var result = await SendAsync(new LoginUserCommand(
            "failedlogin@example.com", "WrongPassword!", "127.0.0.1", null));

        // Assert
        result.IsFailure.Should().BeTrue();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var user = await dbContext.Users.FirstAsync(u => u.Email.NormalizedValue == "FAILEDLOGIN@EXAMPLE.COM");
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksAccount()
    {
        // Arrange
        await RegisterAndActivateUserAsync(email: "lockout@example.com", nationalId: "300300300");

        // Act — fail 5 times
        for (var i = 0; i < 5; i++)
        {
            await SendAsync(new LoginUserCommand("lockout@example.com", "WrongPassword!", "127.0.0.1", null));
        }

        // Assert — account is locked
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var user = await dbContext.Users.FirstAsync(u => u.Email.NormalizedValue == "LOCKOUT@EXAMPLE.COM");
        user.AccountStatus.Should().Be(AccountStatus.Locked);
        user.LockoutEndUtc.Should().NotBeNull();

        // Verify next login fails with lockout message
        var result = await SendAsync(new LoginUserCommand(
            "lockout@example.com", "SecureP@ss123!", "127.0.0.1", null));
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithPendingVerification_ReturnsError()
    {
        // Arrange — register without activating
        await RegisterUserAsync(email: "unverified@example.com", nationalId: "400400400");

        // Act
        var result = await SendAsync(new LoginUserCommand(
            "unverified@example.com", "SecureP@ss123!", "127.0.0.1", null));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    [Fact]
    public async Task Login_InvalidatesPreviousSessions_SingleSessionEnforcement()
    {
        // Arrange
        await RegisterAndActivateUserAsync(email: "singlesession@example.com", nationalId: "500500500");

        // First login
        var firstLogin = await SendAsync(new LoginUserCommand(
            "singlesession@example.com", "SecureP@ss123!", "10.0.0.1", "Browser/1.0"));
        firstLogin.IsSuccess.Should().BeTrue();
        var firstSessionToken = firstLogin.Value!.Session.SessionToken;

        // Act — second login
        var secondLogin = await SendAsync(new LoginUserCommand(
            "singlesession@example.com", "SecureP@ss123!", "10.0.0.2", "Browser/2.0"));

        // Assert
        secondLogin.IsSuccess.Should().BeTrue();
        secondLogin.Value!.Session.SessionToken.Should().NotBe(firstSessionToken);

        // First session should be deactivated
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var firstSession = await dbContext.AuthSessions.FirstAsync(s => s.SessionToken == firstSessionToken);
        firstSession.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsGenericError()
    {
        // Act
        var result = await SendAsync(new LoginUserCommand(
            "nobody@example.com", "AnyPassword!", "127.0.0.1", null));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }
}
