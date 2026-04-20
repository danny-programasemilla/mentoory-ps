using FluentAssertions;
using Mentoory.Access.Application.Commands.AdminEnrollUser;
using Mentoory.Access.Application.Validation;
using Mentoory.Tests.Integration.Fixtures;
using Xunit;

namespace Mentoory.Tests.Integration.Identity;

[Collection(IntegrationTestCollection.Name)]
public class PasswordIdentifyingDataAdminIntegrationTests : IntegrationTestBase
{
    public PasswordIdentifyingDataAdminIntegrationTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    [Trait("Spec", "FR-018-18")]
    [Trait("Floor", "content-policy-rules")]
    public async Task AdminEnrollment_PasswordContainsNationalId_Verbatim_ReturnsPasswordAttributedError()
    {
        var result = await SendAsync(new AdminEnrollUserCommand(
            "admin-pwd-nid-1@example.com",
            "CO",
            "9-123-4567",
            "Test",
            "User",
            "Secure9-123-4567!"));

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(e =>
            e.Context == "Password" && e.Message == PasswordIdentifyingDataRule.Message);
    }

    [Fact]
    [Trait("Spec", "FR-018-18")]
    [Trait("Floor", "content-policy-rules")]
    public async Task AdminEnrollment_PasswordContainsNationalId_Stripped_ReturnsPasswordAttributedError()
    {
        var result = await SendAsync(new AdminEnrollUserCommand(
            "admin-pwd-nid-2@example.com",
            "CO",
            "9-123-4567",
            "Test",
            "User",
            "Secure91234567!"));

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(e =>
            e.Context == "Password" && e.Message == PasswordIdentifyingDataRule.Message);
    }
}
