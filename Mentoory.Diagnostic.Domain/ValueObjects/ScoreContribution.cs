using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.ValueObjects;

public class ScoreContribution : ValueObject
{
    public ScoreContribution(decimal score, SwotClassification swotClassification, OdsrOrientation odsrOrientation)
    {
        Score = score;
        SwotClassification = swotClassification;
        OdsrOrientation = odsrOrientation;
    }

    private ScoreContribution()
    {
    }

    public decimal Score { get; private set; }
    public SwotClassification SwotClassification { get; private set; }
    public OdsrOrientation OdsrOrientation { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Score;
        yield return SwotClassification;
        yield return OdsrOrientation;
    }
}
