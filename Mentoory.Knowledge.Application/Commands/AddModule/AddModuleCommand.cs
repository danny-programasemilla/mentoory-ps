using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddModule;

/// <summary>
/// Represents a command to add a new module to a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="Name">The name of the module.</param>
/// <param name="Description">An optional description for the module.</param>
/// <param name="SortOrder">The sort order of the module within the structure.</param>
public sealed record AddModuleCommand(
    Guid StructureExternalId,
    string Name,
    string? Description,
    int SortOrder) : IBaseRequest<Guid>;
