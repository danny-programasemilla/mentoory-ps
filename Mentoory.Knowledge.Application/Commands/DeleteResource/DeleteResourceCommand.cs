using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteResource;

/// <summary>
/// Represents a command to remove a resource from a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="ResourceExternalId">The external identifier of the resource to remove.</param>
public sealed record DeleteResourceCommand(
    Guid StructureExternalId,
    Guid ResourceExternalId) : IBaseRequest;
