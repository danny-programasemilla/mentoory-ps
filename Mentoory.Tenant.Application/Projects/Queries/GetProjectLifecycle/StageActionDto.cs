using Mentoory.Access.Application.StageActions;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;

public sealed record StageActionDto(
    StageGatedAction Action,
    string DisplayName,
    StageType GatingStageType,
    string GatingStageDisplayName,
    StageGatedActionState State,
    string LinkUrl);
