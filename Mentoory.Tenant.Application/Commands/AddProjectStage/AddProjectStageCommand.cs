using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Application.Commands.AddProjectStage;

public sealed record AddProjectStageCommand(
    long ProjectId,
    StageType StageType,
    int Position) : IBaseRequest<Guid>;
