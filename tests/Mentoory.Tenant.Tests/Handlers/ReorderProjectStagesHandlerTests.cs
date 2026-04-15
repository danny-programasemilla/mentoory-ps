using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Commands.ReorderProjectStages;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Mentoory.Tenant.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class ReorderProjectStagesHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ReorderProjectStagesHandler _handler;

    public ReorderProjectStagesHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _projectRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new ReorderProjectStagesHandler(
            NullLogger<ReorderProjectStagesHandler>.Instance,
            _projectRepo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithValidOrder_ReordersStages()
    {
        var project = CreateProjectWithIds();
        var ordered = project.Stages.OrderBy(s => s.Position).ToList();
        // Swap the two Diagnosis stages (positions 1 and 3)
        var reordered = new List<Guid>
        {
            ordered[0].ExternalId, // Registration stays first
            ordered[3].ExternalId, // Diagnosis 2 -> position 1
            ordered[2].ExternalId, // Mentorship -> position 2
            ordered[1].ExternalId, // Diagnosis 1 -> position 3
            ordered[4].ExternalId, // Closure stays last
        };

        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new ReorderProjectStagesCommand(project.Id, reordered);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ReturnsFailure()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new ReorderProjectStagesCommand(999, [Guid.NewGuid()]);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithClosureNotLast_ReturnsFailure()
    {
        var project = CreateProjectWithIds();
        var ordered = project.Stages.OrderBy(s => s.Position).ToList();
        // Put Closure in the middle
        var reordered = new List<Guid>
        {
            ordered[0].ExternalId, // Registration
            ordered[4].ExternalId, // Closure in wrong position
            ordered[1].ExternalId,
            ordered[2].ExternalId,
            ordered[3].ExternalId,
        };

        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new ReorderProjectStagesCommand(project.Id, reordered);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidStageIds_ReturnsFailure()
    {
        var project = CreateProjectWithIds();
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new ReorderProjectStagesCommand(project.Id, [Guid.NewGuid()]);
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
