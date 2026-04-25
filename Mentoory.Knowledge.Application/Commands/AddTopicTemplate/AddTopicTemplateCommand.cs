using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddTopicTemplate;

/// <summary>
/// Represents a command to add a new topic under a module within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="ModuleExternalId">The external identifier of the parent module.</param>
/// <param name="Name">The name of the topic.</param>
/// <param name="Description">An optional description for the topic.</param>
/// <param name="SortOrder">The sort order of the topic within the module.</param>
public sealed record AddTopicTemplateCommand(
    Guid TemplateExternalId,
    Guid ModuleExternalId,
    string Name,
    string? Description,
    int SortOrder) : IBaseRequest<Guid>;
