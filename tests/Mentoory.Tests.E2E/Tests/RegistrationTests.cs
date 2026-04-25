using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T001 - Validates the public registration form: rendering, country dropdown,
/// validation errors, and backend uniqueness constraints.
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class RegistrationTests
{
    private readonly PlaywrightFixture _fixture;

    public RegistrationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_PageLoads_WithCountryDropdownPopulated()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert page title
            var heading = await page.Locator("h2").TextContentAsync();
            heading.Should().Contain("Crear Cuenta");

            // Assert Country dropdown has CRI option (options are hidden in closed <select>; check count only)
            var criOption = page.Locator("select[name='Country'] option[value='CRI']");
            (await criOption.CountAsync()).Should().Be(1, "Costa Rica option should exist");

            // Assert all 7 form fields present
            (await page.Locator("input[name='Email']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='FirstName']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='LastName']").CountAsync()).Should().Be(1);
            (await page.Locator("select[name='Country']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='NationalId']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='Password']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='ConfirmPassword']").CountAsync()).Should().Be(1);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_PageLoads_WithCountryDropdownPopulated));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Register_CountrySelection_UpdatesNationalIdMaskAndLabel()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Select Costa Rica
            await page.SelectOptionAsync("select[name='Country']", "CRI");

            // Assert placeholder becomes the mask
            var placeholder = await page.Locator("input#nationalIdInput").GetAttributeAsync("placeholder");
            placeholder.Should().Be("0-0000-0000");

            // Assert label text becomes "Cédula Nacional"
            var labelText = await page.Locator("label#nationalIdLabel").TextContentAsync();
            labelText.Should().Be("Cédula Nacional");

            // Assert maxlength=11
            var maxLength = await page.Locator("input#nationalIdInput").GetAttributeAsync("maxlength");
            maxLength.Should().Be("11");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_CountrySelection_UpdatesNationalIdMaskAndLabel));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-03")]
    public async Task Register_SuccessfulRegistration_RedirectsToSuccessPage()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var email = $"e2e-reg-{Guid.NewGuid():N}@test.mentoory.com";

            await RegisterUserAsync(page, email, "1-2345-6789", "SecurePass123!");

            page.Url.Should().Contain("/Access/Register/Success");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Registro Exitoso");
            pageContent.Should().Contain("Revise su correo electrónico para confirmar su cuenta");
            pageContent.Should().Contain("¿Ya tiene una cuenta? Use la opción de recuperar contraseña",
                "success copy must hint at password recovery unconditionally so duplicates cannot be distinguished from fresh registrations");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_SuccessfulRegistration_RedirectsToSuccessPage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-03")]
    [Trait("Spec", "FR-016-04")]
    [Trait("Sc", "SC-016-01")]
    [Trait("Floor", "response-indistinguishability")]
    public async Task Register_DuplicateEmail_RedirectsToSuccess_AndDoesNotRevealDuplicate()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var email = $"e2e-dupemail-{uniqueId}@test.mentoory.com";
            var nationalId1 = $"1-{uniqueId[..4]}-{uniqueId[4..8]}";

            await RegisterUserAsync(page, email, nationalId1, "SecurePass123!");
            page.Url.Should().Contain("/Access/Register/Success");
            var freshPanel = await page.Locator(".card-body.text-center").TextContentAsync();

            var uniqueId2 = Guid.NewGuid().ToString("N")[..8];
            var nationalId2 = $"2-{uniqueId2[..4]}-{uniqueId2[4..8]}";
            await RegisterUserAsync(page, email, nationalId2, "SecurePass123!");

            page.Url.Should().Contain("/Access/Register/Success",
                "duplicate-email submissions must land on the same success URL as fresh registrations to close the enumeration oracle");

            var duplicateContent = await page.ContentAsync();
            duplicateContent.Should().NotContain("Ya existe una cuenta con este correo electrónico",
                "duplicate-email message must never leak to the public path");

            var duplicatePanel = await page.Locator(".card-body.text-center").TextContentAsync();
            duplicatePanel.Should().Be(freshPanel,
                "the Success panel text must be identical for fresh and duplicate-email outcomes");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_DuplicateEmail_RedirectsToSuccess_AndDoesNotRevealDuplicate));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-03")]
    [Trait("Spec", "FR-016-04")]
    [Trait("Sc", "SC-016-01")]
    [Trait("Floor", "response-indistinguishability")]
    public async Task Register_DuplicateNationalId_RedirectsToSuccess_AndDoesNotRevealDuplicate()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var email1 = $"e2e-dupid1-{uniqueId}@test.mentoory.com";
            var nationalId = $"3-{uniqueId[..4]}-{uniqueId[4..8]}";

            await RegisterUserAsync(page, email1, nationalId, "SecurePass123!");
            page.Url.Should().Contain("/Access/Register/Success");
            var freshPanel = await page.Locator(".card-body.text-center").TextContentAsync();

            var email2 = $"e2e-dupid2-{uniqueId}@test.mentoory.com";
            await RegisterUserAsync(page, email2, nationalId, "SecurePass123!");

            page.Url.Should().Contain("/Access/Register/Success");

            var duplicateContent = await page.ContentAsync();
            duplicateContent.Should().NotContain("Ya existe una cuenta con este número de identificación");

            var duplicatePanel = await page.Locator(".card-body.text-center").TextContentAsync();
            duplicatePanel.Should().Be(freshPanel);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_DuplicateNationalId_RedirectsToSuccess_AndDoesNotRevealDuplicate));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-01")]
    public async Task Register_EmptyForm_ShowsValidationErrors()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Submit empty form
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert validation errors appear
            var validationErrors = page.Locator(".text-danger, .input-validation-error, .field-validation-error");
            (await validationErrors.CountAsync()).Should().BeGreaterThan(0,
                "validation errors should appear for required fields");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_EmptyForm_ShowsValidationErrors));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-01")]
    public async Task Register_PasswordMismatch_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var email = $"e2e-pwmis-{Guid.NewGuid():N}@test.mentoory.com";

            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='FirstName']", "Test");
            await page.FillAsync("input[name='LastName']", "User");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='NationalId']", "4-5678-9012");
            await page.FillAsync("input[name='Password']", "SecurePass123!");
            await page.FillAsync("input[name='ConfirmPassword']", "DifferentPass456!");

            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().NotContain("/Success",
                "jQuery unobtrusive validation must block submission when passwords do not match");
            (await page.Locator("input[name='ConfirmPassword'].input-validation-error").CountAsync())
                .Should().Be(1, "the mismatched ConfirmPassword input must be flagged client-side");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_PasswordMismatch_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-01")]
    public async Task Register_ShortPassword_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var email = $"e2e-shortpw-{Guid.NewGuid():N}@test.mentoory.com";

            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='FirstName']", "Test");
            await page.FillAsync("input[name='LastName']", "User");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='NationalId']", "5-6789-0123");
            await page.FillAsync("input[name='Password']", "Short1!");
            await page.FillAsync("input[name='ConfirmPassword']", "Short1!");

            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().NotContain("/Success",
                "jQuery unobtrusive validation must block submission when the password is below the minimum length");
            (await page.Locator("input[name='Password'].input-validation-error").CountAsync())
                .Should().Be(1, "the too-short Password input must be flagged client-side");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_ShortPassword_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-01")]
    [Trait("Spec", "FR-016-02")]
    [Trait("Spec", "FR-016-11")]
    [Trait("Spec", "FR-016-13")]
    [Trait("Spec", "FR-016-14")]
    [Trait("Sc", "SC-016-02")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "response-indistinguishability")]
    [Trait("Floor", "content-policy-rules")]
    public async Task Register_PasswordContainsEmailLocalPart_ShowsGenericBanner_WithoutFieldAttribution()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var email = $"e2e-ident-{uniqueId}@test.mentoory.com";
            var nationalId = $"4-{uniqueId[..4]}-{uniqueId[4..8]}";
            var passwordWithEmail = $"MyEmailIs{email}ButStrong1!";

            await RegisterUserAsync(page, email, nationalId, passwordWithEmail);

            page.Url.Should().NotContain("/Success",
                "a password containing the email must fail FluentValidation and stay on the Register page");

            var bannerText = await page.Locator(".alert.alert-danger").TextContentAsync();
            bannerText.Should().Contain("No fue posible completar el registro");

            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("correo electrónico ni su número de identificación",
                "the identifying-data rule message must never be surfaced field-attributed on the public path");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_PasswordContainsEmailLocalPart_ShowsGenericBanner_WithoutFieldAttribution));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-018-18")]
    [Trait("Floor", "content-policy-rules")]
    public async Task Register_PasswordContainsNationalId_ShowsGenericBanner()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var email = $"e2e-pwd-nid-{Guid.NewGuid():N}@example.com";

            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='FirstName']", "E2E");
            await page.FillAsync("input[name='LastName']", "Test");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            // CRI mask is `0-0000-0000` (regex `^\d-\d{4}-\d{4}$`); the registration
            // form's input-mask JS reformats anything else before submission, so we
            // must use a NID already in the canonical mask shape and embed it
            // verbatim in the password to trigger MustNotContainIdentifyingData.
            await page.FillAsync("input[name='NationalId']", "9-1234-5678");
            await page.FillAsync("input[name='Password']", "Secure9-1234-5678!");
            await page.FillAsync("input[name='ConfirmPassword']", "Secure9-1234-5678!");

            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().NotContain("/Success",
                "a password containing the national ID must fail the FluentValidation rule and stay on the Register page");

            var bannerText = await page.Locator(".alert.alert-danger").TextContentAsync();
            bannerText.Should().Contain(
                "No fue posible completar el registro. Revise los datos e intente nuevamente.");

            (await page.Locator("[data-valmsg-for].field-validation-error").CountAsync())
                .Should().Be(0,
                    "the public registration path must never surface a per-field error span — the validator failure must not leak which field failed");
            (await page.Locator(".field-validation-error").CountAsync())
                .Should().Be(0,
                    "no per-field validation error span may render when the generic banner is shown on the public path");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_PasswordContainsNationalId_ShowsGenericBanner));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-018-20")]
    [Trait("Floor", "form-state-preservation")]
    public async Task Registration_GenericBannerRender_PreservesNonSecretFields()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var email = $"e2e-formstate-{Guid.NewGuid():N}@example.com";
            const string firstName = "Preserved-First";
            const string lastName = "Preserved-Last";
            const string country = "CRI";
            // CRI mask is `0-0000-0000`; use a NID already matching the canonical
            // mask shape so the form's input-mask JS does not reformat it before
            // submission. The password embeds it verbatim to trip
            // MustNotContainIdentifyingData and force the server-side re-render.
            const string nationalId = "9-1234-5678";
            const string password = "Secure9-1234-5678!";

            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='FirstName']", firstName);
            await page.FillAsync("input[name='LastName']", lastName);
            await page.SelectOptionAsync("select[name='Country']", country);
            await page.FillAsync("input[name='NationalId']", nationalId);
            await page.FillAsync("input[name='Password']", password);
            await page.FillAsync("input[name='ConfirmPassword']", password);

            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().NotContain("/Success",
                "the password-contains-NID rule must trigger the generic banner re-render so we can inspect form-state preservation");

            (await page.Locator("input[name='Email']").InputValueAsync())
                .Should().Be(email, "the Email field must repopulate after the generic banner re-render");
            (await page.Locator("input[name='FirstName']").InputValueAsync())
                .Should().Be(firstName, "the FirstName field must repopulate after the generic banner re-render");
            (await page.Locator("input[name='LastName']").InputValueAsync())
                .Should().Be(lastName, "the LastName field must repopulate after the generic banner re-render");
            (await page.Locator("select[name='Country']").InputValueAsync())
                .Should().Be(country, "the Country select must repopulate after the generic banner re-render");
            (await page.Locator("input[name='NationalId']").InputValueAsync())
                .Should().Be(nationalId, "the NationalId field must repopulate after the generic banner re-render");

            (await page.Locator("input[name='Password']").InputValueAsync())
                .Should().BeEmpty("the Password field must NEVER be repopulated after a server-side failure");
            (await page.Locator("input[name='ConfirmPassword']").InputValueAsync())
                .Should().BeEmpty("the ConfirmPassword field must NEVER be repopulated after a server-side failure");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Registration_GenericBannerRender_PreservesNonSecretFields));
            await page.Context.DisposeAsync();
        }
    }

    private async Task RegisterUserAsync(IPage page, string email, string nationalId, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='FirstName']", "Test");
        await page.FillAsync("input[name='LastName']", "User");
        await page.SelectOptionAsync("select[name='Country']", "CRI");
        await page.FillAsync("input[name='NationalId']", nationalId);
        await page.FillAsync("input[name='Password']", password);
        await page.FillAsync("input[name='ConfirmPassword']", password);

        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
