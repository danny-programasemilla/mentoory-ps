using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;

public sealed record ProjectLifecycleDto(
    Guid ExternalId,
    string Name,
    string? Description,
    Guid IncubatorExternalId,
    string IncubatorName,
    bool IsActive,
    StageType CurrentStageType,
    StageState CurrentStageState,
    bool CanAdvance,
    string? CannotAdvanceReason,
    IReadOnlyList<ProjectLifecycleStageDto> Stages,
    IReadOnlyList<StageActionDto> Actions);
