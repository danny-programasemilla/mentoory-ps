using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

public sealed record AdvanceProjectStageCommand(
    Guid ProjectExternalId,
    long ActingUserId,
    long ActingUserIncubatorId,
    bool ActingUserIsGlobalAdmin)
    : IBaseRequest<AdvanceProjectStageResult>;
