using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetTopicScoreAggregation;

/// <summary>
/// Query to retrieve aggregated topic scores for a diagnostic response.
/// </summary>
/// <param name="DiagnosticResponseExternalId">The external GUID identifier of the diagnostic response.</param>
public sealed record GetTopicScoreAggregationQuery(
    Guid DiagnosticResponseExternalId) : IBaseRequest<IReadOnlyList<TopicScoreDto>>;
