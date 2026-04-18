using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Identity;

[Collection(IntegrationTestCollection.Name)]
public class RegistrationTests : IntegrationTestBase
{
    public RegistrationTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Register_WithValidData_CreatesUserWithPendingVerificationStatus()
    {
        var result = await SendAsync(new RegisterUserCommand(
            "john@example.com", "CO", "100200300", "John", "Doe", "SecureP@ss123!"));

        result.IsSuccess.Should().BeTrue();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var user = await dbContext.Users
            .Include(u => u.Credentials)
            .FirstAsync(u => u.Email.NormalizedValue == "JOHN@EXAMPLE.COM");

        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.NationalIdentity.Country.Should().Be("CO");
        user.NationalIdentity.NationalId.Should().Be("100200300");
        user.AccountStatus.Should().Be(AccountStatus.PendingVerification);
        user.ExternalId.Should().NotBeEmpty();
        user.Credentials.Should().HaveCount(1);
        user.Credentials.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsSuccess_AndDoesNotPersistSecondUser()
    {
        await RegisterUserAsync(email: "duplicate@example.com", nationalId: "111111111");

        var result = await SendAsync(new RegisterUserCommand(
            "duplicate@example.com", "CO", "222222222", "Jane", "Doe", "SecureP@ss123!"));

        result.IsSuccess.Should().BeTrue("public registration masks duplicates to close the enumeration oracle");

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var users = await dbContext.Users
            .Where(u => u.Email.NormalizedValue == "DUPLICATE@EXAMPLE.COM")
            .ToListAsync();
        users.Should().HaveCount(1, "the duplicate must not be persisted even though the response says Success");
    }

    [Fact]
    public async Task Register_WithDuplicateNationalIdentity_ReturnsSuccess_AndDoesNotPersistSecondUser()
    {
        await RegisterUserAsync(email: "first@example.com", country: "CO", nationalId: "999888777");

        var result = await SendAsync(new RegisterUserCommand(
            "second@example.com", "CO", "999888777", "Jane", "Smith", "SecureP@ss123!"));

        result.IsSuccess.Should().BeTrue("public registration masks duplicates to close the enumeration oracle");

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var users = await dbContext.Users
            .Where(u => u.NationalIdentity.Country == "CO" && u.NationalIdentity.NationalId == "999888777")
            .ToListAsync();
        users.Should().HaveCount(1);
    }

    [Fact]
    public async Task Register_WithCaseVariantEmail_ReturnsSuccess_AndDoesNotPersistSecondUser()
    {
        await RegisterUserAsync(email: "Test@Example.COM", nationalId: "333444555");

        var result = await SendAsync(new RegisterUserCommand(
            "test@example.com", "CO", "666777888", "Another", "User", "SecureP@ss123!"));

        result.IsSuccess.Should().BeTrue();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var users = await dbContext.Users
            .Where(u => u.Email.NormalizedValue == "TEST@EXAMPLE.COM")
            .ToListAsync();
        users.Should().HaveCount(1, "case-insensitive email uniqueness must still block the second write");
    }
}
