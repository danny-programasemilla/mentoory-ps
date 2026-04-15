namespace Mentoory.Diagnostic.Domain.ValueObjects;

public sealed record ScoreDelta(
    long TopicId,
    decimal PreviousScore,
    decimal CurrentScore)
{
    public decimal Delta => CurrentScore - PreviousScore;

    public decimal PercentageChange => PreviousScore == 0 ? 0 : (Delta / PreviousScore) * 100;
}
