using Mentoory.Access.Application.StageActions;
using Mentoory.Access.Application.Users;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;

public partial class GetProjectLifecycleHandler(
    ILogger<GetProjectLifecycleHandler> logger,
    IProjectRepository projectRepository,
    IIncubatorRepository incubatorRepository,
    IUserDirectory userDirectory)
    : BaseCommandHandler<GetProjectLifecycleQuery, ProjectLifecycleDto>
{
    private static readonly StageType[] OrderedStages = Enum.GetValues<StageType>().OrderBy(s => (int)s).ToArray();
    private static readonly StageGatedAction[] AllActions = Enum.GetValues<StageGatedAction>();

    public override async Task<Result<ProjectLifecycleDto>> Handle(
        GetProjectLifecycleQuery request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByExternalIdWithStagesAsync(request.ProjectExternalId, cancellationToken);
        if (project is null)
        {
            LogProjectNotFound(request.ProjectExternalId);
            return Failure(ResultErrorCodes.ProjectNotFound,
                (nameof(request.ProjectExternalId), "Project not found"));
        }

        if (!request.ActingUserIsGlobalAdmin && project.IncubatorId != request.ActingUserIncubatorId)
        {
            LogLifecycleOutOfScope(request.ProjectExternalId);
            return Failure(ResultErrorCodes.ProjectOutOfScope,
                (nameof(request.ProjectExternalId), "Project is outside the acting user's scope"));
        }

        var incubator = await incubatorRepository.GetByIdAsync(project.IncubatorId, cancellationToken);
        var userNames = await LoadAdvancedByDisplayNamesAsync(project, cancellationToken);
        var stagesByType = project.Stages.ToDictionary(s => s.StageType);

        var stages = OrderedStages
            .Select(stageType => MapStage(stagesByType[stageType], userNames))
            .ToList();

        var actions = AllActions
            .Select(action => MapAction(action, project.CurrentStageType))
            .ToList();

        var (canAdvance, cannotAdvanceReason) = ResolveAdvanceAffordance(project);

        return Success(new ProjectLifecycleDto(
            project.ExternalId,
            project.Name,
            project.Description,
            incubator?.ExternalId ?? Guid.Empty,
            incubator?.Name ?? string.Empty,
            project.IsActive,
            project.CurrentStageType,
            project.CurrentStageState,
            canAdvance,
            cannotAdvanceReason,
            stages,
            actions));
    }

    private static ProjectLifecycleStageDto MapStage(
        ProjectStage stage,
        IReadOnlyDictionary<long, string> userNames)
    {
        string? advancedByDisplay = null;
        if (stage.AdvancedByUserId.HasValue &&
            userNames.TryGetValue(stage.AdvancedByUserId.Value, out var name))
        {
            advancedByDisplay = name;
        }

        return new ProjectLifecycleStageDto(
            stage.StageType,
            StageTypeDisplay.ToSpanish(stage.StageType),
            stage.State,
            stage.StartedAtUtc,
            stage.CompletedAtUtc,
            advancedByDisplay);
    }

    private static StageActionDto MapAction(StageGatedAction action, StageType currentStage)
    {
        var gatingStage = StageActionRegistry.GetGatingStage(action);
        return new StageActionDto(
            action,
            StageActionDisplay.ToSpanish(action),
            gatingStage,
            StageTypeDisplay.ToSpanish(gatingStage),
            StageActionRegistry.GetState(currentStage, action),
            StageActionLinks.Resolve(action));
    }

    private static (bool CanAdvance, string? Reason) ResolveAdvanceAffordance(Project project)
    {
        if (!project.IsActive)
        {
            return (false, "El proyecto está inactivo.");
        }

        if (project.CurrentStageState != StageState.InProgress)
        {
            return (false, "La etapa actual no está en progreso.");
        }

        if (project.CurrentStageType == StageType.Closure)
        {
            return (false, "El proyecto ya está en la etapa final (Cierre).");
        }

        return (true, null);
    }

    private async Task<IReadOnlyDictionary<long, string>> LoadAdvancedByDisplayNamesAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        var userIds = project.Stages
            .Where(s => s.AdvancedByUserId.HasValue)
            .Select(s => s.AdvancedByUserId!.Value)
            .Distinct()
            .ToArray();

        return userIds.Length == 0
            ? new Dictionary<long, string>()
            : await userDirectory.GetDisplayNamesAsync(userIds, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with ExternalId {ExternalId}")]
    partial void LogProjectNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lifecycle query out of scope for project {ExternalId}")]
    partial void LogLifecycleOutOfScope(Guid externalId);
}
