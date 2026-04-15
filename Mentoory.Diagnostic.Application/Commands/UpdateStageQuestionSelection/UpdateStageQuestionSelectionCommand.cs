using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.UpdateStageQuestionSelection;

public sealed record UpdateStageQuestionSelectionCommand(
    Guid AssignmentExternalId,
    IReadOnlyList<long> SelectedQuestionIds) : IBaseRequest;
