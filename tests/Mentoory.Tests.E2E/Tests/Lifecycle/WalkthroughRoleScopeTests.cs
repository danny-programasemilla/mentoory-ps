using FluentAssertions;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tests.E2E.Infrastructure;
using Mentoory.Tests.E2E.Infrastructure.Lifecycle;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests.Lifecycle;

[Collection(E2ETestCollection.Name)]
public class WalkthroughRoleScopeTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _host;
    private readonly LifecycleFixtures _fixtures;

    public WalkthroughRoleScopeTests(PlaywrightFixture host)
    {
        _host = host;
        _fixtures = new LifecycleFixtures(host);
    }

    public Task InitializeAsync() => _fixtures.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task IncubatorAdminA_CannotFetchIncubatorBProjectLifecycle()
    {
        // incadmin1's seed role is scoped to Incubadora Alpha — not to fixture incubator B.
        var (_, incubatorBExternalId) = await _fixtures.EnsureTwoIncubatorsAsync();
        var incubatorBProjectName = LifecycleFixtures.ProjectNamePrefix + "scope-b";
        var incubatorBProjectExternalId = await _fixtures.CreateProjectAsync(
            incubatorBExternalId,
            incubatorBProjectName,
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsIncubatorAdminAsync(page, _host, userNumber: 1);

            var response = await page.GotoAsync(
                $"{_host.BaseUrl}/Coordination/Projects/Lifecycle/{incubatorBProjectExternalId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            AssertRequestWasDenied(response, page,
                "IncubatorAdmin of one incubator must be denied (not 200/500) when fetching a project in a different incubator");

            var content = await page.ContentAsync();
            content.Should().NotContain(incubatorBProjectName,
                "cross-incubator access must not leak the target project's name into the rendered page");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdminA_CannotFetchIncubatorBProjectLifecycle));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Coordinator_WithoutIncubatorContext_RedirectedToSelector()
    {
        // admin@mentoory.com is the only seeded user that ends up with
        // ActiveIncubatorId=0 after /Context/Select (single-role users auto-select
        // their incubator server-side and never reach the no-context branch).
        var page = await _host.CreatePageAsync();
        try
        {
            await EstablishGlobalAdminNoIncubatorSessionAsync(page);

            await page.GotoAsync($"{_host.BaseUrl}/Coordination/Projects");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Context/Select",
                "a session without a selected incubator must be bounced back to the context selector");

            var content = await page.ContentAsync();
            content.Should().NotContain(CoordinationProjectsPageObject.TableElementId,
                "the Coordination projects list must not render when the session has no valid incubator context");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_WithoutIncubatorContext_RedirectedToSelector));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Mentor_CannotAccessCoordinationProjectsList()
    {
        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsMentorAsync(page, _host);

            var response = await page.GotoAsync($"{_host.BaseUrl}/Coordination/Projects");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            AssertRequestWasDenied(response, page,
                "Mentor is outside the Coordination projects controller's allowed roles and must be denied access");

            var content = await page.ContentAsync();
            content.Should().NotContain(CoordinationProjectsPageObject.TableElementId,
                "the Coordination projects list must not render for users whose role is not in the controller's allow-list");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Mentor_CannotAccessCoordinationProjectsList));
            await page.Context.DisposeAsync();
        }
    }

    private static void AssertRequestWasDenied(IResponse? response, IPage page, string because)
    {
        var denied = response?.Status is 403 or 404
                     || page.Url.Contains("/AccessDenied", StringComparison.OrdinalIgnoreCase)
                     || page.Url.Contains("/Access/Login", StringComparison.OrdinalIgnoreCase);
        denied.Should().BeTrue(because);
    }

    private async Task EstablishGlobalAdminNoIncubatorSessionAsync(IPage page)
    {
        await page.GotoAsync($"{_host.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", LifecycleLoginHelpers.GlobalAdminEmail);
        await page.FillAsync("input[name='Password']", LifecycleLoginHelpers.GlobalAdminPassword);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync("**/Context/Select**", new PageWaitForURLOptions { Timeout = 10000 });

        var tokenTask = page.Locator("input[name='__RequestVerificationToken']").First.InputValueAsync();
        var roleIdTask = _fixtures.GetGlobalAdminRoleAssignmentExternalIdAsync();
        await Task.WhenAll(tokenTask, roleIdTask);

        var status = await page.EvaluateAsync<int>(
            @"async (args) => {
                const body = new URLSearchParams();
                body.append('__RequestVerificationToken', args.token);
                body.append('roleAssignmentExternalId', args.roleId);
                const r = await fetch('/Context/Select', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: body.toString(),
                    redirect: 'follow',
                });
                return r.status;
            }",
            new { token = tokenTask.Result, roleId = roleIdTask.Result.ToString() });

        status.Should().BeInRange(200, 399,
            "posting /Context/Select with only the GlobalAdmin role assignment must succeed and leave IncubatorId=0 in the cookie");
    }
}
