using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T052 - Validates that each role sees only the menu items they are permitted to access.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class MenuVisibilityTests
{
    private readonly PlaywrightFixture _fixture;

    public MenuVisibilityTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Entrepreneur_ShouldSee_OnlyParticipantMenuItems()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            var sidebarNav = page.Locator(".sidebar, #sidebar, nav.sidebar, .side-nav");

            // Entrepreneur should see Participant-related menu items
            var participantLinks = sidebarNav.Locator("a[href*='/Participant']");
            (await participantLinks.CountAsync()).Should().BeGreaterThan(0,
                "Entrepreneur should see Participant menu items");

            // Entrepreneur should NOT see Administration or Platform menu items
            var adminLinks = sidebarNav.Locator("a[href*='/Administration']");
            (await adminLinks.CountAsync()).Should().Be(0,
                "Entrepreneur should not see Administration menu items");

            var platformLinks = sidebarNav.Locator("a[href*='/Platform']");
            (await platformLinks.CountAsync()).Should().Be(0,
                "Entrepreneur should not see Platform menu items");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Entrepreneur_ShouldSee_OnlyParticipantMenuItems));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdmin_ShouldSee_AdministrationMenuItems()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var sidebarNav = page.Locator(".sidebar, #sidebar, nav.sidebar, .side-nav");

            // IncubatorAdmin should see Administration menu items
            var adminLinks = sidebarNav.Locator("a[href*='/Administration']");
            (await adminLinks.CountAsync()).Should().BeGreaterThan(0,
                "IncubatorAdmin should see Administration menu items");

            // IncubatorAdmin should NOT see Platform-level menu items
            var platformLinks = sidebarNav.Locator("a[href*='/Platform/Incubators']");
            (await platformLinks.CountAsync()).Should().Be(0,
                "IncubatorAdmin should not see Platform/Incubators menu items");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_ShouldSee_AdministrationMenuItems));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task GlobalAdmin_ShouldSee_AllMenuGroups()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "admin@mentoory.com", "123abc987");

            var sidebarNav = page.Locator(".sidebar, #sidebar, nav.sidebar, .side-nav");

            // GlobalAdmin should see Platform-level menu items
            var platformLinks = sidebarNav.Locator("a[href*='/Platform']");
            (await platformLinks.CountAsync()).Should().BeGreaterThan(0,
                "GlobalAdmin should see Platform menu items");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdmin_ShouldSee_AllMenuGroups));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Coordinator_ShouldSee_CoordinationMenuItems()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            var sidebarNav = page.Locator(".sidebar, #sidebar, nav.sidebar, .side-nav");

            // Coordinator should see Coordination menu items
            var coordLinks = sidebarNav.Locator("a[href*='/Coordination']");
            (await coordLinks.CountAsync()).Should().BeGreaterThan(0,
                "ProjectCoordinator should see Coordination menu items");

            // Coordinator should NOT see Platform menu items
            var platformLinks = sidebarNav.Locator("a[href*='/Platform/Incubators']");
            (await platformLinks.CountAsync()).Should().Be(0,
                "ProjectCoordinator should not see Platform/Incubators menu items");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_ShouldSee_CoordinationMenuItems));
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

        // GlobalAdmin sees context selection with incubator choices; pick the first one
        if (page.Url.Contains("/Context/Select"))
        {
            var firstSubmitButton = page.Locator(".context-card button[type='submit']").First;
            await firstSubmitButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await firstSubmitButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
