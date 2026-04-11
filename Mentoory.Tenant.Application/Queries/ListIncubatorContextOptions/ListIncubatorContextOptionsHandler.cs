using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Queries.ListIncubatorContextOptions;

/// <summary>
/// Handler that returns all active incubators with their active projects
/// for GlobalAdmin context selection. Uses QueryUnfiltered() to bypass
/// tenant query filters so all projects are visible regardless of current context.
/// </summary>
public class ListIncubatorContextOptionsHandler(
    IIncubatorRepository incubatorRepository,
    IProjectRepository projectRepository)
    : BaseCommandHandler<ListIncubatorContextOptionsQuery, List<IncubatorContextOptionDto>>
{
    /// <inheritdoc />
    public override async Task<Result<List<IncubatorContextOptionDto>>> Handle(
        ListIncubatorContextOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var incubators = await incubatorRepository.ToListAsync(
            incubatorRepository.Query()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .Select(i => new { i.Id, i.Name }),
            cancellationToken);

        var projects = await projectRepository.ToListAsync(
            projectRepository.QueryUnfiltered()
                .Where(p => p.IsActive)
                .Select(p => new { p.Id, p.Name, p.IncubatorId }),
            cancellationToken);

        var projectsByIncubator = projects
            .GroupBy(p => p.IncubatorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = incubators
            .Select(i => new IncubatorContextOptionDto(
                i.Id,
                i.Name,
                projectsByIncubator.TryGetValue(i.Id, out var incubatorProjects)
                    ? incubatorProjects
                        .OrderBy(p => p.Name)
                        .Select(p => new ProjectContextOptionDto(p.Id, p.Name))
                        .ToList()
                    : []))
            .ToList();

        return Success(result);
    }
}
