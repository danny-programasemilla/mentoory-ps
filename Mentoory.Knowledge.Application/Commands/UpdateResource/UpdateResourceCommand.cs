using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateResource;

/// <summary>
/// Represents a command to update a resource within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="ResourceExternalId">The external identifier of the resource to update.</param>
/// <param name="Title">The new title of the resource.</param>
/// <param name="Description">The new optional description for the resource.</param>
/// <param name="Url">The new absolute URL of the resource.</param>
/// <param name="ResourceType">The new type of the resource.</param>
public sealed record UpdateResourceCommand(
    Guid StructureExternalId,
    Guid ResourceExternalId,
    string Title,
    string? Description,
    string Url,
    ResourceType ResourceType) : IBaseRequest;
