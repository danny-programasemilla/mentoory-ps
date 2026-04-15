using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.RemoveProjectStage;

public sealed record RemoveProjectStageCommand(
    long ProjectId,
    Guid StageExternalId) : IBaseRequest;
