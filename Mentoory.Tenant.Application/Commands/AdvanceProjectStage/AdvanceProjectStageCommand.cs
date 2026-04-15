using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

public sealed record AdvanceProjectStageCommand(
    long ProjectId,
    long AdvancedByUserId) : IBaseRequest;
