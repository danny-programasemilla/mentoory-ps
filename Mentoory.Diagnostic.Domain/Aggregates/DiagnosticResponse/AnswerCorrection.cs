using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;

public class AnswerCorrection : Entity
{
    private AnswerCorrection()
    {
    }

    public string? PreviousTextValue { get; private set; }
    public decimal? PreviousNumericValue { get; private set; }
    public string? PreviousSelectedOptionIds { get; private set; }
    public long CorrectedByUserId { get; private set; }
    public DateTime CorrectedAtUtc { get; private set; }
    public string? Reason { get; private set; }

    internal static AnswerCorrection Create(
        string? previousTextValue,
        decimal? previousNumericValue,
        string? previousSelectedOptionIds,
        long correctedByUserId,
        string? reason,
        DateTime utcNow)
    {
        return new AnswerCorrection
        {
            PreviousTextValue = previousTextValue,
            PreviousNumericValue = previousNumericValue,
            PreviousSelectedOptionIds = previousSelectedOptionIds,
            CorrectedByUserId = correctedByUserId,
            CorrectedAtUtc = utcNow,
            Reason = reason,
        };
    }
}
