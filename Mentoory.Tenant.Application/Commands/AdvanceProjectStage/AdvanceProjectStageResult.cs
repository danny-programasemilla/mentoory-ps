using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

public sealed record AdvanceProjectStageResult(
    StageType NewCurrentStageType,
    StageState NewCurrentStageState);
