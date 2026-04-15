using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

public partial class AdvanceProjectStageHandler(
    ILogger<AdvanceProjectStageHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<AdvanceProjectStageCommand>
{
    public override async Task<Result> Handle(AdvanceProjectStageCommand request, CancellationToken cancellationToken)
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
            project.AdvanceStage(request.AdvancedByUserId, timeProvider.UtcNow);
            LogStageAdvanced(request.ProjectId);
            return Success();
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                (nameof(request.ProjectId), ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage advanced for project {ProjectId}")]
    partial void LogStageAdvanced(long projectId);
}
