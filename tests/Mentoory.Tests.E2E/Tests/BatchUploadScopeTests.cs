using System.Text;
using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates batch upload authorization scope: ProjectCoordinators see only assigned projects,
/// server rejects unauthorized submissions, and IncubatorAdmin/GlobalAdmin behavior is preserved.
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
    public async Task ProjectCoordinator_SeesOnlyAssignedProject()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var options = page.Locator("select[name='ProjectExternalId'] option:not([value=''])");
            var count = await options.CountAsync();

            count.Should().Be(1, "coord1 is assigned to exactly one project");

            var optionText = await options.First.TextContentAsync();
            optionText.Should().Contain("Innovación",
                "coord1 is assigned to Proyecto Innovación");

            var fullDropdown = await page.Locator("select[name='ProjectExternalId']").InnerHTMLAsync();
            fullDropdown.Should().NotContain("Sostenibilidad",
                "coord1 must NOT see Proyecto Sostenibilidad (assigned to coord2)");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectCoordinator_SeesOnlyAssignedProject));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectCoordinator2_SeesOnlyTheirProject()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord2@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var options = page.Locator("select[name='ProjectExternalId'] option:not([value=''])");
            var count = await options.CountAsync();

            count.Should().Be(1, "coord2 is assigned to exactly one project");

            var optionText = await options.First.TextContentAsync();
            optionText.Should().Contain("Sostenibilidad",
                "coord2 is assigned to Proyecto Sostenibilidad");

            var fullDropdown = await page.Locator("select[name='ProjectExternalId']").InnerHTMLAsync();
            fullDropdown.Should().NotContain("Innovación",
                "coord2 must NOT see Proyecto Innovación (assigned to coord1)");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectCoordinator2_SeesOnlyTheirProject));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectCoordinator_SubmitToUnauthorizedProject_IsForbidden()
    {
        // Get the ExternalId of a project coord1 is NOT assigned to
        var unauthorizedExternalId = await GetProjectExternalIdAsync("Proyecto Sostenibilidad");

        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Prepare a valid CSV
            var csvBytes = Encoding.UTF8.GetBytes(
                "Country,Identification,Email,FirstName,LastName\nCRI,9-9999-0001,forbid-test@test.mentoory.com,Test,User");

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Inject unauthorized project ExternalId into the select
            await page.EvaluateAsync($@"
                const select = document.querySelector('select[name=""ProjectExternalId""]');
                const opt = document.createElement('option');
                opt.value = '{unauthorizedExternalId}';
                opt.text = 'Injected Unauthorized';
                opt.selected = true;
                select.appendChild(opt);
                select.value = '{unauthorizedExternalId}';
            ");

            // Submit the form
            await page.Locator("button[type='submit']").Filter(
                new LocatorFilterOptions { HasText = "Procesar Archivo" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should be denied — 403 or redirect to AccessDenied
            var denied = page.Url.Contains("/AccessDenied")
                         || page.Url.Contains("/Access/AccessDenied")
                         || page.Url.Contains("/Access/Login");

            denied.Should().BeTrue(
                "submitting a batch upload to an unauthorized project must be rejected");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectCoordinator_SubmitToUnauthorizedProject_IsForbidden));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdmin_SeesAllProjectsInIncubator()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var options = page.Locator("select[name='ProjectExternalId'] option:not([value=''])");
            var count = await options.CountAsync();

            count.Should().BeGreaterThanOrEqualTo(2,
                "IncubatorAdmin should see all registration-stage projects in their incubator");

            var dropdown = await page.Locator("select[name='ProjectExternalId']").InnerHTMLAsync();
            dropdown.Should().Contain("Innovación");
            dropdown.Should().Contain("Sostenibilidad");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_SeesAllProjectsInIncubator));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectCoordinator_ValidUpload_Succeeds()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueEmail = $"e2e-scope-{Guid.NewGuid():N}@test.mentoory.com";
            var csvBytes = Encoding.UTF8.GetBytes(
                $"Country,Identification,Email,FirstName,LastName\nCRI,8-8888-0001,{uniqueEmail},Scope,TestUser");

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Select the only available project (coord1's assigned project)
            var projectSelect = page.Locator("select[name='ProjectExternalId']");
            var firstOption = projectSelect.Locator("option:not([value=''])").First;
            var value = await firstOption.GetAttributeAsync("value");
            value.Should().NotBeNullOrWhiteSpace();
            await projectSelect.SelectOptionAsync(value!);

            await page.Locator("button[type='submit']").Filter(
                new LocatorFilterOptions { HasText = "Procesar Archivo" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Resultados de Carga Masiva",
                "ProjectCoordinator uploading to their assigned project should succeed");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectCoordinator_ValidUpload_Succeeds));
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

    private async Task<Guid> GetProjectExternalIdAsync(string projectName)
    {
        await using var conn = new SqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ExternalId FROM tenant.Projects WHERE Name = @Name";
        cmd.Parameters.AddWithValue("@Name", projectName);
        var result = await cmd.ExecuteScalarAsync();
        result.Should().NotBeNull($"project '{projectName}' must exist in seed data");
        return (Guid)result!;
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
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var roleDropdown = page.Locator("[data-cs='role']");
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var confirmBtn = page.Locator("[data-cs='confirm']");
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
