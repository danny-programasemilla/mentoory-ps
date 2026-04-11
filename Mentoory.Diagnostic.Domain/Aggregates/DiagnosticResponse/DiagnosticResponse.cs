using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;

public class DiagnosticResponse : Entity, IAggregateRoot
{
    private readonly List<QuestionResponse> _questionResponses = new();

    private DiagnosticResponse()
    {
    }

    public Guid ExternalId { get; private set; }
    public long ProjectFormId { get; private set; }
    public long ProjectId { get; private set; }
    public long IncubatorId { get; private set; }
    public long EntrepreneurUserId { get; private set; }
    public EvaluationStage EvaluationStage { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<QuestionResponse> QuestionResponses => _questionResponses.AsReadOnly();

    public static DiagnosticResponse Create(
        long projectFormId,
        long projectId,
        long incubatorId,
        long entrepreneurUserId,
        EvaluationStage evaluationStage,
        DateTime utcNow)
    {
        return new DiagnosticResponse
        {
            ExternalId = Guid.NewGuid(),
            ProjectFormId = projectFormId,
            ProjectId = projectId,
            IncubatorId = incubatorId,
            EntrepreneurUserId = entrepreneurUserId,
            EvaluationStage = evaluationStage,
            IsCompleted = false,
            CreatedAtUtc = utcNow,
        };
    }

    public QuestionResponse AddResponse(
        long questionId,
        string? textValue,
        decimal? numericValue,
        IReadOnlyList<long>? selectedOptionIds,
        DateTime utcNow)
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Cannot add responses to a completed diagnostic.");
        }

        var existing = _questionResponses.FirstOrDefault(r => r.QuestionId == questionId);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Response already exists for question {questionId}.");
        }

        var response = QuestionResponse.Create(questionId, textValue, numericValue, selectedOptionIds, utcNow);
        _questionResponses.Add(response);
        return response;
    }

    public void MarkAsCompleted(DateTime utcNow)
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Diagnostic is already completed.");
        }

        IsCompleted = true;
        CompletedAtUtc = utcNow;
    }

    public AnswerCorrection CorrectAnswer(
        long questionResponseId,
        string? newTextValue,
        decimal? newNumericValue,
        IReadOnlyList<long>? newSelectedOptionIds,
        long correctedByUserId,
        string? reason,
        DateTime utcNow)
    {
        var response = _questionResponses.FirstOrDefault(r => r.Id == questionResponseId)
            ?? throw new InvalidOperationException("Question response not found.");

        return response.Correct(newTextValue, newNumericValue, newSelectedOptionIds, correctedByUserId, reason, utcNow);
    }
}
