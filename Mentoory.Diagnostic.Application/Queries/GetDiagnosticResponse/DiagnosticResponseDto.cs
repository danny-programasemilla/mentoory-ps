namespace Mentoory.Diagnostic.Application.Queries.GetDiagnosticResponse;

/// <summary>
/// Data transfer object for an answer correction within a question response.
/// </summary>
/// <param name="Id">The correction ID.</param>
/// <param name="PreviousTextValue">The previous text value before correction.</param>
/// <param name="PreviousNumericValue">The previous numeric value before correction.</param>
/// <param name="PreviousSelectedOptionIds">The previous selected option IDs as a comma-separated string.</param>
/// <param name="CorrectedByUserId">The user who performed the correction.</param>
/// <param name="CorrectedAtUtc">The timestamp of the correction.</param>
/// <param name="Reason">The reason for the correction.</param>
public sealed record AnswerCorrectionDto(
    long Id,
    string? PreviousTextValue,
    decimal? PreviousNumericValue,
    string? PreviousSelectedOptionIds,
    long CorrectedByUserId,
    DateTime CorrectedAtUtc,
    string? Reason);

/// <summary>
/// Data transfer object for a question response within a diagnostic response.
/// </summary>
/// <param name="Id">The question response ID.</param>
/// <param name="QuestionId">The question that was answered.</param>
/// <param name="TextValue">The text response value.</param>
/// <param name="NumericValue">The numeric response value.</param>
/// <param name="SelectedOptionIds">The selected option IDs.</param>
/// <param name="CreatedAtUtc">The response creation timestamp.</param>
/// <param name="Corrections">The list of corrections applied to this response.</param>
public sealed record QuestionResponseDto(
    long Id,
    long QuestionId,
    string? TextValue,
    decimal? NumericValue,
    IReadOnlyList<long> SelectedOptionIds,
    DateTime CreatedAtUtc,
    IReadOnlyList<AnswerCorrectionDto> Corrections);

/// <summary>
/// Data transfer object representing a diagnostic response with its question responses.
/// </summary>
/// <param name="ExternalId">The external GUID identifier for routing.</param>
/// <param name="ProjectFormId">The associated project form ID.</param>
/// <param name="StageFormAssignmentId">The stage form assignment that this diagnostic belongs to.</param>
/// <param name="IsCompleted">Whether the diagnostic has been completed.</param>
/// <param name="CompletedAtUtc">The completion timestamp, if completed.</param>
/// <param name="Responses">The list of question responses.</param>
public sealed record DiagnosticResponseDto(
    Guid ExternalId,
    long ProjectFormId,
    long StageFormAssignmentId,
    bool IsCompleted,
    DateTime? CompletedAtUtc,
    IReadOnlyList<QuestionResponseDto> Responses);
