using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.GetProjectStageInternalId;

public sealed record GetProjectStageInternalIdQuery(
    long ProjectId,
    Guid StageExternalId) : IBaseRequest<long>;
