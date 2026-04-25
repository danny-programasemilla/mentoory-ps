using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Commands.AdvanceProjectStage;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

/// <summary>
/// Concurrency-conflict coverage for <see cref="AdvanceProjectStageHandler"/>.
/// Simulates the SQL-Server rowversion mismatch path by having the unit-of-work throw
/// <see cref="DbUpdateConcurrencyException"/> on save. The plan's integration-level concurrency
/// test (see quickstart.md Walkthrough 5) exercises the real RowVersion mapping end-to-end.
/// </summary>
public class AdvanceProjectStageHandlerConcurrencyTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KsTemplateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_WhenSaveThrowsConcurrencyException_ReturnsLifecycleConcurrencyConflict()
    {
        var project = Project.Create(10, "Test", null, KsTemplateId, UtcNow);

        var projectRepo = new Mock<IProjectRepository>();
        var timeProvider = new Mock<ITimeProvider>();
        var unitOfWork = new Mock<IUnitOfWork>();

        timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        projectRepo.Setup(r => r.UnitOfWork).Returns(unitOfWork.Object);
        projectRepo.Setup(r => r.GetByExternalIdWithStagesAsync(project.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("rowversion mismatch"));

        var handler = new AdvanceProjectStageHandler(
            NullLogger<AdvanceProjectStageHandler>.Instance,
            projectRepo.Object,
            timeProvider.Object);

        var cmd = new AdvanceProjectStageCommand(project.ExternalId, 5, 10, false);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.LifecycleConcurrencyConflict);
        projectRepo.Verify(r => r.Detach(project), Times.Once);
    }
}
