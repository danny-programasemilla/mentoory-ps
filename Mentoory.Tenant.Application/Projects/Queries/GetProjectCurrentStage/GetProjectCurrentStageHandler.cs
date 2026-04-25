using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectCurrentStage;

public sealed class GetProjectCurrentStageHandler(IProjectRepository projectRepository)
    : BaseCommandHandler<GetProjectCurrentStageQuery, GetProjectCurrentStageResult>
{
    public override async Task<Result<GetProjectCurrentStageResult>> Handle(
        GetProjectCurrentStageQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ProjectExternalId is null && request.ProjectId is null)
        {
            return Failure(ResultErrorCodes.ProjectNotFound,
                (nameof(request.ProjectExternalId), "No project id supplied"));
        }

        var query = projectRepository.Query().AsNoTracking();
        if (request.ProjectExternalId is { } externalId)
        {
            query = query.Where(p => p.ExternalId == externalId);
        }
        else
        {
            query = query.Where(p => p.Id == request.ProjectId!.Value);
        }

        var projection = await query
            .Select(p => new GetProjectCurrentStageResult(
                p.ExternalId,
                p.CurrentStageType,
                p.IsActive,
                p.IncubatorId))
            .SingleOrDefaultAsync(cancellationToken);

        return projection is null
            ? Failure(ResultErrorCodes.ProjectNotFound,
                (nameof(request.ProjectExternalId), "Project not found"))
            : Success(projection);
    }
}
