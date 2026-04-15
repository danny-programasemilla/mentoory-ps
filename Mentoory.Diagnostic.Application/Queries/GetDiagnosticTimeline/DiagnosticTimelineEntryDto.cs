namespace Mentoory.Diagnostic.Application.Queries.GetDiagnosticTimeline;

public sealed record DiagnosticTimelineEntryDto(
    Guid ResponseExternalId,
    Guid AssignmentExternalId,
    long ProjectStageId,
    string FormName,
    DateTime CompletedAtUtc,
    int ResponseCount);
