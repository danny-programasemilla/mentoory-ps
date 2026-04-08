using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T004 - Validates server-side session validation: deactivated/expired sessions
/// and disabled users get redirected to login.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class SessionInvalidationTests
{
    private readonly PlaywrightFixture _fixture;

    public SessionInvalidationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SessionDeactivated_NextRequest_RedirectsToLogin()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Login as incadmin1
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            // Verify dashboard loads
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            page.Url.Should().Contain("/Administration/Dashboard",
                "dashboard should load for authenticated user");

            // Deactivate all sessions for this user via DB
            await using var conn = new SqlConnection(_fixture.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE [access].[AuthSessions]
                SET IsActive = 0
                WHERE UserId = (
                    SELECT Id FROM [access].[Users]
                    WHERE NormalizedEmail = N'INCADMIN1@TEST.MENTOORY.COM'
                ) AND IsActive = 1";
            await cmd.ExecuteNonQueryAsync();

            // Navigate again — should be redirected to login
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/Login",
                "deactivated session should redirect to login");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(SessionDeactivated_NextRequest_RedirectsToLogin));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisabledUser_NextRequest_RedirectsToLogin()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var email = $"e2e-disabled-{uniqueId}@test.mentoory.com";
            var nationalId = $"7-{uniqueId[..4]}-{uniqueId[4..8]}";
            const string password = "SecurePass123!";

            // 1. Register via browser
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='FirstName']", "Disabled");
            await page.FillAsync("input[name='LastName']", "TestUser");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='NationalId']", nationalId);
            await page.FillAsync("input[name='Password']", password);
            await page.FillAsync("input[name='ConfirmPassword']", password);

            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 2. Activate user via DB
            await using (var conn = new SqlConnection(_fixture.ConnectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE [access].[Users]
                    SET AccountStatus = 1, EmailVerifiedAtUtc = GETUTCDATE()
                    WHERE NormalizedEmail = @email";
                cmd.Parameters.AddWithValue("@email", email.ToUpperInvariant());
                await cmd.ExecuteNonQueryAsync();
            }

            // 3. Login as the activated user
            await LoginAsync(page, email, password);

            // Verify an authenticated page loads (could be AvailableProjects or Context/Select)
            page.Url.Should().NotContain("/Access/Login",
                "activated user should be able to log in");

            // 4. Disable user via DB (AccountStatus = 3)
            await using (var conn = new SqlConnection(_fixture.ConnectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE [access].[Users]
                    SET AccountStatus = 3
                    WHERE NormalizedEmail = @email";
                cmd.Parameters.AddWithValue("@email", email.ToUpperInvariant());
                await cmd.ExecuteNonQueryAsync();
            }

            // 5. Navigate to any authenticated page
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/Login",
                "disabled user should be redirected to login");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(DisabledUser_NextRequest_RedirectsToLogin));
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
