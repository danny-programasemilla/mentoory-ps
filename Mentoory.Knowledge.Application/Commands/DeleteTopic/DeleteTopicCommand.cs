using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteTopic;

/// <summary>
/// Represents a command to remove a topic from a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="TopicExternalId">The external identifier of the topic to remove.</param>
public sealed record DeleteTopicCommand(
    Guid StructureExternalId,
    Guid TopicExternalId) : IBaseRequest;
