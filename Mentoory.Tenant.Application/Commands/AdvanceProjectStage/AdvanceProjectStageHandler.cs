using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

public partial class AdvanceProjectStageHandler(
    ILogger<AdvanceProjectStageHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<AdvanceProjectStageCommand, AdvanceProjectStageResult>
{
    public override async Task<Result<AdvanceProjectStageResult>> Handle(
        AdvanceProjectStageCommand request,
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
            LogAdvanceOutOfScope(request.ProjectExternalId, request.ActingUserId);
            return Failure(ResultErrorCodes.ProjectOutOfScope,
                (nameof(request.ProjectExternalId), "Project is outside the acting user's scope"));
        }

        if (!project.IsActive)
        {
            return Failure(ResultErrorCodes.ProjectInactive,
                (nameof(request.ProjectExternalId), "Project is inactive"));
        }

        if (project.CurrentStageState != StageState.InProgress)
        {
            return Failure(ResultErrorCodes.StageNotInProgress,
                (nameof(request.ProjectExternalId), "Current stage is not in progress"));
        }

        if (project.CurrentStageType == StageType.Closure)
        {
            return Failure(ResultErrorCodes.ProjectAlreadyClosed,
                (nameof(request.ProjectExternalId), "Project is already at the final stage"));
        }

        var previousStage = project.CurrentStageType;
        project.AdvanceStage(request.ActingUserId, timeProvider.UtcNow);
        projectRepository.Update(project);

        try
        {
            await projectRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Detach so the outer TransactionBehavior's commit SaveEntitiesAsync does not re-issue
            // the same failing UPDATE and mask the typed failure we are about to return.
            projectRepository.Detach(project);
            LogConcurrencyConflict(request.ProjectExternalId, request.ActingUserId);
            return Failure(ResultErrorCodes.LifecycleConcurrencyConflict,
                (nameof(request.ProjectExternalId), "Concurrent modification detected"));
        }

        LogStageAdvanced(request.ProjectExternalId, previousStage, project.CurrentStageType, request.ActingUserId);
        return Success(new AdvanceProjectStageResult(project.CurrentStageType, project.CurrentStageState));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with ExternalId {ExternalId}")]
    partial void LogProjectNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Advance out of scope for project {ExternalId} by user {UserId}")]
    partial void LogAdvanceOutOfScope(Guid externalId, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project {ExternalId} advanced from {From} to {To} by user {UserId}")]
    partial void LogStageAdvanced(Guid externalId, StageType from, StageType to, long userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lifecycle concurrency conflict advancing project {ExternalId} (user {UserId})")]
    partial void LogConcurrencyConflict(Guid externalId, long userId);
}
