namespace Mentoory.Diagnostic.Application.Queries.CompareDiagnosticResults;

public sealed record DiagnosticComparisonDto(
    ComparisonSideDto Earlier,
    ComparisonSideDto Later,
    IReadOnlyList<TopicComparisonDto> TopicComparisons,
    IReadOnlyList<QuestionComparisonDto> SharedQuestions);

public sealed record ComparisonSideDto(
    Guid ResponseExternalId,
    string FormName,
    DateTime CompletedAtUtc);

public sealed record TopicComparisonDto(
    long TopicId,
    decimal PreviousScore,
    decimal CurrentScore,
    decimal Delta,
    decimal PercentageChange);

public sealed record QuestionComparisonDto(
    long QuestionId,
    string QuestionText,
    string? EarlierAnswer,
    string? LaterAnswer,
    decimal? EarlierNumeric,
    decimal? LaterNumeric);
