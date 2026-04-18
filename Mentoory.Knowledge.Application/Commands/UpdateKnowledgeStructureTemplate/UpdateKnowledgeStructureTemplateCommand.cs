using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructureTemplate;

/// <summary>
/// Represents a command to update the details of an existing knowledge structure template.
/// </summary>
/// <param name="ExternalId">The external identifier of the template to update.</param>
/// <param name="Name">The new name of the template.</param>
/// <param name="Description">The new optional description for the template.</param>
public sealed record UpdateKnowledgeStructureTemplateCommand(
    Guid ExternalId,
    string Name,
    string? Description) : IBaseRequest;
