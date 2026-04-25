using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateModule;

/// <summary>
/// Represents a command to update a module within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="ModuleExternalId">The external identifier of the module to update.</param>
/// <param name="Name">The new name of the module.</param>
/// <param name="Description">The new optional description for the module.</param>
public sealed record UpdateModuleCommand(
    Guid StructureExternalId,
    Guid ModuleExternalId,
    string Name,
    string? Description) : IBaseRequest;
