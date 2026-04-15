using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.RemoveProjectStage;

public partial class RemoveProjectStageHandler(
    ILogger<RemoveProjectStageHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<RemoveProjectStageCommand>
{
    public override async Task<Result> Handle(RemoveProjectStageCommand request, CancellationToken cancellationToken)
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
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.StageExternalId), "Etapa no encontrada."));
        }

        try
        {
            project.RemoveStage(stage.Id, timeProvider.UtcNow);
            LogStageRemoved(request.StageExternalId, request.ProjectId);
            return Success();
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                (nameof(request.StageExternalId), ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage {StageExternalId} removed from project {ProjectId}")]
    partial void LogStageRemoved(Guid stageExternalId, long projectId);
}
