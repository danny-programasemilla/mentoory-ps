using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Queries.CompareDiagnosticResults;

public partial class CompareDiagnosticResultsHandler(
    ILogger<CompareDiagnosticResultsHandler> logger,
    IDiagnosticResponseRepository responseRepository,
    IProjectFormRepository formRepository)
    : BaseCommandHandler<CompareDiagnosticResultsQuery, DiagnosticComparisonDto>
{
    public override async Task<Result<DiagnosticComparisonDto>> Handle(
        CompareDiagnosticResultsQuery request,
        CancellationToken cancellationToken)
    {
        var response1 = await responseRepository.GetByExternalIdWithResponsesAsync(
            request.ResponseExternalId1, cancellationToken);
        var response2 = await responseRepository.GetByExternalIdWithResponsesAsync(
            request.ResponseExternalId2, cancellationToken);

        if (response1 is null || response2 is null)
        {
            LogResponseNotFound(request.ResponseExternalId1, request.ResponseExternalId2);
            return Failure(ResultErrorCodes.GenericError,
                ("Responses", "Una o ambas respuestas no fueron encontradas."));
        }

        if (!response1.IsCompleted || !response2.IsCompleted)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid,
                ("Responses", "Ambas respuestas deben estar completadas para comparar."));
        }

        // Determine chronological order
        var (earlier, later) = response1.CompletedAtUtc <= response2.CompletedAtUtc
            ? (response1, response2)
            : (response2, response1);

        var form1 = await formRepository.GetByIdAsync(earlier.ProjectFormId, cancellationToken);
        var form2 = await formRepository.GetByIdAsync(later.ProjectFormId, cancellationToken);

        var earlierSide = new ComparisonSideDto(
            earlier.ExternalId,
            form1?.Name ?? "Formulario",
            earlier.CompletedAtUtc!.Value);

        var laterSide = new ComparisonSideDto(
            later.ExternalId,
            form2?.Name ?? "Formulario",
            later.CompletedAtUtc!.Value);

        // Find shared questions (by QuestionId)
        var earlierByQuestion = earlier.QuestionResponses.ToDictionary(qr => qr.QuestionId);
        var laterByQuestion = later.QuestionResponses.ToDictionary(qr => qr.QuestionId);

        var sharedQuestionIds = earlierByQuestion.Keys.Intersect(laterByQuestion.Keys).ToList();

        var sharedQuestions = sharedQuestionIds.Select(qId =>
        {
            var e = earlierByQuestion[qId];
            var l = laterByQuestion[qId];
            return new QuestionComparisonDto(
                qId,
                $"Pregunta {qId}",
                e.TextValue,
                l.TextValue,
                e.NumericValue,
                l.NumericValue);
        }).ToList();

        // Topic-level aggregation (by TopicId from QuestionResponse → Question relationship)
        // For now, aggregate by numeric values only where both responses have them
        var topicComparisons = new List<TopicComparisonDto>();

        var dto = new DiagnosticComparisonDto(
            earlierSide,
            laterSide,
            topicComparisons,
            sharedQuestions);

        return Success(dto);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Response(s) not found: {ExternalId1}, {ExternalId2}")]
    partial void LogResponseNotFound(Guid externalId1, Guid externalId2);
}
