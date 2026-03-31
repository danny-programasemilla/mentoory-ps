using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.SetLeadMentor;

/// <summary>
/// Handler for setting a mentor assignment as lead mentor.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="projectRepository">The repository for accessing project entities.</param>
public partial class SetLeadMentorHandler(
    ILogger<SetLeadMentorHandler> logger,
    IProjectRepository projectRepository)
    : BaseCommandHandler<SetLeadMentorCommand>
{
    /// <inheritdoc />
    public override async Task<Result> Handle(SetLeadMentorCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByExternalIdAsync(request.ProjectExternalId, cancellationToken);

        if (project is null)
        {
            LogProjectNotFound(request.ProjectExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectExternalId), "Project not found"));
        }

        var assignment = project.MentorAssignments
            .SingleOrDefault(ma => ma.ExternalId == request.MentorAssignmentExternalId);

        if (assignment is null)
        {
            LogMentorAssignmentNotFound(request.MentorAssignmentExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.MentorAssignmentExternalId), "Mentor assignment not found"));
        }

        // Remove lead flag from existing lead for same entrepreneur
        var existingLead = project.MentorAssignments
            .SingleOrDefault(ma =>
                ma.EntrepreneurUserId == assignment.EntrepreneurUserId
                && ma.IsLeadMentor
                && ma.IsActive
                && ma.ExternalId != assignment.ExternalId);

        existingLead?.RemoveLeadFlag();

        assignment.SetAsLead();
        projectRepository.Update(project);

        LogLeadMentorSet(request.MentorAssignmentExternalId, request.ProjectExternalId);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with ExternalId {ExternalId}")]
    partial void LogProjectNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mentor assignment not found with ExternalId {ExternalId}")]
    partial void LogMentorAssignmentNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Lead mentor set for assignment {AssignmentExternalId} in project {ProjectExternalId}")]
    partial void LogLeadMentorSet(Guid assignmentExternalId, Guid projectExternalId);
}
