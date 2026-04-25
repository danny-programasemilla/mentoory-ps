using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderResourceTemplates;

/// <summary>
/// Represents a command to reorder the resources of a subject within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="SubjectExternalId">The external identifier of the parent subject.</param>
/// <param name="ResourceExternalIdsInOrder">The ordered list of resource external identifiers representing the new order.</param>
public sealed record ReorderResourceTemplatesCommand(
    Guid TemplateExternalId,
    Guid SubjectExternalId,
    IReadOnlyList<Guid> ResourceExternalIdsInOrder) : IBaseRequest;
