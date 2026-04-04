using FluentAssertions;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Xunit;

namespace Mentoory.Access.Tests.Domain;

public class UserTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Register_ShouldCreateUserWithPendingVerification()
    {
        var user = User.Register("test@test.com", "CR", "123456", "Juan", "Pérez", "hash123", UtcNow);

        user.AccountStatus.Should().Be(AccountStatus.PendingVerification);
        user.Email.Value.Should().Be("test@test.com");
        user.Email.NormalizedValue.Should().Be("TEST@TEST.COM");
        user.NationalIdentity.Country.Should().Be("CR");
        user.NationalIdentity.NationalId.Should().Be("123456");
        user.FirstName.Should().Be("Juan");
        user.LastName.Should().Be("Pérez");
        user.Credentials.Should().HaveCount(1);
        user.FailedLoginAttempts.Should().Be(0);
        user.ExternalId.Should().NotBeEmpty();
    }

    [Fact]
    public void VerifyEmail_WhenPending_ShouldActivateAccount()
    {
        var user = User.Register("test@test.com", "CR", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow.AddHours(1));

        user.AccountStatus.Should().Be(AccountStatus.Active);
        user.EmailVerifiedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void VerifyEmail_WhenNotPending_ShouldThrow()
    {
        var user = User.Register("test@test.com", "CR", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow.AddHours(1));

        var act = () => user.VerifyEmail(UtcNow.AddHours(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RecordFailedLogin_ShouldLockAfterMaxAttempts()
    {
        var user = User.Register("test@test.com", "CR", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);

        for (int i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(UtcNow, 5, TimeSpan.FromMinutes(15));
        }

        user.AccountStatus.Should().Be(AccountStatus.Locked);
        user.LockoutEndUtc.Should().NotBeNull();
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldResetFailedAttempts()
    {
        var user = User.Register("test@test.com", "CR", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);
        user.RecordFailedLogin(UtcNow, 5, TimeSpan.FromMinutes(15));
        user.RecordFailedLogin(UtcNow, 5, TimeSpan.FromMinutes(15));

        user.RecordSuccessfulLogin(UtcNow);

        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void IsLockedOut_ShouldAutoUnlockAfterExpiry()
    {
        var user = User.Register("test@test.com", "CR", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);
        user.Lock(UtcNow, TimeSpan.FromMinutes(15));

        var isLocked = user.IsLockedOut(UtcNow.AddMinutes(16));

        isLocked.Should().BeFalse();
        user.AccountStatus.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void ChangePassword_ShouldDeactivateOldCredential()
    {
        var user = User.Register("test@test.com", "CR", "123", "A", "B", "hash1", UtcNow);

        user.ChangePassword("hash2", UtcNow.AddDays(1));

        user.Credentials.Should().HaveCount(2);
        user.Credentials.Count(c => c.IsActive).Should().Be(1);
        user.GetActiveCredential()!.PasswordHash.Should().Be("hash2");
    }

    [Fact]
    public void Lock_WhenAlreadyLocked_ShouldThrow()
    {
        var user = User.Register("test@test.com", "CR", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);
        user.Lock(UtcNow, TimeSpan.FromMinutes(15));

        var act = () => user.Lock(UtcNow, TimeSpan.FromMinutes(15));
        act.Should().Throw<InvalidOperationException>();
    }
}
