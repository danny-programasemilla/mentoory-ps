namespace Mentoory.Diagnostic.Application.Queries.GetEntrepreneurDiagnosticStatus;

public sealed record StageFormStatusDto(
    Guid AssignmentExternalId,
    long ProjectStageId,
    string FormName,
    int QuestionCount,
    bool IsCompleted,
    DateTime? CompletedAtUtc);
