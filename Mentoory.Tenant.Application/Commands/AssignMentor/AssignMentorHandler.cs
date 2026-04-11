using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.AssignMentor;

/// <summary>
/// Handler for assigning a mentor to an entrepreneur in a project.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="projectRepository">The repository for accessing project entities.</param>
/// <param name="timeProvider">The time provider for getting the current UTC time.</param>
public partial class AssignMentorHandler(
    ILogger<AssignMentorHandler> logger,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<AssignMentorCommand, Guid>
{
    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(AssignMentorCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByExternalIdAsync(request.ProjectExternalId, cancellationToken);

        if (project is null)
        {
            LogProjectNotFound(request.ProjectExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectExternalId), "Project not found"));
        }

        var assignment = project.AssignMentor(
            request.MentorUserId,
            request.EntrepreneurUserId,
            request.IsLeadMentor,
            timeProvider.UtcNow);

        projectRepository.Update(project);

        LogMentorAssigned(assignment.ExternalId, request.ProjectExternalId);

        return Success(assignment.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with ExternalId {ExternalId}")]
    partial void LogProjectNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Mentor assigned with ExternalId {ExternalId} in project {ProjectExternalId}")]
    partial void LogMentorAssigned(Guid externalId, Guid projectExternalId);
}
