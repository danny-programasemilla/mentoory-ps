using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Commands.RenameProjectStage;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Mentoory.Tenant.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class RenameProjectStageHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RenameProjectStageHandler _handler;

    public RenameProjectStageHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _projectRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new RenameProjectStageHandler(
            NullLogger<RenameProjectStageHandler>.Instance,
            _projectRepo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithValidStage_RenamesSuccessfully()
    {
        var project = CreateProjectWithIds();
        var diagnosisStage = project.Stages.First(s => s.StageType == StageType.Diagnosis);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new RenameProjectStageCommand(project.Id, diagnosisStage.ExternalId, "Diagnóstico Inicial");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        diagnosisStage.DisplayName.Should().Be("Diagnóstico Inicial");
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new RenameProjectStageCommand(999, Guid.NewGuid(), "New Name");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentStage_ReturnsFailure()
    {
        var project = CreateProjectWithIds();
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new RenameProjectStageCommand(project.Id, Guid.NewGuid(), "New Name");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    private static Project CreateProjectWithIds()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        var stages = project.Stages.OrderBy(s => s.Position).ToList();
        for (var i = 0; i < stages.Count; i++)
        {
            EntityIdSetter.SetId(stages[i], i + 1);
        }

        return project;
    }
}
