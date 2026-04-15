using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.ResolveProjectExternalId;

/// <summary>
/// Resolves a project's external identifier from its internal identifier.
/// </summary>
public partial class ResolveProjectExternalIdHandler(
    ILogger<ResolveProjectExternalIdHandler> logger,
    IProjectRepository repository)
    : BaseCommandHandler<ResolveProjectExternalIdQuery, Guid>
{
    public override async Task<Result<Guid>> Handle(
        ResolveProjectExternalIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            LogProjectNotFound(request.ProjectId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectId), "Proyecto no encontrado."));
        }

        return Success(project.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found with Id {ProjectId}")]
    partial void LogProjectNotFound(long projectId);
}
