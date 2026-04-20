using System.Text.Json;
using Mentoory.Access.Application.StageActions;
using Mentoory.Tenant.Domain.Enums;
using Microsoft.Playwright;

namespace Mentoory.Tests.E2E.Infrastructure.Lifecycle;

public sealed class LifecyclePageObject
{
    public const string StageStateCompleted = "Completada";
    public const string StageStateInProgress = "En progreso";
    public const string StageStatePending = "Pendiente";

    public const string ClosureFinalStageMessage = "El proyecto ya está en la etapa final (Cierre).";

    private readonly IPage _page;
    private readonly PlaywrightFixture _host;

    public LifecyclePageObject(IPage page, PlaywrightFixture host)
    {
        _page = page;
        _host = host;
    }

    public ILocator AdvanceButton => _page.Locator("button[data-bs-target='#advanceStageModal']");

    public Task GotoAsync(Guid projectExternalId)
        => GotoUrlAsync($"/Coordination/Projects/Lifecycle/{projectExternalId}");

    public async Task GotoUrlAsync(string relativeUrl)
    {
        await _page.GotoAsync($"{_host.BaseUrl}{relativeUrl}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public ILocator StageRow(StageType stage)
    {
        var displayName = StageTypeDisplay.ToSpanish(stage);
        return _page.Locator($"ul.steps li.step-item:has(> div > strong:text-is(\"{displayName}\"))");
    }

    public async Task<string> ReadStageStateAsync(StageType stage)
    {
        var badge = StageRow(stage).Locator("span.badge").First;
        return (await badge.InnerTextAsync()).Trim();
    }

    public async Task<(string? StartedAt, string? CompletedAt)> ReadStageTimestampsAsync(StageType stage)
    {
        var container = StageRow(stage).Locator("div.text-muted.small").First;
        var text = await container.InnerTextAsync();
        return (ExtractAfter(text, "Inicio:"), ExtractAfter(text, "Fin:"));
    }

    public async Task<string?> ReadStageAdvancedByAsync(StageType stage)
    {
        var advancedBy = StageRow(stage).Locator("div:has-text(\"Avanzada por:\")");
        if (await advancedBy.CountAsync() == 0)
        {
            return null;
        }

        return ExtractAfter(await advancedBy.First.InnerTextAsync(), "Avanzada por:");
    }

    public async Task<bool> IsAdvanceButtonVisibleAsync()
    {
        var button = AdvanceButton;
        return await button.CountAsync() > 0 && await button.IsVisibleAsync();
    }

    public async Task<string?> ReadCannotAdvanceReasonAsync()
    {
        var reason = _page.Locator("div.card:has(ul.steps) .card-footer span.text-muted").First;
        if (await reason.CountAsync() == 0)
        {
            return null;
        }

        return (await reason.InnerTextAsync()).Trim();
    }

    public async Task ClickAdvanceAndConfirmAsync()
    {
        await AdvanceButton.ClickAsync();
        var modal = _page.Locator("#advanceStageModal");
        await modal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await modal.Locator("button[type='submit']").ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public Task<string?> ReadAdvanceAntiforgeryTokenAsync()
        => _page.Locator("#advanceStageModal input[name='__RequestVerificationToken']")
            .First
            .GetAttributeAsync("value");

    public async Task<string?> HarvestAdvanceTokenFromAsync(Guid tokenCarrierProjectExternalId)
    {
        await GotoAsync(tokenCarrierProjectExternalId);
        return await ReadAdvanceAntiforgeryTokenAsync();
    }

    // Posts via in-browser fetch so session cookies attach automatically; Playwright's
    // IAPIRequestContext does not share cookies with the page context in this host setup.
    // `followRedirects=false` keeps TempData intact for a subsequent page navigation to read
    // the toast — TempData is consumed on the first GET after the POST.
    public Task<JsonElement> PostAdvanceStageAsync(Guid projectExternalId, string? antiforgeryToken, bool followRedirects)
    {
        var relativeUrl = $"/Coordination/Projects/AdvanceStage/{projectExternalId}";
        return _page.EvaluateAsync<JsonElement>(@"
            async ({ url, token, followRedirects }) => {
                const body = token ? ('__RequestVerificationToken=' + encodeURIComponent(token)) : '';
                const res = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: body,
                    redirect: followRedirects ? 'follow' : 'manual',
                });
                return { type: res.type, url: res.url, status: res.status };
            }", new { url = relativeUrl, token = antiforgeryToken, followRedirects });
    }

    public ILocator ActionCard(StageGatedAction action)
    {
        var displayName = StageActionDisplay.ToSpanish(action);
        return _page.Locator($".card-action:has(.fw-bold:text-is(\"{displayName}\"))");
    }

    public async Task<StageGatedActionState> ReadActionStateAsync(StageGatedAction action)
    {
        var classValue = await ActionCard(action).First.GetAttributeAsync("class") ?? string.Empty;
        if (classValue.Contains("border-primary"))
        {
            return StageGatedActionState.Available;
        }

        if (classValue.Contains("border-secondary"))
        {
            return StageGatedActionState.Locked;
        }

        if (classValue.Contains("border-success"))
        {
            return StageGatedActionState.Past;
        }

        throw new InvalidOperationException(
            $"Action card for {action} has no known border class. class='{classValue}'.");
    }

    public Task<string?> ReadActionTooltipAsync(StageGatedAction action)
        => ActionCard(action).First.GetAttributeAsync("data-bs-title");

    public async Task<string?> ReadActionHrefAsync(StageGatedAction action)
    {
        var link = ActionCard(action).Locator("a").First;
        if (await link.CountAsync() == 0)
        {
            return null;
        }

        return await link.GetAttributeAsync("href");
    }

    public Task<string?> ReadSuccessToastAsync() => ReadToastAsync(".alert-success");

    public Task<string?> ReadErrorToastAsync() => ReadToastAsync(".alert-danger");

    public Task<string?> ReadWarningToastAsync() => ReadToastAsync(".alert-warning");

    private static string? ExtractAfter(string text, string prefix)
    {
        var index = text.IndexOf(prefix, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var tail = text[(index + prefix.Length)..];
        var separatorIndex = tail.IndexOfAny(new[] { '·', '\n', '\r' });
        var slice = separatorIndex < 0 ? tail : tail[..separatorIndex];
        return slice.Trim();
    }

    private async Task<string?> ReadToastAsync(string cssSelector)
    {
        var alert = _page.Locator(cssSelector).First;
        if (await alert.CountAsync() == 0)
        {
            return null;
        }

        return (await alert.InnerTextAsync()).Trim();
    }
}
