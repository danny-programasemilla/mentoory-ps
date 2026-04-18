using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;

public sealed record ProjectLifecycleStageDto(
    StageType StageType,
    string DisplayName,
    StageState State,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? AdvancedByDisplay);
