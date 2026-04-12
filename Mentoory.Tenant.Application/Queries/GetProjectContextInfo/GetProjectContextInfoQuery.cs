using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.GetProjectContextInfo;

public sealed record GetProjectContextInfoQuery(long ProjectId) : IBaseRequest<ProjectContextInfoDto>;
