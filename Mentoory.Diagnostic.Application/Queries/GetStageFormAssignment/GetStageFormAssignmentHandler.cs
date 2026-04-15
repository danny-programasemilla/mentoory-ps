using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Queries.GetStageFormAssignment;

public partial class GetStageFormAssignmentHandler(
    ILogger<GetStageFormAssignmentHandler> logger,
    IStageFormAssignmentRepository assignmentRepository,
    IProjectFormRepository projectFormRepository)
    : BaseCommandHandler<GetStageFormAssignmentQuery, StageFormAssignmentDto>
{
    public override async Task<Result<StageFormAssignmentDto>> Handle(
        GetStageFormAssignmentQuery request,
        CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.GetByExternalIdAsync(request.AssignmentExternalId, cancellationToken);
        if (assignment is null)
        {
            LogAssignmentNotFound(request.AssignmentExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.AssignmentExternalId), "Asignación no encontrada."));
        }

        var form = await projectFormRepository.GetByIdWithQuestionsAsync(assignment.ProjectFormId, cancellationToken);
        if (form is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.AssignmentExternalId), "Formulario asociado no encontrado."));
        }

        var assignedQuestionIds = assignment.AssignedQuestions
            .ToDictionary(aq => aq.QuestionId, aq => aq.SortOrder);

        var assignedQuestions = form.Questions
            .Where(q => assignedQuestionIds.ContainsKey(q.Id))
            .OrderBy(q => assignedQuestionIds[q.Id])
            .Select(q => new AssignedQuestionDto(
                q.Id,
                q.QuestionText,
                q.QuestionType.ToString(),
                assignedQuestionIds[q.Id]))
            .ToList();

        var allFormQuestions = form.Questions
            .OrderBy(q => q.SortOrder)
            .Select(q => new AssignedQuestionDto(
                q.Id,
                q.QuestionText,
                q.QuestionType.ToString(),
                q.SortOrder))
            .ToList();

        var dto = new StageFormAssignmentDto(
            assignment.ExternalId,
            assignment.ProjectFormId,
            form.ExternalId,
            form.Name,
            assignment.ProjectStageId,
            assignment.IsActive,
            assignment.CreatedAtUtc,
            assignedQuestions,
            allFormQuestions);

        return Success(dto);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stage form assignment not found with ExternalId {ExternalId}")]
    partial void LogAssignmentNotFound(Guid externalId);
}
