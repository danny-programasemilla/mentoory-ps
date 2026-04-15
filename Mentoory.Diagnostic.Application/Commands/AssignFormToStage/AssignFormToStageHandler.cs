using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.AssignFormToStage;

public partial class AssignFormToStageHandler(
    ILogger<AssignFormToStageHandler> logger,
    IProjectFormRepository projectFormRepository,
    IStageFormAssignmentRepository assignmentRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<AssignFormToStageCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(AssignFormToStageCommand request, CancellationToken cancellationToken)
    {
        var form = await projectFormRepository.GetByExternalIdWithQuestionsAsync(
            request.ProjectFormExternalId, request.ProjectId, cancellationToken);

        if (form is null)
        {
            LogFormNotFound(request.ProjectFormExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ProjectFormExternalId), "Formulario no encontrado."));
        }

        if (form.Questions.Count == 0)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                (nameof(request.ProjectFormExternalId), "El formulario no tiene preguntas."));
        }

        // Default: all questions selected
        var questionIds = form.Questions
            .OrderBy(q => q.SortOrder)
            .Select(q => q.Id)
            .ToList();

        var assignment = StageFormAssignment.Create(
            request.ProjectId,
            request.IncubatorId,
            request.ProjectStageId,
            form.Id,
            questionIds,
            timeProvider.UtcNow);

        assignmentRepository.Add(assignment);

        LogAssignmentCreated(assignment.ExternalId, request.ProjectStageId);
        return Success(assignment.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project form not found with ExternalId {ExternalId}")]
    partial void LogFormNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage form assignment {AssignmentExternalId} created for stage {StageId}")]
    partial void LogAssignmentCreated(Guid assignmentExternalId, long stageId);
}
