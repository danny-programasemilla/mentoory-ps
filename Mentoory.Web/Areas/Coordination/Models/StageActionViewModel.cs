using Mentoory.Access.Application.StageActions;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Web.Areas.Coordination.Models;

public sealed record StageActionViewModel(
    StageGatedAction Action,
    string DisplayName,
    StageType GatingStageType,
    string GatingStageDisplayName,
    StageGatedActionState State,
    string LinkUrl);
