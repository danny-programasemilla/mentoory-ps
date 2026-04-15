using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Commands.AddProjectStage;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class AddProjectStageHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AddProjectStageHandler _handler;

    public AddProjectStageHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _projectRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new AddProjectStageHandler(
            NullLogger<AddProjectStageHandler>.Instance,
            _projectRepo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithValidProject_AddsStageAndReturnsExternalId()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new AddProjectStageCommand(project.Id, StageType.Diagnosis, 1);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        project.Stages.Should().HaveCount(6); // 5 default + 1 new
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new AddProjectStageCommand(999, StageType.Diagnosis, 1);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithRegistrationType_ReturnsFailure()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new AddProjectStageCommand(project.Id, StageType.Registration, 1);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AddsStage_ShiftsSubsequentPositions()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Insert Mentorship at position 1 (between Registration and first Diagnosis)
        var command = new AddProjectStageCommand(project.Id, StageType.Mentorship, 1);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var orderedStages = project.Stages.OrderBy(s => s.Position).ToList();
        orderedStages[0].StageType.Should().Be(StageType.Registration);
        orderedStages[1].StageType.Should().Be(StageType.Mentorship);
        orderedStages.Last().StageType.Should().Be(StageType.Closure);
    }
}
