using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Application.Queries.ListProjects;

namespace Mentoory.Tenant.Application.Queries.GetProjectByExternalId;

public sealed record GetProjectByExternalIdQuery(Guid ExternalId, long? CallerIncubatorId) : IBaseRequest<ProjectDto>;
