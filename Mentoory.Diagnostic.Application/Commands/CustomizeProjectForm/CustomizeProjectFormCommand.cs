using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.CustomizeProjectForm;

/// <summary>
/// The type of customization action to perform on a project form.
/// </summary>
public enum CustomizeAction
{
    AddQuestion = 0,
    RemoveQuestion = 1,
    ReorderQuestions = 2,
}

/// <summary>
/// Data for a new question to be added to the form.
/// </summary>
public sealed record QuestionData(
    long TopicId,
    string QuestionText,
    QuestionType QuestionType,
    int SortOrder,
    string? BlockGroup,
    bool IsOptional);

/// <summary>
/// Represents a command to customize a project form by adding, removing, or reordering questions.
/// </summary>
public sealed record CustomizeProjectFormCommand(
    Guid ProjectFormExternalId,
    CustomizeAction Action,
    QuestionData? QuestionData,
    long? QuestionIdToRemove,
    List<long>? QuestionIdsInOrder) : IBaseRequest;
