using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteKnowledgeStructureTemplate;

/// <summary>
/// Represents a command to delete a knowledge structure template.
/// </summary>
/// <param name="ExternalId">The external identifier of the template to delete.</param>
public sealed record DeleteKnowledgeStructureTemplateCommand(Guid ExternalId) : IBaseRequest;
