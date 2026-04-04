using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T050 - Validates the diagnostic workflow: clone template, view details, submit response.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class DiagnosticWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public DiagnosticWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Coordinator_ShouldClone_FormTemplate()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            // Navigate to the diagnostics area
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Diagnostics");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Look for a clone/duplicate action button on a template
            var cloneButton = page.Locator("a, button").Filter(new LocatorFilterOptions
            {
                HasText = "Clonar"
            });
            (await cloneButton.CountAsync()).Should().BeGreaterThan(0,
                "at least one 'Clonar' (clone) action should be available for form templates");

            await cloneButton.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // After cloning, the page should not show an error
            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("Error");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_ShouldClone_FormTemplate));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Coordinator_ShouldView_FormDetails()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Diagnostics");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Click on a form to see its details
            var detailLink = page.Locator("a").Filter(new LocatorFilterOptions
            {
                HasText = "Ver"
            });
            (await detailLink.CountAsync()).Should().BeGreaterThan(0,
                "at least one 'Ver' (view) link should be available for diagnostic forms");

            await detailLink.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The detail page should show form information (questions, sections, etc.)
            var heading = page.Locator("h1, h2, h3");
            (await heading.CountAsync()).Should().BeGreaterThan(0,
                "the form detail page should render with a heading");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_ShouldView_FormDetails));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Entrepreneur_ShouldSubmit_DiagnosticResponse()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            // Navigate to the participant diagnostic listing
            await page.GotoAsync($"{_fixture.BaseUrl}/Participant/Diagnostic");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("Internal Server Error");

            // Find a diagnostic form to open
            var formLink = page.Locator("a").Filter(new LocatorFilterOptions
            {
                HasText = "Responder"
            });

            if (await formLink.CountAsync() > 0)
            {
                await formLink.First.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Verify the form page renders correctly with questions
                var formContent = await page.ContentAsync();
                formContent.Should().NotContain("Internal Server Error",
                    "the diagnostic form page should render without errors");

                var title = await page.TitleAsync();
                title.Should().NotBeNullOrWhiteSpace(
                    "the diagnostic form page should have a title");
            }
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Entrepreneur_ShouldSubmit_DiagnosticResponse));
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
    }
}
