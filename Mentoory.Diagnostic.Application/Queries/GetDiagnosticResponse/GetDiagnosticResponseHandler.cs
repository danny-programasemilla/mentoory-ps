using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetDiagnosticResponse;

/// <summary>
/// Handles the query to retrieve a diagnostic response by its external identifier.
/// </summary>
public class GetDiagnosticResponseHandler : BaseCommandHandler<GetDiagnosticResponseQuery, DiagnosticResponseDto?>
{
    private readonly IDiagnosticResponseRepository _diagnosticResponseRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDiagnosticResponseHandler"/> class.
    /// </summary>
    /// <param name="diagnosticResponseRepository">The diagnostic response repository for data access.</param>
    public GetDiagnosticResponseHandler(IDiagnosticResponseRepository diagnosticResponseRepository)
    {
        _diagnosticResponseRepository = diagnosticResponseRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<DiagnosticResponseDto?>> Handle(GetDiagnosticResponseQuery request, CancellationToken cancellationToken)
    {
        var response = request.ProjectId.HasValue
            ? await _diagnosticResponseRepository.GetByExternalIdWithResponsesAsync(
                request.ExternalId, request.ProjectId.Value, cancellationToken)
            : await _diagnosticResponseRepository.GetByExternalIdWithResponsesAsync(
                request.ExternalId, cancellationToken);

        if (response is null)
        {
            return Success((DiagnosticResponseDto?)null);
        }

        var dto = new DiagnosticResponseDto(
            response.ExternalId,
            response.ProjectFormId,
            response.StageFormAssignmentId,
            response.IsCompleted,
            response.CompletedAtUtc,
            response.QuestionResponses
                .Select(qr => new QuestionResponseDto(
                    qr.Id,
                    qr.QuestionId,
                    qr.TextValue,
                    qr.NumericValue,
                    OptionIdParser.Parse(qr.SelectedOptionIdsRaw),
                    qr.CreatedAtUtc,
                    qr.Corrections
                        .OrderBy(c => c.CorrectedAtUtc)
                        .Select(c => new AnswerCorrectionDto(
                            c.Id,
                            c.PreviousTextValue,
                            c.PreviousNumericValue,
                            c.PreviousSelectedOptionIds,
                            c.CorrectedByUserId,
                            c.CorrectedAtUtc,
                            c.Reason))
                        .ToList()))
                .ToList());

        return Success((DiagnosticResponseDto?)dto);
    }
}
