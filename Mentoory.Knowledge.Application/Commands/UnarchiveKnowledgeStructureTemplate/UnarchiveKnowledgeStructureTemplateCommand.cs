using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UnarchiveKnowledgeStructureTemplate;

/// <summary>
/// Represents a command to unarchive a knowledge structure template.
/// </summary>
/// <param name="ExternalId">The external identifier of the template to unarchive.</param>
public sealed record UnarchiveKnowledgeStructureTemplateCommand(Guid ExternalId) : IBaseRequest;
