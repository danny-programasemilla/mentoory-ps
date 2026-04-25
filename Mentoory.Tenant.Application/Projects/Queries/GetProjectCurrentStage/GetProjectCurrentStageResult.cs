using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectCurrentStage;

public sealed record GetProjectCurrentStageResult(
    Guid ProjectExternalId,
    StageType CurrentStageType,
    bool IsActive,
    long IncubatorId);
