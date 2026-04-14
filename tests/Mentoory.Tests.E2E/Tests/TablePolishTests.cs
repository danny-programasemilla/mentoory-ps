using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates table polish: zebra striping, hover classes, header icons,
/// status-dot rendering, and info-line padding across DataTable and static table views.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class TablePolishTests
{
    private readonly PlaywrightFixture _fixture;

    public TablePolishTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminUsersTable_ShouldHave_StripedAndHoverClasses()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");
            await ajaxResponseTask;

            var table = page.Locator("#usersTable");
            var classes = await table.GetAttributeAsync("class");

            classes.Should().Contain("table-striped", "DataTable should have zebra striping");
            classes.Should().Contain("table-hover", "DataTable should have hover effect");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AdminUsersTable_ShouldHave_StripedAndHoverClasses));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminUsersTable_ShouldHave_HeaderIcons()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");
            await ajaxResponseTask;

            // Header icons are injected by applyHeaderIcons() via initComplete callback
            var iconHeaders = page.Locator("#usersTable thead th .ti, #usersTable thead th .dt-column-title .ti");
            var iconCount = await iconHeaders.CountAsync();

            // At minimum: correo(ti-mail), nombre(ti-user), apellido(ti-users),
            // estado(ti-circle-check), fecha(ti-calendar) = 5 icons
            iconCount.Should().BeGreaterThanOrEqualTo(5,
                "all 5 column headers should have Tabler icons");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AdminUsersTable_ShouldHave_HeaderIcons));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminUsersTable_ShouldRender_StatusDots()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");
            await ajaxResponseTask;

            // Wait for DataTable to render rows
            await page.WaitForSelectorAsync("#usersTable tbody tr td", new() { Timeout = 10000 });

            // Status column should use .status (dots) not .badge
            var statusDots = page.Locator("#usersTable tbody .status");
            var dotCount = await statusDots.CountAsync();
            dotCount.Should().BeGreaterThan(0,
                "status column should render Tabler status-dot indicators");

            var badges = page.Locator("#usersTable tbody .badge");
            var badgeCount = await badges.CountAsync();
            badgeCount.Should().Be(0,
                "status column should no longer use badge elements");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AdminUsersTable_ShouldRender_StatusDots));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminUsersTable_InfoLine_ShouldHavePadding()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");
            await ajaxResponseTask;

            await page.WaitForSelectorAsync("#usersTable tbody tr td", new() { Timeout = 10000 });

            // Verify the info line element exists and has padding
            var infoLine = page.Locator(".dt-info");
            (await infoLine.IsVisibleAsync()).Should().BeTrue(
                "info line should be visible after data loads");

            var paddingLeft = await infoLine.EvaluateAsync<string>(
                "el => getComputedStyle(el).paddingLeft");
            paddingLeft.Should().Be("16px",
                "info line should have 1rem (16px) left padding");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AdminUsersTable_InfoLine_ShouldHavePadding));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task PlatformIncubatorsTable_ShouldHave_StripedHoverAndIcons()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "admin@mentoory.com", "123abc987");

            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Platform/Incubators/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Incubators");
            await ajaxResponseTask;

            var table = page.Locator("#incubatorsTable");
            var classes = await table.GetAttributeAsync("class");
            classes.Should().Contain("table-striped");
            classes.Should().Contain("table-hover");

            var iconHeaders = page.Locator("#incubatorsTable thead th .ti, #incubatorsTable thead th .dt-column-title .ti");
            (await iconHeaders.CountAsync()).Should().BeGreaterThanOrEqualTo(5,
                "incubators table headers should have icons");

            // Wait for rows and check status dots for isActive column
            await page.WaitForSelectorAsync("#incubatorsTable tbody tr td", new() { Timeout = 10000 });
            var statusDots = page.Locator("#incubatorsTable tbody .status");
            (await statusDots.CountAsync()).Should().BeGreaterThan(0,
                "isActive column should use status-dot indicators");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(PlatformIncubatorsTable_ShouldHave_StripedHoverAndIcons));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminUsersTable_ShouldPreserve_SortingFunctionality()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var ajaxResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");
            await ajaxResponseTask;

            await page.WaitForSelectorAsync("#usersTable tbody tr td", new() { Timeout = 10000 });

            // Click a column header to trigger sorting — wait for new AJAX call
            var sortResponseTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Administration/Users/Data") && r.Request.Method == "POST");

            await page.ClickAsync("#usersTable thead th:first-child");
            var sortResponse = await sortResponseTask;

            sortResponse.Status.Should().Be(200,
                "sorting should trigger a new AJAX call that succeeds");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AdminUsersTable_ShouldPreserve_SortingFunctionality));
            await page.Context.DisposeAsync();
        }
    }

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
