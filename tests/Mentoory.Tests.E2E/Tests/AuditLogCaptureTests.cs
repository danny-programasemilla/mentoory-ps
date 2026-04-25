using System.Text.Json;
using FluentAssertions;
using MediatR;
using Mentoory.Access.Application.Commands.AssignRole;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Phase 2 (US2) — UI-driven audit capture flows. Registration, login (success + failure),
/// role assignment, and answer correction each produce a correctly-shaped audit row with
/// the expected Outcome and (for Registration) password redaction in Details.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AuditLogCaptureTests : E2ETestBase
{
    private const string GlobalAdminEmail = "admin@mentoory.com";
    private const string GlobalAdminPassword = "123abc987";
    private const string MentorEmail = "mentor1@test.mentoory.com";
    private const string CoordinatorEmail = "coord1@test.mentoory.com";
    private const string SeededPassword = "Test123!@#";
    private const string AuditLogPath = "/Administration/AuditLog";
    private const string AuditDataEndpoint = "/Administration/AuditLog/Data";

    public AuditLogCaptureTests(PlaywrightFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task RegisterUser_ProducesAuditRow_WithRedactedPassword()
    {
        var email = $"e2e-register-{Guid.NewGuid():N}@test.local";
        const string password = "SecureP@ss123!";
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
            await Fixture.TakeScreenshotOnFailureAsync(registerPage, nameof(RegisterUser_ProducesAuditRow_WithRedactedPassword));
            await registerPage.Context.DisposeAsync();
        }

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            await ApplyFiltersAsync(viewerPage, userEmail: email);
            var rows = await ReadDataRowsAsync(viewerPage);
            rows.Should().HaveCount(1, "exactly one User.Registered row must be recorded for the new user");

            var row = rows[0];
            row[1].Trim().Should().Be(AuditEventTypes.UserRegistered);
            row[2].Trim().Should().Be(email);
            row[4].Should().Contain(AuditSpanishCopy.ExitoOption);

            var detailText = await ExpandFirstRowAndReadDetailsAsync(viewerPage);
            detailText.Should().Contain("***REDACTED***",
                "the redactor must mask the Password field in the audit payload");
            detailText.Should().NotContain(password,
                "the plaintext password must not leak into the audit log under any circumstance");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(RegisterUser_ProducesAuditRow_WithRedactedPassword));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task LoginWithInvalidPassword_ProducesFailureRow()
    {
        const string email = GlobalAdminEmail;
        const string wrongPassword = "WrongP@ss!";

        var loginPage = await Fixture.CreatePageAsync();
        try
        {
            await loginPage.GotoAsync($"{Fixture.BaseUrl}/Access/Login");
            await loginPage.FillAsync("input[name='Email']", email);
            await loginPage.FillAsync("input[name='Password']", wrongPassword);
            await loginPage.ClickAsync("button[type='submit']");
            await loginPage.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });
            loginPage.Url.Should().Contain("/Access/Login", "bad credentials keep the user on the login page");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(loginPage, nameof(LoginWithInvalidPassword_ProducesFailureRow));
            await loginPage.Context.DisposeAsync();
        }

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            await ApplyFiltersAsync(viewerPage,
                userEmail: email,
                eventType: AuditEventTypes.UserLoggedIn,
                outcome: "Failure");

            var rows = await ReadDataRowsAsync(viewerPage);
            rows.Should().HaveCount(1,
                "exactly one User.LoggedIn/Failure row must be recorded for the bad login attempt");

            var row = rows[0];
            row[1].Trim().Should().Be(AuditEventTypes.UserLoggedIn);
            row[2].Trim().Should().Be(email);
            row[4].Should().Contain(AuditSpanishCopy.FalloOption);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(LoginWithInvalidPassword_ProducesFailureRow));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task LoginWithValidPassword_ProducesSuccessRow()
    {
        const string email = MentorEmail;

        var loginPage = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(loginPage, email, SeededPassword, Fixture.BaseUrl);
            loginPage.Url.Should().NotContain("/Access/Login",
                "successful login must redirect off the login page");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(loginPage, nameof(LoginWithValidPassword_ProducesSuccessRow));
            await loginPage.Context.DisposeAsync();
        }

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            await ApplyFiltersAsync(viewerPage, userEmail: email, eventType: AuditEventTypes.UserLoggedIn);
            var rows = await ReadDataRowsAsync(viewerPage);
            rows.Should().HaveCount(1,
                "exactly one User.LoggedIn/Success row must be recorded for the mentor login");

            var row = rows[0];
            row[1].Trim().Should().Be(AuditEventTypes.UserLoggedIn);
            row[2].Trim().Should().Be(email);
            row[4].Should().Contain(AuditSpanishCopy.ExitoOption);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(LoginWithValidPassword_ProducesSuccessRow));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task AssignRole_ProducesAuditRow()
    {
        // No public UI dispatches AssignRoleCommand — FR-017 forbids adding one. We drive
        // the command through the real MediatR pipeline (AuditingBehavior still runs) and
        // then verify the viewer surfaces the row. UserEmail on the row will be null since
        // the dispatch has no ITenantContext; assertions focus on EventType / Action / Outcome.
        using var scope = Fixture.Services.CreateScope();
        var accessCtx = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var tenantCtx = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        var targetUser = await accessCtx.Users.AsNoTracking()
            .FirstAsync(u => u.Email.NormalizedValue == "SPONSOR1@TEST.MENTOORY.COM");
        var incubator = await tenantCtx.Incubators.AsNoTracking()
            .FirstAsync(i => i.Name == "Incubadora Alpha");

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(
            new AssignRoleCommand(targetUser.Id, incubator.Id, null, Roles.Mentor));
        result.IsSuccess.Should().BeTrue(
            "AssignRoleCommand must succeed for a role the user does not already hold");

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            await ApplyFiltersAsync(viewerPage, eventType: AuditEventTypes.RoleAssigned);
            var rows = await ReadDataRowsAsync(viewerPage);
            rows.Should().HaveCount(1,
                "exactly one Role.Assigned row must be recorded from the MediatR dispatch");

            var row = rows[0];
            row[1].Trim().Should().Be(AuditEventTypes.RoleAssigned);
            row[3].Trim().Should().Be(nameof(AssignRoleCommand));
            row[4].Should().Contain(AuditSpanishCopy.ExitoOption);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(AssignRole_ProducesAuditRow));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CorrectAnswer_ProducesRowWithBeforeAndAfter()
    {
        const string originalAnswer = "OriginalAnswer";
        const string fixedAnswer = "FixedAnswer";

        var (responseExternalId, questionResponseId) = await SeedDiagnosticResponseAsync(originalAnswer);

        await SubmitAnswerCorrectionViaUiAsync(responseExternalId, questionResponseId, fixedAnswer);

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            await ApplyFiltersAsync(viewerPage, eventType: AuditEventTypes.AnswerCorrected);
            var rows = await ReadDataRowsAsync(viewerPage);
            rows.Should().NotBeEmpty("the corrected answer must produce at least one audit row");

            var detailText = await ExpandFirstRowAndReadDetailsAsync(viewerPage);
            detailText.Should().Contain(originalAnswer,
                "Manual-mode CorrectAnswer records the previous TextValue under Before");
            detailText.Should().Contain(fixedAnswer,
                "Manual-mode CorrectAnswer records the NewTextValue under After");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(CorrectAnswer_ProducesRowWithBeforeAndAfter));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CorrectAnswer_ProducesExactlyOneRow_NoDoubleWrite()
    {
        const string originalAnswer = "OriginalAnswer";
        const string fixedAnswer = "FixedAnswer";

        var (responseExternalId, questionResponseId) = await SeedDiagnosticResponseAsync(originalAnswer);

        await SubmitAnswerCorrectionViaUiAsync(responseExternalId, questionResponseId, fixedAnswer);

        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            await ApplyFiltersAsync(viewerPage, eventType: AuditEventTypes.AnswerCorrected);
            var rows = await ReadDataRowsAsync(viewerPage);
            rows.Should().HaveCount(1,
                "Manual-mode commands must write exactly one audit row — the handler, not the behavior");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(CorrectAnswer_ProducesExactlyOneRow_NoDoubleWrite));
            await viewerPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ExpandButton_RevealsPrettyPrintedDetails()
    {
        // Any audited command seeds the viewer. The GlobalAdmin login itself produces a
        // User.LoggedIn row plus a Context.Activated row, which is sufficient for this test.
        var viewerPage = await LoginAsGlobalAdminAndOpenViewerAsync();
        try
        {
            var rowCount = await viewerPage.Locator("#auditLogTable tbody tr:not(.dt-empty):not(.child)").CountAsync();
            rowCount.Should().BeGreaterThan(0,
                "the GlobalAdmin login flow itself must have seeded at least one audit row");

            var detailText = await ExpandFirstRowAndReadDetailsAsync(viewerPage);

            using var doc = JsonDocument.Parse(detailText);
            doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object,
                "the Details payload must be a JSON object");
            detailText.Should().Contain("\n",
                "pretty-printing must introduce newlines (JSON.stringify(value, null, 2))");
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(viewerPage, nameof(ExpandButton_RevealsPrettyPrintedDetails));
            await viewerPage.Context.DisposeAsync();
        }
    }

    private static async Task ApplyFiltersAsync(
        IPage page,
        string? userEmail = null,
        string? eventType = null,
        string? outcome = null)
    {
        var filterToggle = page.Locator("#auditLogTable-filter-toggle");
        await Assertions.Expect(filterToggle).ToBeVisibleAsync(new() { Timeout = 15000 });
        await filterToggle.ClickAsync();

        var filterForm = page.Locator("#auditLogTable-filter-form");
        await Assertions.Expect(filterForm).ToBeVisibleAsync(new() { Timeout = 15000 });

        if (userEmail is not null)
        {
            await filterForm.Locator("input[name='userEmail']").FillAsync(userEmail);
        }

        if (eventType is not null)
        {
            await filterForm.Locator("select[name='eventType']").SelectOptionAsync(eventType);
        }

        if (outcome is not null)
        {
            await filterForm.Locator("select[name='outcome']").SelectOptionAsync(outcome);
        }

        var ajaxTask = page.WaitForResponseAsync(
            r => r.Url.Contains(AuditDataEndpoint) && r.Status == 200,
            new() { Timeout = 15000 });
        await filterForm.Locator("button[type='submit']").ClickAsync();
        await ajaxTask;
    }

    private static async Task<IReadOnlyList<IReadOnlyList<string>>> ReadDataRowsAsync(IPage page)
    {
        var rowLocators = await page.Locator("#auditLogTable tbody tr:not(.child)").AllAsync();
        var data = new List<IReadOnlyList<string>>();
        foreach (var row in rowLocators)
        {
            var cells = await row.Locator("td").AllInnerTextsAsync();
            // Empty-state row is a single colspan'd <td> — skip it
            if (cells.Count < 6)
            {
                continue;
            }

            data.Add(cells);
        }

        return data;
    }

    private static async Task<string> ExpandFirstRowAndReadDetailsAsync(IPage page)
    {
        var expandButton = page.Locator("#auditLogTable .audit-expand").First;
        await Assertions.Expect(expandButton).ToBeVisibleAsync(new() { Timeout = 15000 });
        await expandButton.ClickAsync();

        // DataTables inserts a child <tr> with a nested <pre> via row.child(html).show().
        // The parent row gains a display class; the child pre is the unique <pre> under tbody.
        var detailPre = page.Locator("#auditLogTable tbody pre").First;
        await Assertions.Expect(detailPre).ToBeVisibleAsync(new() { Timeout = 15000 });
        return await detailPre.InnerTextAsync();
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

    private async Task<(Guid ResponseExternalId, long QuestionResponseId)> SeedDiagnosticResponseAsync(
        string originalAnswer)
    {
        using var scope = Fixture.Services.CreateScope();
        var tenantCtx = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var diagnosticCtx = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

        var project = await tenantCtx.Projects.AsNoTracking()
            .FirstAsync(p => p.Name == "Proyecto Innovación");

        // Bundle 019: feature 016-project-lifecycle-finish gates AnswerCorrection
        // (StageGatedAction.AnswerCorrection → StageType.Analysis). The seeded "Proyecto
        // Innovación" starts at Registration, so the controller's [RequiresStage] would short-
        // circuit before the test ever sees the correction modal. Advance the seeded project
        // to Analysis idempotently. The helper is a no-op once the project is past Analysis,
        // so back-to-back tests in the collection are safe.
        if (project.CurrentStageType < StageType.Analysis)
        {
            var accessCtx = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
            var coordUserId = await accessCtx.Users.AsNoTracking()
                .Where(u => u.Email.NormalizedValue == CoordinatorEmail.ToUpperInvariant())
                .Select(u => u.Id)
                .FirstAsync();
            await KnowledgeIntegrationHelpers.AdvanceProjectToStageAsync(
                Fixture, project.ExternalId, coordUserId, project.IncubatorId, StageType.Analysis);
        }

        var template = FormTemplate.Create($"AuditE2E-{Guid.NewGuid():N}", null, null, DateTime.UtcNow);
        template.AddQuestion(1, "Q1", QuestionType.Text, StageApplicability.Both, 1, null, false);
        diagnosticCtx.FormTemplates.Add(template);
        await diagnosticCtx.SaveChangesAsync();

        var form = ProjectForm.CloneFromTemplate(template, project.Id, project.IncubatorId, DateTime.UtcNow);
        diagnosticCtx.ProjectForms.Add(form);
        await diagnosticCtx.SaveChangesAsync();

        var persistedForm = await diagnosticCtx.ProjectForms
            .Include(f => f.Questions)
            .FirstAsync(f => f.Id == form.Id);
        var questionId = persistedForm.Questions.First().Id;

        var response = DiagnosticResponse.Create(
            form.Id, project.Id, project.IncubatorId, 100, EvaluationStage.Initial, DateTime.UtcNow);
        response.AddResponse(questionId, originalAnswer, null, null, DateTime.UtcNow);
        diagnosticCtx.DiagnosticResponses.Add(response);
        await diagnosticCtx.SaveChangesAsync();

        return (response.ExternalId, response.QuestionResponses.First().Id);
    }

    private async Task SubmitAnswerCorrectionViaUiAsync(
        Guid responseExternalId,
        long questionResponseId,
        string fixedAnswer)
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, CoordinatorEmail, SeededPassword, Fixture.BaseUrl);
            await page.GotoAsync($"{Fixture.BaseUrl}/Coordination/AnswerCorrection/{responseExternalId}");

            var openModalButton = page.Locator($"button[data-bs-target='#correctModal-{questionResponseId}']");
            await Assertions.Expect(openModalButton).ToBeVisibleAsync(new() { Timeout = 15000 });
            await openModalButton.ClickAsync();

            var modal = page.Locator($"#correctModal-{questionResponseId}");
            await Assertions.Expect(modal).ToBeVisibleAsync(new() { Timeout = 15000 });

            await modal.Locator("textarea[name='newTextValue']").FillAsync(fixedAnswer);
            await modal.Locator("textarea[name='reason']").FillAsync("E2E test correction");

            var submitTask = page.WaitForResponseAsync(
                r => r.Url.Contains("/Coordination/AnswerCorrection/Correct"),
                new() { Timeout = 15000 });
            await modal.Locator("button[type='submit']").ClickAsync();
            await submitTask;
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(SubmitAnswerCorrectionViaUiAsync));
            await page.Context.DisposeAsync();
        }
    }
}
