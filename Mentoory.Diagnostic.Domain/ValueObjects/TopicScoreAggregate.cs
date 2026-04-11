using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.ValueObjects;

public class TopicScoreAggregate : ValueObject
{
    public TopicScoreAggregate(long topicId, decimal totalScore, int questionCount)
    {
        TopicId = topicId;
        TotalScore = totalScore;
        QuestionCount = questionCount;
    }

    private TopicScoreAggregate()
    {
    }

    public long TopicId { get; private set; }
    public decimal TotalScore { get; private set; }
    public int QuestionCount { get; private set; }
    public decimal AverageScore => QuestionCount > 0 ? TotalScore / QuestionCount : 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TopicId;
        yield return TotalScore;
        yield return QuestionCount;
    }
}
