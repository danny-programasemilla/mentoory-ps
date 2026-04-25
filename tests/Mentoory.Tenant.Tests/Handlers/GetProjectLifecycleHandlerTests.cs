using FluentAssertions;
using Mentoory.Access.Application.StageActions;
using Mentoory.Access.Application.Users;
using Mentoory.Shared.Application;
using Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class GetProjectLifecycleHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KsTemplateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IIncubatorRepository> _incubatorRepo = new();
    private readonly Mock<IUserDirectory> _userDirectory = new();
    private readonly GetProjectLifecycleHandler _handler;

    public GetProjectLifecycleHandlerTests()
    {
        _userDirectory.Setup(d => d.GetDisplayNamesAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, string>());

        _handler = new GetProjectLifecycleHandler(
            NullLogger<GetProjectLifecycleHandler>.Instance,
            _projectRepo.Object,
            _incubatorRepo.Object,
            _userDirectory.Object);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        _projectRepo.Setup(r => r.GetByExternalIdWithStagesAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new GetProjectLifecycleQuery(missingId, 1, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.ProjectNotFound);
    }

    [Fact]
    public async Task Handle_OutOfScope_ReturnsFailure()
    {
        var project = SeedProject(incubatorId: 10);
        var result = await _handler.Handle(new GetProjectLifecycleQuery(project.ExternalId, 99, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.ProjectOutOfScope);
    }

    [Fact]
    public async Task Handle_GlobalAdmin_BypassesScope()
    {
        var project = SeedProject(incubatorId: 10);
        var result = await _handler.Handle(new GetProjectLifecycleQuery(project.ExternalId, 0, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsAllSevenStagesInCanonicalOrder()
    {
        var project = SeedProject();
        var result = await _handler.Handle(new GetProjectLifecycleQuery(project.ExternalId, 10, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Stages.Should().HaveCount(7);
        result.Value.Stages.Select(s => s.StageType).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_CanAdvance_FalseWhenInactive()
    {
        var project = SeedProject();
        typeof(Project).GetProperty(nameof(Project.IsActive))!.SetValue(project, false);

        var result = await _handler.Handle(new GetProjectLifecycleQuery(project.ExternalId, 10, false), CancellationToken.None);

        result.Value!.CanAdvance.Should().BeFalse();
        result.Value.CannotAdvanceReason.Should().Contain("inactivo");
    }

    [Fact]
    public async Task Handle_CanAdvance_FalseAtClosureInProgress()
    {
        var project = SeedProject(advanceTimes: 6);
        project.CurrentStageType.Should().Be(StageType.Closure);

        var result = await _handler.Handle(new GetProjectLifecycleQuery(project.ExternalId, 10, false), CancellationToken.None);

        result.Value!.CanAdvance.Should().BeFalse();
        result.Value.CannotAdvanceReason.Should().Contain("final");
    }

    [Fact]
    public async Task Handle_ReturnsAllRegistryActionsWithState()
    {
        var project = SeedProject();
        var result = await _handler.Handle(new GetProjectLifecycleQuery(project.ExternalId, 10, false), CancellationToken.None);

        result.Value!.Actions.Should().HaveCount(Enum.GetValues<StageGatedAction>().Length);
        // At Registration, every action is locked.
        result.Value.Actions.Should().OnlyContain(a => a.State == StageGatedActionState.Locked);
    }

    private Project SeedProject(long incubatorId = 10, int advanceTimes = 0)
    {
        var project = Project.Create(incubatorId, "Test Project", "Description", KsTemplateId, UtcNow);
        for (var i = 0; i < advanceTimes; i++)
        {
            project.AdvanceStage(1, UtcNow.AddHours(i + 1));
        }

        var incubator = Incubator.Create("Acme Incubator", null, UtcNow);
        typeof(Incubator).GetProperty("Id")!.SetValue(incubator, incubatorId);

        _projectRepo.Setup(r => r.GetByExternalIdWithStagesAsync(project.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _incubatorRepo.Setup(r => r.GetByIdAsync(incubatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        return project;
    }
}
