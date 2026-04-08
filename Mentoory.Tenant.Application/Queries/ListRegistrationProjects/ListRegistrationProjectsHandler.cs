using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Queries.ListRegistrationProjects;

public class ListRegistrationProjectsHandler(
    IProjectRepository projectRepository,
    IIncubatorRepository incubatorRepository)
    : BaseCommandHandler<ListRegistrationProjectsQuery, RegistrationProjectsResult>
{
    public override async Task<Result<RegistrationProjectsResult>> Handle(
        ListRegistrationProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var incubator = await incubatorRepository.GetByIdAsync(
            request.IncubatorId, cancellationToken);

        if (incubator is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.IncubatorId), "Incubadora no encontrada."));
        }

        var projects = await projectRepository.ToListAsync(
            projectRepository.Query()
                .Where(p => p.IsActive
                            && p.CurrentStageType == StageType.Registration
                            && p.CurrentStageState == StageState.InProgress)
                .OrderBy(p => p.Name)
                .Select(p => new RegistrationProjectDto(p.ExternalId, p.Name)),
            cancellationToken);

        return Success(new RegistrationProjectsResult(incubator.ExternalId, projects));
    }
}
