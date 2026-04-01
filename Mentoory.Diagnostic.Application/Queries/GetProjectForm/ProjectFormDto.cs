using Mentoory.Diagnostic.Domain.Enums;

namespace Mentoory.Diagnostic.Application.Queries.GetProjectForm;

/// <summary>
/// Data transfer object for a follow-up question within a question.
/// </summary>
/// <param name="Id">The follow-up question ID.</param>
/// <param name="QuestionText">The follow-up question text.</param>
/// <param name="SortOrder">The display sort order.</param>
public sealed record FollowUpQuestionDto(
    long Id,
    string QuestionText,
    int SortOrder);

/// <summary>
/// Data transfer object for an answer option within a question.
/// </summary>
/// <param name="Id">The answer option ID.</param>
/// <param name="OptionText">The option text.</param>
/// <param name="Score">The score value for this option.</param>
/// <param name="SwotClassification">The SWOT classification.</param>
/// <param name="OdsrOrientation">The ODSR orientation.</param>
/// <param name="SortOrder">The display sort order.</param>
public sealed record AnswerOptionDto(
    long Id,
    string OptionText,
    decimal Score,
    SwotClassification SwotClassification,
    OdsrOrientation OdsrOrientation,
    int SortOrder);

/// <summary>
/// Data transfer object for a question within a project form.
/// </summary>
/// <param name="Id">The question ID.</param>
/// <param name="ExternalId">The question external identifier.</param>
/// <param name="TopicId">The topic identifier.</param>
/// <param name="QuestionText">The question text.</param>
/// <param name="QuestionType">The type of question.</param>
/// <param name="StageApplicability">The evaluation stage applicability.</param>
/// <param name="SortOrder">The display sort order.</param>
/// <param name="BlockGroup">The optional block group.</param>
/// <param name="IsOptional">Whether the question is optional.</param>
/// <param name="AnswerOptions">The answer options for this question.</param>
/// <param name="FollowUpQuestions">The follow-up questions for this question.</param>
public sealed record QuestionDto(
    long Id,
    Guid ExternalId,
    long TopicId,
    string QuestionText,
    QuestionType QuestionType,
    StageApplicability StageApplicability,
    int SortOrder,
    string? BlockGroup,
    bool IsOptional,
    IReadOnlyList<AnswerOptionDto> AnswerOptions,
    IReadOnlyList<FollowUpQuestionDto> FollowUpQuestions);

/// <summary>
/// Data transfer object representing a project form with its questions.
/// </summary>
/// <param name="ExternalId">The external GUID identifier for routing.</param>
/// <param name="Name">The form name.</param>
/// <param name="ProjectId">The associated project ID.</param>
/// <param name="IncubatorId">The associated incubator ID.</param>
/// <param name="SyncMode">The template synchronization mode.</param>
/// <param name="CreatedAtUtc">The form creation timestamp.</param>
/// <param name="Questions">The list of questions in the form.</param>
public sealed record ProjectFormDto(
    Guid ExternalId,
    string Name,
    long ProjectId,
    long IncubatorId,
    SyncMode SyncMode,
    DateTime CreatedAtUtc,
    IReadOnlyList<QuestionDto> Questions);
