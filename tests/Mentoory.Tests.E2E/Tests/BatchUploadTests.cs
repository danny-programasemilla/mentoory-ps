using System.Text;
using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T006 - Validates CSV batch upload form elements, per-row result reporting,
/// toggle-controlled temporary password generation, validation errors,
/// and session-context enforcement (no project dropdown).
/// </summary>
[Collection(E2ETestCollection.Name)]
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
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert heading
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Carga Masiva de Usuarios",
                "page should show 'Carga Masiva de Usuarios' heading");

            // Assert file input with CSV accept
            var fileInput = page.Locator("input[type='file'][accept='.csv']");
            (await fileInput.CountAsync()).Should().Be(1,
                "page should have a file input that accepts .csv files");

            // Assert toggle switches exist
            var skipEmailToggle = page.Locator("input[type='checkbox'][name='SkipEmailVerification']");
            (await skipEmailToggle.CountAsync()).Should().Be(1,
                "page should have a SkipEmailVerification toggle switch");

            var skipInvitationToggle = page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']");
            (await skipInvitationToggle.CountAsync()).Should().Be(1,
                "page should have a SkipInvitationAcceptance toggle switch");

            // Assert submit button
            var submitButton = page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            });
            (await submitButton.CountAsync()).Should().Be(1,
                "page should have a 'Procesar Archivo' submit button");

            // Assert NO project dropdown (project comes from session context)
            var projectDropdown = page.Locator("select[name='ProjectExternalId']");
            (await projectDropdown.CountAsync()).Should().Be(0,
                "project dropdown should NOT exist; project comes from session context");
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
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Create CSV content with a unique user
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-batch-{uniqueId}@test.mentoory.com";
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

            // Both toggles OFF by default — just submit
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert results page
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Resultados de Carga Masiva",
                "results page should display 'Resultados de Carga Masiva'");

            // Assert at least one Creado row (table-success)
            var successRows = page.Locator("table tbody tr.table-success");
            (await successRows.CountAsync()).Should().BeGreaterThan(0,
                "at least one row should have the table-success class (Creado)");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchUpload_ValidCsv_ShowsResultsPage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task BatchUpload_WithBothTogglesOn_ShowsTemporaryPassword()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Create CSV with unique user
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-batch-{uniqueId}@test.mentoory.com";
            var csvContent = $"Country,Identification,Email,FirstName,LastName\nCRI,4-5678-9012,{uniqueEmail},Batch,UserTwo";
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Turn BOTH toggles ON
            await page.Locator("input[type='checkbox'][name='SkipEmailVerification']").CheckAsync();
            await page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']").CheckAsync();

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert results page loads
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Resultados de Carga Masiva",
                "results page should display 'Resultados de Carga Masiva'");

            // Assert temporary password column (6th column) in success row is non-empty
            var tempPasswordCell = page.Locator("table tbody tr.table-success td:nth-child(6)");
            var tempPassword = await tempPasswordCell.TextContentAsync();

            tempPassword.Should().NotBeNullOrWhiteSpace(
                "temporary password column should contain a value when both toggles are ON");
            tempPassword!.Trim().Should().NotBe("\u2014",
                "temporary password should not be a dash placeholder");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchUpload_WithBothTogglesOn_ShowsTemporaryPassword));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task BatchUpload_EmptyForm_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Submit without selecting a file
            await page.ClickAsync("button[type='submit']");
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

    [Fact]
    public async Task BatchUpload_NoProjectContext_FormDisabled()
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
                "page should show a warning when no project is in context");

            // Assert form fieldset is disabled
            var disabledFieldset = page.Locator("fieldset[disabled]");
            (await disabledFieldset.CountAsync()).Should().BeGreaterThan(0,
                "form fieldset should be disabled when no project is in context");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchUpload_NoProjectContext_FormDisabled));
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
