using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddResource;

/// <summary>
/// Represents a command to add a new resource under a subject within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="SubjectExternalId">The external identifier of the parent subject.</param>
/// <param name="Title">The title of the resource.</param>
/// <param name="Description">An optional description for the resource.</param>
/// <param name="Url">The absolute URL of the resource.</param>
/// <param name="ResourceType">The type of the resource.</param>
/// <param name="SortOrder">The sort order of the resource within the subject.</param>
public sealed record AddResourceCommand(
    Guid StructureExternalId,
    Guid SubjectExternalId,
    string Title,
    string? Description,
    string Url,
    ResourceType ResourceType,
    int SortOrder) : IBaseRequest<Guid>;
