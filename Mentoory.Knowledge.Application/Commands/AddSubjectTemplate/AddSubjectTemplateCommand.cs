using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddSubjectTemplate;

/// <summary>
/// Represents a command to add a new subject under a topic within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="TopicExternalId">The external identifier of the parent topic.</param>
/// <param name="Name">The name of the subject.</param>
/// <param name="Description">An optional description for the subject.</param>
/// <param name="SortOrder">The sort order of the subject within the topic.</param>
public sealed record AddSubjectTemplateCommand(
    Guid TemplateExternalId,
    Guid TopicExternalId,
    string Name,
    string? Description,
    int SortOrder) : IBaseRequest<Guid>;
