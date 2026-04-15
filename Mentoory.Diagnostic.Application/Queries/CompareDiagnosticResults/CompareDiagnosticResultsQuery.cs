using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.CompareDiagnosticResults;

public sealed record CompareDiagnosticResultsQuery(
    Guid ResponseExternalId1,
    Guid ResponseExternalId2) : IBaseRequest<DiagnosticComparisonDto>;
