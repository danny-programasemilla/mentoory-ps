using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T005 - Validates internal user registration form with country dropdown,
/// project assignment, and verification toggle.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AdminInternalRegistrationTests
{
    private readonly PlaywrightFixture _fixture;

    public AdminInternalRegistrationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegisterInternal_PageLoads_WithCountryDropdownAndFields()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/RegisterInternal");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert heading (rendered as <h5> inside .card-header)
            var heading = page.Locator("h1, h2, h3, h4, h5").Filter(new LocatorFilterOptions
            {
                HasText = "Registrar Usuario Interno"
            });
            (await heading.CountAsync()).Should().BeGreaterThan(0,
                "page should show 'Registrar Usuario Interno' heading");

            // Assert all required form fields are present
            (await page.Locator("input[name='Email']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='Password']").CountAsync()).Should().Be(1);
            (await page.Locator("select[name='Country']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='Identification']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='ProjectExternalId']").CountAsync()).Should().BeGreaterThanOrEqualTo(1);
            // ASP.NET renders a hidden input alongside the checkbox; target only the checkbox
            (await page.Locator("input[type='checkbox'][name='RequireEmailVerification']").CountAsync()).Should().Be(1);

            // Assert Country dropdown has Costa Rica option
            var costaRicaOption = page.Locator("select[name='Country'] option[value='CRI']");
            (await costaRicaOption.CountAsync()).Should().Be(1,
                "Country dropdown should contain Costa Rica (CRI) option");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(RegisterInternal_PageLoads_WithCountryDropdownAndFields));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task RegisterInternal_SuccessfulRegistration_RedirectsToUsersList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Lookup ProjectExternalId from DB
            var projectExternalId = await GetFirstProjectExternalIdAsync();

            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/RegisterInternal");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-reginternal-{uniqueId}@test.mentoory.com";
            var uniqueNationalId = $"1-{uniqueId[..4]}-{uniqueId[4..8]}";

            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.FillAsync("input[name='Password']", "SecurePass12!@");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='Identification']", uniqueNationalId);

            // Fill ProjectExternalId (could be hidden input or select)
            var projectInput = page.Locator("input[name='ProjectExternalId']");
            if (await projectInput.CountAsync() > 0)
            {
                await projectInput.FillAsync(projectExternalId);
            }
            else
            {
                await page.SelectOptionAsync("select[name='ProjectExternalId']", projectExternalId);
            }

            // Ensure RequireEmailVerification is unchecked (use type='checkbox' to avoid hidden input)
            var verificationCheckbox = page.Locator("input[type='checkbox'][name='RequireEmailVerification']");
            if (await verificationCheckbox.IsCheckedAsync())
            {
                await verificationCheckbox.UncheckAsync();
            }

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Registrar Usuario"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Administration/Users",
                "after successful internal registration, should redirect to users list");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Usuario registrado exitosamente",
                "success message should be displayed");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(RegisterInternal_SuccessfulRegistration_RedirectsToUsersList));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task RegisterInternal_EmptyForm_ShowsValidationErrors()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/RegisterInternal");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Submit empty form
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert validation errors appear
            var validationErrors = page.Locator(".text-danger, .input-validation-error, .validation-summary-errors, .field-validation-error");
            (await validationErrors.CountAsync()).Should().BeGreaterThan(0,
                "submitting an empty form should show validation errors");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(RegisterInternal_EmptyForm_ShowsValidationErrors));
            await page.Context.DisposeAsync();
        }
    }

    private async Task<string> GetFirstProjectExternalIdAsync()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP 1 CAST(ExternalId AS NVARCHAR(50)) FROM [tenant].[Projects]";

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? throw new InvalidOperationException("No projects found in seed data");
    }

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for login redirect chain to complete (login -> context -> home)
        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"), new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // If redirected to context selection, pick the first available context
        if (page.Url.Contains("/Context/Select"))
        {
            var roleDropdown = page.Locator("[data-mode='page'] [data-cs='role']");
            await page.WaitForFunctionAsync(
                "sel => sel.options.length > 1",
                await roleDropdown.ElementHandleAsync(),
                new() { Timeout = 10000 });
            if (await roleDropdown.IsEnabledAsync())
            {
                await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }

            var incubatorDropdown = page.Locator("[data-mode='page'] [data-cs='incubator']");
            await page.WaitForFunctionAsync(
                "sel => sel.options.length > 1",
                await incubatorDropdown.ElementHandleAsync(),
                new() { Timeout = 10000 });
            if (await incubatorDropdown.IsEnabledAsync())
            {
                await incubatorDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }

            var confirmBtn = page.Locator("[data-mode='page'] [data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 15000 });
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
