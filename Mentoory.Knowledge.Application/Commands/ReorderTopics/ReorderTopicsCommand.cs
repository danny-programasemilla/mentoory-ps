using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderTopics;

/// <summary>
/// Represents a command to reorder topics within a module of a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="ModuleExternalId">The external identifier of the module whose topics are being reordered.</param>
/// <param name="TopicExternalIdsInOrder">The external identifiers of the topics in the desired order.</param>
public sealed record ReorderTopicsCommand(
    Guid StructureExternalId,
    Guid ModuleExternalId,
    IReadOnlyList<Guid> TopicExternalIdsInOrder) : IBaseRequest;
