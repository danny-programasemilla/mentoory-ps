using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Phase 4 (US4) — defensive regressions. No plaintext password ever leaks into Details,
/// every Spanish UI label stays pinned, Outcome badges render with the correct color class,
/// and validator-rejected commands produce zero audit rows (pipeline order
/// Validator → Auditing → Transaction).
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AuditLogRegressionTests : E2ETestBase
{
    private const string GlobalAdminEmail = "admin@mentoory.com";
    private const string GlobalAdminPassword = "123abc987";
    private const string MentorEmail = "mentor1@test.mentoory.com";
    private const string AuditLogPath = "/Administration/AuditLog";
    private const string AuditDataEndpoint = "/Administration/AuditLog/Data";

    public AuditLogRegressionTests(PlaywrightFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task NoPlaintextPasswordAppearsInAnyDetails()
    {
        const string password = "SecureP@ss123!";
        var email = $"e2e-regression-{Guid.NewGuid():N}@test.local";
        var nationalIdSuffix = Guid.NewGuid().ToString("N")[..8];
        var nationalId = $"1-{nationalIdSuffix[..4]}-{nationalIdSuffix[4..8]}";

        var registerPage = await Fixture.CreatePageAsync();
        try
        {
            await RegisterUserViaUiAsync(registerPage, email, nationalId, password);
            registerPage.Url.Should().Contain("/Access/Register/Success");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(registerPage, nameof(NoPlaintextPasswordAppearsInAnyDetails));
            await registerPage.Context.DisposeAsync();
        }

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            var expandButtons = viewerPage.Locator("#auditLogTable .audit-expand");
            var buttonCount = await expandButtons.CountAsync();
            buttonCount.Should().BeGreaterThan(0,
                "the viewer must surface the freshly seeded audit rows");

            for (var i = 0; i < buttonCount; i++)
            {
                await expandButtons.Nth(i).ClickAsync();
            }

            var detailPanels = viewerPage.Locator("#auditLogTable tbody pre");
            await Assertions.Expect(detailPanels).ToHaveCountAsync(buttonCount,
                new() { Timeout = 15000 });

            var texts = await detailPanels.AllInnerTextsAsync();
            var combined = string.Join("\n", texts);

            combined.Should().Contain("***REDACTED***",
                "at least one audit row must carry the redacted-password marker (proves the scan ran)");
            combined.Should().NotContain(password,
                "the plaintext password must not appear in any Details panel under any circumstance");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(NoPlaintextPasswordAppearsInAnyDetails));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task FilterBarButtons_HaveSpanishLabels()
    {
        var page = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            var filterToggle = page.Locator("#auditLogTable-filter-toggle");
            await Assertions.Expect(filterToggle).ToBeVisibleAsync(new() { Timeout = 15000 });
            await filterToggle.ClickAsync();

            var filterForm = page.Locator("#auditLogTable-filter-form");
            await Assertions.Expect(filterForm).ToBeVisibleAsync(new() { Timeout = 15000 });

            var submitText = (await filterForm.Locator("button[type='submit']").InnerTextAsync()).Trim();
            submitText.Should().Be(AuditSpanishCopy.FiltrarButton);

            var clearText = (await filterForm.Locator(".filter-clear-btn").InnerTextAsync()).Trim();
            clearText.Should().Be(AuditSpanishCopy.LimpiarButton);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(FilterBarButtons_HaveSpanishLabels));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task PaginationControls_HaveSpanishLabels()
    {
        var page = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            var paging = page.Locator("#auditLogTable_wrapper .dt-paging");
            await Assertions.Expect(paging).ToBeVisibleAsync(new() { Timeout = 15000 });

            var pagingText = await paging.InnerTextAsync();

            pagingText.Should().Contain(AuditSpanishCopy.Primero,
                "DataTables' full_numbers pagination must render the 'Primero' label");
            pagingText.Should().Contain(AuditSpanishCopy.Anterior,
                "DataTables pagination must render the 'Anterior' label");
            pagingText.Should().Contain(AuditSpanishCopy.Siguiente,
                "DataTables pagination must render the 'Siguiente' label");
            pagingText.Should().Contain(AuditSpanishCopy.Ultimo,
                "DataTables' full_numbers pagination must render the 'Ultimo' label");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(PaginationControls_HaveSpanishLabels));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task OutcomeBadges_RenderSpanishTextAndColor()
    {
        // Arrange one failure row (invalid login) first, then the GlobalAdmin viewer session
        // will itself seed two success rows (User.LoggedIn + Context.Activated).
        await AttemptInvalidLoginAsync(MentorEmail, "WrongP@ss!");

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            var successBadge = viewerPage.Locator(
                "#auditLogTable tbody tr:not(.child) td .status.status-success").First;
            await Assertions.Expect(successBadge).ToBeVisibleAsync(new() { Timeout = 15000 });
            (await successBadge.InnerTextAsync()).Trim()
                .Should().Be(AuditSpanishCopy.ExitoOption,
                    "success rows must render with Spanish 'Éxito' text");

            var failureBadge = viewerPage.Locator(
                "#auditLogTable tbody tr:not(.child) td .status.status-danger").First;
            await Assertions.Expect(failureBadge).ToBeVisibleAsync(new() { Timeout = 15000 });
            (await failureBadge.InnerTextAsync()).Trim()
                .Should().Be(AuditSpanishCopy.FalloOption,
                    "failure rows must render with Spanish 'Fallo' text");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(OutcomeBadges_RenderSpanishTextAndColor));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ValidationRejectedCommand_ProducesNoAuditRow()
    {
        // The email is intentionally malformed so either the ViewModel's [EmailAddress]
        // DataAnnotation or the command's FluentValidation rejects before MediatR reaches
        // the AuditingBehavior. Either path proves the invariant: validator-rejected
        // commands never produce an audit row.
        var invalidEmail = $"not-an-email-{Guid.NewGuid():N}";
        var nationalIdSuffix = Guid.NewGuid().ToString("N")[..8];
        var nationalId = $"1-{nationalIdSuffix[..4]}-{nationalIdSuffix[4..8]}";

        var registerPage = await Fixture.CreatePageAsync();
        try
        {
            await registerPage.GotoAsync($"{Fixture.BaseUrl}/Access/Register");
            await registerPage.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });

            // Bypass HTML5 constraint validation so the malformed email reaches the server.
            // Bundle 019: the hardened registration view (016-registration-access-hardening)
            // emits jQuery-unobtrusive `data-val-*` attributes that block the submit on the
            // client. Strip the validator's binding so the malformed POST hits the controller
            // and surfaces the generic 'No fue posible completar el registro.' banner.
            await registerPage.EvaluateAsync(@"() => {
                const form = document.querySelector('form');
                form.noValidate = true;
                if (window.jQuery && window.jQuery.fn.validate) {
                    const $form = window.jQuery(form);
                    $form.removeData('validator');
                    $form.removeData('unobtrusiveValidation');
                    form.querySelectorAll('[data-val=\""true\""]').forEach(el => {
                        el.removeAttribute('data-val');
                    });
                }
            }");

            await registerPage.FillAsync("input[name='Email']", invalidEmail);
            await registerPage.FillAsync("input[name='FirstName']", "Test");
            await registerPage.FillAsync("input[name='LastName']", "User");
            await registerPage.SelectOptionAsync("select[name='Country']", "CRI");
            await registerPage.FillAsync("input[name='NationalId']", nationalId);
            await registerPage.FillAsync("input[name='Password']", "SecureP@ss123!");
            await registerPage.FillAsync("input[name='ConfirmPassword']", "SecureP@ss123!");
            await registerPage.ClickAsync("button[type='submit']");
            await registerPage.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });

            registerPage.Url.Should().Contain("/Access/Register",
                "validation rejection must keep the user on the Register page, not redirect to Success");

            // Bundle 019: feature 016-registration-access-hardening clears ModelState on
            // validation failure and renders a generic 'No fue posible...' banner instead of
            // per-field text-danger spans (anti-enumeration). The point of this test —
            // confirming the form rejected the submission so AuditingBehavior never ran — is
            // preserved by the banner; we just assert against the new surface.
            var genericError = registerPage.Locator(".alert.alert-danger").Filter(
                new() { HasTextString = "No fue posible completar el registro" });
            await Assertions.Expect(genericError).ToBeVisibleAsync(new() { Timeout = 15000 });
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(registerPage, nameof(ValidationRejectedCommand_ProducesNoAuditRow));
            await registerPage.Context.DisposeAsync();
        }

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            await ApplyFilterByUserEmailAsync(viewerPage, invalidEmail);

            var rowCount = await viewerPage.Locator("#auditLogTable tbody tr:not(.child)").CountAsync();
            var dataRowCount = await CountDataRowsAsync(viewerPage);

            dataRowCount.Should().Be(0,
                "validation must short-circuit BEFORE AuditingBehavior runs — no row may be written");
            rowCount.Should().BeGreaterThan(0,
                "the viewer always renders at least the empty-state row (sanity check: the filter actually applied)");

            var tbodyText = await viewerPage.Locator("#auditLogTable tbody").InnerTextAsync();
            tbodyText.Should().Contain(AuditSpanishCopy.EmptyState,
                "filtering by the invalid email must yield the Spanish empty-state copy");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(ValidationRejectedCommand_ProducesNoAuditRow));
            await viewerPage.Context.DisposeAsync();
        }
    }

    private static async Task ApplyFilterByUserEmailAsync(IPage page, string userEmail)
    {
        var filterToggle = page.Locator("#auditLogTable-filter-toggle");
        await Assertions.Expect(filterToggle).ToBeVisibleAsync(new() { Timeout = 15000 });
        await filterToggle.ClickAsync();

        var filterForm = page.Locator("#auditLogTable-filter-form");
        await Assertions.Expect(filterForm).ToBeVisibleAsync(new() { Timeout = 15000 });

        await filterForm.Locator("input[name='userEmail']").FillAsync(userEmail);

        var ajaxTask = page.WaitForResponseAsync(
            r => r.Url.Contains(AuditDataEndpoint) && r.Status == 200,
            new() { Timeout = 15000 });
        await filterForm.Locator("button[type='submit']").ClickAsync();
        await ajaxTask;
    }

    private static async Task<int> CountDataRowsAsync(IPage page)
    {
        var rows = await page.Locator("#auditLogTable tbody tr:not(.child)").AllAsync();
        var count = 0;
        foreach (var row in rows)
        {
            var cells = await row.Locator("td").CountAsync();
            // The empty-state row is a single colspan'd <td>; data rows have the full 7 columns.
            if (cells >= 6)
            {
                count++;
            }
        }

        return count;
    }

    private async Task<IPage> LoginAsGlobalAdminAndOpenViewerAsync()
    {
        var page = await Fixture.CreatePageAsync();
        await LoginHelper.LoginAsync(page, GlobalAdminEmail, GlobalAdminPassword, Fixture.BaseUrl);

        var dataTask = page.WaitForResponseAsync(
            r => r.Url.Contains(AuditDataEndpoint) && r.Status == 200,
            new() { Timeout = 15000 });
        await page.GotoAsync($"{Fixture.BaseUrl}{AuditLogPath}");
        await dataTask;

        await page.WaitForSelectorAsync("#auditLogTable tbody", new() { Timeout = 15000 });
        return page;
    }

    private async Task AttemptInvalidLoginAsync(string email, string wrongPassword)
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{Fixture.BaseUrl}/Access/Login");
            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='Password']", wrongPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }

    private async Task RegisterUserViaUiAsync(IPage page, string email, string nationalId, string password)
    {
        await page.GotoAsync($"{Fixture.BaseUrl}/Access/Register");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });

        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='FirstName']", "Test");
        await page.FillAsync("input[name='LastName']", "User");
        await page.SelectOptionAsync("select[name='Country']", "CRI");
        await page.FillAsync("input[name='NationalId']", nationalId);
        await page.FillAsync("input[name='Password']", password);
        await page.FillAsync("input[name='ConfirmPassword']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });
    }
}
