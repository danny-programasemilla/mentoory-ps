using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Tenant.Application.IntegrationEvents;

public sealed record ProjectStageRemovedEvent(
    Guid ProjectExternalId,
    Guid StageExternalId,
    DateTime OccurredOn)
    : IntegrationEvent(OccurredOn);
