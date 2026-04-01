using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T046 - Validates that each role lands on the correct dashboard after login and context selection.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class LoginRoutingTests
{
    private readonly PlaywrightFixture _fixture;

    public LoginRoutingTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GlobalAdmin_ShouldLandOn_PlatformIncubators()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "admin@mentoory.com", "123abc987");

            page.Url.Should().Contain("/Platform/Incubators");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdmin_ShouldLandOn_PlatformIncubators));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdmin_ShouldLandOn_AdministrationDashboard()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            page.Url.Should().Contain("/Administration/Dashboard");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_ShouldLandOn_AdministrationDashboard));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectCoordinator_ShouldLandOn_CoordinationDiagnostics()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            page.Url.Should().Contain("/Coordination/Diagnostics");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectCoordinator_ShouldLandOn_CoordinationDiagnostics));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Entrepreneur_ShouldLandOn_ParticipantDiagnostic()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            page.Url.Should().Contain("/Participant/Diagnostic");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Entrepreneur_ShouldLandOn_ParticipantDiagnostic));
            await page.Context.DisposeAsync();
        }
    }

    private async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Identity/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
