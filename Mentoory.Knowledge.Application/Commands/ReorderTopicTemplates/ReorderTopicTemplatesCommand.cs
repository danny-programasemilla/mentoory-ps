using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderTopicTemplates;

/// <summary>
/// Represents a command to reorder topics within a module of a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="ModuleExternalId">The external identifier of the parent module.</param>
/// <param name="TopicExternalIdsInOrder">The external identifiers of the topics in the desired order.</param>
public sealed record ReorderTopicTemplatesCommand(
    Guid TemplateExternalId,
    Guid ModuleExternalId,
    IReadOnlyList<Guid> TopicExternalIdsInOrder) : IBaseRequest;
