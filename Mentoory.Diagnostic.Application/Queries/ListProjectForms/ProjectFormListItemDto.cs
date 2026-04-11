using Mentoory.Diagnostic.Domain.Enums;

namespace Mentoory.Diagnostic.Application.Queries.ListProjectForms;

/// <summary>
/// List item DTO for project forms.
/// </summary>
public sealed record ProjectFormListItemDto(
    Guid ExternalId,
    string Name,
    SyncMode SyncMode,
    int QuestionCount,
    DateTime CreatedAtUtc);
