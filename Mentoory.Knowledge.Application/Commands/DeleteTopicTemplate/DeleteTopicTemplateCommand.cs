using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteTopicTemplate;

/// <summary>
/// Represents a command to remove a topic from a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="TopicExternalId">The external identifier of the topic to remove.</param>
public sealed record DeleteTopicTemplateCommand(
    Guid TemplateExternalId,
    Guid TopicExternalId) : IBaseRequest;
