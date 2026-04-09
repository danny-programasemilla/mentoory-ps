using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Tenant.Application.Queries.ListRegistrationProjects;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class ListRegistrationProjectsHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IIncubatorRepository> _incubatorRepo = new();

    [Fact]
    public async Task Handle_WithAuthorizedProjectIds_ReturnsOnlyAuthorizedProjects()
    {
        SetupIncubator(1);
        SetupProjects((1, "Alpha"), (2, "Beta"), (3, "Gamma"), (4, "Delta"), (5, "Epsilon"));

        var handler = CreateHandler();
        var query = new ListRegistrationProjectsQuery(1, [1, 3], 1L);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().HaveCount(2);
        result.Value.Projects.Select(p => p.Name).Should().BeEquivalentTo(["Alpha", "Gamma"]);
    }

    [Fact]
    public async Task Handle_WithEmptyAuthorizedProjectIds_ReturnsEmptyList()
    {
        SetupIncubator(1);
        SetupProjects((1, "Alpha"), (2, "Beta"), (3, "Gamma"));

        var handler = CreateHandler();
        var query = new ListRegistrationProjectsQuery(1, [], 1L);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNullAuthorizedProjectIds_ReturnsAllProjects()
    {
        SetupIncubator(1);
        SetupProjects((1, "Alpha"), (2, "Beta"), (3, "Gamma"), (4, "Delta"), (5, "Epsilon"));

        var handler = CreateHandler();
        var query = new ListRegistrationProjectsQuery(1, null, 1L);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithMismatchedIncubatorId_ReturnsFailure()
    {
        var handler = CreateHandler();
        var query = new ListRegistrationProjectsQuery(1, null, 2L);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().Contain(m => m.Message.Contains("autorización"));
    }

    [Fact]
    public async Task Handle_WithAuthorizedProjectIds_FiltersCorrectly()
    {
        SetupIncubator(1);
        SetupProjects((10, "Project10"), (20, "Project20"), (30, "Project30"));

        var handler = CreateHandler();
        var query = new ListRegistrationProjectsQuery(1, [20], 1L);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().HaveCount(1);
        result.Value.Projects[0].Name.Should().Be("Project20");
    }

    [Fact]
    public async Task Handle_WithNullCallerIncubatorId_SkipsIncubatorValidation()
    {
        SetupIncubator(1);
        SetupProjects((1, "Alpha"), (2, "Beta"));

        var handler = CreateHandler();
        var query = new ListRegistrationProjectsQuery(1, null, null);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().HaveCount(2);
    }

    private static List<Project> CreateProjects(long incubatorId, params (long Id, string Name)[] projects)
    {
        var result = new List<Project>();
        foreach (var (id, name) in projects)
        {
            var project = Project.Create(incubatorId, name, null, UtcNow);
            typeof(Mentoory.Shared.Domain.SeedWork.Entity)
                .GetProperty(nameof(Mentoory.Shared.Domain.SeedWork.Entity.Id))!
                .SetValue(project, id);
            result.Add(project);
        }

        return result;
    }

    private static Incubator CreateIncubator(long id = 1)
    {
        var incubator = Incubator.Create("Test Incubator", null, UtcNow);
        typeof(Mentoory.Shared.Domain.SeedWork.Entity)
            .GetProperty(nameof(Mentoory.Shared.Domain.SeedWork.Entity.Id))!
            .SetValue(incubator, id);
        return incubator;
    }

    private ListRegistrationProjectsHandler CreateHandler() =>
        new(_projectRepo.Object, _incubatorRepo.Object);

    private void SetupIncubator(long incubatorId)
    {
        var incubator = CreateIncubator(incubatorId);
        _incubatorRepo.Setup(r => r.GetByIdAsync(incubatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);
    }

    private void SetupProjects(params (long Id, string Name)[] projects)
    {
        var allProjects = CreateProjects(1, projects);

        _projectRepo.Setup(r => r.Query()).Returns(allProjects.AsQueryable());
        _projectRepo.Setup(r => r.ToListAsync(
                It.IsAny<IQueryable<RegistrationProjectDto>>(),
                It.IsAny<CancellationToken>()))
            .Returns((IQueryable<RegistrationProjectDto> q, CancellationToken _) =>
                Task.FromResult(q.ToList()));
    }
}
