using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Application.Queries.ListIncubators;

namespace Mentoory.Tenant.Application.Queries.GetIncubatorByExternalId;

public sealed record GetIncubatorByExternalIdQuery(Guid ExternalId, long? CallerIncubatorId) : IBaseRequest<IncubatorDto>;
