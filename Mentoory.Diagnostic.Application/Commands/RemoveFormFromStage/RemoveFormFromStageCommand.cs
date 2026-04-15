using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.RemoveFormFromStage;

public sealed record RemoveFormFromStageCommand(
    Guid AssignmentExternalId) : IBaseRequest;
