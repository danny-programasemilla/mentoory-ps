using FluentAssertions;
using Mentoory.Access.Application.Commands.AdminEnrollUser;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Identity;

[Collection(IntegrationTestCollection.Name)]
public class AdminEnrollmentTests : IntegrationTestBase
{
    public AdminEnrollmentTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AdminEnroll_WithValidData_ReturnsSuccess_AndPersistsUser()
    {
        var result = await SendAsync(new AdminEnrollUserCommand(
            "enrolled@example.com", "CO", "500600700", "Enrolled", "Admin", "SecureP@ss12345!"));

        result.IsSuccess.Should().BeTrue();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var user = await dbContext.Users.FirstAsync(u => u.Email.NormalizedValue == "ENROLLED@EXAMPLE.COM");
        user.NationalIdentity.NationalId.Should().Be("500600700");
    }

    [Fact]
    public async Task AdminEnroll_WithDuplicateEmail_ReturnsFailure_AttributedToEmailField()
    {
        await RegisterUserAsync(email: "dup-admin@example.com", nationalId: "700800900");

        var result = await SendAsync(new AdminEnrollUserCommand(
            "dup-admin@example.com", "CO", "111222333", "Other", "Admin", "SecureP@ss12345!"));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().ContainSingle(e => e.Context == "Email"
            && e.Message == "Ya existe una cuenta con este correo electrónico.");
    }

    [Fact]
    public async Task AdminEnroll_WithDuplicateNationalIdentity_ReturnsFailure_AttributedToNationalIdField()
    {
        await RegisterUserAsync(email: "first-admin@example.com", country: "CO", nationalId: "444555666");

        var result = await SendAsync(new AdminEnrollUserCommand(
            "second-admin@example.com", "CO", "444555666", "Other", "Admin", "SecureP@ss12345!"));

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().ContainSingle(e => e.Context == "NationalId"
            && e.Message == "Ya existe una cuenta con este número de identificación.");
    }
}
