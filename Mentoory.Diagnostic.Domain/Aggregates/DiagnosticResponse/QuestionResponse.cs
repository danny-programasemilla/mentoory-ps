using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;

public class QuestionResponse : Entity
{
    private readonly List<AnswerCorrection> _corrections = new();
    private List<long> _selectedOptionIds = new();

    private QuestionResponse()
    {
    }

    public long QuestionId { get; private set; }
    public string? TextValue { get; private set; }
    public decimal? NumericValue { get; private set; }
    public string? SelectedOptionIdsRaw { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<long> SelectedOptionIds => _selectedOptionIds.AsReadOnly();
    public IReadOnlyCollection<AnswerCorrection> Corrections => _corrections.AsReadOnly();

    internal static QuestionResponse Create(
        long questionId,
        string? textValue,
        decimal? numericValue,
        IReadOnlyList<long>? selectedOptionIds,
        DateTime utcNow)
    {
        var ids = selectedOptionIds?.ToList() ?? new List<long>();
        return new QuestionResponse
        {
            QuestionId = questionId,
            TextValue = textValue,
            NumericValue = numericValue,
            _selectedOptionIds = ids,
            SelectedOptionIdsRaw = ids.Count > 0 ? string.Join(",", ids) : null,
            CreatedAtUtc = utcNow,
        };
    }

    internal AnswerCorrection Correct(
        string? newTextValue,
        decimal? newNumericValue,
        IReadOnlyList<long>? newSelectedOptionIds,
        long correctedByUserId,
        string? reason,
        DateTime utcNow)
    {
        var correction = AnswerCorrection.Create(
            previousTextValue: TextValue,
            previousNumericValue: NumericValue,
            previousSelectedOptionIds: SelectedOptionIdsRaw,
            correctedByUserId,
            reason,
            utcNow);

        _corrections.Add(correction);

        TextValue = newTextValue;
        NumericValue = newNumericValue;
        _selectedOptionIds = newSelectedOptionIds?.ToList() ?? new List<long>();
        SelectedOptionIdsRaw = _selectedOptionIds.Count > 0 ? string.Join(",", _selectedOptionIds) : null;

        return correction;
    }
}
