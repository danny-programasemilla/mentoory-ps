using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructure;

/// <summary>
/// Represents a command to update the details of an existing knowledge structure (project clone).
/// </summary>
/// <param name="ExternalId">The external identifier of the knowledge structure to update.</param>
/// <param name="Name">The new name of the structure.</param>
/// <param name="Description">The new optional description for the structure.</param>
public sealed record UpdateKnowledgeStructureCommand(
    Guid ExternalId,
    string Name,
    string? Description) : IBaseRequest;
