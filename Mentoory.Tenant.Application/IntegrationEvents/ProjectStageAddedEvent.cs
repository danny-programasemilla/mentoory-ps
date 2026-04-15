using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Tenant.Application.IntegrationEvents;

public sealed record ProjectStageAddedEvent(
    Guid ProjectExternalId,
    Guid StageExternalId,
    string StageType,
    int Position,
    DateTime OccurredOn)
    : IntegrationEvent(OccurredOn);
