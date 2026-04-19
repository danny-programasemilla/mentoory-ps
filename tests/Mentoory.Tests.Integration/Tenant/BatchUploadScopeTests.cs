using FluentAssertions;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Application.Queries.GetIncubatorByExternalId;
using Mentoory.Tenant.Application.Queries.GetProjectByExternalId;
using Mentoory.Tenant.Application.Queries.ListRegistrationProjects;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Tenant;

[Collection(IntegrationTestCollection.Name)]
public class BatchUploadScopeTests : IntegrationTestBase
{
    public BatchUploadScopeTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ListRegistrationProjects_CrossIncubator_ReturnsFailure()
    {
        var alphaId = await CreateIncubatorAsync("Scope Alpha");
        var betaId = await CreateIncubatorAsync("Scope Beta");
        await CreateProjectInIncubatorAsync(alphaId, "Alpha Project");

        var result = await SendWithTenantAsync(
            new ListRegistrationProjectsQuery(alphaId, null, betaId), betaId);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(m => m.Message.Contains("autorización"));
    }

    [Fact]
    public async Task ListRegistrationProjects_SameIncubator_ReturnsProjects()
    {
        var incubatorId = await CreateIncubatorAsync("Same Inc");
        await CreateProjectInIncubatorAsync(incubatorId, "Visible Project");

        var result = await SendWithTenantAsync(
            new ListRegistrationProjectsQuery(incubatorId, null, incubatorId), incubatorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().ContainSingle(p => p.Name == "Visible Project");
    }

    [Fact]
    public async Task ListRegistrationProjects_AuthorizedProjectIds_FiltersCorrectly()
    {
        var incubatorId = await CreateIncubatorAsync("Filter Inc");
        var p1 = await CreateProjectInIncubatorAsync(incubatorId, "Project One");
        await CreateProjectInIncubatorAsync(incubatorId, "Project Two");
        var p3 = await CreateProjectInIncubatorAsync(incubatorId, "Project Three");

        var result = await SendWithTenantAsync(
            new ListRegistrationProjectsQuery(incubatorId, [p1, p3], incubatorId), incubatorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().HaveCount(2);
        result.Value.Projects.Select(p => p.Name).Should()
            .BeEquivalentTo(["Project One", "Project Three"]);
    }

    [Fact]
    public async Task ListRegistrationProjects_EmptyAuthorizedProjectIds_ReturnsEmpty()
    {
        var incubatorId = await CreateIncubatorAsync("Empty Inc");
        await CreateProjectInIncubatorAsync(incubatorId, "Should Not Appear");

        var result = await SendWithTenantAsync(
            new ListRegistrationProjectsQuery(incubatorId, [], incubatorId), incubatorId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectByExternalId_CrossIncubator_ReturnsFailure()
    {
        var alphaId = await CreateIncubatorAsync("Proj Alpha");
        var betaId = await CreateIncubatorAsync("Proj Beta");
        var (_, projectExternalId) = await CreateProjectAsync(alphaId, "Alpha Only");

        var result = await SendWithTenantAsync(
            new GetProjectByExternalIdQuery(projectExternalId, betaId), betaId);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(m => m.Message.Contains("autorización"));
    }

    [Fact]
    public async Task GetIncubatorByExternalId_CrossIncubator_ReturnsFailure()
    {
        var (_, alphaExternalId) = await CreateIncubatorWithExternalIdAsync("Inc Alpha");
        var betaId = await CreateIncubatorAsync("Inc Beta");

        var result = await SendWithTenantAsync(
            new GetIncubatorByExternalIdQuery(alphaExternalId, betaId), betaId);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(m => m.Message.Contains("autorización"));
    }

    private async Task<long> CreateIncubatorAsync(string name)
    {
        var result = await SendAsync(new CreateIncubatorCommand(name, null));
        result.IsSuccess.Should().BeTrue();
        return await GetIncubatorIdAsync(result.Value!);
    }

    private async Task<(long Id, Guid ExternalId)> CreateIncubatorWithExternalIdAsync(string name)
    {
        var result = await SendAsync(new CreateIncubatorCommand(name, null));
        result.IsSuccess.Should().BeTrue();
        var id = await GetIncubatorIdAsync(result.Value!);
        return (id, result.Value!);
    }

    private async Task<long> CreateProjectInIncubatorAsync(long incubatorId, string name)
    {
        var incubatorExternalId = await GetIncubatorExternalIdAsync(incubatorId);
        var result = await SendAsync(new CreateProjectCommand(incubatorExternalId, name, null, Guid.Parse("11111111-1111-1111-1111-111111111111")));
        result.IsSuccess.Should().BeTrue();
        return await GetProjectIdAsync(result.Value!);
    }

    private async Task<(long Id, Guid ExternalId)> CreateProjectAsync(long incubatorId, string name)
    {
        var incubatorExternalId = await GetIncubatorExternalIdAsync(incubatorId);
        var result = await SendAsync(new CreateProjectCommand(incubatorExternalId, name, null, Guid.Parse("11111111-1111-1111-1111-111111111111")));
        result.IsSuccess.Should().BeTrue();
        var id = await GetProjectIdAsync(result.Value!);
        return (id, result.Value!);
    }

    private async Task<long> GetIncubatorIdAsync(Guid externalId)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var incubator = await dbContext.Incubators.FirstAsync(i => i.ExternalId == externalId);
        return incubator.Id;
    }

    private async Task<Guid> GetIncubatorExternalIdAsync(long id)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var incubator = await dbContext.Incubators.FirstAsync(i => i.Id == id);
        return incubator.ExternalId;
    }

    private async Task<long> GetProjectIdAsync(Guid externalId)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var project = await dbContext.Projects.IgnoreQueryFilters()
            .FirstAsync(p => p.ExternalId == externalId);
        return project.Id;
    }
}
