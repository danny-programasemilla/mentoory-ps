namespace Mentoory.Diagnostic.Application.Queries.ListStageFormAssignments;

public sealed record StageFormAssignmentSummaryDto(
    Guid ExternalId,
    string FormName,
    int QuestionCount,
    bool IsActive,
    DateTime CreatedAtUtc);
