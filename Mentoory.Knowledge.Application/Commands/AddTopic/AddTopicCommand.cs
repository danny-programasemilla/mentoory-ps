using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddTopic;

/// <summary>
/// Represents a command to add a new topic under a module within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="ModuleExternalId">The external identifier of the parent module.</param>
/// <param name="Name">The name of the topic.</param>
/// <param name="Description">An optional description for the topic.</param>
/// <param name="SortOrder">The sort order of the topic within the module.</param>
public sealed record AddTopicCommand(
    Guid StructureExternalId,
    Guid ModuleExternalId,
    string Name,
    string? Description,
    int SortOrder) : IBaseRequest<Guid>;
