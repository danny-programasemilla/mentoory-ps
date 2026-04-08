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
            var heading = await page.Locator("h5").TextContentAsync();
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
            pageContent.Should().Contain("Revise su correo electrónico para verificar su cuenta");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_SuccessfulRegistration_RedirectsToSuccessPage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShowsFieldError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var email = $"e2e-dupemail-{uniqueId}@test.mentoory.com";
            var nationalId1 = $"1-{uniqueId[..4]}-{uniqueId[4..8]}";

            // Register first user
            await RegisterUserAsync(page, email, nationalId1, "SecurePass123!");
            page.Url.Should().Contain("/Access/Register/Success");

            // Attempt to register again with same email, different NationalId
            var uniqueId2 = Guid.NewGuid().ToString("N")[..8];
            var nationalId2 = $"2-{uniqueId2[..4]}-{uniqueId2[4..8]}";
            await RegisterUserAsync(page, email, nationalId2, "SecurePass123!");

            page.Url.Should().Contain("/Access/Register");
            page.Url.Should().NotContain("/Success");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Ya existe una cuenta con este correo electrónico");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_DuplicateEmail_ShowsFieldError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Register_DuplicateNationalId_ShowsFieldError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var email1 = $"e2e-dupid1-{uniqueId}@test.mentoory.com";
            var nationalId = $"3-{uniqueId[..4]}-{uniqueId[4..8]}";

            // Register first user
            await RegisterUserAsync(page, email1, nationalId, "SecurePass123!");
            page.Url.Should().Contain("/Access/Register/Success");

            // Attempt to register again with different email, same NationalId
            var email2 = $"e2e-dupid2-{uniqueId}@test.mentoory.com";
            await RegisterUserAsync(page, email2, nationalId, "SecurePass123!");

            page.Url.Should().Contain("/Access/Register");
            page.Url.Should().NotContain("/Success");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Ya existe una cuenta con este número de identificación");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_DuplicateNationalId_ShowsFieldError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
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

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Las contraseñas no coinciden");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_PasswordMismatch_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
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

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("La contraseña debe tener al menos 12 caracteres");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Register_ShortPassword_ShowsValidationError));
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
