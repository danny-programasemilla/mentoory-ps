using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderSubjects;

/// <summary>
/// Represents a command to reorder subjects within a topic of a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="TopicExternalId">The external identifier of the topic whose subjects are being reordered.</param>
/// <param name="SubjectExternalIdsInOrder">The external identifiers of the subjects in the desired order.</param>
public sealed record ReorderSubjectsCommand(
    Guid StructureExternalId,
    Guid TopicExternalId,
    IReadOnlyList<Guid> SubjectExternalIdsInOrder) : IBaseRequest;
