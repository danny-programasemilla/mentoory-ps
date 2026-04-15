using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetDiagnosticTimeline;

public sealed record GetDiagnosticTimelineQuery(
    long ProjectId,
    long EntrepreneurUserId) : IBaseRequest<IReadOnlyList<DiagnosticTimelineEntryDto>>;
