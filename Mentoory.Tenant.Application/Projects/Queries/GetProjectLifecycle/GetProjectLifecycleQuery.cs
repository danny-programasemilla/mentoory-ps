using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;

public sealed record GetProjectLifecycleQuery(
    Guid ProjectExternalId,
    long ActingUserIncubatorId,
    bool ActingUserIsGlobalAdmin)
    : IBaseRequest<ProjectLifecycleDto>;
