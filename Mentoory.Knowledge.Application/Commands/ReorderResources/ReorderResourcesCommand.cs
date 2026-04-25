using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderResources;

/// <summary>
/// Represents a command to reorder resources within a subject of a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="SubjectExternalId">The external identifier of the subject whose resources are being reordered.</param>
/// <param name="ResourceExternalIdsInOrder">The external identifiers of the resources in the desired order.</param>
public sealed record ReorderResourcesCommand(
    Guid StructureExternalId,
    Guid SubjectExternalId,
    IReadOnlyList<Guid> ResourceExternalIdsInOrder) : IBaseRequest;
