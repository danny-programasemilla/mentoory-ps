using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Web.Areas.Coordination.Models;

public sealed record LifecycleStageViewModel(
    StageType StageType,
    string DisplayName,
    StageState State,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? AdvancedByDisplay);
