using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application;
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
        // Act
        var result = await SendAsync(new RegisterUserCommand(
            "john@example.com", "CO", "100200300", "John", "Doe", "SecureP@ss123!"));

        // Assert
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
    public async Task Register_WithDuplicateEmail_ReturnsGenericError()
    {
        // Arrange
        await RegisterUserAsync(email: "duplicate@example.com", nationalId: "111111111");

        // Act
        var result = await SendAsync(new RegisterUserCommand(
            "duplicate@example.com", "CO", "222222222", "Jane", "Doe", "SecureP@ss123!"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    [Fact]
    public async Task Register_WithDuplicateNationalIdentity_ReturnsGenericError()
    {
        // Arrange
        await RegisterUserAsync(email: "first@example.com", country: "CO", nationalId: "999888777");

        // Act
        var result = await SendAsync(new RegisterUserCommand(
            "second@example.com", "CO", "999888777", "Jane", "Smith", "SecureP@ss123!"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    [Fact]
    public async Task Register_WithCaseVariantEmail_DetectsDuplicate()
    {
        // Arrange
        await RegisterUserAsync(email: "Test@Example.COM", nationalId: "333444555");

        // Act
        var result = await SendAsync(new RegisterUserCommand(
            "test@example.com", "CO", "666777888", "Another", "User", "SecureP@ss123!"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }
}
