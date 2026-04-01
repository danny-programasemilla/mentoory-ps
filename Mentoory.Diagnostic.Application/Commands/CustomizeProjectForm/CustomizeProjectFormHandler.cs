using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.CustomizeProjectForm;

/// <summary>
/// Handles the customization of a project form (add, remove, or reorder questions).
/// </summary>
public partial class CustomizeProjectFormHandler : BaseCommandHandler<CustomizeProjectFormCommand>
{
    private readonly IProjectFormRepository _projectFormRepository;
    private readonly ILogger<CustomizeProjectFormHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomizeProjectFormHandler"/> class.
    /// </summary>
    /// <param name="projectFormRepository">The project form repository for persistence operations.</param>
    /// <param name="logger">The logger instance.</param>
    public CustomizeProjectFormHandler(
        IProjectFormRepository projectFormRepository,
        ILogger<CustomizeProjectFormHandler> logger)
    {
        _projectFormRepository = projectFormRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(CustomizeProjectFormCommand request, CancellationToken cancellationToken)
    {
        var form = await _projectFormRepository.GetByExternalIdWithQuestionsAsync(
            request.ProjectFormExternalId, cancellationToken);

        if (form is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Form", "El formulario del proyecto no fue encontrado."));
        }

        switch (request.Action)
        {
            case CustomizeAction.AddQuestion:
                if (request.QuestionData is null)
                {
                    return Failure(ResultErrorCodes.GenericError,
                        ("QuestionData", "Los datos de la pregunta son requeridos para agregar una pregunta."));
                }

                var qd = request.QuestionData;
                form.AddQuestion(
                    qd.TopicId,
                    qd.QuestionText,
                    qd.QuestionType,
                    qd.StageApplicability,
                    qd.SortOrder,
                    qd.BlockGroup,
                    qd.IsOptional);

                LogQuestionAdded(form.ExternalId, qd.QuestionText);
                break;

            case CustomizeAction.RemoveQuestion:
                if (request.QuestionIdToRemove is null)
                {
                    return Failure(ResultErrorCodes.GenericError,
                        ("QuestionIdToRemove", "El identificador de la pregunta a eliminar es requerido."));
                }

                form.RemoveQuestion(request.QuestionIdToRemove.Value);
                LogQuestionRemoved(form.ExternalId, request.QuestionIdToRemove.Value);
                break;

            case CustomizeAction.ReorderQuestions:
                if (request.QuestionIdsInOrder is null || request.QuestionIdsInOrder.Count == 0)
                {
                    return Failure(ResultErrorCodes.GenericError,
                        ("QuestionIdsInOrder", "La lista de identificadores de preguntas en orden es requerida."));
                }

                form.ReorderQuestions(request.QuestionIdsInOrder);
                LogQuestionsReordered(form.ExternalId);
                break;

            default:
                return Failure(ResultErrorCodes.GenericError,
                    ("Action", "La acción especificada no es válida."));
        }

        _projectFormRepository.Update(form);
        await _projectFormRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Success();
    }

    /// <summary>
    /// Logs when a question is added to a form.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Question added to project form {ProjectFormExternalId}: {QuestionText}")]
    partial void LogQuestionAdded(Guid projectFormExternalId, string questionText);

    /// <summary>
    /// Logs when a question is removed from a form.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Question {QuestionId} removed from project form {ProjectFormExternalId}")]
    partial void LogQuestionRemoved(Guid projectFormExternalId, long questionId);

    /// <summary>
    /// Logs when questions in a form are reordered.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Questions reordered in project form {ProjectFormExternalId}")]
    partial void LogQuestionsReordered(Guid projectFormExternalId);
}
