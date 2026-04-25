using Microsoft.Playwright;

namespace Mentoory.Tests.E2E.Infrastructure.Lifecycle;

public static class LifecycleLoginHelpers
{
    public const string DefaultTestPassword = "Test123!@#";
    public const string GlobalAdminEmail = "admin@mentoory.com";
    public const string GlobalAdminPassword = "123abc987";

    public static Task AsCoordinatorAsync(
        IPage page,
        PlaywrightFixture host,
        int userNumber = 1,
        bool autoSelectContext = true)
        => LoginAndMaybeSelectContextAsync(page, host, $"coord{userNumber}@test.mentoory.com", DefaultTestPassword, autoSelectContext);

    public static Task AsIncubatorAdminAsync(
        IPage page,
        PlaywrightFixture host,
        int userNumber = 1,
        bool autoSelectContext = true)
        => LoginAndMaybeSelectContextAsync(page, host, $"incadmin{userNumber}@test.mentoory.com", DefaultTestPassword, autoSelectContext);

    public static Task AsMentorAsync(IPage page, PlaywrightFixture host)
        => LoginAndMaybeSelectContextAsync(page, host, "mentor1@test.mentoory.com", DefaultTestPassword, autoSelectContext: false);

    public static Task AsGlobalAdminAsync(IPage page, PlaywrightFixture host)
        => LoginAndMaybeSelectContextAsync(page, host, GlobalAdminEmail, GlobalAdminPassword, autoSelectContext: true);

    private static async Task LoginAndMaybeSelectContextAsync(
        IPage page,
        PlaywrightFixture host,
        string email,
        string password,
        bool autoSelectContext)
    {
        await page.GotoAsync($"{host.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        if (autoSelectContext && page.Url.Contains("/Context/Select", StringComparison.OrdinalIgnoreCase))
        {
            await SelectFirstContextAsync(page);
        }
    }

    private static async Task SelectFirstContextAsync(IPage page)
    {
        var container = page.Locator("[data-mode='page']");
        var roleDropdown = container.Locator("[data-cs='role']");
        var incubatorDropdown = container.Locator("[data-cs='incubator']");
        var confirmBtn = container.Locator("[data-cs='confirm']");

        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await roleDropdown.ElementHandleAsync(),
            new PageWaitForFunctionOptions { Timeout = 10000 });

        if (await roleDropdown.IsEnabledAsync())
        {
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await incubatorDropdown.ElementHandleAsync(),
            new PageWaitForFunctionOptions { Timeout = 10000 });

        if (await incubatorDropdown.IsEnabledAsync())
        {
            await incubatorDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 15000 });
        await confirmBtn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
