using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateTopicTemplate;

/// <summary>
/// Represents a command to update an existing topic within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="TopicExternalId">The external identifier of the topic to update.</param>
/// <param name="Name">The new name of the topic.</param>
/// <param name="Description">An optional description for the topic.</param>
public sealed record UpdateTopicTemplateCommand(
    Guid TemplateExternalId,
    Guid TopicExternalId,
    string Name,
    string? Description) : IBaseRequest;
