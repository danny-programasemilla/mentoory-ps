using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T003 - Validates that users with PasswordResetRequired status are forced to change
/// their password before accessing any other page.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class ForcedPasswordChangeTests
{
    private readonly PlaywrightFixture _fixture;

    public ForcedPasswordChangeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PasswordResetRequired_Login_RedirectsToChangePassword()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var (email, password) = await CreatePasswordResetRequiredUserAsync(page);

            await LoginAsync(page, email, password);

            page.Url.Should().Contain("/Access/ChangePassword",
                "PasswordResetRequired user should be redirected to change password after login");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Debe cambiar su contraseña para continuar");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(PasswordResetRequired_Login_RedirectsToChangePassword));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ChangePassword_PageLoads_WithCorrectElements()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var (email, password) = await CreatePasswordResetRequiredUserAsync(page);

            await LoginAsync(page, email, password);

            page.Url.Should().Contain("/Access/ChangePassword");

            // Assert form fields present
            (await page.Locator("input[name='CurrentPassword']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='NewPassword']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='ConfirmNewPassword']").CountAsync()).Should().Be(1);

            // Assert submit button (TopBar also has a submit button; filter by text)
            var submitButton = page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar Contraseña"
            });
            var buttonText = await submitButton.TextContentAsync();
            buttonText.Should().Contain("Cambiar Contraseña");

            // Assert logout link
            var logoutLink = page.Locator("a:has-text('Cerrar sesión')");
            (await logoutLink.CountAsync()).Should().BeGreaterThan(0);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ChangePassword_PageLoads_WithCorrectElements));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ChangePassword_ValidChange_RedirectsToContextSelect()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var (email, originalPassword) = await CreatePasswordResetRequiredUserAsync(page);

            await LoginAsync(page, email, originalPassword);

            page.Url.Should().Contain("/Access/ChangePassword");

            // Fill the change password form
            await page.FillAsync("input[name='CurrentPassword']", originalPassword);
            await page.FillAsync("input[name='NewPassword']", "NewSecurePass12!");
            await page.FillAsync("input[name='ConfirmNewPassword']", "NewSecurePass12!");

            // Target the form submit button specifically (TopBar also has a submit button)
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar Contraseña"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should redirect away from ChangePassword
            page.Url.Should().NotContain("/Access/ChangePassword",
                "successful password change should redirect away from change password page");

            var urlContainsExpected = page.Url.Contains("/Context/Select")
                                     || page.Url.Contains("/AvailableProjects");
            urlContainsExpected.Should().BeTrue(
                "user should be redirected to context selection or available projects");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ChangePassword_ValidChange_RedirectsToContextSelect));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ShowsError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var (email, _) = await CreatePasswordResetRequiredUserAsync(page);

            await LoginAsync(page, email, "OriginalPass123!");

            page.Url.Should().Contain("/Access/ChangePassword");

            // Fill with wrong current password
            await page.FillAsync("input[name='CurrentPassword']", "WrongCurrent999!");
            await page.FillAsync("input[name='NewPassword']", "NewSecurePass12!");
            await page.FillAsync("input[name='ConfirmNewPassword']", "NewSecurePass12!");

            // Target the form submit button specifically (TopBar also has a submit button)
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar Contraseña"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/ChangePassword",
                "wrong current password should stay on change password page");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("La contraseña actual es incorrecta");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ChangePassword_WrongCurrentPassword_ShowsError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ChangePassword_PasswordMismatch_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var (email, originalPassword) = await CreatePasswordResetRequiredUserAsync(page);

            await LoginAsync(page, email, originalPassword);

            page.Url.Should().Contain("/Access/ChangePassword");

            // Fill with mismatched new passwords
            await page.FillAsync("input[name='CurrentPassword']", originalPassword);
            await page.FillAsync("input[name='NewPassword']", "NewSecurePass12!");
            await page.FillAsync("input[name='ConfirmNewPassword']", "DifferentPass99!");

            // Target the form submit button specifically (TopBar also has a submit button)
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar Contraseña"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait for client-side or server-side validation error to appear
            var validationError = page.Locator(".field-validation-error, .text-danger, .validation-summary-errors")
                .Filter(new LocatorFilterOptions { HasText = "contraseñas no coinciden" });
            await validationError.Or(page.Locator("text=contraseñas no coinciden")).First
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("contraseñas no coinciden");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ChangePassword_PasswordMismatch_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task PasswordResetRequired_CannotNavigateAway_RedirectsBack()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var (email, originalPassword) = await CreatePasswordResetRequiredUserAsync(page);

            await LoginAsync(page, email, originalPassword);

            page.Url.Should().Contain("/Access/ChangePassword");

            // Attempt to navigate to a protected page
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/ChangePassword",
                "PasswordResetRequired user should be redirected back to change password page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(PasswordResetRequired_CannotNavigateAway_RedirectsBack));
            await page.Context.DisposeAsync();
        }
    }

    /// <summary>
    /// Registers a user via the browser, then activates and sets PasswordResetRequired via direct SQL.
    /// Returns the email and password used for registration.
    /// </summary>
    private async Task<(string Email, string Password)> CreatePasswordResetRequiredUserAsync(IPage page)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"e2e-pwreset-{uniqueId}@test.mentoory.com";
        var nationalId = $"6-{uniqueId[..4]}-{uniqueId[4..8]}";
        const string password = "OriginalPass123!";

        // 1. Register via browser
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='FirstName']", "PwReset");
        await page.FillAsync("input[name='LastName']", "TestUser");
        await page.SelectOptionAsync("select[name='Country']", "CRI");
        await page.FillAsync("input[name='NationalId']", nationalId);
        await page.FillAsync("input[name='Password']", password);
        await page.FillAsync("input[name='ConfirmPassword']", password);

        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 2. Activate and set PasswordResetRequired via DB
        await using var conn = new SqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE [access].[Users]
            SET AccountStatus = 4, EmailVerifiedAtUtc = GETUTCDATE()
            WHERE NormalizedEmail = @email";
        cmd.Parameters.AddWithValue("@email", email.ToUpperInvariant());
        await cmd.ExecuteNonQueryAsync();

        return (email, password);
    }

    private async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
