using FluentAssertions;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Tenant;

[Collection(IntegrationTestCollection.Name)]
public class CrossTenantIsolationTests : IntegrationTestBase
{
    public CrossTenantIsolationTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateProject_ScopedToCorrectIncubator()
    {
        // Arrange
        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Scoped Inc", "Test incubator"));
        incubatorResult.IsSuccess.Should().BeTrue();

        // Act
        var projectResult = await SendAsync(new CreateProjectCommand(
            incubatorResult.Value!, "My Project", "A test project"));

        // Assert
        projectResult.IsSuccess.Should().BeTrue();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var project = await dbContext.Projects.IgnoreQueryFilters()
            .Include(p => p.Stages)
            .FirstAsync(p => p.ExternalId == projectResult.Value!);

        project.Name.Should().Be("My Project");
        project.Stages.Should().HaveCount(7);

        var incubator = await dbContext.Incubators.FirstAsync(i => i.ExternalId == incubatorResult.Value!);
        project.IncubatorId.Should().Be(incubator.Id);
    }

    [Fact]
    public async Task Projects_BelongToCorrectIncubator_DataIsolation()
    {
        // Arrange — create two incubators with one project each
        var inc1Result = await SendAsync(new CreateIncubatorCommand("Incubator Alpha", null));
        var inc2Result = await SendAsync(new CreateIncubatorCommand("Incubator Beta", null));

        await SendAsync(new CreateProjectCommand(inc1Result.Value!, "Alpha Project", null));
        await SendAsync(new CreateProjectCommand(inc2Result.Value!, "Beta Project", null));

        // Get incubator IDs
        var inc1Id = await GetIncubatorIdAsync(inc1Result.Value!);
        var inc2Id = await GetIncubatorIdAsync(inc2Result.Value!);

        // Act — query all projects and verify data-level tenant scoping
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var allProjects = await dbContext.Projects.IgnoreQueryFilters().ToListAsync();

        // Assert — each project belongs to its correct incubator
        var alphaProject = allProjects.Single(p => p.Name == "Alpha Project");
        alphaProject.IncubatorId.Should().Be(inc1Id);

        var betaProject = allProjects.Single(p => p.Name == "Beta Project");
        betaProject.IncubatorId.Should().Be(inc2Id);

        // Verify no cross-contamination
        allProjects.Where(p => p.IncubatorId == inc1Id)
            .Should().OnlyContain(p => p.Name == "Alpha Project");
        allProjects.Where(p => p.IncubatorId == inc2Id)
            .Should().OnlyContain(p => p.Name == "Beta Project");
    }

    [Fact]
    public async Task DifferentIncubators_HaveIndependentProjects()
    {
        // Arrange — three incubators, projects in first two
        var inc1Result = await SendAsync(new CreateIncubatorCommand("Isolated A", null));
        var inc2Result = await SendAsync(new CreateIncubatorCommand("Isolated B", null));
        var inc3Result = await SendAsync(new CreateIncubatorCommand("Isolated C (empty)", null));

        await SendAsync(new CreateProjectCommand(inc1Result.Value!, "Project A1", null));
        await SendAsync(new CreateProjectCommand(inc1Result.Value!, "Project A2", null));
        await SendAsync(new CreateProjectCommand(inc2Result.Value!, "Project B1", null));

        var inc1Id = await GetIncubatorIdAsync(inc1Result.Value!);
        var inc2Id = await GetIncubatorIdAsync(inc2Result.Value!);
        var inc3Id = await GetIncubatorIdAsync(inc3Result.Value!);

        // Act — query all projects bypassing query filter
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var allProjects = await dbContext.Projects.IgnoreQueryFilters().ToListAsync();

        // Assert — verify data isolation at the IncubatorId level
        allProjects.Where(p => p.IncubatorId == inc1Id).Should().HaveCount(2);
        allProjects.Where(p => p.IncubatorId == inc2Id).Should().HaveCount(1);
        allProjects.Where(p => p.IncubatorId == inc3Id).Should().BeEmpty();
    }

    [Fact]
    public async Task QueryFilter_ReturnsAllProjects_WhenTenantContextIsNull()
    {
        // Arrange
        var inc1Result = await SendAsync(new CreateIncubatorCommand("All-View A", null));
        var inc2Result = await SendAsync(new CreateIncubatorCommand("All-View B", null));

        await SendAsync(new CreateProjectCommand(inc1Result.Value!, "View A Project", null));
        await SendAsync(new CreateProjectCommand(inc2Result.Value!, "View B Project", null));

        // Act — default tenant context is null (no tenant set), so query filter passes all
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var projects = await dbContext.Projects.ToListAsync();

        // Assert — null tenant context means the filter returns all projects
        projects.Count.Should().BeGreaterThanOrEqualTo(2);
        projects.Should().Contain(p => p.Name == "View A Project");
        projects.Should().Contain(p => p.Name == "View B Project");
    }

    private async Task<long> GetIncubatorIdAsync(Guid externalId)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var incubator = await dbContext.Incubators.FirstAsync(i => i.ExternalId == externalId);
        return incubator.Id;
    }
}
