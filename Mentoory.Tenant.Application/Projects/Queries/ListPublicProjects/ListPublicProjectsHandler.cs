using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Projects.Queries.ListPublicProjects;

/// <summary>
/// Handler for listing public projects that are currently in the Registration stage
/// and accepting enrollments. Uses QueryUnfiltered() to bypass tenant filters so
/// all public projects are visible regardless of the user's current context.
/// </summary>
public class ListPublicProjectsHandler(
    IProjectRepository projectRepository,
    IIncubatorRepository incubatorRepository)
    : BaseCommandHandler<ListPublicProjectsQuery, List<PublicProjectDto>>
{
    /// <inheritdoc />
    public override async Task<Result<List<PublicProjectDto>>> Handle(
        ListPublicProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var incubators = await incubatorRepository.ToListAsync(
            incubatorRepository.Query()
                .Where(i => i.IsActive)
                .Select(i => new { i.Id, i.Name }),
            cancellationToken);

        var incubatorNames = incubators.ToDictionary(i => i.Id, i => i.Name);

        var projects = await projectRepository.ToListAsync(
            projectRepository.QueryUnfiltered()
                .Where(p => p.IsPublic
                            && p.CurrentStageType == StageType.Registration
                            && p.CurrentStageState == StageState.InProgress
                            && p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new { p.ExternalId, p.Name, p.Description, p.IncubatorId }),
            cancellationToken);

        var result = projects
            .Where(p => incubatorNames.ContainsKey(p.IncubatorId))
            .Select(p => new PublicProjectDto(
                p.ExternalId,
                p.Name,
                p.Description,
                incubatorNames[p.IncubatorId]))
            .ToList();

        return Success(result);
    }
}
