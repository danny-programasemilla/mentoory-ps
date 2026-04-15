using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.AssignFormToStage;

public sealed record AssignFormToStageCommand(
    long ProjectId,
    long IncubatorId,
    long ProjectStageId,
    Guid ProjectFormExternalId) : IBaseRequest<Guid>;
