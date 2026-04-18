using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderSubjectTemplates;

/// <summary>
/// Represents a command to reorder the subjects of a topic within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="TopicExternalId">The external identifier of the parent topic.</param>
/// <param name="SubjectExternalIdsInOrder">The ordered list of subject external identifiers representing the new order.</param>
public sealed record ReorderSubjectTemplatesCommand(
    Guid TemplateExternalId,
    Guid TopicExternalId,
    IReadOnlyList<Guid> SubjectExternalIdsInOrder) : IBaseRequest;
