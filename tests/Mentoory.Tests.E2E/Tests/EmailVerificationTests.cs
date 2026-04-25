using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T010 - Validates email verification page behavior for invalid tokens
/// and unverified login attempts.
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class EmailVerificationTests
{
    private readonly PlaywrightFixture _fixture;

    public EmailVerificationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_ShowsErrorMessage()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/VerifyEmail?token=invalid-token-abc123");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert error message in text-danger element
            var errorText = page.Locator(".text-danger");
            (await errorText.CountAsync()).Should().BeGreaterThan(0,
                "an invalid verification token should display an error message in a text-danger element");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(VerifyEmail_InvalidToken_ShowsErrorMessage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task VerifyEmail_UnverifiedUser_CannotLogin()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Register a new user (gets PendingVerification status)
            var uniqueEmail = $"e2e-unverified-{Guid.NewGuid():N}@test.mentoory.com";

            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.FillAsync("input[name='FirstName']", "Unverified");
            await page.FillAsync("input[name='LastName']", "User");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='NationalId']", "5-6789-0123");
            await page.FillAsync("input[name='Password']", "SecurePass12!@");
            await page.FillAsync("input[name='ConfirmPassword']", "SecurePass12!@");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Attempt to login with the unverified user
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.FillAsync("input[name='Password']", "SecurePass12!@");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert login fails with email verification error
            page.Url.Should().Contain("/Access/Login",
                "unverified user should remain on the login page");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Verifique su correo electr\u00f3nico",
                "error message should tell user to verify their email");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(VerifyEmail_UnverifiedUser_CannotLogin));
            await page.Context.DisposeAsync();
        }
    }
}
