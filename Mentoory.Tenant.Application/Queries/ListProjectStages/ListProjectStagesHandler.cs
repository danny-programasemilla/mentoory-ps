using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.ListProjectStages;

public partial class ListProjectStagesHandler(
    ILogger<ListProjectStagesHandler> logger,
    IProjectRepository repository)
    : BaseCommandHandler<ListProjectStagesQuery, IReadOnlyList<ProjectStageDto>>
{
    public override async Task<Result<IReadOnlyList<ProjectStageDto>>> Handle(
        ListProjectStagesQuery request,
        CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            LogProjectNotFound(request.ProjectId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectId), "Proyecto no encontrado."));
        }

        var stages = project.Stages
            .OrderBy(s => s.Position)
            .Select(s => new ProjectStageDto(
                s.ExternalId,
                s.StageType.ToString(),
                s.State.ToString(),
                s.Position,
                s.DisplayName))
            .ToList();

        return Success((IReadOnlyList<ProjectStageDto>)stages);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);
}
