using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;

/// <summary>
/// Represents a single response item for a question in the diagnostic form.
/// </summary>
/// <param name="QuestionId">The ID of the question being answered.</param>
/// <param name="TextValue">The text response value, if applicable.</param>
/// <param name="NumericValue">The numeric response value, if applicable.</param>
/// <param name="SelectedOptionIds">The selected option IDs for single/multi-select questions.</param>
public sealed record ResponseItem(
    long QuestionId,
    string? TextValue,
    decimal? NumericValue,
    List<long>? SelectedOptionIds);

/// <summary>
/// Represents a command to submit a complete diagnostic response for a project form.
/// </summary>
/// <param name="ProjectFormExternalId">The external identifier of the project form being responded to.</param>
/// <param name="ProjectId">The project associated with this diagnostic.</param>
/// <param name="IncubatorId">The incubator that owns the project.</param>
/// <param name="EntrepreneurUserId">The user submitting the diagnostic response.</param>
/// <param name="EvaluationStage">The evaluation stage (Initial or Final).</param>
/// <param name="Responses">The list of question responses.</param>
public sealed record SubmitDiagnosticResponseCommand(
    Guid ProjectFormExternalId,
    long ProjectId,
    long IncubatorId,
    long EntrepreneurUserId,
    EvaluationStage EvaluationStage,
    List<ResponseItem> Responses) : IBaseRequest;
