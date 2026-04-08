using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ListRegistrationProjects;

public sealed record ListRegistrationProjectsQuery(long IncubatorId)
    : IBaseRequest<RegistrationProjectsResult>;
