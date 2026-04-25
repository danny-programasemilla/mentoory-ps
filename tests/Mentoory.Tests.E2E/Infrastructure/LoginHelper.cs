using Microsoft.Playwright;

namespace Mentoory.Tests.E2E.Infrastructure;

/// <summary>
/// Shared Playwright login flow used by every E2E test that needs an authenticated session.
/// Handles the post-login GlobalAdmin context selector by selecting the first available
/// role + incubator and clicking Confirmar.
/// </summary>
public static class LoginHelper
{
    public static async Task LoginAsync(IPage page, string email, string password, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });

        // GlobalAdmin sees context selection with incubator choices; pick the first one
        if (page.Url.Contains("/Context/Select"))
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });
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
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });
        }
    }
}
