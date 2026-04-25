using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.CreateKnowledgeStructureTemplate;

/// <summary>
/// Represents a command to create a new knowledge structure template.
/// </summary>
/// <param name="Name">The name of the template.</param>
/// <param name="Description">An optional description for the template.</param>
public sealed record CreateKnowledgeStructureTemplateCommand(
    string Name,
    string? Description) : IBaseRequest<Guid>;
