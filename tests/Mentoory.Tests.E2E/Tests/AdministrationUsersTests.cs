using System.Text.Json;
using FluentAssertions;
using Mentoory.Access.Application.Validation;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates that the Administration Users list page loads correctly
/// for IncubatorAdmin users with an active incubator context.
/// The DataTable loads data via an async AJAX POST to /Administration/Users/Data,
/// so tests must intercept the network response — not just check the page HTTP status.
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class AdministrationUsersTests
{
    private readonly PlaywrightFixture _fixture;

    public AdministrationUsersTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IncubatorAdmin_UsersDataTableAjax_Returns200WithData()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            // Start listening for the DataTable AJAX call BEFORE navigating
            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");

            // Wait for the deferred AJAX response
            var ajaxResponse = await ajaxResponseTask;

            ajaxResponse.Status.Should().Be(200,
                "the DataTable AJAX POST to /Administration/Users/Data must return 200, not a server error");

            // Parse the JSON payload and verify it has the expected DataTable structure
            var body = await ajaxResponse.TextAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            root.TryGetProperty("recordsTotal", out var recordsTotal).Should().BeTrue(
                "response must include recordsTotal for DataTables");
            recordsTotal.GetInt32().Should().BeGreaterThan(0,
                "there should be at least one user in this incubator from seed data");

            root.TryGetProperty("data", out var data).Should().BeTrue(
                "response must include a data array");
            data.GetArrayLength().Should().BeGreaterThan(0,
                "data array should contain user records");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_UsersDataTableAjax_Returns200WithData));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdmin_UsersDataTableAjax_ReturnsUserDetails()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");

            var ajaxResponse = await ajaxResponseTask;
            var body = await ajaxResponse.TextAsync();
            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");

            // Verify the first user record has all expected fields
            var firstUser = data[0];
            firstUser.TryGetProperty("email", out _).Should().BeTrue("user records must include email");
            firstUser.TryGetProperty("firstName", out _).Should().BeTrue("user records must include firstName");
            firstUser.TryGetProperty("lastName", out _).Should().BeTrue("user records must include lastName");
            firstUser.TryGetProperty("accountStatus", out _).Should().BeTrue("user records must include accountStatus");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_UsersDataTableAjax_ReturnsUserDetails));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-016-07")]
    [Trait("Spec", "FR-016-08")]
    [Trait("Sc", "SC-016-03")]
    [Trait("Floor", "public-vs-admin-attribution")]
    public async Task AdminEnroll_DuplicateEmail_ShowsFieldAttributedError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var email = $"e2e-admindup-{uniqueId}@test.mentoory.com";
            var nationalId1 = $"5-{uniqueId[..4]}-{uniqueId[4..8]}";
            var nationalId2 = $"6-{uniqueId[..4]}-{uniqueId[4..8]}";

            await EnrollUserAsync(page, email, nationalId1, "SecureP@ss12345!");
            page.Url.Should().Contain("/Administration/Users", "first enrollment should redirect to the users list on success");

            await EnrollUserAsync(page, email, nationalId2, "SecureP@ss12345!");

            page.Url.Should().Contain("/Administration/Users/Enroll",
                "duplicate admin enrollment must re-render the form, not redirect");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Ya existe una cuenta con este correo electrónico",
                "admin path must surface attributed duplicate-email errors to help resolve conflicts");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AdminEnroll_DuplicateEmail_ShowsFieldAttributedError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Spec", "FR-018-18")]
    [Trait("Floor", "public-vs-admin-attribution")]
    public async Task AdminEnroll_PasswordContainsIdentifyingData_ShowsAttributedError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var uniqueId = Guid.NewGuid().ToString("N");
            var email = $"e2e-adminpwd-{uniqueId}@example.com";
            var nationalId = $"NID-{uniqueId[..8]}";
            // Password embeds the verbatim national ID to trip the shared
            // MustNotContainIdentifyingData rule. The admin Enroll form has no
            // input mask on NationalId (free-text input), so the value POSTs
            // unchanged and `password.Contains(nationalId)` matches.
            var password = $"AdminPwd{nationalId}Test!";

            await EnrollUserAsync(page, email, nationalId, password, firstName: "Admin", lastName: "Pwd");

            page.Url.Should().Contain("/Administration/Users/Enroll",
                "an enrollment whose password contains identifying data must re-render the form, not redirect");

            // The Razor `asp-validation-for="Password"` tag helper renders a span with
            // `data-valmsg-for="Password"`. Locate the field-attributed error there
            // (the inverse of the public path's masked banner).
            var passwordError = page.Locator("[data-valmsg-for=\"Password\"]");
            await Assertions.Expect(passwordError).ToHaveTextAsync(PasswordIdentifyingDataRule.Message);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AdminEnroll_PasswordContainsIdentifyingData_ShowsAttributedError));
            await page.Context.DisposeAsync();
        }
    }

    private async Task EnrollUserAsync(IPage page, string email, string nationalId, string password, string firstName = "Admin", string lastName = "Enrolled")
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Enroll");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='FirstName']", firstName);
        await page.FillAsync("input[name='LastName']", lastName);
        await page.FillAsync("input[name='Country']", "CO");
        await page.FillAsync("input[name='NationalId']", nationalId);
        await page.FillAsync("input[name='Password']", password);
        await page.FillAsync("input[name='ConfirmPassword']", password);

        // The shared layout has hidden submit buttons (e.g. logout form inside the
        // user-menu dropdown), so a bare `button[type='submit']` resolves to multiple
        // elements. Target the Enroll form's primary action by its visible label.
        await page.ClickAsync("button[type='submit']:has-text('Inscribir Usuario')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
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
