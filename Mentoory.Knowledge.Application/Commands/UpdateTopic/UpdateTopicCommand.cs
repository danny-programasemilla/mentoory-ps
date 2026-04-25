using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateTopic;

/// <summary>
/// Represents a command to update a topic within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="TopicExternalId">The external identifier of the topic to update.</param>
/// <param name="Name">The new name of the topic.</param>
/// <param name="Description">The new optional description for the topic.</param>
public sealed record UpdateTopicCommand(
    Guid StructureExternalId,
    Guid TopicExternalId,
    string Name,
    string? Description) : IBaseRequest;
