using Microsoft.Playwright;

namespace Mentoory.Tests.E2E.Infrastructure.Lifecycle;

public sealed class CoordinationProjectsPageObject
{
    public const string TableElementId = "coordinationProjectsTable";
    private const string TableSelector = "#" + TableElementId;
    private const string DataRowSelector = TableSelector + " tbody tr:not(.dataTables_empty)";

    private readonly IPage _page;
    private readonly PlaywrightFixture _host;

    public CoordinationProjectsPageObject(IPage page, PlaywrightFixture host)
    {
        _page = page;
        _host = host;
    }

    public async Task GotoAsync()
    {
        await _page.GotoAsync($"{_host.BaseUrl}/Coordination/Projects");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // DataTables renders rows via AJAX after DOM ready; wait until tbody has a cell
        // (either a data row or the empty-state row) so row queries aren't racy.
        await _page.WaitForSelectorAsync($"{TableSelector} tbody tr td", new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    public async Task<int> RowCountAsync()
    {
        var rows = _page.Locator(DataRowSelector);
        return await rows.CountAsync();
    }

    public async Task<bool> ContainsProjectNamedAsync(string name)
    {
        var row = _page.Locator(DataRowSelector).Filter(new LocatorFilterOptions { HasText = name });
        return await row.CountAsync() > 0;
    }

    public async Task ClickProjectRowAsync(string name)
    {
        var row = _page.Locator(DataRowSelector).Filter(new LocatorFilterOptions { HasText = name }).First;
        var lifecycleLink = row.Locator("a[title='Ver ciclo de vida']");
        await lifecycleLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
