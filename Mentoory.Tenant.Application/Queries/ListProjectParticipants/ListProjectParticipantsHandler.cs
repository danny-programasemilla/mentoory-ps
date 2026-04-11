using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.ListProjectParticipants;

/// <summary>
/// Handler for listing participants of a project.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for accessing project entities.</param>
public partial class ListProjectParticipantsHandler(
    ILogger<ListProjectParticipantsHandler> logger,
    IProjectRepository repository)
    : BaseCommandHandler<ListProjectParticipantsQuery, List<ProjectParticipantDto>>
{
    /// <inheritdoc />
    public override async Task<Result<List<ProjectParticipantDto>>> Handle(
        ListProjectParticipantsQuery request,
        CancellationToken cancellationToken)
    {
        var project = await repository.GetByExternalIdWithParticipantsAsync(
            request.ProjectExternalId, cancellationToken);

        if (project is null)
        {
            LogProjectNotFound(request.ProjectExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectExternalId), "Project not found"));
        }

        var participants = project.Participants
            .Select(p => new ProjectParticipantDto(
                p.ExternalId,
                p.UserId,
                p.Role,
                p.IsActive,
                p.EnrolledAtUtc))
            .ToList();

        LogParticipantsListed(participants.Count, request.ProjectExternalId);

        return Success(participants);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with ExternalId {ExternalId}")]
    partial void LogProjectNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} participants for project {ProjectExternalId}")]
    partial void LogParticipantsListed(int count, Guid projectExternalId);
}
