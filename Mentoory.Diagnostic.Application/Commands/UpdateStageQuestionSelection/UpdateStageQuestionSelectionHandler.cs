using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.UpdateStageQuestionSelection;

public partial class UpdateStageQuestionSelectionHandler(
    ILogger<UpdateStageQuestionSelectionHandler> logger,
    IStageFormAssignmentRepository assignmentRepository)
    : BaseCommandHandler<UpdateStageQuestionSelectionCommand>
{
    public override async Task<Result> Handle(UpdateStageQuestionSelectionCommand request, CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.GetByExternalIdAsync(request.AssignmentExternalId, cancellationToken);
        if (assignment is null)
        {
            LogAssignmentNotFound(request.AssignmentExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.AssignmentExternalId), "Asignación no encontrada."));
        }

        try
        {
            assignment.UpdateQuestionSelection(request.SelectedQuestionIds);
            LogSelectionUpdated(request.AssignmentExternalId, request.SelectedQuestionIds.Count);
            return Success();
        }
        catch (ArgumentException ex)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                (nameof(request.SelectedQuestionIds), ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stage form assignment not found with ExternalId {ExternalId}")]
    partial void LogAssignmentNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Question selection updated for assignment {ExternalId}: {QuestionCount} questions")]
    partial void LogSelectionUpdated(Guid externalId, int questionCount);
}
