using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Web.Areas.Coordination.Models;

public sealed record LifecycleProjectViewModel(
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
    IReadOnlyList<LifecycleStageViewModel> Stages,
    IReadOnlyList<StageActionViewModel> Actions);
