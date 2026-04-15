using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.ReorderProjectStages;

public partial class ReorderProjectStagesHandler(
    ILogger<ReorderProjectStagesHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<ReorderProjectStagesCommand>
{
    public override async Task<Result> Handle(ReorderProjectStagesCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            LogProjectNotFound(request.ProjectId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectId), "Proyecto no encontrado."));
        }

        // Map ExternalIds to internal Ids
        var stageMap = project.Stages.ToDictionary(s => s.ExternalId, s => s.Id);
        var orderedIds = new List<long>(request.OrderedStageExternalIds.Count);

        foreach (var externalId in request.OrderedStageExternalIds)
        {
            if (!stageMap.TryGetValue(externalId, out var id))
            {
                return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                    (nameof(request.OrderedStageExternalIds), $"Etapa con ID {externalId} no encontrada."));
            }

            orderedIds.Add(id);
        }

        try
        {
            project.ReorderStages(orderedIds, timeProvider.UtcNow);
            LogStagesReordered(request.ProjectId);
            return Success();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                (nameof(request.OrderedStageExternalIds), ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stages reordered for project {ProjectId}")]
    partial void LogStagesReordered(long projectId);
}
