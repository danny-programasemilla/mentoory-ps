using FluentAssertions;
using Mentoory.Access.Application.Commands.AdminEnrollUser;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
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
    [Trait("Spec", "FR-016-07")]
    [Trait("Spec", "FR-016-10")]
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
    [Trait("Spec", "FR-016-07")]
    [Trait("Spec", "FR-016-08")]
    [Trait("Sc", "SC-016-03")]
    [Trait("Floor", "public-vs-admin-attribution")]
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
    [Trait("Spec", "FR-016-07")]
    [Trait("Spec", "FR-016-08")]
    [Trait("Spec", "FR-018-15")]
    [Trait("Sc", "SC-016-03")]
    [Trait("Floor", "public-vs-admin-attribution")]
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

    [Fact]
    [Trait("Spec", "FR-018-16")]
    [Trait("Floor", "public-vs-admin-attribution")]
    public async Task AdminEnrollment_ValidData_RedirectsToUsersList()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var enrollPage = await client.GetAsync("/Administration/Users/Enroll");
        enrollPage.EnsureSuccessStatusCode();
        var enrollPageHtml = await enrollPage.Content.ReadAsStringAsync();
        var antiforgeryToken = AntiforgeryHelper.ExtractToken(enrollPageHtml);

        var freshEmail = $"e2e-fresh-{Guid.NewGuid():N}@example.com";
        const string password = "SecureP@ss12345!";
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
            new KeyValuePair<string, string>("Email", freshEmail),
            new KeyValuePair<string, string>("Country", "CO"),
            new KeyValuePair<string, string>("NationalId", "999111222"),
            new KeyValuePair<string, string>("FirstName", "Fresh"),
            new KeyValuePair<string, string>("LastName", "Enrollee"),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("ConfirmPassword", password),
        });

        var response = await client.PostAsync("/Administration/Users/Enroll", formContent);

        ((int)response.StatusCode).Should().Be(302,
            "the controller redirects to the users list on successful enrollment");
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Administration/Users");

        // TempData success indicator: the framework persists TempData via the
        // .AspNetCore.Mvc.CookieTempDataProvider cookie (HMAC-protected). Asserting
        // on its presence is a sufficient proxy for "TempData success key was set".
        var setCookies = response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.ToList()
            : new List<string>();
        setCookies.Should().Contain(c => c.Contains(".AspNetCore.Mvc.CookieTempDataProvider"),
            "the controller writes TempData[\"SuccessMessage\"] which is persisted via the CookieTempDataProvider");
    }

    [Fact]
    [Trait("Spec", "FR-016-09")]
    [Trait("Spec", "FR-018-17")]
    [Trait("Floor", "defense-in-depth-controls")]
    public async Task AdminEnrollment_Unauthenticated_RedirectsToLoginOrReturns401()
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "anon@example.com"),
            new KeyValuePair<string, string>("Country", "CO"),
            new KeyValuePair<string, string>("NationalId", "000111222"),
            new KeyValuePair<string, string>("FirstName", "Anon"),
            new KeyValuePair<string, string>("LastName", "User"),
            new KeyValuePair<string, string>("Password", "SecureP@ss12345!"),
            new KeyValuePair<string, string>("ConfirmPassword", "SecureP@ss12345!"),
        });

        var response = await client.PostAsync("/Administration/Users/Enroll", formContent);

        var statusCode = (int)response.StatusCode;
        var locationHeader = response.Headers.Location?.ToString() ?? string.Empty;

        statusCode.Should().Match(
            s => s == 401 || (s == 302 && locationHeader.Contains("/Access/Login")),
            "the [Authorize] policy must either return 401 Unauthorized or redirect anonymous callers to the login page");
    }
}
