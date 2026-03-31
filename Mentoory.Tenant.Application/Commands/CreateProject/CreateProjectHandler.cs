using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.CreateProject;

/// <summary>
/// Handler for creating a new project within an incubator.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="incubatorRepository">The repository for accessing incubator entities.</param>
/// <param name="projectRepository">The repository for persisting project entities.</param>
/// <param name="timeProvider">The time provider for getting the current UTC time.</param>
public partial class CreateProjectHandler(
    ILogger<CreateProjectHandler> logger,
    IIncubatorRepository incubatorRepository,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<CreateProjectCommand, Guid>
{
    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var incubator = await incubatorRepository.GetByExternalIdAsync(request.IncubatorExternalId, cancellationToken);

        if (incubator is null)
        {
            LogIncubatorNotFound(request.IncubatorExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.IncubatorExternalId), "Incubator not found"));
        }

        var project = Project.Create(incubator.Id, request.Name, request.Description, timeProvider.UtcNow);
        projectRepository.Add(project);

        LogProjectCreated(project.ExternalId, request.IncubatorExternalId);

        return Success(project.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Incubator not found with ExternalId {ExternalId}")]
    partial void LogIncubatorNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project created with ExternalId {ExternalId} in incubator {IncubatorExternalId}")]
    partial void LogProjectCreated(Guid externalId, Guid incubatorExternalId);
}
