using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateResourceTemplate;

/// <summary>
/// Represents a command to update an existing resource within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="ResourceExternalId">The external identifier of the resource to update.</param>
/// <param name="Title">The new title of the resource.</param>
/// <param name="Description">The new optional description for the resource.</param>
/// <param name="Url">The new absolute URL of the resource.</param>
/// <param name="ResourceType">The new type of the resource.</param>
public sealed record UpdateResourceTemplateCommand(
    Guid TemplateExternalId,
    Guid ResourceExternalId,
    string Title,
    string? Description,
    string Url,
    ResourceType ResourceType) : IBaseRequest;
