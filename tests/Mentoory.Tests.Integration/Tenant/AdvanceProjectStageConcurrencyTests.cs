using FluentAssertions;
using MediatR;
using Mentoory.Shared.Application;
using Mentoory.Tenant.Application.Commands.AdvanceProjectStage;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Tenant;

/// <summary>
/// End-to-end rowversion coverage for <see cref="AdvanceProjectStageHandler"/>.
/// Validates that the SSDT ROWVERSION column plus the EF <c>IsRowVersion()</c>
/// mapping (TenantDbContext.ConfigureProject) produces the handler's typed
/// <c>LifecycleConcurrencyConflict</c> failure when two advances race.
/// Complements the mock-based unit test in Mentoory.Tenant.Tests, which proves
/// the handler's catch-and-detach path but cannot prove the schema mapping.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AdvanceProjectStageConcurrencyTests : IntegrationTestBase
{
    public AdvanceProjectStageConcurrencyTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Handle_WhenProjectMovesUnderneathStaleTracker_ReturnsLifecycleConcurrencyConflict()
    {
        // Seed an incubator and a project through the normal command pipeline.
        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Concurrency Inc", null));
        incubatorResult.IsSuccess.Should().BeTrue();

        var projectResult = await SendAsync(new CreateProjectCommand(
            incubatorResult.Value!, "Concurrency Project", null));
        projectResult.IsSuccess.Should().BeTrue();

        var projectExternalId = projectResult.Value!;

        // Scope A tracks the project with its current RowVersion snapshot.
        using var scopeA = CreateScope();
        var dbContextA = scopeA.ServiceProvider.GetRequiredService<TenantDbContext>();
        var trackedProject = await dbContextA.Projects
            .Include(p => p.Stages)
            .FirstAsync(p => p.ExternalId == projectExternalId);
        var initialRowVersion = (byte[])trackedProject.RowVersion.Clone();

        // Scope B (implicit, via SendAsync) advances the project to completion,
        // which bumps the DB rowversion.
        var firstAdvance = await SendAsync(new AdvanceProjectStageCommand(
            projectExternalId,
            ActingUserId: 1,
            ActingUserIncubatorId: 0,
            ActingUserIsGlobalAdmin: true));
        firstAdvance.IsSuccess.Should().BeTrue();

        // Scope A still holds the stale snapshot. Route a second advance through
        // scope A's mediator — the handler's GetByExternalIdWithStagesAsync call
        // resolves to the already-tracked entity via EF's identity map, so the
        // handler operates on the stale rowversion and EF's UPDATE ... WHERE
        // RowVersion = <stale> returns 0 rows, triggering DbUpdateConcurrencyException.
        var mediatorA = scopeA.ServiceProvider.GetRequiredService<IMediator>();
        var staleAdvance = await mediatorA.Send(new AdvanceProjectStageCommand(
            projectExternalId,
            ActingUserId: 2,
            ActingUserIncubatorId: 0,
            ActingUserIsGlobalAdmin: true));

        staleAdvance.IsFailure.Should().BeTrue();
        staleAdvance.ErrorCode.Should().Be(ResultErrorCodes.LifecycleConcurrencyConflict);

        // Sanity: the first advance actually changed the rowversion in the database.
        using var verifyScope = CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var refreshed = await verifyDb.Projects
            .AsNoTracking()
            .FirstAsync(p => p.ExternalId == projectExternalId);
        refreshed.RowVersion.Should().NotEqual(initialRowVersion);
    }
}
