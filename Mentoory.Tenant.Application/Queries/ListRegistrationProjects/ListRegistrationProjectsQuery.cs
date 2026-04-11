using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ListRegistrationProjects;

public sealed record ListRegistrationProjectsQuery(
    long IncubatorId,
    IReadOnlyList<long>? AuthorizedProjectIds,
    long? CallerIncubatorId)
    : IBaseRequest<RegistrationProjectsResult>;
