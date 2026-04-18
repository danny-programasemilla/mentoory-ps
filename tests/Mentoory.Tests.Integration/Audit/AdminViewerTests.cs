using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.Queries.Audit;
using Mentoory.Tests.Integration.Fixtures;
using Xunit;

namespace Mentoory.Tests.Integration.Audit;

/// <summary>
/// Exercises <see cref="GetAuditLogPagedQuery"/> against the real database to verify
/// that the admin viewer's query surface paginates, filters, and sorts correctly.
/// HTTP-level authorization (GlobalAdmin-only) is covered by the
/// <c>[Authorize(Roles = "GlobalAdmin")]</c> attribute on the controller; a
/// full HTTP round-trip test is deferred pending an authenticated-client fixture.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AdminViewerTests : IntegrationTestBase
{
    public AdminViewerTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAuditLogPaged_ReturnsRowsProducedByAuditedCommand()
    {
        await SendAsync(new RegisterUserCommand(
            "viewer-user@example.com", "CO", "700700700", "Viewer", "Query", "SecureP@ss123!"));

        var response = await SendAsync(new GetAuditLogPagedQuery(
            new DataTableRequest(1, 0, 10, "occurredAtUtc", "desc", null, null)));

        response.IsSuccess.Should().BeTrue();
        response.Value!.RecordsTotal.Should().BeGreaterThan(0);
        response.Value.Data.Should()
            .Contain(r => r.EventType == AuditEventTypes.UserRegistered
                       && r.UserEmail == "viewer-user@example.com");
    }

    [Fact]
    public async Task GetAuditLogPaged_FiltersByEventType()
    {
        await SendAsync(new RegisterUserCommand(
            "viewer-filter@example.com", "CO", "800800800", "Viewer", "Filter", "SecureP@ss123!"));

        var filters = new Dictionary<string, string>
        {
            ["eventType"] = AuditEventTypes.UserRegistered,
        };

        var response = await SendAsync(new GetAuditLogPagedQuery(
            new DataTableRequest(1, 0, 50, "occurredAtUtc", "desc", null, filters)));

        response.IsSuccess.Should().BeTrue();
        response.Value!.Data.Should().OnlyContain(r => r.EventType == AuditEventTypes.UserRegistered);
    }

    [Fact]
    public async Task GetAuditLogPaged_FiltersByOutcomeAndUserEmail()
    {
        await SendAsync(new RegisterUserCommand(
            "viewer-outcome@example.com", "CO", "900900900", "Viewer", "Outcome", "SecureP@ss123!"));

        var filters = new Dictionary<string, string>
        {
            ["outcome"] = "Success",
            ["userEmail"] = "viewer-outcome@example.com",
        };

        var response = await SendAsync(new GetAuditLogPagedQuery(
            new DataTableRequest(1, 0, 10, "occurredAtUtc", "desc", null, filters)));

        response.IsSuccess.Should().BeTrue();
        response.Value!.Data.Should()
            .OnlyContain(r => r.Outcome == "Success" && r.UserEmail == "viewer-outcome@example.com");
    }

    [Fact]
    public async Task GetAuditLogPaged_RejectsInvalidPageSize()
    {
        var response = await SendAsync(new GetAuditLogPagedQuery(
            new DataTableRequest(1, 0, 10_000, "occurredAtUtc", "desc", null, null)));

        response.IsFailure.Should().BeTrue("Length > 100 must fail validation");
    }

    [Fact]
    public async Task GetAuditLogPaged_RejectsUnknownOutcome()
    {
        var filters = new Dictionary<string, string> { ["outcome"] = "Partial" };

        var response = await SendAsync(new GetAuditLogPagedQuery(
            new DataTableRequest(1, 0, 10, "occurredAtUtc", "desc", null, filters)));

        response.IsFailure.Should().BeTrue();
    }
}
