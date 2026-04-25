using System.Text;
using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T006 - Validates CSV batch upload, per-row result reporting,
/// and temporary password generation.
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class BatchUploadTests
{
    private readonly PlaywrightFixture _fixture;

    public BatchUploadTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BatchUpload_PageLoads_WithCorrectElements()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert heading (admin pages use h5.card-title)
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Carga Masiva de Usuarios",
                "page should show 'Carga Masiva de Usuarios' heading");

            // Assert file input with CSV accept
            var fileInput = page.Locator("input[type='file'][accept='.csv']");
            (await fileInput.CountAsync()).Should().Be(1,
                "page should have a file input that accepts .csv files");

            // Assert ProjectExternalId field
            (await page.Locator("input[name='ProjectExternalId'], select[name='ProjectExternalId']").CountAsync())
                .Should().BeGreaterThanOrEqualTo(1,
                    "page should have a ProjectExternalId field");

            // Assert submit button
            var submitButton = page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            });
            (await submitButton.CountAsync()).Should().Be(1,
                "page should have a 'Procesar Archivo' submit button");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchUpload_PageLoads_WithCorrectElements));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task BatchUpload_ValidCsv_ShowsResultsPage()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Create CSV content with a unique user
            var uniqueEmail = $"e2e-batch-{Guid.NewGuid():N}@test.mentoory.com";
            var csvContent = $"Country,Identification,Email,FirstName,LastName\nCRI,3-4567-8901,{uniqueEmail},Batch,UserOne";
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            // Upload CSV file
            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Select the first available project from the dropdown
            await SelectFirstProjectOptionAsync(page);

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert results page
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Resultados de Carga Masiva",
                "results page should display 'Resultados de Carga Masiva'");

            // Assert success row exists (results table uses status-success spans inside rows)
            var successRows = page.Locator("table tbody tr:has(.status-success)");
            (await successRows.CountAsync()).Should().BeGreaterThan(0,
                "at least one row should contain a success status indicator");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchUpload_ValidCsv_ShowsResultsPage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task BatchUpload_ResultsPage_ShowsTemporaryPassword()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Create CSV with unique user
            var uniqueEmail = $"e2e-batch-{Guid.NewGuid():N}@test.mentoory.com";
            var csvContent = $"Country,Identification,Email,FirstName,LastName\nCRI,4-5678-9012,{uniqueEmail},Batch,UserTwo";
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Select the first available project from the dropdown
            await SelectFirstProjectOptionAsync(page);

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert temporary password column in success row is non-empty
            var tempPasswordCell = page.Locator("table tbody tr:has(.status-success) td:nth-child(6)");
            var tempPassword = await tempPasswordCell.TextContentAsync();

            tempPassword.Should().NotBeNullOrWhiteSpace(
                "temporary password column should contain a value");
            tempPassword!.Trim().Should().NotBe("\u2014",
                "temporary password should not be a dash placeholder");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchUpload_ResultsPage_ShowsTemporaryPassword));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task BatchUpload_EmptyForm_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Submit without selecting a file (use filtered locator to avoid TopBar's hidden logout submit)
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert validation error appears
            var validationErrors = page.Locator(".text-danger, .validation-summary-errors, .field-validation-error, .alert-danger");
            (await validationErrors.CountAsync()).Should().BeGreaterThan(0,
                "submitting without a file should show a validation error");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchUpload_EmptyForm_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task SelectFirstProjectOptionAsync(IPage page)
    {
        var projectSelect = page.Locator("select[name='ProjectExternalId']");
        // Select the first non-empty option (skip the "Seleccione un proyecto" placeholder)
        var firstOption = projectSelect.Locator("option:not([value=''])").First;
        var value = await firstOption.GetAttributeAsync("value");
        value.Should().NotBeNullOrWhiteSpace("there should be at least one project in Registration stage");
        await projectSelect.SelectOptionAsync(value!);
    }

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for login redirect chain to complete (login -> context -> home)
        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"), new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // If redirected to context selection, pick the first available context
        if (page.Url.Contains("/Context/Select"))
        {
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
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
