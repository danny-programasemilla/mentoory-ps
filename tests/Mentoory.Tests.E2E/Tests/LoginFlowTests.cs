using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T002 - Validates login page rendering, credential validation, and navigation.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class LoginFlowTests
{
    private readonly PlaywrightFixture _fixture;

    public LoginFlowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_PageLoads_WithCorrectElements()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert heading
            var heading = await page.Locator("h2").TextContentAsync();
            heading.Should().Contain("Iniciar Sesión");

            // Assert form fields present
            (await page.Locator("input[name='Email']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='Password']").CountAsync()).Should().Be(1);
            (await page.Locator("button[type='submit']").CountAsync()).Should().BeGreaterThan(0);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Login_PageLoads_WithCorrectElements));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Login_ValidCredentials_RedirectsAway()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            page.Url.Should().NotContain("/Access/Login",
                "successful login should redirect away from the login page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Login_ValidCredentials_RedirectsAway));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsErrorMessage()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "admin@mentoory.com", "WrongPassword123!");

            page.Url.Should().Contain("/Access/Login",
                "failed login should stay on the login page");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Credenciales inválidas");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Login_InvalidCredentials_ShowsErrorMessage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Login_EmptyFields_ShowsValidationErrors()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Submit empty form
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert validation errors appear
            var validationErrors = page.Locator(".text-danger, .input-validation-error, .field-validation-error");
            (await validationErrors.CountAsync()).Should().BeGreaterThan(0,
                "validation errors should appear for empty Email and Password fields");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Login_EmptyFields_ShowsValidationErrors));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Login_RegisterLink_NavigatesToRegistration()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Click the "Crear una cuenta" link
            await page.ClickAsync("a:has-text('Crear una cuenta')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/Register");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Login_RegisterLink_NavigatesToRegistration));
            await page.Context.DisposeAsync();
        }
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
