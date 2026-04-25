using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteModule;

/// <summary>
/// Represents a command to remove a module from a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="ModuleExternalId">The external identifier of the module to remove.</param>
public sealed record DeleteModuleCommand(
    Guid StructureExternalId,
    Guid ModuleExternalId) : IBaseRequest;
