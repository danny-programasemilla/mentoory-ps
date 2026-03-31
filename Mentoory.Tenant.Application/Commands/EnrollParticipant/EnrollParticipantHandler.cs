using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.EnrollParticipant;

/// <summary>
/// Handler for enrolling a participant in a project.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="projectRepository">The repository for accessing project entities.</param>
/// <param name="timeProvider">The time provider for getting the current UTC time.</param>
public partial class EnrollParticipantHandler(
    ILogger<EnrollParticipantHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<EnrollParticipantCommand, Guid>
{
    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(EnrollParticipantCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByExternalIdAsync(request.ProjectExternalId, cancellationToken);

        if (project is null)
        {
            LogProjectNotFound(request.ProjectExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectExternalId), "Project not found"));
        }

        var participant = project.EnrollParticipant(request.UserId, request.Role, timeProvider.UtcNow);
        projectRepository.Update(project);

        LogParticipantEnrolled(participant.ExternalId, request.ProjectExternalId);

        return Success(participant.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with ExternalId {ExternalId}")]
    partial void LogProjectNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Participant enrolled with ExternalId {ExternalId} in project {ProjectExternalId}")]
    partial void LogParticipantEnrolled(Guid externalId, Guid projectExternalId);
}
