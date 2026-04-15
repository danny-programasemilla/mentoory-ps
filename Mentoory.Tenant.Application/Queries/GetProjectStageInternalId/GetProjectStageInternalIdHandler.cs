using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.GetProjectStageInternalId;

public partial class GetProjectStageInternalIdHandler(
    ILogger<GetProjectStageInternalIdHandler> logger,
    IProjectRepository projectRepository)
    : BaseCommandHandler<GetProjectStageInternalIdQuery, long>
{
    public override async Task<Result<long>> Handle(
        GetProjectStageInternalIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            LogProjectNotFound(request.ProjectId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectId), "Proyecto no encontrado."));
        }

        var stage = project.Stages.SingleOrDefault(s => s.ExternalId == request.StageExternalId);
        if (stage is null)
        {
            LogStageNotFound(request.StageExternalId, request.ProjectId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.StageExternalId), "Etapa no encontrada."));
        }

        return Success(stage.Id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stage {StageExternalId} not found in project {ProjectId}")]
    partial void LogStageNotFound(Guid stageExternalId, long projectId);
}
