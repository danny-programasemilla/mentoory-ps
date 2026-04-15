using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.RenameProjectStage;

public partial class RenameProjectStageHandler(
    ILogger<RenameProjectStageHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<RenameProjectStageCommand>
{
    public override async Task<Result> Handle(RenameProjectStageCommand request, CancellationToken cancellationToken)
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
            project.RenameStage(stage.Id, request.DisplayName, timeProvider.UtcNow);
            LogStageRenamed(request.StageExternalId, request.DisplayName);
            return Success();
        }
        catch (ArgumentException ex)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                (nameof(request.DisplayName), ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage {StageExternalId} renamed to '{DisplayName}'")]
    partial void LogStageRenamed(Guid stageExternalId, string displayName);
}
