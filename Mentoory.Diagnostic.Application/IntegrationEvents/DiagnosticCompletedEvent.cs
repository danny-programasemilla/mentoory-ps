using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Diagnostic.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a diagnostic response is completed.
/// </summary>
/// <param name="DiagnosticResponseId">The internal ID of the completed diagnostic response.</param>
/// <param name="ProjectFormId">The internal ID of the associated project form.</param>
/// <param name="ProjectId">The internal ID of the associated project.</param>
/// <param name="IncubatorId">The internal ID of the incubator that owns the project.</param>
/// <param name="EntrepreneurUserId">The internal ID of the entrepreneur who submitted the response.</param>
/// <param name="StageFormAssignmentId">The internal ID of the stage form assignment.</param>
/// <param name="ProjectStageId">The internal ID of the project stage.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the event occurred.</param>
public sealed record DiagnosticCompletedEvent(
    long DiagnosticResponseId,
    long ProjectFormId,
    long ProjectId,
    long IncubatorId,
    long EntrepreneurUserId,
    long StageFormAssignmentId,
    long ProjectStageId,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
