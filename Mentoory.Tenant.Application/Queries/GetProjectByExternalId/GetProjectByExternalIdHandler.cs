using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Application.Queries.ListProjects;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.GetProjectByExternalId;

/// <summary>
/// Handler for retrieving a single project by its external identifier.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for accessing project entities.</param>
public partial class GetProjectByExternalIdHandler(
    ILogger<GetProjectByExternalIdHandler> logger,
    IProjectRepository repository)
    : BaseCommandHandler<GetProjectByExternalIdQuery, ProjectDto>
{
    /// <inheritdoc />
    public override async Task<Result<ProjectDto>> Handle(
        GetProjectByExternalIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);

        if (project is null)
        {
            LogProjectNotFound(request.ExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ExternalId), "Project not found"));
        }

        var dto = new ProjectDto(
            project.ExternalId,
            project.IncubatorId,
            project.Name,
            project.Description,
            project.CurrentStageType.ToString(),
            project.CurrentStageState.ToString(),
            project.IsActive,
            project.CreatedAtUtc,
            project.UpdatedAtUtc);

        return Success(dto);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with ExternalId {ExternalId}")]
    partial void LogProjectNotFound(Guid externalId);
}
