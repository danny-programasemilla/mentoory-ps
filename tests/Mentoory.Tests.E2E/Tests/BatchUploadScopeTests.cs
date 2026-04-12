using System.Text;
using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates batch upload authorization scope: project context comes from session
/// (not form dropdown), coordinators with project context can upload,
/// entrepreneurs are denied, and users without project context see a warning.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class BatchUploadScopeTests
{
    private readonly PlaywrightFixture _fixture;

    public BatchUploadScopeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProjectCoordinator_WithProjectContext_CanUpload()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // coord1 auto-gets project context (Innovacion)
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-scope-{uniqueId}@test.mentoory.com";
            var csvBytes = Encoding.UTF8.GetBytes(
                $"Country,Identification,Email,FirstName,LastName\nCRI,8-8888-0001,{uniqueEmail},Scope,TestUser");

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            await page.Locator("button[type='submit']").Filter(
                new LocatorFilterOptions { HasText = "Procesar Archivo" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Resultados de Carga Masiva",
                "ProjectCoordinator with project context should see results page after upload");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectCoordinator_WithProjectContext_CanUpload));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectCoordinator_SeesProjectNameFromContext()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // coord1 auto-gets project context (Innovacion)
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Card header should contain "Proyecto:" text with the project name from context
            var cardHeader = page.Locator(".card-header");
            var headerContent = await cardHeader.TextContentAsync();
            headerContent.Should().Contain("Proyecto:",
                "card header should display 'Proyecto:' label from session context");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectCoordinator_SeesProjectNameFromContext));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Entrepreneur_CannotAccess_BatchUpload()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");

            var denied = response?.Status == 403
                         || page.Url.Contains("/Access/Login")
                         || page.Url.Contains("/AccessDenied")
                         || page.Url.Contains("/Access/AccessDenied");

            denied.Should().BeTrue(
                "Entrepreneur role must not have access to batch upload");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Entrepreneur_CannotAccess_BatchUpload));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdmin_WithoutProjectContext_SeesWarning()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // incadmin1 auto-skips context selection but has NO project in context
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert warning about needing to select a project
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Seleccione un proyecto",
                "IncubatorAdmin without project context should see a warning to select a project");

            // Assert form fieldset is disabled
            var disabledFieldset = page.Locator("fieldset[disabled]");
            (await disabledFieldset.CountAsync()).Should().BeGreaterThan(0,
                "form should be disabled when no project is in context");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_WithoutProjectContext_SeesWarning));
            await page.Context.DisposeAsync();
        }
    }

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"),
            new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        if (page.Url.Contains("/Context/Select"))
        {
            var confirmBtn = page.Locator("[data-mode='page'] [data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 20000 });
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
