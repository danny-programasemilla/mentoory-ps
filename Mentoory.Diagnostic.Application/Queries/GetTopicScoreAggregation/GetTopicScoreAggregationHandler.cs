using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetTopicScoreAggregation;

/// <summary>
/// Handles the query to retrieve aggregated topic scores for a diagnostic response.
/// Uses the project form's questions to map question responses to topics and calculate scores
/// based on selected answer options.
/// </summary>
public class GetTopicScoreAggregationHandler : BaseCommandHandler<GetTopicScoreAggregationQuery, IReadOnlyList<TopicScoreDto>>
{
    private readonly IDiagnosticResponseRepository _diagnosticResponseRepository;
    private readonly IProjectFormRepository _projectFormRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTopicScoreAggregationHandler"/> class.
    /// </summary>
    /// <param name="diagnosticResponseRepository">The diagnostic response repository for data access.</param>
    /// <param name="projectFormRepository">The project form repository for resolving question topics and scores.</param>
    public GetTopicScoreAggregationHandler(
        IDiagnosticResponseRepository diagnosticResponseRepository,
        IProjectFormRepository projectFormRepository)
    {
        _diagnosticResponseRepository = diagnosticResponseRepository;
        _projectFormRepository = projectFormRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<IReadOnlyList<TopicScoreDto>>> Handle(GetTopicScoreAggregationQuery request, CancellationToken cancellationToken)
    {
        var diagnosticResponse = await _diagnosticResponseRepository.GetByExternalIdWithResponsesAsync(
            request.DiagnosticResponseExternalId, cancellationToken);

        if (diagnosticResponse is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("DiagnosticResponse", "La respuesta diagnóstica no fue encontrada."));
        }

        var formWithQuestions = await _projectFormRepository.GetByIdWithQuestionsAsync(
            diagnosticResponse.ProjectFormId, cancellationToken);

        if (formWithQuestions is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("ProjectForm", "El formulario del proyecto asociado no fue encontrado."));
        }

        var questionLookup = formWithQuestions.Questions
            .ToDictionary(
                q => q.Id,
                q => (q.TopicId, Options: q.AnswerOptions.ToDictionary(ao => ao.Id, ao => ao.Score)));

        var topicScores = new Dictionary<long, (decimal TotalScore, int Count)>();

        foreach (var qr in diagnosticResponse.QuestionResponses)
        {
            if (!questionLookup.TryGetValue(qr.QuestionId, out var questionInfo))
            {
                continue;
            }

            var selectedIds = OptionIdParser.Parse(qr.SelectedOptionIdsRaw);
            if (selectedIds.Count == 0)
            {
                continue;
            }

            var responseScore = selectedIds
                .Sum(optId => questionInfo.Options.TryGetValue(optId, out var s) ? s : 0);

            topicScores.TryGetValue(questionInfo.TopicId, out var current);
            topicScores[questionInfo.TopicId] = (current.TotalScore + responseScore, current.Count + 1);
        }

        var result = topicScores
            .Select(kvp => new TopicScoreDto(
                kvp.Key,
                kvp.Value.TotalScore,
                kvp.Value.Count,
                kvp.Value.Count > 0 ? kvp.Value.TotalScore / kvp.Value.Count : 0))
            .OrderBy(t => t.TopicId)
            .ToList();

        return Success((IReadOnlyList<TopicScoreDto>)result);
    }
}
