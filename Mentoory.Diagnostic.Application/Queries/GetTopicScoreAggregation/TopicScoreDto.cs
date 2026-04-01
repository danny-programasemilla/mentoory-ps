namespace Mentoory.Diagnostic.Application.Queries.GetTopicScoreAggregation;

/// <summary>
/// Data transfer object representing an aggregated topic score.
/// </summary>
/// <param name="TopicId">The topic identifier.</param>
/// <param name="TotalScore">The total accumulated score for the topic.</param>
/// <param name="QuestionCount">The number of questions contributing to the score.</param>
/// <param name="AverageScore">The average score per question for the topic.</param>
public sealed record TopicScoreDto(
    long TopicId,
    decimal TotalScore,
    int QuestionCount,
    decimal AverageScore);
