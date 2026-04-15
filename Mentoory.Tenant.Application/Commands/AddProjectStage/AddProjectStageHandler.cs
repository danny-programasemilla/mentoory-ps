using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.AddProjectStage;

public partial class AddProjectStageHandler(
    ILogger<AddProjectStageHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<AddProjectStageCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(AddProjectStageCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            LogProjectNotFound(request.ProjectId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectId), "Proyecto no encontrado."));
        }

        try
        {
            var stage = project.AddStage(request.StageType, request.Position, timeProvider.UtcNow);
            LogStageAdded(stage.ExternalId, request.ProjectId);
            return Success(stage.ExternalId);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                (nameof(request.StageType), ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage {StageExternalId} added to project {ProjectId}")]
    partial void LogStageAdded(Guid stageExternalId, long projectId);
}
