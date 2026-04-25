using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Commands.AdvanceProjectStage;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class AdvanceProjectStageHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KsTemplateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AdvanceProjectStageHandler _handler;

    public AdvanceProjectStageHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _projectRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new AdvanceProjectStageHandler(
            NullLogger<AdvanceProjectStageHandler>.Instance,
            _projectRepo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_AdvancesProjectAndSaves()
    {
        var project = SeedProject();
        var cmd = new AdvanceProjectStageCommand(project.ExternalId, 5, 10, false);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NewCurrentStageType.Should().Be(StageType.Forms);
        result.Value.NewCurrentStageState.Should().Be(StageState.InProgress);
        _projectRepo.Verify(r => r.Update(project), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        _projectRepo.Setup(r => r.GetByExternalIdWithStagesAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var cmd = new AdvanceProjectStageCommand(missingId, 5, 10, false);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.ProjectNotFound);
        _projectRepo.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonMatchingIncubator_ReturnsOutOfScope()
    {
        var project = SeedProject(incubatorId: 10);
        var cmd = new AdvanceProjectStageCommand(project.ExternalId, 5, 99, false);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.ProjectOutOfScope);
    }

    [Fact]
    public async Task Handle_GlobalAdmin_BypassesIncubatorScopeCheck()
    {
        var project = SeedProject(incubatorId: 10);
        var cmd = new AdvanceProjectStageCommand(project.ExternalId, 5, 0, true);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InactiveProject_ReturnsProjectInactive()
    {
        var project = SeedProject(active: false);
        var cmd = new AdvanceProjectStageCommand(project.ExternalId, 5, 10, false);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.ProjectInactive);
    }

    [Fact]
    public async Task Handle_AtClosure_ReturnsProjectAlreadyClosed()
    {
        // After 6 advances the project is at Closure InProgress. The handler must reject
        // further advancement even though the domain method would mark Closure completed.
        var project = SeedProject(advanceTimes: 6);
        project.CurrentStageType.Should().Be(StageType.Closure);
        project.CurrentStageState.Should().Be(StageState.InProgress);

        var cmd = new AdvanceProjectStageCommand(project.ExternalId, 5, 10, false);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.ProjectAlreadyClosed);
    }

    private Project SeedProject(long incubatorId = 10, bool active = true, int advanceTimes = 0)
    {
        var project = Project.Create(incubatorId, "Test Project", null, KsTemplateId, UtcNow);
        for (var i = 0; i < advanceTimes; i++)
        {
            project.AdvanceStage(1, UtcNow.AddMinutes(i + 1));
        }

        if (!active)
        {
            typeof(Project).GetProperty(nameof(Project.IsActive))!
                .SetValue(project, false);
        }

        _projectRepo.Setup(r => r.GetByExternalIdWithStagesAsync(project.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        return project;
    }
}
