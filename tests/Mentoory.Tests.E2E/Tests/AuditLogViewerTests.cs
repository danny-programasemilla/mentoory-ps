using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Phase 1 (US1) — admin audit-log viewer renders for GlobalAdmin, is denied to every lower
/// role, and exposes the correct Spanish shell + menu visibility.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AuditLogViewerTests : E2ETestBase
{
    private const string GlobalAdminEmail = "admin@mentoory.com";
    private const string GlobalAdminPassword = "123abc987";
    private const string IncubatorAdminEmail = "incadmin1@test.mentoory.com";
    private const string EntrepreneurEmail = "entrepreneur1@test.mentoory.com";
    private const string MentorEmail = "mentor1@test.mentoory.com";
    private const string SponsorEmail = "sponsor1@test.mentoory.com";
    private const string SeededPassword = "Test123!@#";
    private const string AuditLogPath = "/Administration/AuditLog";

    public AuditLogViewerTests(PlaywrightFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GlobalAdmin_CanOpenAuditLog_TableRenders()
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, GlobalAdminEmail, GlobalAdminPassword, Fixture.BaseUrl);

            var response = await page.GotoAsync($"{Fixture.BaseUrl}{AuditLogPath}");

            response!.Status.Should().Be(200, "GlobalAdmin must access the audit log viewer");

            await page.WaitForSelectorAsync("#auditLogTable thead th", new() { Timeout = 15000 });

            // Use text content (source DOM text) rather than inner text — the rendered
            // headers carry a CSS text-transform: uppercase that changes "Fecha (UTC)"
            // to "FECHA (UTC)" visually but leaves the underlying string intact.
            var headerTexts = await page.Locator("#auditLogTable thead th").AllTextContentsAsync();
            var trimmed = headerTexts.Select(t => t.Trim()).ToList();

            trimmed.Should().Contain(AuditSpanishCopy.FechaUtcHeader);
            trimmed.Should().Contain(AuditSpanishCopy.EventoHeader);
            trimmed.Should().Contain(AuditSpanishCopy.UsuarioHeader);
            trimmed.Should().Contain(AuditSpanishCopy.AccionHeader);
            trimmed.Should().Contain(AuditSpanishCopy.ResultadoHeader);
            trimmed.Should().Contain(AuditSpanishCopy.RolHeader);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdmin_CanOpenAuditLog_TableRenders));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public Task IncubatorAdmin_IsDenied()
        => AssertAuditLogIsDenied(IncubatorAdminEmail, SeededPassword, nameof(IncubatorAdmin_IsDenied));

    [Fact]
    public Task Entrepreneur_IsDenied()
        => AssertAuditLogIsDenied(EntrepreneurEmail, SeededPassword, nameof(Entrepreneur_IsDenied));

    [Fact]
    public Task Mentor_IsDenied()
        => AssertAuditLogIsDenied(MentorEmail, SeededPassword, nameof(Mentor_IsDenied));

    [Fact]
    public Task Sponsor_IsDenied()
        => AssertAuditLogIsDenied(SponsorEmail, SeededPassword, nameof(Sponsor_IsDenied));

    [Fact]
    public async Task GlobalAdmin_SeesMenuEntryUnderPlataforma()
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, GlobalAdminEmail, GlobalAdminPassword, Fixture.BaseUrl);
            await page.GotoAsync($"{Fixture.BaseUrl}/");
            await page.WaitForSelectorAsync("#sidebar", new() { Timeout = 15000 });

            var plataformaGroup = page.Locator(
                "#sidebar li.nav-item.dropdown:has(span.nav-link-title:text-is(\"Plataforma\"))");
            await Assertions.Expect(plataformaGroup).ToHaveCountAsync(1,
                new() { Timeout = 15000 });

            var auditEntry = plataformaGroup.Locator($"a.dropdown-item[href='{AuditLogPath}']");
            await Assertions.Expect(auditEntry).ToHaveCountAsync(1,
                new() { Timeout = 15000 });
            await Assertions.Expect(auditEntry).ToContainTextAsync(
                AuditSpanishCopy.RegistroDeAuditoriaMenu,
                new() { Timeout = 15000 });
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdmin_SeesMenuEntryUnderPlataforma));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdmin_DoesNotSeeMenuEntry()
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, IncubatorAdminEmail, SeededPassword, Fixture.BaseUrl);
            await page.GotoAsync($"{Fixture.BaseUrl}/");
            await page.WaitForSelectorAsync("#sidebar", new() { Timeout = 15000 });

            var auditEntry = page.Locator($"#sidebar a[href='{AuditLogPath}']");
            (await auditEntry.CountAsync()).Should().Be(0,
                "IncubatorAdmin must not see the audit log menu entry");

            var menuText = await page.Locator("#sidebar").InnerTextAsync();
            menuText.Should().NotContain(AuditSpanishCopy.RegistroDeAuditoriaMenu,
                "IncubatorAdmin must not see the audit log menu copy");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_DoesNotSeeMenuEntry));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task FilterWithNoMatch_ShowsSpanishEmptyState()
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, GlobalAdminEmail, GlobalAdminPassword, Fixture.BaseUrl);
            await page.GotoAsync($"{Fixture.BaseUrl}{AuditLogPath}");

            var filterToggle = page.Locator("#auditLogTable-filter-toggle");
            await Assertions.Expect(filterToggle).ToBeVisibleAsync(new() { Timeout = 15000 });
            await filterToggle.ClickAsync();

            var filterForm = page.Locator("#auditLogTable-filter-form");
            await Assertions.Expect(filterForm).ToBeVisibleAsync(new() { Timeout = 15000 });

            var noMatchEmail = $"no-match-{Guid.NewGuid():N}@example.invalid";
            await filterForm.Locator("input[name='userEmail']").FillAsync(noMatchEmail);

            var ajaxResponseTask = page.WaitForResponseAsync(
                resp => resp.Url.Contains("/Administration/AuditLog/Data") && resp.Status == 200,
                new() { Timeout = 15000 });

            await filterForm.Locator("button[type='submit']").ClickAsync();
            await ajaxResponseTask;

            await page.WaitForFunctionAsync(
                "() => { var t = document.querySelector('#auditLogTable tbody'); return t && t.innerText.indexOf('No se encontraron resultados') !== -1; }",
                null,
                new() { Timeout = 15000 });

            var tbodyText = await page.Locator("#auditLogTable tbody").InnerTextAsync();
            tbodyText.Should().Contain(AuditSpanishCopy.EmptyState);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(FilterWithNoMatch_ShowsSpanishEmptyState));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task OutcomeDropdown_HasSpanishOptions()
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, GlobalAdminEmail, GlobalAdminPassword, Fixture.BaseUrl);
            await page.GotoAsync($"{Fixture.BaseUrl}{AuditLogPath}");

            var filterToggle = page.Locator("#auditLogTable-filter-toggle");
            await Assertions.Expect(filterToggle).ToBeVisibleAsync(new() { Timeout = 15000 });
            await filterToggle.ClickAsync();

            var outcomeSelect = page.Locator("select[name='outcome']");
            await Assertions.Expect(outcomeSelect).ToBeVisibleAsync(new() { Timeout = 15000 });

            var options = await outcomeSelect.Locator("option").AllInnerTextsAsync();
            var trimmed = options.Select(o => o.Trim()).ToArray();

            trimmed.Should().Equal(
                AuditSpanishCopy.TodosOption,
                AuditSpanishCopy.ExitoOption,
                AuditSpanishCopy.FalloOption);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(OutcomeDropdown_HasSpanishOptions));
            await page.Context.DisposeAsync();
        }
    }

    private async Task AssertAuditLogIsDenied(string email, string password, string testName)
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, email, password, Fixture.BaseUrl);

            var response = await page.GotoAsync($"{Fixture.BaseUrl}{AuditLogPath}");

            var denied = response?.Status == 403
                         || page.Url.Contains("/Access/Login")
                         || page.Url.Contains("/AccessDenied")
                         || page.Url.Contains("/Access/AccessDenied");

            denied.Should().BeTrue(
                $"role for {email} must not have access to /Administration/AuditLog (status={response?.Status}, url={page.Url})");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, testName);
            await page.Context.DisposeAsync();
        }
    }
}
