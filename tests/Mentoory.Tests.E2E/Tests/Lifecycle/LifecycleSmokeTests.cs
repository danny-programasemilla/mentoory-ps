using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Mentoory.Tests.E2E.Infrastructure.Lifecycle;
using Xunit;

namespace Mentoory.Tests.E2E.Tests.Lifecycle;

[Collection(E2ETestCollection.Name)]
public class LifecycleSmokeTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _host;
    private readonly LifecycleFixtures _fixtures;

    public LifecycleSmokeTests(PlaywrightFixture host)
    {
        _host = host;
        _fixtures = new LifecycleFixtures(host);
    }

    public Task InitializeAsync() => _fixtures.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Fixtures_CanLoginAsCoordinatorAndOpenCoordinationProjectsList()
    {
        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);

            var projects = new CoordinationProjectsPageObject(page, _host);
            await projects.GotoAsync();

            var content = await page.ContentAsync();
            content.Should().NotContain("Internal Server Error");

            (await projects.ContainsProjectNamedAsync(LifecycleFixtures.SeededInnovacionProjectName))
                .Should().BeTrue($"coord1 is assigned to '{LifecycleFixtures.SeededInnovacionProjectName}' and should see it in the projects list.");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Fixtures_CanLoginAsCoordinatorAndOpenCoordinationProjectsList));
            await page.Context.DisposeAsync();
        }
    }
}
