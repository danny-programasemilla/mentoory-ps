using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Diagnostic.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when an answer in a diagnostic response is corrected.
/// </summary>
/// <param name="DiagnosticResponseId">The internal ID of the diagnostic response containing the corrected answer.</param>
/// <param name="QuestionResponseId">The internal ID of the question response that was corrected.</param>
/// <param name="CorrectedByUserId">The internal ID of the user who performed the correction.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the event occurred.</param>
public sealed record AnswerCorrectedEvent(
    long DiagnosticResponseId,
    long QuestionResponseId,
    long CorrectedByUserId,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
