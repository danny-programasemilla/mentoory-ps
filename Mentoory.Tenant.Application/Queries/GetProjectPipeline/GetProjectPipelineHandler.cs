using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.GetProjectPipeline;

public partial class GetProjectPipelineHandler(
    ILogger<GetProjectPipelineHandler> logger,
    IProjectRepository repository)
    : BaseCommandHandler<GetProjectPipelineQuery, ProjectPipelineDto>
{
    public override async Task<Result<ProjectPipelineDto>> Handle(
        GetProjectPipelineQuery request,
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
            .Select(s => new PipelineStageDto(
                s.ExternalId,
                s.StageType.ToString(),
                s.State.ToString(),
                s.Position,
                s.DisplayName,
                s.PlannedStartDate,
                s.PlannedEndDate,
                s.StartedAtUtc,
                s.CompletedAtUtc))
            .ToList();

        var dto = new ProjectPipelineDto(
            project.ExternalId,
            project.Name,
            project.CurrentStageState.ToString(),
            stages);

        return Success(dto);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);
}
