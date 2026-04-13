using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Queries.GetProjectContextInfo;

public class GetProjectContextInfoHandler(
    IProjectRepository projectRepository,
    IIncubatorRepository incubatorRepository)
    : BaseCommandHandler<GetProjectContextInfoQuery, ProjectContextInfoDto>
{
    public override async Task<Result<ProjectContextInfoDto>> Handle(
        GetProjectContextInfoQuery request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Project", "Proyecto no encontrado."));
        }

        var incubator = await incubatorRepository.GetByIdAsync(project.IncubatorId, cancellationToken);
        if (incubator is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Incubator", "Incubadora no encontrada."));
        }

        return Success(new ProjectContextInfoDto(project.ExternalId, incubator.ExternalId));
    }
}
