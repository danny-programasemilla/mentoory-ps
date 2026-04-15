using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetEntrepreneurDiagnosticStatus;

public sealed record GetEntrepreneurDiagnosticStatusQuery(
    long ProjectId,
    long EntrepreneurUserId) : IBaseRequest<IReadOnlyList<StageFormStatusDto>>;
