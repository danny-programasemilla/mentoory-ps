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
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

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
    public async Task Handle_AdvancesFromRegistrationToDiagnosis()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new AdvanceProjectStageCommand(project.Id, 100);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.CurrentStageType.Should().Be(StageType.Diagnosis);
        project.CurrentStageState.Should().Be(StageState.InProgress);
    }

    [Fact]
    public async Task Handle_AdvancesToClosure_CompletesProject()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Advance through all stages: Reg→Diag1→Mentor→Diag2→Closure
        for (var i = 0; i < 4; i++)
        {
            project.AdvanceStage(100, UtcNow);
        }

        // Now at Closure stage InProgress — advance to complete
        var command = new AdvanceProjectStageCommand(project.Id, 100);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.CurrentStageState.Should().Be(StageState.Completed);
    }

    [Fact]
    public async Task Handle_WhenAlreadyCompleted_ReturnsFailure()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Complete all stages
        for (var i = 0; i < 5; i++)
        {
            project.AdvanceStage(100, UtcNow);
        }

        var command = new AdvanceProjectStageCommand(project.Id, 100);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new AdvanceProjectStageCommand(999, 100);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
