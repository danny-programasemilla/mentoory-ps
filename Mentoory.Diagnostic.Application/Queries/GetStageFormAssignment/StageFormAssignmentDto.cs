namespace Mentoory.Diagnostic.Application.Queries.GetStageFormAssignment;

public sealed record StageFormAssignmentDto(
    Guid ExternalId,
    long ProjectFormId,
    Guid FormExternalId,
    string FormName,
    long ProjectStageId,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyList<AssignedQuestionDto> AssignedQuestions,
    IReadOnlyList<AssignedQuestionDto> AllFormQuestions);

public sealed record AssignedQuestionDto(
    long QuestionId,
    string QuestionText,
    string QuestionType,
    int SortOrder);
