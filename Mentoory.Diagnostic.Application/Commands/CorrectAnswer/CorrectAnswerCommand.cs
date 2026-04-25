using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.CorrectAnswer;

/// <summary>
/// Represents a command to correct an answer in a completed diagnostic response.
/// Manual-mode audit: the handler writes the audit row itself so it can capture
/// both the before-value and the after-value of the corrected answer.
/// </summary>
/// <param name="DiagnosticResponseExternalId">The external identifier of the diagnostic response.</param>
/// <param name="QuestionResponseId">The ID of the question response to correct.</param>
/// <param name="NewTextValue">The corrected text value, if applicable.</param>
/// <param name="NewNumericValue">The corrected numeric value, if applicable.</param>
/// <param name="NewSelectedOptionIds">The corrected selected option IDs, if applicable.</param>
/// <param name="CorrectedByUserId">The user performing the correction.</param>
/// <param name="Reason">The reason for the correction.</param>
[Audited(AuditEventTypes.AnswerCorrected, EntityType = "AnswerCorrection", Mode = AuditMode.Manual)]
public sealed record CorrectAnswerCommand(
    Guid DiagnosticResponseExternalId,
    long QuestionResponseId,
    string? NewTextValue,
    decimal? NewNumericValue,
    List<long>? NewSelectedOptionIds,
    long CorrectedByUserId,
    string? Reason,
    long? ProjectId = null) : IBaseRequest;
