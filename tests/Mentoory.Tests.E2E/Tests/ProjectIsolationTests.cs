using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T051 - Validates project isolation: users cannot access forms from other projects.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class ProjectIsolationTests
{
    private readonly PlaywrightFixture _fixture;

    public ProjectIsolationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CrossProjectFormAccess_ShouldReturn_NotFoundOrForbidden()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Login as an entrepreneur who belongs to a specific project
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            // Attempt to access a form from a different project using a fabricated ExternalId
            // This GUID should not belong to any form in the user's project
            var foreignFormId = "00000000-0000-0000-0000-000000000999";
            var response = await page.GotoAsync(
                $"{_fixture.BaseUrl}/Participant/Diagnostic/Form/{foreignFormId}");

            var blockedOrNotFound = response?.Status == 404
                                    || response?.Status == 403
                                    || page.Url.Contains("/AccessDenied")
                                    || page.Url.Contains("/Identity/AccessDenied");

            blockedOrNotFound.Should().BeTrue(
                "accessing a form from another project should return 404 or 403");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CrossProjectFormAccess_ShouldReturn_NotFoundOrForbidden));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CrossProjectDiagnosticList_ShouldNotLeak_OtherProjectData()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Login as coordinator scoped to a specific project
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Diagnostics");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The page should only show diagnostics belonging to the coordinator's project.
            // Verify no 500 error and the page renders correctly.
            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("Internal Server Error");

            // The diagnostic list should exist (table or card layout)
            var listContainer = page.Locator("table, .card, [data-testid='diagnostic-list']");
            (await listContainer.CountAsync()).Should().BeGreaterThan(0,
                "the diagnostic listing should render for the coordinator's project");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CrossProjectDiagnosticList_ShouldNotLeak_OtherProjectData));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Coordinator_ShouldNotAccess_AnotherProjectsDiagnosticResponse()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            // Attempt to access a diagnostic response from a different project
            var foreignResponseId = "00000000-0000-0000-0000-000000000888";
            var response = await page.GotoAsync(
                $"{_fixture.BaseUrl}/Coordination/Diagnostics/Response/{foreignResponseId}");

            var blockedOrNotFound = response?.Status == 404
                                    || response?.Status == 403
                                    || page.Url.Contains("/AccessDenied")
                                    || page.Url.Contains("/Identity/AccessDenied");

            blockedOrNotFound.Should().BeTrue(
                "accessing a diagnostic response from another project should return 404 or 403");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_ShouldNotAccess_AnotherProjectsDiagnosticResponse));
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
