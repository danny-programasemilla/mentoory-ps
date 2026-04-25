using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderModules;

/// <summary>
/// Represents a command to reorder modules within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="ModuleExternalIdsInOrder">The external identifiers of the modules in the desired order.</param>
public sealed record ReorderModulesCommand(
    Guid StructureExternalId,
    IReadOnlyList<Guid> ModuleExternalIdsInOrder) : IBaseRequest;
