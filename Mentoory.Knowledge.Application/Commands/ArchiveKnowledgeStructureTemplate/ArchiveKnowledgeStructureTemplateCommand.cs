using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ArchiveKnowledgeStructureTemplate;

/// <summary>
/// Represents a command to archive a knowledge structure template.
/// </summary>
/// <param name="ExternalId">The external identifier of the template to archive.</param>
public sealed record ArchiveKnowledgeStructureTemplateCommand(Guid ExternalId) : IBaseRequest;
