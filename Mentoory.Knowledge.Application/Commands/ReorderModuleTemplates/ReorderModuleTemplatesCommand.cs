using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderModuleTemplates;

/// <summary>
/// Represents a command to reorder modules within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="ModuleExternalIdsInOrder">The external identifiers of the modules in the desired order.</param>
public sealed record ReorderModuleTemplatesCommand(
    Guid TemplateExternalId,
    IReadOnlyList<Guid> ModuleExternalIdsInOrder) : IBaseRequest;
